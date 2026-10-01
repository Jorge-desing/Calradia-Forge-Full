#include <windows.h>
#include <algorithm>
#include <array>
#include <cmath>
#include <cstdint>
#include <cwchar>
#include <filesystem>
#include <fstream>
#include <locale>
#include <string>
#include <vector>

namespace {
constexpr int kStatusId = 101;
constexpr int kButtonId = 102;
constexpr int kInputId = 103;
constexpr int kCtrlKCommand = 104;
constexpr int kShiftF6Command = 105;
constexpr int kAltF8Command = 106;
constexpr int kKeyboardStatusId = 107;
constexpr int kAltOem3Command = 108;
constexpr int kAltShiftOem3Command = 109;
constexpr int kAltOem5Command = 110;
constexpr UINT_PTR kAutoCloseTimerId = 1;
constexpr UINT_PTR kMeasurementTimerId = 2;
constexpr size_t kMaximumPointerSamples = 131072;
constexpr ULONG kMaximumMeasurementSeconds = 90;

enum class PointerEvent : uint32_t {
    MouseMove,
    Moving,
    EnterSizeMove,
    ExitSizeMove,
};

struct PointerSample {
    PointerEvent event{};
    int64_t qpc{};
    DWORD message_time{};
    DWORD dispatch_tick{};
    LONG x{};
    LONG y{};
    double move_duration_ms{};
};

struct PointerMeasurement {
    bool enabled = false;
    bool overflow = false;
    ULONG duration_seconds = 0;
    std::wstring output_path;
    LARGE_INTEGER qpc_frequency{};
    LARGE_INTEGER move_size_started{};
    bool move_size_active = false;
    std::array<PointerSample, kMaximumPointerSamples> samples{};
    size_t sample_count = 0;
};

HWND g_status = nullptr;
HWND g_input = nullptr;
HWND g_button = nullptr;
HWND g_keyboard_status = nullptr;
unsigned g_f10_down = 0;
unsigned g_f10_up = 0;
unsigned g_oem3_down = 0;
unsigned g_oem3_up = 0;
unsigned g_oem5_down = 0;
unsigned g_oem5_up = 0;
unsigned g_last_scan = 0;
unsigned g_down_scan = 0, g_up_scan = 0;
const wchar_t* g_keyboard_observation = L"Keyboard probe ready";
PointerMeasurement g_measurement;

void RecordPointerEvent(PointerEvent event, WPARAM w, LPARAM l) {
    if (!g_measurement.enabled) return;

    LARGE_INTEGER timestamp{};
    if (!QueryPerformanceCounter(&timestamp)) return;

    PointerSample sample{};
    sample.event = event;
    sample.qpc = timestamp.QuadPart;
    sample.message_time = static_cast<DWORD>(GetMessageTime());
    sample.dispatch_tick = GetTickCount();
    if (event == PointerEvent::MouseMove) {
        sample.x = static_cast<short>(LOWORD(l));
        sample.y = static_cast<short>(HIWORD(l));
    } else if (event == PointerEvent::Moving && l != 0) {
        const auto* rect = reinterpret_cast<const RECT*>(l);
        sample.x = rect->left;
        sample.y = rect->top;
    } else if (event == PointerEvent::EnterSizeMove) {
        g_measurement.move_size_started = timestamp;
        g_measurement.move_size_active = true;
    } else if (event == PointerEvent::ExitSizeMove && g_measurement.move_size_active) {
        sample.move_duration_ms = 1000.0 * static_cast<double>(timestamp.QuadPart -
            g_measurement.move_size_started.QuadPart) / g_measurement.qpc_frequency.QuadPart;
        g_measurement.move_size_active = false;
    }

    if (g_measurement.sample_count == g_measurement.samples.size()) {
        g_measurement.overflow = true;
        return;
    }
    g_measurement.samples[g_measurement.sample_count++] = sample;
    (void)w;
}

const wchar_t* EventName(PointerEvent event) {
    switch (event) {
    case PointerEvent::MouseMove: return L"WM_MOUSEMOVE";
    case PointerEvent::Moving: return L"WM_MOVING";
    case PointerEvent::EnterSizeMove: return L"WM_ENTERSIZEMOVE";
    case PointerEvent::ExitSizeMove: return L"WM_EXITSIZEMOVE";
    default: return L"UNKNOWN";
    }
}

bool WritePointerMeasurement() {
    if (!g_measurement.enabled) return true;

    std::wofstream output(std::filesystem::path(g_measurement.output_path), std::ios::out | std::ios::trunc);
    if (!output) return false;
    output.imbue(std::locale::classic());
    output << L"record,event,count,p50_ms,p95_ms,p99_ms,qpc_ticks,interval_ms,queue_age_ms,x,y,duration_ms\n";

    std::vector<double> mouse_intervals;
    std::vector<double> mouse_queue_ages;
    std::vector<double> moving_intervals;
    std::vector<double> move_size_durations;
    mouse_intervals.reserve(g_measurement.sample_count);
    mouse_queue_ages.reserve(g_measurement.sample_count);
    moving_intervals.reserve(g_measurement.sample_count);
    move_size_durations.reserve(32);

    int64_t last_mouse_qpc = 0;
    int64_t last_moving_qpc = 0;
    size_t mouse_count = 0;
    size_t moving_count = 0;
    size_t enter_count = 0;
    size_t exit_count = 0;
    for (size_t index = 0; index < g_measurement.sample_count; ++index) {
        const auto& sample = g_measurement.samples[index];
        double interval_ms = -1.0;
        double queue_age_ms = -1.0;
        if (sample.event == PointerEvent::MouseMove) {
            ++mouse_count;
            if (last_mouse_qpc != 0) {
                interval_ms = 1000.0 * static_cast<double>(sample.qpc - last_mouse_qpc) /
                    g_measurement.qpc_frequency.QuadPart;
                mouse_intervals.push_back(interval_ms);
            }
            last_mouse_qpc = sample.qpc;
            // WM_MOUSEMOVE is posted. This coarse DWORD delta approximates time
            // spent queued; it is not hardware-to-window input latency.
            queue_age_ms = static_cast<double>(static_cast<DWORD>(sample.dispatch_tick - sample.message_time));
            mouse_queue_ages.push_back(queue_age_ms);
        } else if (sample.event == PointerEvent::Moving) {
            ++moving_count;
            if (last_moving_qpc != 0) {
                interval_ms = 1000.0 * static_cast<double>(sample.qpc - last_moving_qpc) /
                    g_measurement.qpc_frequency.QuadPart;
                moving_intervals.push_back(interval_ms);
            }
            last_moving_qpc = sample.qpc;
        } else if (sample.event == PointerEvent::EnterSizeMove) {
            ++enter_count;
        } else if (sample.event == PointerEvent::ExitSizeMove) {
            ++exit_count;
            if (sample.move_duration_ms > 0.0) move_size_durations.push_back(sample.move_duration_ms);
        }

        output << L"sample," << EventName(sample.event) << L",,,,," << sample.qpc << L',';
        if (interval_ms >= 0.0) output << interval_ms;
        output << L',';
        if (queue_age_ms >= 0.0) output << queue_age_ms;
        output << L',' << sample.x << L',' << sample.y << L',';
        if (sample.move_duration_ms > 0.0) output << sample.move_duration_ms;
        output << L'\n';
    }

    const auto write_summary = [&output](const wchar_t* name, std::vector<double>& values, size_t count) {
        output << L"summary," << name << L',' << count << L',';
        if (values.empty()) {
            output << L",,,,,,,,,\n";
            return;
        }
        // Sort once, then calculate all nearest-rank percentiles from the same samples.
        std::sort(values.begin(), values.end());
        const auto percentile = [&values](double fraction) {
            const size_t rank = static_cast<size_t>(std::ceil(fraction * values.size()));
            return values[std::clamp(rank, size_t{1}, values.size()) - 1];
        };
        output << percentile(0.50) << L',' << percentile(0.95) << L',' << percentile(0.99) << L",,,,,,\n";
    };
    write_summary(L"WM_MOUSEMOVE_interarrival", mouse_intervals, mouse_intervals.size());
    write_summary(L"WM_MOUSEMOVE_queue_age_approx", mouse_queue_ages, mouse_queue_ages.size());
    write_summary(L"WM_MOVING_interarrival", moving_intervals, moving_intervals.size());
    write_summary(L"size_move_duration", move_size_durations, move_size_durations.size());
    output << L"summary,WM_MOUSEMOVE_samples," << mouse_count << L",,,,,,,,,\n";
    output << L"summary,WM_MOVING_samples," << moving_count << L",,,,,,,,,\n";
    output << L"summary,WM_ENTERSIZEMOVE_samples," << enter_count << L",,,,,,,,,\n";
    output << L"summary,WM_EXITSIZEMOVE_samples," << exit_count << L",,,,,,,,,\n";
    output << L"summary,sample_buffer_overflow," << (g_measurement.overflow ? 1 : 0) << L",,,,,,,,,\n";
    output << L"note,delivery_cadence_only;not_hardware_to_screen_latency,,,,,,,,,,\n";
    output.flush();
    return output.good();
}

bool ParseMeasurementArguments(PCWSTR command_line) {
    const wchar_t* cursor = command_line;
    while (*cursor == L' ' || *cursor == L'\t') ++cursor;
    if (*cursor == L'\0') return true;

    constexpr wchar_t mode[] = L"--measure-pointer";
    const size_t mode_length = ARRAYSIZE(mode) - 1;
    if (std::wcsncmp(cursor, mode, mode_length) != 0 ||
        (cursor[mode_length] != L' ' && cursor[mode_length] != L'\t')) return false;
    cursor += mode_length;
    while (*cursor == L' ' || *cursor == L'\t') ++cursor;
    wchar_t* duration_end = nullptr;
    const unsigned long duration = std::wcstoul(cursor, &duration_end, 10);
    if (duration_end == cursor || duration < 5 || duration > kMaximumMeasurementSeconds ||
        (*duration_end != L' ' && *duration_end != L'\t')) return false;
    cursor = duration_end;
    while (*cursor == L' ' || *cursor == L'\t') ++cursor;
    if (*cursor != L'"') return false;
    const wchar_t* path_start = ++cursor;
    while (*cursor != L'\0' && *cursor != L'"') ++cursor;
    if (*cursor != L'"' || cursor == path_start) return false;
    g_measurement.output_path.assign(path_start, cursor);
    ++cursor;
    while (*cursor == L' ' || *cursor == L'\t') ++cursor;
    if (*cursor != L'\0') return false;
    if (GetFileAttributesW(g_measurement.output_path.c_str()) != INVALID_FILE_ATTRIBUTES) return false;
    if (!QueryPerformanceFrequency(&g_measurement.qpc_frequency) || g_measurement.qpc_frequency.QuadPart <= 0) return false;

    g_measurement.duration_seconds = static_cast<ULONG>(duration);
    g_measurement.enabled = true;
    return true;
}

void SetFixtureStatus(const wchar_t* value) {
    if (g_status) SetWindowTextW(g_status, value);
}

void UpdateKeyboardStatus() {
    if (!g_keyboard_status) return;
    wchar_t text[512]{};
    swprintf_s(text, L"%ls; scan=0x%02X; down/up scans=0x%02X/0x%02X\r\nF10 down/up=%u/%u; OEM3 down/up=%u/%u; OEM5 down/up=%u/%u\r\nCurrently pressed: Ctrl=%d Alt=%d Shift=%d",
        g_keyboard_observation, g_last_scan, g_down_scan, g_up_scan, g_f10_down, g_f10_up, g_oem3_down, g_oem3_up,
        g_oem5_down, g_oem5_up,
        (GetAsyncKeyState(VK_CONTROL) & 0x8000) != 0,
        (GetAsyncKeyState(VK_MENU) & 0x8000) != 0,
        (GetAsyncKeyState(VK_SHIFT) & 0x8000) != 0);
    SetWindowTextW(g_keyboard_status, text);
}

void ObserveFixtureKeyboard(const MSG& message, HWND window) {
    // Observe only queued messages addressed to this fixture or its controls.
    // The normal accelerator/dialog dispatch still receives every message.
    if (message.hwnd != window && !IsChild(window, message.hwnd)) return;
    const bool down = message.message == WM_KEYDOWN || message.message == WM_SYSKEYDOWN;
    const bool up = message.message == WM_KEYUP || message.message == WM_SYSKEYUP;
    if (!down && !up) return;
    if (message.wParam == VK_F10 || message.wParam == VK_OEM_3 || message.wParam == VK_OEM_5) {
        g_last_scan = static_cast<unsigned>((message.lParam >> 16) & 0xff);
        if (down) g_down_scan = g_last_scan; else g_up_scan = g_last_scan;
        if (message.wParam == VK_F10) {
            if (down) ++g_f10_down; else ++g_f10_up;
            g_keyboard_observation = down ? L"Observed F10 down" : L"Observed F10 up";
        } else if (message.wParam == VK_OEM_3) {
            if (down) ++g_oem3_down; else ++g_oem3_up;
            // Keep the accelerator observation when its matching key-up arrives.
            if (down) g_keyboard_observation = L"Observed OEM3 down";
        } else {
            if (down) ++g_oem5_down; else ++g_oem5_up;
            if (down) g_keyboard_observation = L"Observed OEM5 down";
        }
        UpdateKeyboardStatus();
    } else if (message.wParam == VK_CONTROL || message.wParam == VK_LCONTROL || message.wParam == VK_RCONTROL ||
        message.wParam == VK_MENU || message.wParam == VK_LMENU || message.wParam == VK_RMENU ||
        message.wParam == VK_SHIFT || message.wParam == VK_LSHIFT || message.wParam == VK_RSHIFT) {
        UpdateKeyboardStatus();
    }
}

LRESULT CALLBACK TestWindowProc(HWND window, UINT message, WPARAM w, LPARAM l) {
    if (message == WM_MOUSEMOVE) RecordPointerEvent(PointerEvent::MouseMove, w, l);
    else if (message == WM_MOVING) RecordPointerEvent(PointerEvent::Moving, w, l);
    else if (message == WM_ENTERSIZEMOVE) RecordPointerEvent(PointerEvent::EnterSizeMove, w, l);
    else if (message == WM_EXITSIZEMOVE) RecordPointerEvent(PointerEvent::ExitSizeMove, w, l);

    if (message == WM_CREATE) {
        const auto* create = reinterpret_cast<LPCREATESTRUCTW>(l);
        const wchar_t* status = g_measurement.enabled
            ? L"Measurement active: move only over this fixture; drag only its title bar."
            : L"Fixture ready; no project action is connected.";
        g_status = CreateWindowExW(0, L"STATIC", status,
            WS_CHILD | WS_VISIBLE | SS_LEFT, 24, 32, 560, 26, window,
            reinterpret_cast<HMENU>(static_cast<INT_PTR>(kStatusId)), create->hInstance, nullptr);
        g_input = CreateWindowExW(WS_EX_CLIENTEDGE, L"EDIT", L"",
            WS_CHILD | WS_VISIBLE | WS_TABSTOP | ES_AUTOHSCROLL, 24, 76, 560, 32, window,
            reinterpret_cast<HMENU>(static_cast<INT_PTR>(kInputId)), create->hInstance, nullptr);
        g_button = CreateWindowExW(0, L"BUTTON", L"Safe fixture control",
            WS_CHILD | WS_VISIBLE | WS_TABSTOP | BS_PUSHBUTTON, 24, 124, 200, 36, window,
            reinterpret_cast<HMENU>(static_cast<INT_PTR>(kButtonId)), create->hInstance, nullptr);
        g_keyboard_status = CreateWindowExW(0, L"STATIC", L"Keyboard probe ready; no global hook is installed.",
            WS_CHILD | WS_VISIBLE | SS_LEFT, 24, 178, 560, 100, window,
            reinterpret_cast<HMENU>(static_cast<INT_PTR>(kKeyboardStatusId)), create->hInstance, nullptr);
        if (!g_status || !g_input || !g_button || !g_keyboard_status) return -1;
        return 0;
    }
    if (message == WM_COMMAND) {
        const int command = LOWORD(w);
        if (command == kButtonId && HIWORD(w) == BN_CLICKED) {
            SetFixtureStatus(L"Safe fixture control activated.");
            return 0;
        }
        if (l == 0 && command == kCtrlKCommand) {
            SetFixtureStatus(L"Observed Ctrl+K.");
            return 0;
        }
        if (l == 0 && command == kShiftF6Command) {
            SetFixtureStatus(L"Observed Shift+F6.");
            return 0;
        }
        if (l == 0 && command == kAltF8Command) {
            SetFixtureStatus(L"Observed Alt+F8.");
            return 0;
        }
        if (l == 0 && (command == kAltOem3Command || command == kAltShiftOem3Command || command == kAltOem5Command)) {
            g_keyboard_observation = command == kAltOem5Command ? L"Observed Alt+OEM5" :
                command == kAltOem3Command ? L"Observed Alt+OEM3" : L"Observed Alt+Shift+OEM3";
            UpdateKeyboardStatus();
            return 0;
        }
    }
    if (message == WM_DESTROY) { PostQuitMessage(0); return 0; }
    if (message == WM_TIMER && (w == kAutoCloseTimerId || w == kMeasurementTimerId)) {
        DestroyWindow(window);
        return 0;
    }
    if (message == WM_PAINT) {
        PAINTSTRUCT paint{};
        HDC dc = BeginPaint(window, &paint);
        RECT rect{}; GetClientRect(window, &rect);
        HBRUSH brush = CreateSolidBrush(RGB(24, 52, 67));
        FillRect(dc, &rect, brush); DeleteObject(brush);
        SetBkMode(dc, TRANSPARENT); SetTextColor(dc, RGB(255, 255, 255));
        RECT heading = rect;
        heading.left += 24; heading.top += 8;
        DrawTextW(dc, L"Codex Computer Use isolated fixture", -1, &heading,
                  DT_LEFT | DT_TOP | DT_SINGLELINE);
        EndPaint(window, &paint); return 0;
    }
    return DefWindowProcW(window, message, w, l);
}
}

int WINAPI wWinMain(HINSTANCE instance, HINSTANCE, PWSTR command_line, int) {
    if (!ParseMeasurementArguments(command_line)) return 2;
    WNDCLASSW klass{};
    klass.lpfnWndProc = TestWindowProc; klass.hInstance = instance;
    klass.lpszClassName = L"CodexCaptureTestWindow";
    if (!RegisterClassW(&klass)) return 1;
    const wchar_t* title = g_measurement.enabled
        ? L"Codex Computer Use pointer measurement fixture"
        : L"Codex Computer Use fixture";
    HWND window = CreateWindowExW(0, klass.lpszClassName, title,
        WS_OVERLAPPEDWINDOW, 80, 80, 640, 400, nullptr, nullptr, instance, nullptr);
    if (!window) return 1;
    const ACCEL entries[] = {
        { static_cast<BYTE>(FVIRTKEY | FCONTROL), 'K', kCtrlKCommand },
        { static_cast<BYTE>(FVIRTKEY | FSHIFT), VK_F6, kShiftF6Command },
        { static_cast<BYTE>(FVIRTKEY | FALT), VK_F8, kAltF8Command },
        { static_cast<BYTE>(FVIRTKEY | FALT), VK_OEM_3, kAltOem3Command },
        { static_cast<BYTE>(FVIRTKEY | FALT | FSHIFT), VK_OEM_3, kAltShiftOem3Command },
        { static_cast<BYTE>(FVIRTKEY | FALT), VK_OEM_5, kAltOem5Command },
    };
    HACCEL accelerators = CreateAcceleratorTableW(const_cast<LPACCEL>(entries), ARRAYSIZE(entries));
    if (!accelerators) { DestroyWindow(window); return 1; }

    SetTimer(window, kAutoCloseTimerId, 600000, nullptr);
    ShowWindow(window, SW_SHOWNORMAL);
    UpdateWindow(window);
    SetFocus(g_input);
    if (g_measurement.enabled && !SetTimer(window, kMeasurementTimerId,
        g_measurement.duration_seconds * 1000, nullptr)) {
        DestroyAcceleratorTable(accelerators);
        DestroyWindow(window);
        return 3;
    }
    MSG message{};
    while (GetMessageW(&message, nullptr, 0, 0) > 0) {
        ObserveFixtureKeyboard(message, window);
        if (!TranslateAcceleratorW(window, accelerators, &message) &&
            !IsDialogMessageW(window, &message)) {
            TranslateMessage(&message);
            DispatchMessageW(&message);
        }
    }
    DestroyAcceleratorTable(accelerators);
    return WritePointerMeasurement() ? 0 : 4;
}
