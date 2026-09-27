#include "../src/compat.h"
#include <windows.graphics.capture.interop.h>
#include <windows.graphics.directx.direct3d11.interop.h>
#include <d3d11.h>
#include <winrt/Windows.Foundation.h>
#include <winrt/Windows.Graphics.Capture.h>
#include <winrt/Windows.Graphics.DirectX.Direct3D11.h>
#include <winrt/Windows.Graphics.Imaging.h>
#include <cstdio>
#include <chrono>
#include <algorithm>
#include <cmath>
#include <cstdint>
#include <fstream>
#include <mutex>
#include <string>
#include <vector>
#include <atomic>
#include <future>
#include <memory>
#include <thread>

namespace wgc = winrt::Windows::Graphics::Capture;
namespace wdx = winrt::Windows::Graphics::DirectX;
namespace cap = ABI::Windows::Graphics::Capture;

namespace {
struct TestWindowState {
    bool animate = false;
    std::atomic<ULONG> paint_frames{0};
    std::mutex animation_ticks_mutex;
    std::vector<std::chrono::steady_clock::time_point> animation_tick_timestamps;
};

LRESULT CALLBACK WindowProc(HWND window, UINT message, WPARAM w, LPARAM l) {
    if (message == WM_NCCREATE) {
        auto* create = reinterpret_cast<CREATESTRUCTW*>(l);
        SetWindowLongPtrW(window, GWLP_USERDATA, reinterpret_cast<LONG_PTR>(create->lpCreateParams));
        return TRUE;
    }
    auto* state = reinterpret_cast<TestWindowState*>(GetWindowLongPtrW(window, GWLP_USERDATA));
    if (message == WM_DESTROY) { PostQuitMessage(0); return 0; }
    if (message == WM_TIMER && state && state->animate) {
        {
            std::lock_guard<std::mutex> lock(state->animation_ticks_mutex);
            state->animation_tick_timestamps.push_back(std::chrono::steady_clock::now());
        }
        state->paint_frames.fetch_add(1, std::memory_order_relaxed);
        InvalidateRect(window, nullptr, FALSE);
        return 0;
    }
    if (message == WM_PAINT) {
        PAINTSTRUCT paint{};
        HDC dc = BeginPaint(window, &paint);
        RECT rect{}; GetClientRect(window, &rect);
        ULONG frame = state ? state->paint_frames.load(std::memory_order_relaxed) : 0;
        COLORREF color = state && state->animate
            ? RGB(24 + (frame * 37) % 200, 48 + (frame * 53) % 180, 72 + (frame * 29) % 160)
            : RGB(32, 128, 224);
        HBRUSH brush = CreateSolidBrush(color);
        FillRect(dc, &rect, brush); DeleteObject(brush);
        SetBkMode(dc, TRANSPARENT); SetTextColor(dc, RGB(255, 255, 255));
        if (state && state->animate) {
            wchar_t label[96]{};
            swprintf_s(label, L"Animated WGC throughput fixture · frame %lu", frame);
            DrawTextW(dc, label, -1, &rect, DT_CENTER | DT_TOP | DT_SINGLELINE);
        } else {
            DrawTextW(dc, L"Codex Win10 capture compatibility test", -1, &rect, DT_CENTER | DT_TOP | DT_SINGLELINE);
        }
        EndPaint(window, &paint); return 0;
    }
    return DefWindowProcW(window, message, w, l);
}
void PumpMessages() {
    MSG message{};
    while (PeekMessageW(&message, nullptr, 0, 0, PM_REMOVE)) {
        TranslateMessage(&message); DispatchMessageW(&message);
    }
}
// The target window always pumps on its own thread. This isolates capture
// apartment / callback behavior from a hung target application's UI thread.
class TestWindow {
    std::unique_ptr<TestWindowState> state_;
    std::thread thread_;
    HWND window_ = nullptr;
public:
    explicit TestWindow(bool animate = false) : state_(std::make_unique<TestWindowState>()) {
        state_->animate = animate;
        if (animate) state_->animation_tick_timestamps.reserve(120 * 120);
        std::promise<HWND> ready;
        auto result = ready.get_future();
        auto* state = state_.get();
        thread_ = std::thread([ready = std::move(ready), state]() mutable {
            WNDCLASSW klass{};
            klass.lpfnWndProc = WindowProc; klass.hInstance = GetModuleHandleW(nullptr);
            klass.lpszClassName = L"CodexCaptureCompatProbe";
            if (!RegisterClassW(&klass)) { ready.set_value(nullptr); return; }
            HWND window = CreateWindowExW(0, klass.lpszClassName,
                L"Codex capture compatibility test", WS_OVERLAPPEDWINDOW,
                50, 50, 520, 300, nullptr, nullptr, klass.hInstance, state);
            if (!window) { ready.set_value(nullptr); return; }
            if (state->animate && !SetTimer(window, 1, 16, nullptr)) {
                DestroyWindow(window); ready.set_value(nullptr); return;
            }
            ShowWindow(window, SW_SHOWNOACTIVATE); UpdateWindow(window);
            ready.set_value(window);
            MSG message{};
            while (GetMessageW(&message, nullptr, 0, 0) > 0) {
                TranslateMessage(&message); DispatchMessageW(&message);
            }
        });
        window_ = result.get();
        if (!window_) { thread_.join(); winrt::throw_hresult(E_FAIL); }
    }
    ~TestWindow() {
        if (window_) PostMessageW(window_, WM_CLOSE, 0, 0);
        if (thread_.joinable()) thread_.join();
    }
    HWND get() const { return window_; }
    ULONG animationTicksBetween(std::chrono::steady_clock::time_point start,
                                std::chrono::steady_clock::time_point end) const {
        std::lock_guard<std::mutex> lock(state_->animation_ticks_mutex);
        return static_cast<ULONG>(std::count_if(state_->animation_tick_timestamps.begin(),
            state_->animation_tick_timestamps.end(), [start, end](const auto& timestamp) {
                return timestamp >= start && timestamp < end;
            }));
    }
};
void VerifyVersionProxy() {
    HMODULE version = GetModuleHandleW(L"version.dll");
    if (!version) winrt::throw_hresult(E_FAIL);
    // windowsapp.lib may resolve direct calls through API sets. Resolve these
    // names explicitly so the exact exports imported by Codex are tested.
    auto info_size = reinterpret_cast<decltype(&GetFileVersionInfoSizeExW)>(GetProcAddress(version, "GetFileVersionInfoSizeExW"));
    auto get_info = reinterpret_cast<decltype(&GetFileVersionInfoExW)>(GetProcAddress(version, "GetFileVersionInfoExW"));
    auto query = reinterpret_cast<decltype(&VerQueryValueW)>(GetProcAddress(version, "VerQueryValueW"));
    if (!info_size || !get_info || !query) winrt::throw_hresult(E_FAIL);
    wchar_t path[MAX_PATH]{};
    GetSystemDirectoryW(path, MAX_PATH);
    wcscat_s(path, L"\\kernel32.dll");
    DWORD ignored = 0;
    DWORD size = info_size(FILE_VER_GET_NEUTRAL, path, &ignored);
    if (!size) winrt::throw_last_error();
    std::vector<BYTE> bytes(size);
    if (!get_info(FILE_VER_GET_NEUTRAL, path, 0, size, bytes.data())) winrt::throw_last_error();
    VS_FIXEDFILEINFO* info = nullptr;
    UINT length = 0;
    if (!query(bytes.data(), L"\\", reinterpret_cast<void**>(&info), &length) ||
        length < sizeof(*info) || info->dwSignature != VS_FFI_SIGNATURE)
        winrt::throw_hresult(E_FAIL);
    // Exercise a version export with eight arguments, including stack arguments.
    wchar_t current[MAX_PATH]{}, destination[MAX_PATH]{};
    UINT current_length = MAX_PATH, destination_length = MAX_PATH;
    VerFindFileW(0, L"does-not-exist.test", L"C:\\Windows", L"C:\\Windows",
                 current, &current_length, destination, &destination_length);
    std::printf("version_forwarding=PASS kernel32=%u.%u.%u.%u\n", HIWORD(info->dwFileVersionMS),
                LOWORD(info->dwFileVersionMS), HIWORD(info->dwFileVersionLS), LOWORD(info->dwFileVersionLS));
}
void SaveAndCheckFrame(const wgc::Direct3D11CaptureFrame& frame, ID3D11Device* device,
                       ID3D11DeviceContext* context, const std::wstring& path) {
    auto access = frame.Surface().as<Windows::Graphics::DirectX::Direct3D11::IDirect3DDxgiInterfaceAccess>();
    winrt::com_ptr<ID3D11Texture2D> source;
    winrt::check_hresult(access->GetInterface(__uuidof(ID3D11Texture2D), source.put_void()));
    D3D11_TEXTURE2D_DESC description{}; source->GetDesc(&description);
    description.Usage = D3D11_USAGE_STAGING;
    description.BindFlags = 0; description.CPUAccessFlags = D3D11_CPU_ACCESS_READ; description.MiscFlags = 0;
    winrt::com_ptr<ID3D11Texture2D> copy;
    winrt::check_hresult(device->CreateTexture2D(&description, nullptr, copy.put()));
    context->CopyResource(copy.get(), source.get());
    D3D11_MAPPED_SUBRESOURCE mapped{};
    winrt::check_hresult(context->Map(copy.get(), 0, D3D11_MAP_READ, 0, &mapped));
    size_t matching = 0;
    for (UINT y = 0; y < description.Height; ++y) {
        auto row = static_cast<const BYTE*>(mapped.pData) + y * mapped.RowPitch;
        for (UINT x = 0; x < description.Width; ++x) {
            if (row[x*4] == 224 && row[x*4+1] == 128 && row[x*4+2] == 32) ++matching;
        }
    }
    if (!path.empty()) {
        BITMAPFILEHEADER file{}; BITMAPINFOHEADER bitmap{};
        file.bfType = 0x4d42;
        file.bfOffBits = sizeof(file) + sizeof(bitmap);
        file.bfSize = file.bfOffBits + description.Width * description.Height * 4;
        bitmap.biSize = sizeof(bitmap); bitmap.biWidth = description.Width;
        bitmap.biHeight = -static_cast<LONG>(description.Height);
        bitmap.biPlanes = 1; bitmap.biBitCount = 32; bitmap.biCompression = BI_RGB;
        std::ofstream output(path, std::ios::binary);
        output.write(reinterpret_cast<const char*>(&file), sizeof(file));
        output.write(reinterpret_cast<const char*>(&bitmap), sizeof(bitmap));
        for (UINT y = 0; y < description.Height; ++y)
            output.write(static_cast<const char*>(mapped.pData) + y * mapped.RowPitch, description.Width * 4);
        if (!output) { context->Unmap(copy.get(), 0); winrt::throw_hresult(E_FAIL); }
    }
    context->Unmap(copy.get(), 0);
    std::printf("capture_frame=%ux%u expected_color_pixels=%zu\n", description.Width, description.Height, matching);
    if (matching < static_cast<size_t>(description.Width) * description.Height / 4) winrt::throw_hresult(E_FAIL);
}

double PercentileNearestRank(std::vector<double> values, double percentile) {
    if (values.empty()) return 0.0;
    std::sort(values.begin(), values.end());
    const auto rank = static_cast<size_t>(std::ceil(percentile * values.size()));
    const size_t index = std::clamp(rank, size_t{1}, values.size()) - 1;
    return values[index];
}

void RunSustainedFrameRateBenchmark(wgc::GraphicsCaptureSession const& session,
                                    wgc::Direct3D11CaptureFramePool const& pool,
                                    TestWindow const& target, bool pump,
                                    ULONG duration_seconds) {
    struct BenchmarkState {
        std::atomic<bool> measuring{false};
        std::atomic<ULONG> active_callbacks{0};
        std::atomic<ULONG> callback_count{0};
        std::atomic<ULONG> frame_count{0};
        std::chrono::steady_clock::time_point measurement_start{};
        std::chrono::steady_clock::time_point measurement_end{};
        std::mutex timestamps_mutex;
        std::vector<std::chrono::steady_clock::time_point> callback_entry_timestamps;
    };

    auto state = std::make_shared<BenchmarkState>();
    state->callback_entry_timestamps.reserve(static_cast<size_t>(duration_seconds) * 120);
    auto registration = pool.FrameArrived([state](auto const& sender, auto const&) {
        state->active_callbacks.fetch_add(1, std::memory_order_acq_rel);
        struct CallbackExit {
            std::atomic<ULONG>& active;
            ~CallbackExit() { active.fetch_sub(1, std::memory_order_acq_rel); }
        } callback_exit{state->active_callbacks};
        const auto callback_entry = std::chrono::steady_clock::now();
        // One immutable timestamp/window decision governs callback and acquired
        // frame counters so callbacks at either boundary cannot split counts.
        const bool in_measurement_window = state->measuring.load(std::memory_order_acquire) &&
            callback_entry >= state->measurement_start && callback_entry < state->measurement_end;
        if (in_measurement_window) {
            state->callback_count.fetch_add(1, std::memory_order_relaxed);
            std::lock_guard<std::mutex> lock(state->timestamps_mutex);
            state->callback_entry_timestamps.push_back(callback_entry);
        }
        auto frame = sender.TryGetNextFrame();
        if (!frame) return;
        if (in_measurement_window) {
            state->frame_count.fetch_add(1, std::memory_order_relaxed);
        }
        frame.Close();
    });

    session.StartCapture();
    // Warm up capture and DWM before collecting; those first frames are not
    // representative of steady-state throughput.
    const auto warmup_end = GetTickCount64() + 1000;
    while (GetTickCount64() < warmup_end) {
        if (pump) PumpMessages();
        Sleep(8);
    }

    const auto started = std::chrono::steady_clock::now();
    const auto deadline = started + std::chrono::seconds(duration_seconds);
    state->measurement_start = started;
    state->measurement_end = deadline;
    state->measuring.store(true, std::memory_order_release);
    while (std::chrono::steady_clock::now() < deadline) {
        if (pump) PumpMessages();
        Sleep(8);
    }
    state->measuring.store(false, std::memory_order_release);
    // Use the exact requested measurement window; fixture timer samples are
    // timestamped and filtered against the same half-open interval.
    const auto finished = deadline;
    pool.FrameArrived(registration);
    const auto callbacks_deadline = GetTickCount64() + 2000;
    while (state->active_callbacks.load(std::memory_order_acquire) && GetTickCount64() < callbacks_deadline)
        Sleep(1);
    if (state->active_callbacks.load(std::memory_order_acquire))
        winrt::throw_hresult(HRESULT_FROM_WIN32(WAIT_TIMEOUT));

    std::vector<std::chrono::steady_clock::time_point> callback_entry_timestamps;
    {
        std::lock_guard<std::mutex> lock(state->timestamps_mutex);
        callback_entry_timestamps = state->callback_entry_timestamps;
    }
    std::vector<double> callback_entry_intervals_ms;
    callback_entry_intervals_ms.reserve(callback_entry_timestamps.size() > 0 ? callback_entry_timestamps.size() - 1 : 0);
    for (size_t i = 1; i < callback_entry_timestamps.size(); ++i) {
        callback_entry_intervals_ms.push_back(std::chrono::duration<double, std::milli>(
            callback_entry_timestamps[i] - callback_entry_timestamps[i - 1]).count());
    }

    const double elapsed_seconds = std::chrono::duration<double>(finished - started).count();
    const ULONG callbacks = state->callback_count.load(std::memory_order_relaxed);
    const ULONG frames = state->frame_count.load(std::memory_order_relaxed);
    const ULONG animation_ticks = target.animationTicksBetween(started, finished);
    const bool liveness_floor_passed = animation_ticks >= duration_seconds * 20 &&
        frames >= duration_seconds * 10;
    std::printf("liveness_smoke_floor=%s sustained_wgc_window_seconds=%.3f fixture_animation_ticks=%lu minimum_animation_ticks=%lu frame_arrived_callbacks=%lu callbacks_with_acquired_frame=%lu minimum_acquired_frames=%lu callback_fps=%.2f acquired_frame_fps=%.2f callback_entry_interval_ms_p50_nearest_rank=%.3f callback_entry_interval_ms_p95_nearest_rank=%.3f\n",
        liveness_floor_passed ? "PASS" : "FAIL", elapsed_seconds, animation_ticks, duration_seconds * 20,
        callbacks, frames, duration_seconds * 10,
        elapsed_seconds > 0 ? callbacks / elapsed_seconds : 0,
        elapsed_seconds > 0 ? frames / elapsed_seconds : 0,
        PercentileNearestRank(callback_entry_intervals_ms, 0.50),
        PercentileNearestRank(callback_entry_intervals_ms, 0.95));
    std::puts("note=smoke_liveness_threshold_only_not_a_performance_comparison");
    if (!liveness_floor_passed)
        winrt::throw_hresult(HRESULT_FROM_WIN32(WAIT_TIMEOUT));
}
}

int wmain(int argc, wchar_t** argv) {
    std::setvbuf(stdout, nullptr, _IONBF, 0);
    bool expect_shim = false, capture_frame = false, sta = false, pump = true, poll_only = false;
    bool late_subscribe = false, recreate = false;
    bool software_bitmap = false;
    bool expect_deferred = false, close_subscribed = false;
    ULONG sustained_fps_seconds = 0;
    std::wstring output;
    for (int i = 1; i < argc; ++i) {
        if (wcscmp(argv[i], L"--expect-shim") == 0) expect_shim = true;
        else if (wcscmp(argv[i], L"--capture") == 0) capture_frame = true;
        else if (wcscmp(argv[i], L"--sta") == 0) sta = true;
        else if (wcscmp(argv[i], L"--no-pump") == 0) pump = false;
        else if (wcscmp(argv[i], L"--poll-only") == 0) poll_only = true;
        else if (wcscmp(argv[i], L"--late-subscribe") == 0) late_subscribe = true;
        else if (wcscmp(argv[i], L"--recreate") == 0) recreate = true;
        else if (wcscmp(argv[i], L"--software-bitmap") == 0) software_bitmap = true;
        else if (wcscmp(argv[i], L"--expect-deferred") == 0) expect_deferred = true;
        else if (wcscmp(argv[i], L"--close-subscribed") == 0) close_subscribed = true;
        else if (wcscmp(argv[i], L"--sustained-fps") == 0 && i + 1 < argc) {
            wchar_t* end = nullptr;
            unsigned long parsed = wcstoul(argv[++i], &end, 10);
            if (!end || *end != L'\0' || parsed < 3 || parsed > 120) {
                std::fprintf(stderr, "--sustained-fps requires a duration from 3 to 120 seconds\n");
                return 2;
            }
            sustained_fps_seconds = static_cast<ULONG>(parsed);
        }
        else if (wcscmp(argv[i], L"--output") == 0 && i + 1 < argc) output = argv[++i];
        else { std::fprintf(stderr, "unknown argument\n"); return 2; }
    }
    try {
        VerifyVersionProxy();
        using StatusFn = BOOL(WINAPI*)(capture_compat::Status*, DWORD);
        auto status_fn = reinterpret_cast<StatusFn>(GetProcAddress(GetModuleHandleW(L"version.dll"),
                                                                  "CodexCaptureCompatGetStatus"));
        capture_compat::Status status{};
        if (status_fn) { status_fn(&status, sizeof(status)); }
        std::printf("proxy_loaded=%d iat_hooks=%ld\n", status_fn != nullptr, status.iat_hooks);
        if (expect_shim && (!status_fn || status.iat_hooks != 1)) winrt::throw_hresult(E_FAIL);
        winrt::init_apartment(sta ? winrt::apartment_type::single_threaded : winrt::apartment_type::multi_threaded);
        std::printf("stage=apartment mode=%s pump=%d poll_only=%d capture_thread=%lu\n",
                    sta ? "STA" : "MTA", pump, poll_only, GetCurrentThreadId());
        if (sustained_fps_seconds && (capture_frame || late_subscribe || software_bitmap || expect_deferred || close_subscribed)) {
            std::fprintf(stderr, "--sustained-fps cannot be combined with one-shot capture options\n");
            return 2;
        }
        TestWindow target(sustained_fps_seconds != 0);
        HWND window = target.get();
        std::puts("stage=window");
        winrt::com_ptr<ID3D11Device> device;
        winrt::com_ptr<ID3D11DeviceContext> context;
        winrt::check_hresult(D3D11CreateDevice(nullptr, D3D_DRIVER_TYPE_HARDWARE, nullptr,
            D3D11_CREATE_DEVICE_BGRA_SUPPORT, nullptr, 0, D3D11_SDK_VERSION, device.put(), nullptr, context.put()));
        std::puts("stage=d3d_device");
        auto dxgi = device.as<IDXGIDevice>();
        winrt::com_ptr<IInspectable> inspectable;
        winrt::check_hresult(CreateDirect3D11DeviceFromDXGIDevice(dxgi.get(), inspectable.put()));
        auto direct3d = inspectable.as<winrt::Windows::Graphics::DirectX::Direct3D11::IDirect3DDevice>();
        std::puts("stage=winrt_device");
        auto interop = winrt::get_activation_factory<wgc::GraphicsCaptureItem, IGraphicsCaptureItemInterop>();
        std::puts("stage=item_factory");
        wgc::GraphicsCaptureItem item{nullptr};
        winrt::check_hresult(interop->CreateForWindow(window, winrt::guid_of<wgc::GraphicsCaptureItem>(), winrt::put_abi(item)));
        std::puts("stage=capture_item");
        // Explicit import ensures the probe exercises the exact IAT hook entry.
        winrt::com_ptr<cap::IDirect3D11CaptureFramePoolStatics2> factory;
        winrt::hstring class_name(L"Windows.Graphics.Capture.Direct3D11CaptureFramePool");
        winrt::check_hresult(RoGetActivationFactory(static_cast<HSTRING>(winrt::get_abi(class_name)),
            __uuidof(cap::IDirect3D11CaptureFramePoolStatics2), factory.put_void()));
        winrt::com_ptr<cap::IDirect3D11CaptureFramePool> raw_pool;
        winrt::check_hresult(factory->CreateFreeThreaded(
            reinterpret_cast<ABI::Windows::Graphics::DirectX::Direct3D11::IDirect3DDevice*>(winrt::get_abi(direct3d)),
            ABI::Windows::Graphics::DirectX::DirectXPixelFormat_B8G8R8A8UIntNormalized, 2,
            {item.Size().Width, item.Size().Height}, raw_pool.put()));
        auto pool = raw_pool.as<wgc::Direct3D11CaptureFramePool>();
        auto session = pool.CreateCaptureSession(item);
        HRESULT border_hr = S_OK;
        try { session.IsBorderRequired(false); } catch (const winrt::hresult_error& e) { border_hr = e.code(); }
        std::printf("SetIsBorderRequired_hr=0x%08lX\n", static_cast<unsigned long>(border_hr));
        if (expect_shim) {
            winrt::check_hresult(border_hr);
            if (!session.IsBorderRequired()) winrt::throw_hresult(E_FAIL);
            session.IsCursorCaptureEnabled(false);
        } else if (border_hr != E_NOINTERFACE) {
            std::fprintf(stderr, "Expected Win10 E_NOINTERFACE baseline\n"); winrt::throw_hresult(E_FAIL);
        }
        if (sustained_fps_seconds) {
            RunSustainedFrameRateBenchmark(session, pool, target, pump, sustained_fps_seconds);
            session.Close(); pool.Close();
            std::puts("sustained_wgc_benchmark=PASS");
            return 0;
        }
        if (capture_frame) {
            std::chrono::steady_clock::time_point capture_started{};
            long long start_capture_call_us = -1;
            if (late_subscribe) {
                capture_started = std::chrono::steady_clock::now();
                session.StartCapture();
                start_capture_call_us = std::chrono::duration_cast<std::chrono::microseconds>(
                    std::chrono::steady_clock::now() - capture_started).count();
                Sleep(200);
                unsigned drained = 0;
                while (auto old = pool.TryGetNextFrame()) { old.Close(); ++drained; }
                std::printf("drained_before_subscribe=%u\n", drained);
            }
            struct CallbackState {
                winrt::handle arrived{CreateEventW(nullptr, FALSE, FALSE, nullptr)};
                std::atomic<ULONG> count{0}, thread{0};
                winrt::Windows::Foundation::IAsyncOperation<winrt::Windows::Graphics::Imaging::SoftwareBitmap> operation{nullptr};
                wgc::Direct3D11CaptureFrame frame{nullptr};
                HRESULT error = S_OK;
                int status_in_callback = -1;
            };
            auto callback = std::make_shared<CallbackState>();
            if (!callback->arrived) winrt::throw_last_error();
            auto registration = pool.FrameArrived([callback, software_bitmap](auto const& sender, auto const&) {
                callback->thread = GetCurrentThreadId();
                if (callback->count.fetch_add(1) != 0 && software_bitmap) return;
                if (software_bitmap) {
                    try {
                        callback->frame = sender.TryGetNextFrame();
                        if (!callback->frame) winrt::throw_hresult(E_FAIL);
                        std::puts("software_bitmap=create.enter");
                        callback->operation = winrt::Windows::Graphics::Imaging::SoftwareBitmap::CreateCopyFromSurfaceAsync(callback->frame.Surface());
                        std::puts("software_bitmap=create.leave");
                        auto deadline = GetTickCount64() + 1500;
                        while (callback->operation.Status() == winrt::Windows::Foundation::AsyncStatus::Started && GetTickCount64() < deadline) Sleep(10);
                        callback->status_in_callback = static_cast<int>(callback->operation.Status());
                        std::printf("software_bitmap=status_in_callback_%d\n", callback->status_in_callback);
                    } catch (const winrt::hresult_error& error) { callback->error = error.code(); }
                }
                SetEvent(callback->arrived.get());
            });
            if (!late_subscribe) {
                capture_started = std::chrono::steady_clock::now();
                session.StartCapture();
                start_capture_call_us = std::chrono::duration_cast<std::chrono::microseconds>(
                    std::chrono::steady_clock::now() - capture_started).count();
            }
            if (recreate) pool.Recreate(direct3d, wdx::DirectXPixelFormat::B8G8R8A8UIntNormalized, 2, item.Size());
            wgc::Direct3D11CaptureFrame frame{nullptr};
            const ULONGLONG capture_deadline = GetTickCount64() + 8000;
            const auto wait_started = std::chrono::steady_clock::now();
            while (!frame && GetTickCount64() < capture_deadline) {
                if (pump) PumpMessages();
                DWORD wait = WaitForSingleObject(callback->arrived.get(), 16);
                if (wait == WAIT_OBJECT_0 && software_bitmap) {
                    winrt::check_hresult(callback->error);
                    frame = callback->frame;
                } else if (!software_bitmap && (poll_only || wait == WAIT_OBJECT_0)) frame = pool.TryGetNextFrame();
            }
            if (!close_subscribed) pool.FrameArrived(registration);
            const auto frame_observed = std::chrono::steady_clock::now();
            const auto frame_wait_us = std::chrono::duration_cast<std::chrono::microseconds>(
                frame_observed - wait_started).count();
            const auto start_to_frame_us = !late_subscribe && frame
                ? std::chrono::duration_cast<std::chrono::microseconds>(frame_observed - capture_started).count()
                : -1;
            std::printf("frame_events=%lu callback_thread=%lu start_capture_call_us=%lld ",
                callback->count.load(), callback->thread.load(), start_capture_call_us);
            if (late_subscribe) {
                std::printf("start_capture_to_first_frame_us=unavailable(late-subscribe) ");
            } else if (frame) {
                std::printf("start_capture_to_first_frame_us=%lld ", start_to_frame_us);
            } else {
                std::printf("start_capture_to_first_frame_us=unavailable(timeout) ");
            }
            if (late_subscribe)
                std::printf("frame_wait_after_subscription_us=%lld\n", frame_wait_us);
            else
                std::printf("frame_wait_after_start_return_us=%lld\n", frame_wait_us);
            if (!frame) winrt::throw_hresult(HRESULT_FROM_WIN32(WAIT_TIMEOUT));
            if (software_bitmap) {
                if (expect_deferred && callback->status_in_callback != 1) winrt::throw_hresult(HRESULT_FROM_WIN32(WAIT_TIMEOUT));
                auto bitmap_deadline = GetTickCount64() + 3000;
                while (callback->operation.Status() == winrt::Windows::Foundation::AsyncStatus::Started && GetTickCount64() < bitmap_deadline) Sleep(10);
                std::printf("software_bitmap=status_after_callback_%d\n", static_cast<int>(callback->operation.Status()));
                if (callback->operation.Status() != winrt::Windows::Foundation::AsyncStatus::Completed)
                    winrt::throw_hresult(HRESULT_FROM_WIN32(WAIT_TIMEOUT));
                auto bitmap = callback->operation.GetResults();
                std::printf("software_bitmap=%dx%d\n", bitmap.PixelWidth(), bitmap.PixelHeight());
                bitmap.Close();
            }
            SaveAndCheckFrame(frame, device.get(), context.get(), output);
            frame.Close();
        }
        session.Close(); pool.Close();
        if (status_fn) {
            status_fn(&status, sizeof(status));
            std::printf("factory_calls=%ld pool_hooks=%ld session_hooks=%ld border_interfaces=%ld border_noops=%ld hook_errors=%ld\n",
                status.factory_calls, status.pool_hooks, status.session_hooks, status.border_interfaces,
                status.border_noops, status.hook_errors);
            // A callback that just signaled our event may still be releasing
            // its last references. Bound the lifetime verification wait.
            auto deadline = GetTickCount64() + 2000;
            while (status.live_handlers && GetTickCount64() < deadline) {
                Sleep(10); status_fn(&status, sizeof(status));
            }
            std::printf("live_handlers=%ld dispatched_calls=%ld dispatch_errors=%ld\n",
                        status.live_handlers, status.dispatched_calls, status.dispatch_errors);
            if (expect_deferred && (status.live_handlers || status.dispatched_calls < 1 || status.dispatch_errors))
                winrt::throw_hresult(E_FAIL);
            if (expect_shim && (status.border_noops < 1 || status.hook_errors)) winrt::throw_hresult(E_FAIL);
        }
        std::puts("PASS"); return 0;
    } catch (const winrt::hresult_error& error) {
        std::fprintf(stderr, "FAIL HRESULT=0x%08lX\n", static_cast<unsigned long>(error.code().value));
        return 1;
    } catch (const std::exception& error) {
        std::fprintf(stderr, "FAIL: %s\n", error.what());
        return 1;
    }
}
