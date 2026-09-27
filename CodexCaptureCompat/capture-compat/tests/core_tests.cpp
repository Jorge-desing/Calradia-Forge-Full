#include "../src/compat.h"
#include "../src/frame_dispatch.h"
#include <algorithm>
#include <atomic>
#include <cstdio>
#include <cstdlib>
#include <cstring>
#include <limits>
#include <thread>
#include <vector>

using namespace capture_compat;
namespace {
void Check(bool value, const char* message) {
    if (!value) { std::fprintf(stderr, "FAIL: %s\n", message); std::exit(1); }
}
std::atomic<int> destroyed = 0;
constexpr SIZE_T kFixtureImageSize = 0x1000;

std::vector<BYTE> MakePeImportFixture() {
    std::vector<BYTE> image(kFixtureImageSize, 0);
    auto dos = reinterpret_cast<IMAGE_DOS_HEADER*>(image.data());
    dos->e_magic = IMAGE_DOS_SIGNATURE;
    dos->e_lfanew = 0x80;
    auto nt = reinterpret_cast<IMAGE_NT_HEADERS64*>(image.data() + dos->e_lfanew);
    nt->Signature = IMAGE_NT_SIGNATURE;
    nt->FileHeader.Machine = IMAGE_FILE_MACHINE_AMD64;
    nt->FileHeader.SizeOfOptionalHeader = sizeof(IMAGE_OPTIONAL_HEADER64);
    nt->OptionalHeader.Magic = IMAGE_NT_OPTIONAL_HDR64_MAGIC;
    nt->OptionalHeader.SizeOfImage = static_cast<DWORD>(image.size());
    nt->OptionalHeader.NumberOfRvaAndSizes = IMAGE_NUMBEROF_DIRECTORY_ENTRIES;
    nt->OptionalHeader.DataDirectory[IMAGE_DIRECTORY_ENTRY_IMPORT] = {0x200, 2 * sizeof(IMAGE_IMPORT_DESCRIPTOR)};

    auto descriptors = reinterpret_cast<IMAGE_IMPORT_DESCRIPTOR*>(image.data() + 0x200);
    descriptors[0].Name = 0x300;
    descriptors[0].OriginalFirstThunk = 0x400;
    descriptors[0].FirstThunk = 0x500;
    std::memcpy(image.data() + 0x300, "test.dll", sizeof("test.dll"));

    auto names = reinterpret_cast<IMAGE_THUNK_DATA64*>(image.data() + 0x400);
    names[0].u1.AddressOfData = 0x600;
    auto addresses = reinterpret_cast<IMAGE_THUNK_DATA64*>(image.data() + 0x500);
    addresses[0].u1.Function = 0x12345678;
    auto import = reinterpret_cast<IMAGE_IMPORT_BY_NAME*>(image.data() + 0x600);
    import->Hint = 0;
    std::memcpy(import->Name, "RoGetActivationFactory", sizeof("RoGetActivationFactory"));
    return image;
}

void TestBoundedImportParser() {
    auto valid = MakePeImportFixture();
    const auto original = valid;
    size_t offset = std::numeric_limits<size_t>::max();
    Check(FindActivationFactoryIatForTest(valid.data(), valid.size(), offset) && offset == 0x500,
          "bounded PE parser finds the resolved activation-factory IAT slot");
    Check(valid == original, "PE parser inspection never mutates the image");

    offset = 77;
    Check(!FindActivationFactoryIatForTest(valid.data(), sizeof(IMAGE_DOS_HEADER), offset) && offset == 77,
          "truncated PE headers fail without changing the output");

    auto bad = MakePeImportFixture();
    reinterpret_cast<IMAGE_DOS_HEADER*>(bad.data())->e_lfanew = static_cast<LONG>(bad.size() - 4);
    Check(!FindActivationFactoryIatForTest(bad.data(), bad.size(), offset), "out-of-range NT header rejected");

    bad = MakePeImportFixture();
    reinterpret_cast<IMAGE_NT_HEADERS64*>(bad.data() + 0x80)->OptionalHeader.SizeOfImage =
        static_cast<DWORD>(bad.size() + 1);
    Check(!FindActivationFactoryIatForTest(bad.data(), bad.size(), offset), "declared image larger than buffer rejected");

    bad = MakePeImportFixture();
    reinterpret_cast<IMAGE_NT_HEADERS64*>(bad.data() + 0x80)->OptionalHeader.DataDirectory[IMAGE_DIRECTORY_ENTRY_IMPORT] =
        {static_cast<DWORD>(bad.size() - sizeof(IMAGE_IMPORT_DESCRIPTOR) / 2), sizeof(IMAGE_IMPORT_DESCRIPTOR)};
    Check(!FindActivationFactoryIatForTest(bad.data(), bad.size(), offset), "truncated import directory rejected");

    bad = MakePeImportFixture();
    reinterpret_cast<IMAGE_NT_HEADERS64*>(bad.data() + 0x80)->OptionalHeader.DataDirectory[IMAGE_DIRECTORY_ENTRY_IMPORT].Size =
        sizeof(IMAGE_IMPORT_DESCRIPTOR);
    Check(!FindActivationFactoryIatForTest(bad.data(), bad.size(), offset), "missing import descriptor terminator rejected");

    bad = MakePeImportFixture();
    reinterpret_cast<IMAGE_IMPORT_DESCRIPTOR*>(bad.data() + 0x200)[0].Name = static_cast<DWORD>(bad.size() - 1);
    bad.back() = 'x';
    Check(!FindActivationFactoryIatForTest(bad.data(), bad.size(), offset), "unterminated descriptor module name rejected");

    bad = MakePeImportFixture();
    reinterpret_cast<IMAGE_IMPORT_DESCRIPTOR*>(bad.data() + 0x200)[0].FirstThunk = static_cast<DWORD>(bad.size() - 4);
    Check(!FindActivationFactoryIatForTest(bad.data(), bad.size(), offset), "out-of-range IAT rejected");

    bad = MakePeImportFixture();
    reinterpret_cast<IMAGE_THUNK_DATA64*>(bad.data() + 0x400)[0].u1.AddressOfData = bad.size() - 2;
    Check(!FindActivationFactoryIatForTest(bad.data(), bad.size(), offset), "truncated IMAGE_IMPORT_BY_NAME rejected");

    bad = MakePeImportFixture();
    auto invalid_name = reinterpret_cast<IMAGE_IMPORT_BY_NAME*>(bad.data() + 0xfe0);
    invalid_name->Hint = 0;
    std::fill(bad.begin() + 0xfe2, bad.end(), static_cast<BYTE>('x'));
    reinterpret_cast<IMAGE_THUNK_DATA64*>(bad.data() + 0x400)[0].u1.AddressOfData = 0xfe0;
    Check(!FindActivationFactoryIatForTest(bad.data(), bad.size(), offset), "unterminated import name rejected");

    bad = MakePeImportFixture();
    reinterpret_cast<IMAGE_NT_HEADERS64*>(bad.data() + 0x80)->OptionalHeader.DataDirectory[IMAGE_DIRECTORY_ENTRY_IMPORT] = {};
    Check(!FindActivationFactoryIatForTest(bad.data(), bad.size(), offset), "missing import directory rejected");

    Check(!FindActivationFactoryIatForTest(reinterpret_cast<const void*>(1), 0x1000, offset, true),
          "unmapped image pointer rejected before any PE read");

    auto mapped = static_cast<BYTE*>(VirtualAlloc(nullptr, 0x2000, MEM_RESERVE | MEM_COMMIT, PAGE_READWRITE));
    Check(mapped != nullptr, "mapped PE fixture allocation succeeds");
    std::memcpy(mapped, valid.data(), valid.size());
    reinterpret_cast<IMAGE_NT_HEADERS64*>(mapped + 0x80)->OptionalHeader.SizeOfImage = 0x2000;
    Check(FindActivationFactoryIatForTest(mapped, 0x2000, offset, true) && offset == 0x500,
          "mapped PE fixture validates readable sections and resolves the IAT");

    reinterpret_cast<IMAGE_IMPORT_DESCRIPTOR*>(mapped + 0x200)[0].Name = 0xff8;
    std::memcpy(mapped + 0xff8, "test.dll", sizeof("test.dll") - 1);
    mapped[0x1000] = '\0';
    DWORD old_protection = 0;
    Check(VirtualProtect(mapped + 0x1000, 0x1000, PAGE_NOACCESS, &old_protection) != FALSE,
          "second PE fixture page can be protected");
    Check(!FindActivationFactoryIatForTest(mapped, 0x2000, offset, true),
          "unterminated string cannot be read across a no-access page");
    VirtualFree(mapped, 0, MEM_RELEASE);
}

struct DeferredRaceContext {
    HANDLE entered = CreateEventW(nullptr, TRUE, FALSE, nullptr);
    HANDLE unblock = CreateEventW(nullptr, TRUE, FALSE, nullptr);
    HANDLE destroyed = CreateEventW(nullptr, TRUE, FALSE, nullptr);
    std::atomic<int> calls{0};
    ~DeferredRaceContext() { CloseHandle(entered); CloseHandle(unblock); CloseHandle(destroyed); }
};

class DeferredRaceHandler final : public FrameHandler, public IAgileObject {
    std::atomic<ULONG> references_{1};
    DeferredRaceContext& context_;
public:
    explicit DeferredRaceHandler(DeferredRaceContext& context) : context_(context) {}
    ~DeferredRaceHandler() { SetEvent(context_.destroyed); }
    HRESULT STDMETHODCALLTYPE QueryInterface(REFIID iid, void** result) override {
        if (!result) return E_POINTER;
        *result = nullptr;
        if (iid == __uuidof(IUnknown) || iid == __uuidof(FrameHandler))
            *result = static_cast<FrameHandler*>(this);
        else if (iid == __uuidof(IAgileObject)) *result = static_cast<IAgileObject*>(this);
        else return E_NOINTERFACE;
        AddRef();
        return S_OK;
    }
    ULONG STDMETHODCALLTYPE AddRef() override { return ++references_; }
    ULONG STDMETHODCALLTYPE Release() override {
        const ULONG remaining = --references_;
        if (!remaining) delete this;
        return remaining;
    }
    HRESULT STDMETHODCALLTYPE Invoke(capture::IDirect3D11CaptureFramePool*, IInspectable*) override {
        ++context_.calls;
        SetEvent(context_.entered);
        return WaitForSingleObject(context_.unblock, 3000) == WAIT_OBJECT_0 ? S_OK : E_ABORT;
    }
};

void TestCloseDuringPendingSubscriptionCommit() {
    int pool_storage = 0;
    DeferredRaceContext context;
    Check(context.entered && context.unblock && context.destroyed, "subscription-race events created");
    FrameHandler* original = new DeferredRaceHandler(context);
    FrameHandler* deferred = nullptr;
    Check(MakeDeferredHandler(original, &deferred) == S_OK, "subscription-race deferred wrapper created");
    original->Release();

    auto pending = BeginSubscriptionForTest(&pool_storage, deferred);
    Check(pending != nullptr, "pending subscription published before native add completes");
    Check(deferred->Invoke(nullptr, nullptr) == S_OK &&
          WaitForSingleObject(context.entered, 3000) == WAIT_OBJECT_0,
          "in-flight frame callback is active before close cancellation");

    HANDLE commit_gate = CreateEventW(nullptr, TRUE, FALSE, nullptr);
    HANDLE commit_finished = CreateEventW(nullptr, TRUE, FALSE, nullptr);
    Check(commit_gate && commit_finished, "subscription-race barriers created");
    EventRegistrationToken token{};
    token.value = 42;
    std::atomic<bool> committed{true};
    std::thread add_completion([&] {
        if (WaitForSingleObject(commit_gate, 3000) != WAIT_OBJECT_0) {
            committed = true;
            SetEvent(commit_finished);
            return;
        }
        committed = CommitSubscriptionForTest(pending, token);
        if (!committed.load()) AbortSubscriptionForTest(pending);
        SetEvent(commit_finished);
    });

    // This deterministic barrier models native AddFrame having succeeded
    // while the registration is still pending, then ClosePool cancelling it.
    CancelSubscriptionsForTest(&pool_storage);
    SetEvent(commit_gate);
    Check(WaitForSingleObject(commit_finished, 3000) == WAIT_OBJECT_0,
          "late add commit completes after close cancellation");
    add_completion.join();
    Check(!committed.load(), "close wins over a pending subscription commit");
    Check(WaitForSingleObject(context.destroyed, 0) == WAIT_TIMEOUT,
          "close releases registry ownership but in-flight callback keeps handler alive");

    SetEvent(context.unblock);
    Check(WaitForSingleObject(context.destroyed, 3000) == WAIT_OBJECT_0,
          "in-flight callback releases handler after the close race");
    CloseHandle(commit_gate);
    CloseHandle(commit_finished);
}

class FakeSession final : public capture::IGraphicsCaptureSession, public capture::IGraphicsCaptureSession3 {
    std::atomic<ULONG> refs_{1};
public:
    HRESULT border_result;
    boolean border = true;
    int setter_calls = 0;
    explicit FakeSession(HRESULT result) : border_result(result) {}
    ~FakeSession() { ++destroyed; }
    HRESULT STDMETHODCALLTYPE QueryInterface(REFIID iid, void** result) override {
        if (!result) return E_POINTER;
        *result = nullptr;
        if (iid == __uuidof(IUnknown) || iid == __uuidof(IInspectable) ||
            iid == __uuidof(capture::IGraphicsCaptureSession))
            *result = static_cast<capture::IGraphicsCaptureSession*>(this);
        else if (iid == __uuidof(capture::IGraphicsCaptureSession3)) {
            if (FAILED(border_result)) return border_result;
            *result = static_cast<capture::IGraphicsCaptureSession3*>(this);
        } else return E_NOINTERFACE;
        AddRef();
        return S_OK;
    }
    ULONG STDMETHODCALLTYPE AddRef() override { return ++refs_; }
    ULONG STDMETHODCALLTYPE Release() override { ULONG n = --refs_; if (!n) delete this; return n; }
    HRESULT STDMETHODCALLTYPE GetIids(ULONG* count, IID** values) override {
        if (!count || !values) return E_POINTER;
        *values = nullptr; *count = 0; return S_OK;
    }
    HRESULT STDMETHODCALLTYPE GetRuntimeClassName(HSTRING* result) override {
        return WindowsCreateString(L"FakeSession", 11, result);
    }
    HRESULT STDMETHODCALLTYPE GetTrustLevel(TrustLevel* result) override {
        if (!result) return E_POINTER; *result = BaseTrust; return S_OK;
    }
    HRESULT STDMETHODCALLTYPE StartCapture() override { return S_FALSE; }
    HRESULT STDMETHODCALLTYPE get_IsBorderRequired(boolean* result) override {
        if (!result) return E_POINTER; *result = border; return S_OK;
    }
    HRESULT STDMETHODCALLTYPE put_IsBorderRequired(boolean value) override {
        border = value; ++setter_calls; return S_OK;
    }
};

// Force the same ABI vtable dispatch used by the Rust Windows projection.
__declspec(noinline) HRESULT Query(capture::IGraphicsCaptureSession* session, REFIID iid, void** out) {
    auto table = *reinterpret_cast<void***>(session);
    using Fn = HRESULT(STDMETHODCALLTYPE*)(void*, REFIID, void**);
    return reinterpret_cast<Fn>(table[0])(session, iid, out);
}
}

int main() {
    TestBoundedImportParser();
    TestCloseDuringPendingSubscriptionCommit();
    auto missing = new FakeSession(E_NOINTERFACE);
    auto base = static_cast<capture::IGraphicsCaptureSession*>(missing);
    Check(HookSession(base), "install session hook");
    Check(HookSession(base), "idempotent installation");
    capture::IGraphicsCaptureSession3* border = nullptr;
    Check(Query(base, __uuidof(capture::IGraphicsCaptureSession3), reinterpret_cast<void**>(&border)) == S_OK,
          "missing border interface supplied");
    Check(border->put_IsBorderRequired(false) == S_OK, "optional false setter succeeds");
    Check(border->put_IsBorderRequired(true) == S_OK, "optional true setter succeeds");
    boolean is_required = false;
    Check(border->get_IsBorderRequired(&is_required) == S_OK && is_required, "default border stays on");
    Check(missing->setter_calls == 0, "no unsupported OS setter called");
    Check(border->get_IsBorderRequired(nullptr) == E_POINTER, "null getter rejected");
    Check(Query(base, __uuidof(capture::IGraphicsCaptureSession3), nullptr) == E_POINTER, "null QI rejected");
    IUnknown* identity = nullptr;
    Check(border->QueryInterface(IID_PPV_ARGS(&identity)) == S_OK && identity == static_cast<IUnknown*>(base),
          "original COM IUnknown identity retained");
    identity->Release();
    void* other = nullptr;
    const GUID unrelated = {0x1a2b3c4d, 0x1111, 0x2222, {0x88,0x11,0x22,0x33,0x44,0x55,0x66,0x77}};
    Check(Query(base, unrelated, &other) == E_NOINTERFACE && !other, "unrelated missing IID unchanged");
    Check(base->StartCapture() == S_FALSE, "other method result unchanged");
    std::vector<std::thread> threads;
    for (int t = 0; t < 8; ++t) threads.emplace_back([base] {
        for (int i = 0; i < 200; ++i) {
            capture::IGraphicsCaptureSession3* p = nullptr;
            Check(Query(base, __uuidof(capture::IGraphicsCaptureSession3), reinterpret_cast<void**>(&p)) == S_OK,
                  "concurrent QI");
            p->Release();
        }
    });
    for (auto& thread : threads) thread.join();
    base->Release();
    Check(destroyed == 0, "fallback keeps owner alive");
    border->Release();
    Check(destroyed == 1, "owner released with fallback");

    for (HRESULT preserved : {E_ACCESSDENIED, E_FAIL}) {
        auto item = new FakeSession(preserved);
        auto session = static_cast<capture::IGraphicsCaptureSession*>(item);
        border = nullptr;
        Check(Query(session, __uuidof(capture::IGraphicsCaptureSession3), reinterpret_cast<void**>(&border)) == preserved,
              "non-compatibility failures preserved");
        Check(!border, "failed query does not supply fake interface");
        session->Release();
    }
    auto supported = new FakeSession(S_OK);
    auto session = static_cast<capture::IGraphicsCaptureSession*>(supported);
    border = nullptr;
    Check(Query(session, __uuidof(capture::IGraphicsCaptureSession3), reinterpret_cast<void**>(&border)) == S_OK &&
          border == static_cast<capture::IGraphicsCaptureSession3*>(supported), "supported native interface unchanged");
    Check(border->put_IsBorderRequired(false) == S_OK && supported->setter_calls == 1 && !supported->border,
          "native setter delegated");
    border->Release();
    session->Release();
    Check(destroyed == 4, "all test owners destroyed exactly once");
    Status status{};
    GetStatus(status);
    Check(status.hook_errors == 0, "no hook errors");
    std::printf("PASS: bounded PE parser, missing/native/error QI, identity, lifetime, nulls, concurrency; fallbacks=%ld\n",
                status.border_interfaces);
}
