#include "frame_dispatch.h"
#include <atomic>
#include <new>

namespace capture_compat {
namespace {
std::atomic<LONG> live_handlers{0}, dispatched_calls{0}, dispatch_errors{0};
std::atomic<LONG> work_items_created{0};

class DeferredHandler final : public FrameHandler, public IAgileObject {
    std::atomic<ULONG> refs_{1};
    std::atomic<bool> cancelled_{false}, busy_{false};
    FrameHandler* original_;
    PTP_WORK work_item_ = nullptr;
    struct Work {
        DeferredHandler* owner;
        capture::IDirect3D11CaptureFramePool* sender;
        IInspectable* args;
    };
    // Invoke() permits only one queued or running callback per subscription,
    // so its work item can live with the handler instead of allocating one for
    // every delivered frame notification.
    Work work_{};
    static void Dispose(Work* work) noexcept {
        if (work->args) work->args->Release();
        if (work->sender) work->sender->Release();
        auto owner = work->owner;
        work->args = nullptr;
        work->sender = nullptr;
        work->owner = nullptr;
        owner->busy_ = false;
        // Releasing the queued reference can destroy owner. Do not access work
        // after this point because it is embedded in that same object.
        owner->Release();
    }
    static void CALLBACK Run(PTP_CALLBACK_INSTANCE instance, void* context, PTP_WORK) noexcept {
        CallbackMayRunLong(instance);
        auto owner = static_cast<DeferredHandler*>(context);
        auto work = &owner->work_;
        HRESULT initialized = RoInitialize(RO_INIT_MULTITHREADED);
        if (SUCCEEDED(initialized)) {
            // Suppress queued calls after cancellation. An already running call
            // retains its own references until it finishes.
            if (!work->owner->cancelled_.load()) {
                ++dispatched_calls;
                if (FAILED(InvokeHandlerSafely(work->owner->original_, work->sender, work->args)))
                    ++dispatch_errors;
            }
        } else ++dispatch_errors;
        Dispose(work);
        if (SUCCEEDED(initialized)) RoUninitialize();
    }
public:
    explicit DeferredHandler(FrameHandler* original) noexcept : original_(original) {
        original_->AddRef();
        work_item_ = CreateThreadpoolWork(&Run, this, nullptr);
        if (work_item_) ++work_items_created;
        ++live_handlers;
    }
    bool Ready() const noexcept { return work_item_ != nullptr; }
    ~DeferredHandler() {
        if (work_item_) CloseThreadpoolWork(work_item_);
        original_->Release(); --live_handlers;
    }
    void Cancel() noexcept { cancelled_ = true; }
    HRESULT STDMETHODCALLTYPE QueryInterface(REFIID iid, void** result) override {
        if (!result) return E_POINTER;
        *result = nullptr;
        if (iid == __uuidof(IUnknown) || iid == __uuidof(FrameHandler))
            *result = static_cast<FrameHandler*>(this);
        else if (iid == __uuidof(IAgileObject)) *result = static_cast<IAgileObject*>(this);
        else return E_NOINTERFACE;
        AddRef(); return S_OK;
    }
    ULONG STDMETHODCALLTYPE AddRef() override { return ++refs_; }
    ULONG STDMETHODCALLTYPE Release() override {
        ULONG count = --refs_; if (!count) delete this; return count;
    }
    HRESULT STDMETHODCALLTYPE Invoke(capture::IDirect3D11CaptureFramePool* sender, IInspectable* args) override {
        if (cancelled_.load()) return S_OK;
        // WGC currently supplies null args. Preserve native delivery if a
        // future implementation supplies apartment-bound event arguments.
        if (args) {
            IAgileObject* agile = nullptr;
            HRESULT hr = args->QueryInterface(IID_PPV_ARGS(&agile));
            if (FAILED(hr)) {
                hr = InvokeHandlerSafely(original_, sender, args);
                if (FAILED(hr)) ++dispatch_errors;
                return hr;
            }
            agile->Release();
        }
        bool expected = false;
        // Availability notifications can be coalesced. Bound work to one
        // queued/running callback per event subscription. That single-flight
        // invariant also makes the handler-owned work item safe to reuse.
        if (!busy_.compare_exchange_strong(expected, true)) return S_OK;
        auto work = &work_;
        work->owner = this;
        work->sender = sender;
        work->args = args;
        AddRef();
        if (sender) sender->AddRef();
        if (args) args->AddRef();
        SubmitThreadpoolWork(work_item_);
        return S_OK;
    }
};
}

HRESULT MakeDeferredHandler(FrameHandler* original, FrameHandler** result) noexcept {
    if (!result) return E_POINTER;
    *result = nullptr;
    if (!original) return E_POINTER;
    IAgileObject* agile = nullptr;
    HRESULT hr = original->QueryInterface(IID_PPV_ARGS(&agile));
    if (FAILED(hr)) return hr;
    agile->Release();
    auto handler = new (std::nothrow) DeferredHandler(original);
    if (!handler) return E_OUTOFMEMORY;
    if (!handler->Ready()) {
        HRESULT setup_hr = ThreadpoolSetupFailure(GetLastError());
        handler->Release();
        return setup_hr;
    }
    *result = handler; return S_OK;
}

HRESULT InvokeHandlerSafely(FrameHandler* handler,
                            capture::IDirect3D11CaptureFramePool* sender,
                            IInspectable* args) noexcept {
    try {
        return handler->Invoke(sender, args);
    } catch (...) {
        // Keep exceptions inside this native ABI boundary on both dispatch paths.
        return E_UNEXPECTED;
    }
}

void CancelDeferredHandler(FrameHandler* handler) noexcept {
    static_cast<DeferredHandler*>(handler)->Cancel();
}

void GetDispatchStatus(LONG& live, LONG& calls, LONG& errors) noexcept {
    live = live_handlers.load(); calls = dispatched_calls.load(); errors = dispatch_errors.load();
}

LONG GetWorkItemCreationCount() noexcept { return work_items_created.load(); }
}
