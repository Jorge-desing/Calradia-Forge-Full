#include "../src/frame_dispatch.h"
#include <atomic>
#include <cstdio>
#include <cstdlib>
#include <initializer_list>

using namespace capture_compat;
void Check(bool ok, const char* message) {
    if (!ok) { std::fprintf(stderr, "FAIL: %s\n", message); std::exit(1); }
}
struct Context {
    HANDLE entered = CreateEventW(nullptr, TRUE, FALSE, nullptr);
    HANDLE unblock = CreateEventW(nullptr, TRUE, FALSE, nullptr);
    HANDLE destroyed = CreateEventW(nullptr, TRUE, FALSE, nullptr);
    std::atomic<int> calls{0};
    std::atomic<DWORD> thread{0};
    std::atomic<int> apartment{-1};
    HRESULT result = S_OK;
    bool throw_on_invoke = false;
    ~Context() { CloseHandle(entered); CloseHandle(unblock); CloseHandle(destroyed); }
};
class Handler final : public FrameHandler, public IAgileObject {
    std::atomic<ULONG> refs_{1};
    Context& context_;
    bool agile_;
    bool throw_on_invoke_;
public:
    Handler(Context& context, bool agile, bool throw_on_invoke = false)
        : context_(context), agile_(agile), throw_on_invoke_(throw_on_invoke) {}
    ~Handler() { SetEvent(context_.destroyed); }
    HRESULT STDMETHODCALLTYPE QueryInterface(REFIID iid, void** result) override {
        if (!result) return E_POINTER;
        *result = nullptr;
        if (iid == __uuidof(IUnknown) || iid == __uuidof(FrameHandler)) *result = static_cast<FrameHandler*>(this);
        else if (agile_ && iid == __uuidof(IAgileObject)) *result = static_cast<IAgileObject*>(this);
        else return E_NOINTERFACE;
        AddRef(); return S_OK;
    }
    ULONG STDMETHODCALLTYPE AddRef() override { return ++refs_; }
    ULONG STDMETHODCALLTYPE Release() override { ULONG n = --refs_; if (!n) delete this; return n; }
    HRESULT STDMETHODCALLTYPE Invoke(capture::IDirect3D11CaptureFramePool*, IInspectable*) override {
        if (throw_on_invoke_) throw E_UNEXPECTED;
        ++context_.calls; context_.thread = GetCurrentThreadId();
        APTTYPE type{}; APTTYPEQUALIFIER qualifier{};
        if (SUCCEEDED(CoGetApartmentType(&type, &qualifier))) context_.apartment = type;
        SetEvent(context_.entered);
        Check(WaitForSingleObject(context_.unblock, 3000) == WAIT_OBJECT_0, "callback release deadline");
        return context_.result;
    }
};

int main() {
    FrameHandler* deferred = nullptr;
    Check(ThreadpoolSetupFailure(ERROR_NOT_ENOUGH_MEMORY) == HRESULT_FROM_WIN32(ERROR_NOT_ENOUGH_MEMORY),
          "threadpool setup failure preserves its Win32 error");
    Check(ThreadpoolSetupFailure(ERROR_SUCCESS) == E_FAIL,
          "threadpool setup failure never returns success for a missing error code");
    Check(MakeDeferredHandler(nullptr, &deferred) == E_POINTER && !deferred, "null original rejected");
    {
        Context context;
        auto original = new Handler(context, false);
        Check(MakeDeferredHandler(original, &deferred) == E_NOINTERFACE && !deferred, "non-agile delegate preserved");
        original->Release();
        Check(WaitForSingleObject(context.destroyed, 0) == WAIT_OBJECT_0, "rejected handler not retained");
    }
    for (bool fail : {false, true}) {
        Context context;
        context.result = fail ? E_FAIL : S_OK;
        auto original = new Handler(context, true);
        Check(MakeDeferredHandler(original, &deferred) == S_OK, "agile wrapper created");
        original->Release();
        Check(deferred->Invoke(nullptr, nullptr) == S_OK, "event producer returns before consumer unblocks");
        Check(WaitForSingleObject(context.entered, 3000) == WAIT_OBJECT_0, "worker entered");
        Check(context.thread != GetCurrentThreadId() && context.apartment == APTTYPE_MTA, "separate MTA worker");
        for (int i = 0; i < 100; ++i) Check(deferred->Invoke(nullptr, nullptr) == S_OK, "coalesced notifications");
        Check(context.calls == 1, "at most one running callback");
        CancelDeferredHandler(deferred);
        Check(deferred->Invoke(nullptr, nullptr) == S_OK && context.calls == 1, "cancelled delegate suppresses calls");
        deferred->Release(); deferred = nullptr;
        Check(WaitForSingleObject(context.destroyed, 0) == WAIT_TIMEOUT, "in-flight work retains original");
        SetEvent(context.unblock);
        Check(WaitForSingleObject(context.destroyed, 3000) == WAIT_OBJECT_0, "worker releases original exactly once");
    }
    {
        Context context;
        auto original = new Handler(context, true);
        Check(MakeDeferredHandler(original, &deferred) == S_OK, "cancel-before-invoke setup");
        original->Release();
        CancelDeferredHandler(deferred);
        Check(deferred->Invoke(nullptr, nullptr) == S_OK && context.calls == 0, "cancel before queueing");
        deferred->Release();
    }
    {
        Context context;
        SetEvent(context.unblock);
        LONG live_before = 0, dispatched_before = 0, errors_before = 0;
        const ULONGLONG release_deadline = GetTickCount64() + 3000;
        do {
            GetDispatchStatus(live_before, dispatched_before, errors_before);
            if (live_before == 0) break;
            Sleep(1);
        } while (GetTickCount64() < release_deadline);
        Check(live_before == 0, "previous dispatch callbacks are fully released");
        Check(errors_before == 1, "previous asynchronous failure remains recorded");
        LONG work_items_before = GetWorkItemCreationCount();
        auto original = new Handler(context, true);
        Check(MakeDeferredHandler(original, &deferred) == S_OK, "reusable work-item setup");
        original->Release();
        Check(GetWorkItemCreationCount() == work_items_before + 1,
              "one threadpool work object is created per delegate");
        for (int expected = 1; expected <= 4; ++expected) {
            const ULONGLONG deadline = GetTickCount64() + 3000;
            LONG live_now = 0, dispatched_now = dispatched_before, errors_now = 0;
            do {
                Check(deferred->Invoke(nullptr, nullptr) == S_OK, "reusable work-item dispatch");
                GetDispatchStatus(live_now, dispatched_now, errors_now);
                if (context.calls == expected && dispatched_now >= dispatched_before + expected) break;
                Sleep(1);
            } while (GetTickCount64() < deadline);
            Check(context.calls == expected && dispatched_now >= dispatched_before + expected,
                  "sequential callbacks reuse the single-flight work item");
            Check(GetWorkItemCreationCount() == work_items_before + 1,
                  "sequential callbacks do not allocate additional work objects");
            Check(live_now == 1 && errors_now == 1, "reusable dispatch lifetime and error counters remain stable");
        }
        CancelDeferredHandler(deferred);
        deferred->Release(); deferred = nullptr;
        Check(WaitForSingleObject(context.destroyed, 3000) == WAIT_OBJECT_0,
              "reused work item releases its handler");
    }
    {
        Context context;
        auto original = new Handler(context, false, true);
        HRESULT result = InvokeHandlerSafely(original, nullptr, nullptr);
        original->Release();
        Check(result == E_UNEXPECTED, "synchronous non-agile fallback contains delegate exceptions");
        Check(WaitForSingleObject(context.destroyed, 0) == WAIT_OBJECT_0,
              "synchronous fallback releases the delegate");
    }
    {
        Context context;
        auto original = new Handler(context, true, true);
        Check(MakeDeferredHandler(original, &deferred) == S_OK, "throwing delegate setup");
        original->Release();
        LONG live_before = 0, calls_before = 0, errors_before = 0;
        GetDispatchStatus(live_before, calls_before, errors_before);
        Check(live_before >= 1, "throwing delegate wrapper is tracked");
        Check(deferred->Invoke(nullptr, nullptr) == S_OK, "throwing delegate is queued asynchronously");
        deferred->Release(); deferred = nullptr;
        Check(WaitForSingleObject(context.destroyed, 3000) == WAIT_OBJECT_0,
              "throwing delegate is released after callback failure");
        LONG live_after = 1, calls_after = calls_before, errors_after = errors_before;
        const ULONGLONG deadline = GetTickCount64() + 3000;
        do {
            GetDispatchStatus(live_after, calls_after, errors_after);
            if (live_after == 0) break;
            Sleep(1);
        } while (GetTickCount64() < deadline);
        Check(live_after == 0 && calls_after == calls_before + 1 && errors_after == errors_before + 1,
              "delegate exception is contained and recorded exactly once");
    }
    LONG live = 0, calls = 0, errors = 0;
    GetDispatchStatus(live, calls, errors);
    // Destructor signals before the live counter decrement; allow its epilogue.
    for (int i = 0; live && i < 100; ++i) { Sleep(10); GetDispatchStatus(live, calls, errors); }
    Check(live == 0 && calls == 7 && errors == 2, "lifetime and async failure diagnostics");
    std::puts("PASS: asynchronous MTA delivery, persistent work object, reusable callback context, async/sync exception containment, setup errors, bounded queue, cancellation, lifetime, diagnostics");
}
