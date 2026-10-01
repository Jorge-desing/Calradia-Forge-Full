#include "compat.h"
#include "frame_dispatch.h"
#include <algorithm>
#include <atomic>
#include <cstring>
#include <cwchar>
#include <limits>
#include <new>
#include <cstdio>

namespace capture_compat {
namespace {
namespace dx = ABI::Windows::Graphics::DirectX;
using Device = dx::Direct3D11::IDirect3DDevice;
using Size = ABI::Windows::Graphics::SizeInt32;
using FactoryFn = HRESULT(WINAPI*)(HSTRING, REFIID, void**);
using CreatePoolFn = HRESULT(STDMETHODCALLTYPE*)(void*, Device*, dx::DirectXPixelFormat,
                                                INT32, Size, capture::IDirect3D11CaptureFramePool**);
using CreateSessionFn = HRESULT(STDMETHODCALLTYPE*)(void*, capture::IGraphicsCaptureItem*,
                                                   capture::IGraphicsCaptureSession**);
using QueryFn = HRESULT(STDMETHODCALLTYPE*)(void*, REFIID, void**);
using AddFrameFn = HRESULT(STDMETHODCALLTYPE*)(void*, FrameHandler*, EventRegistrationToken*);
using InvokeFn = HRESULT(STDMETHODCALLTYPE*)(void*, capture::IDirect3D11CaptureFramePool*, IInspectable*);
using NextFrameFn = HRESULT(STDMETHODCALLTYPE*)(void*, capture::IDirect3D11CaptureFrame**);
using StartFn = HRESULT(STDMETHODCALLTYPE*)(void*);

void Trace(const char* operation, void* object, HRESULT hr = S_OK) noexcept {
#ifdef CAPTURE_COMPAT_TRACE
    wchar_t path[MAX_PATH]{};
    DWORD length = GetTempPathW(_countof(path), path);
    if (!length || length >= _countof(path)) return;
    swprintf_s(path + length, _countof(path) - length, L"codex-capture-compat-%lu.log", GetCurrentProcessId());
    HANDLE file = CreateFileW(path, FILE_APPEND_DATA, FILE_SHARE_READ | FILE_SHARE_WRITE,
                              nullptr, OPEN_ALWAYS, FILE_ATTRIBUTE_NORMAL, nullptr);
    if (file == INVALID_HANDLE_VALUE) return;
    char line[192]{};
    int count = sprintf_s(line, "%llu tid=%lu %s object=%p hr=%08lX\r\n",
        GetTickCount64(), GetCurrentThreadId(), operation, object, static_cast<unsigned long>(hr));
    DWORD written = 0;
    if (count > 0) WriteFile(file, line, static_cast<DWORD>(count), &written, nullptr);
    CloseHandle(file);
#else
    (void)operation; (void)object; (void)hr;
#endif
}

struct SlotRecord { void** slot; void* original; };
SRWLOCK slots_lock = SRWLOCK_INIT;
SlotRecord slots[64]{};
size_t slot_count = 0;
struct OriginalLookupCache { void** slot = nullptr; void* original = nullptr; };
// Patched modules are pinned, so a resolved vtable slot and its original
// function remain valid for the lifetime of this process. Repeated frame
// polling can therefore bypass the shared registry lock after the first hit.
thread_local OriginalLookupCache last_original_lookup;
FactoryFn original_factory = nullptr;
KeyboardSendFn original_send_input = nullptr;
volatile LONG iat_hooks = 0, factory_calls = 0, pool_hooks = 0, session_hooks = 0;
volatile LONG border_interfaces = 0, border_noops = 0, hook_errors = 0;
volatile LONG deferred_capture = 0;

struct Subscription {
    void* pool;
    EventRegistrationToken token;
    FrameHandler* handler;
    Subscription* next;
    bool pending;
    bool cancelled;
};
SRWLOCK subscriptions_lock = SRWLOCK_INIT;
Subscription* subscriptions = nullptr;

Subscription* BeginSubscription(void* pool, FrameHandler* handler) noexcept {
    if (!pool || !handler) return nullptr;
    auto entry = new (std::nothrow) Subscription{pool, {}, handler, nullptr, true, false};
    if (!entry) return nullptr;
    // Publish a pending record before calling the underlying AddFrame. Close
    // can now cancel it even if it races the AddFrame return/commit boundary.
    AcquireSRWLockExclusive(&subscriptions_lock);
    entry->next = subscriptions;
    subscriptions = entry;
    ReleaseSRWLockExclusive(&subscriptions_lock);
    return entry;
}

bool CommitSubscription(Subscription* entry, EventRegistrationToken token) noexcept {
    if (!entry) return false;
    AcquireSRWLockExclusive(&subscriptions_lock);
    entry->token = token;
    entry->pending = false;
    const bool active = !entry->cancelled;
    ReleaseSRWLockExclusive(&subscriptions_lock);
    return active;
}

void AbortSubscription(Subscription* entry) noexcept {
    if (!entry) return;
    AcquireSRWLockExclusive(&subscriptions_lock);
    auto link = &subscriptions;
    while (*link && *link != entry) link = &(*link)->next;
    if (*link == entry) *link = entry->next;
    entry->pending = false;
    entry->cancelled = true;
    ReleaseSRWLockExclusive(&subscriptions_lock);
    entry->handler->Release();
    delete entry;
}

void CancelSubscriptions(void* pool, const EventRegistrationToken* token = nullptr, bool unsubscribe = false) noexcept {
    Subscription* removed = nullptr;
    AcquireSRWLockExclusive(&subscriptions_lock);
    auto link = &subscriptions;
    while (*link) {
        auto item = *link;
        if (item->pool == pool && (!token || (!item->pending && item->token.value == token->value))) {
            *link = item->next;
            item->cancelled = true;
            CancelDeferredHandler(item->handler);
            if (!item->pending) { item->next = removed; removed = item; }
        } else link = &item->next;
    }
    ReleaseSRWLockExclusive(&subscriptions_lock);
    // Release outside the registry lock: COM destructors may reenter.
    while (removed) {
        auto item = removed; removed = item->next;
        if (unsubscribe) static_cast<capture::IDirect3D11CaptureFramePool*>(pool)->remove_FrameArrived(item->token);
        item->handler->Release(); delete item;
    }
}

HRESULT STDMETHODCALLTYPE AddFrame(void*, FrameHandler*, EventRegistrationToken*) noexcept;
HRESULT STDMETHODCALLTYPE RemoveFrame(void*, EventRegistrationToken) noexcept;
HRESULT STDMETHODCALLTYPE ClosePool(void*) noexcept;

void Error() noexcept {
    InterlockedIncrement(&hook_errors);
    OutputDebugStringW(L"[CodexCaptureCompat] Hook installation failed; original operation preserved.\n");
}

void** Slot(void* object, size_t index) noexcept {
    return *reinterpret_cast<void***>(object) + index;
}

void* Original(void* object, size_t index) noexcept {
    void** target = Slot(object, index);
    if (last_original_lookup.slot == target) return last_original_lookup.original;
    void* result = nullptr;
    AcquireSRWLockShared(&slots_lock);
    for (size_t i = 0; i < slot_count; ++i) {
        if (slots[i].slot == target) { result = slots[i].original; break; }
    }
    ReleaseSRWLockShared(&slots_lock);
    if (result) last_original_lookup = {target, result};
    return result;
}

bool PatchSlot(void* object, size_t index, void* replacement) noexcept {
    void** target = Slot(object, index);
    AcquireSRWLockExclusive(&slots_lock);
    for (size_t i = 0; i < slot_count; ++i) {
        if (slots[i].slot == target) {
            bool ok = *target == replacement;
            ReleaseSRWLockExclusive(&slots_lock);
            if (!ok) Error();
            return ok;
        }
    }
    if (slot_count == _countof(slots)) {
        ReleaseSRWLockExclusive(&slots_lock);
        Error();
        return false;
    }
    // A factory can be released while its shared vtable remains patched.
    // Pin both modules, so neither the vtable nor our hook becomes dangling.
    HMODULE ignored = nullptr;
    if (!GetModuleHandleExW(GET_MODULE_HANDLE_EX_FLAG_FROM_ADDRESS | GET_MODULE_HANDLE_EX_FLAG_PIN,
                           reinterpret_cast<LPCWSTR>(*target), &ignored) ||
        !GetModuleHandleExW(GET_MODULE_HANDLE_EX_FLAG_FROM_ADDRESS | GET_MODULE_HANDLE_EX_FLAG_PIN,
                           reinterpret_cast<LPCWSTR>(replacement), &ignored)) {
        ReleaseSRWLockExclusive(&slots_lock);
        Error();
        return false;
    }
    DWORD protection = 0;
    if (!VirtualProtect(target, sizeof(void*), PAGE_READWRITE, &protection)) {
        ReleaseSRWLockExclusive(&slots_lock);
        Error();
        return false;
    }
    slots[slot_count++] = { target, *target };
    InterlockedExchangePointer(target, replacement);
    DWORD ignored_protection = 0;
    bool restored = !!VirtualProtect(target, sizeof(void*), protection, &ignored_protection);
    ReleaseSRWLockExclusive(&slots_lock);
    if (!restored) Error();
    return true;
}

// Only supplied if the actual session returns E_NOINTERFACE for Session3.
// The OS continues showing its default capture border. No borderless access
// request, capture permission, input check, or screenshot state is changed.
class BorderFallback final : public capture::IGraphicsCaptureSession3 {
    std::atomic<ULONG> references_{1};
    capture::IGraphicsCaptureSession* owner_;
public:
    explicit BorderFallback(capture::IGraphicsCaptureSession* owner) noexcept : owner_(owner) {
        owner_->AddRef();
    }
    ~BorderFallback() { owner_->Release(); }
    HRESULT STDMETHODCALLTYPE QueryInterface(REFIID iid, void** result) override {
        if (!result) return E_POINTER;
        *result = nullptr;
        if (iid == __uuidof(capture::IGraphicsCaptureSession3)) {
            *result = static_cast<capture::IGraphicsCaptureSession3*>(this);
            AddRef();
            return S_OK;
        }
        // In particular, IUnknown identity belongs to the real session.
        return owner_->QueryInterface(iid, result);
    }
    ULONG STDMETHODCALLTYPE AddRef() override { return ++references_; }
    ULONG STDMETHODCALLTYPE Release() override {
        ULONG remaining = --references_;
        if (!remaining) delete this;
        return remaining;
    }
    HRESULT STDMETHODCALLTYPE GetIids(ULONG* count, IID** values) override {
        return owner_->GetIids(count, values);
    }
    HRESULT STDMETHODCALLTYPE GetRuntimeClassName(HSTRING* value) override {
        return owner_->GetRuntimeClassName(value);
    }
    HRESULT STDMETHODCALLTYPE GetTrustLevel(TrustLevel* value) override {
        return owner_->GetTrustLevel(value);
    }
    HRESULT STDMETHODCALLTYPE get_IsBorderRequired(boolean* value) override {
        if (!value) return E_POINTER;
        *value = true;
        return S_OK;
    }
    HRESULT STDMETHODCALLTYPE put_IsBorderRequired(boolean) override {
        InterlockedIncrement(&border_noops);
        OutputDebugStringW(L"[CodexCaptureCompat] Optional border setter skipped; OS default border retained.\n");
        return S_OK;
    }
};

HRESULT STDMETHODCALLTYPE SessionQuery(void* self, REFIID iid, void** result) noexcept {
    auto original = reinterpret_cast<QueryFn>(Original(self, 0));
    if (!original) { Error(); return E_UNEXPECTED; }
    HRESULT hr = original(self, iid, result);
    if (hr != E_NOINTERFACE || iid != __uuidof(capture::IGraphicsCaptureSession3) || !result)
        return hr;
    *result = nullptr;
    auto fallback = new (std::nothrow) BorderFallback(static_cast<capture::IGraphicsCaptureSession*>(self));
    if (!fallback) return E_OUTOFMEMORY;
    *result = static_cast<capture::IGraphicsCaptureSession3*>(fallback);
    InterlockedIncrement(&border_interfaces);
    return S_OK;
}

HRESULT STDMETHODCALLTYPE CreateSession(void* self, capture::IGraphicsCaptureItem* item,
                                       capture::IGraphicsCaptureSession** result) noexcept {
    auto original = reinterpret_cast<CreateSessionFn>(Original(self, 10));
    if (!original) { Error(); return E_UNEXPECTED; }
    HRESULT hr = original(self, item, result);
    if (SUCCEEDED(hr) && result && *result) {
        // Always query the native QI, including when its shared vtable was
        // already patched by an earlier capture session.
        auto query = reinterpret_cast<QueryFn>(Original(*result, 0));
        if (!query) query = reinterpret_cast<QueryFn>(*Slot(*result, 0));
        capture::IGraphicsCaptureSession3* native = nullptr;
        HRESULT border_hr = query(*result, __uuidof(capture::IGraphicsCaptureSession3), reinterpret_cast<void**>(&native));
        if (native) native->Release();
        if (border_hr == E_NOINTERFACE) {
            InterlockedExchange(&deferred_capture, 1);
            PatchSlot(self, 8, reinterpret_cast<void*>(&AddFrame));
            PatchSlot(self, 9, reinterpret_cast<void*>(&RemoveFrame));
            ABI::Windows::Foundation::IClosable* closable = nullptr;
            if (SUCCEEDED(static_cast<IUnknown*>(self)->QueryInterface(IID_PPV_ARGS(&closable)))) {
                PatchSlot(closable, 6, reinterpret_cast<void*>(&ClosePool));
                closable->Release();
            }
        }
        HookSession(*result);
    }
    return hr;
}

HRESULT STDMETHODCALLTYPE FrameInvoke(void* self, capture::IDirect3D11CaptureFramePool* sender,
                                      IInspectable* args) noexcept {
    auto original = reinterpret_cast<InvokeFn>(Original(self, 3));
    if (!original) return E_UNEXPECTED;
    Trace("Invoke.enter", self);
    HRESULT hr = original(self, sender, args);
    Trace("Invoke.leave", self, hr);
    return hr;
}

HRESULT STDMETHODCALLTYPE AddFrame(void* self, FrameHandler* handler, EventRegistrationToken* token) noexcept {
    auto original = reinterpret_cast<AddFrameFn>(Original(self, 8));
    if (!original) return E_UNEXPECTED;
    Trace("FrameArrived.add.enter", self);
#ifdef CAPTURE_COMPAT_TRACE
    if (handler) PatchSlot(handler, 3, reinterpret_cast<void*>(&FrameInvoke));
#endif
    ABI::Windows::System::IDispatcherQueue* queue = nullptr;
    auto pool = static_cast<capture::IDirect3D11CaptureFramePool*>(self);
    HRESULT queue_hr = pool->get_DispatcherQueue(&queue);
    bool free_threaded = SUCCEEDED(queue_hr) && !queue;
    if (queue) queue->Release();
    if (handler && token && free_threaded && InterlockedCompareExchange(&deferred_capture, 0, 0)) {
        FrameHandler* deferred = nullptr;
        HRESULT wrap_hr = MakeDeferredHandler(handler, &deferred);
        if (SUCCEEDED(wrap_hr)) {
            auto entry = BeginSubscription(self, deferred);
            if (!entry) { deferred->Release(); return E_OUTOFMEMORY; }
            HRESULT hr = original(self, deferred, token);
            if (FAILED(hr)) { AbortSubscription(entry); return hr; }
            if (!CommitSubscription(entry, *token)) {
                // Close won the race after the native add succeeded. Remove
                // the late registration directly through the original slot;
                // the pending entry remains locally owned until this cleanup.
                using RemoveFn = HRESULT(STDMETHODCALLTYPE*)(void*, EventRegistrationToken);
                auto remove = reinterpret_cast<RemoveFn>(Original(self, 9));
                if (remove) remove(self, *token);
                else Error();
                AbortSubscription(entry);
            }
            Trace("FrameArrived.add.deferred", self, hr);
            return hr;
        }
        Trace("FrameArrived.add.native_fallback", self, wrap_hr);
    }
    HRESULT hr = original(self, handler, token);
    Trace("FrameArrived.add.leave", self, hr);
    return hr;
}

HRESULT STDMETHODCALLTYPE NextFrame(void* self, capture::IDirect3D11CaptureFrame** frame) noexcept {
    auto original = reinterpret_cast<NextFrameFn>(Original(self, 7));
    if (!original) return E_UNEXPECTED;
    Trace("TryGetNextFrame.enter", self);
    HRESULT hr = original(self, frame);
    Trace(SUCCEEDED(hr) && frame && *frame ? "TryGetNextFrame.frame" : "TryGetNextFrame.empty", self, hr);
    return hr;
}

HRESULT STDMETHODCALLTYPE RemoveFrame(void* self, EventRegistrationToken token) noexcept {
    using Fn = HRESULT(STDMETHODCALLTYPE*)(void*, EventRegistrationToken);
    auto original = reinterpret_cast<Fn>(Original(self, 9));
    if (!original) return E_UNEXPECTED;
    HRESULT hr = original(self, token);
    if (SUCCEEDED(hr)) CancelSubscriptions(self, &token);
    return hr;
}

HRESULT STDMETHODCALLTYPE ClosePool(void* self) noexcept {
    auto original = reinterpret_cast<StartFn>(Original(self, 6));
    if (!original) return E_UNEXPECTED;
    capture::IDirect3D11CaptureFramePool* pool = nullptr;
    if (SUCCEEDED(static_cast<IUnknown*>(self)->QueryInterface(IID_PPV_ARGS(&pool)))) {
        CancelSubscriptions(pool, nullptr, true);
        pool->Release();
    }
    return original(self);
}

HRESULT STDMETHODCALLTYPE StartCapture(void* self) noexcept {
    auto original = reinterpret_cast<StartFn>(Original(self, 6));
    if (!original) return E_UNEXPECTED;
    APTTYPE apartment{}; APTTYPEQUALIFIER qualifier{};
    HRESULT apartment_hr = CoGetApartmentType(&apartment, &qualifier);
    Trace(SUCCEEDED(apartment_hr) && apartment == APTTYPE_MTA ? "StartCapture.MTA" : "StartCapture.nonMTA", self, apartment_hr);
    HRESULT hr = original(self);
    Trace("StartCapture.leave", self, hr);
    return hr;
}

HRESULT STDMETHODCALLTYPE CreatePool(void* self, Device* device, dx::DirectXPixelFormat format,
                                    INT32 buffers, Size size,
                                    capture::IDirect3D11CaptureFramePool** result) noexcept {
    auto original = reinterpret_cast<CreatePoolFn>(Original(self, 6));
    if (!original) { Error(); return E_UNEXPECTED; }
    HRESULT hr = original(self, device, format, buffers, size, result);
    if (SUCCEEDED(hr) && result && *result &&
        PatchSlot(*result, 10, reinterpret_cast<void*>(&CreateSession))) {
        InterlockedIncrement(&pool_hooks);
#ifdef CAPTURE_COMPAT_TRACE
        PatchSlot(*result, 8, reinterpret_cast<void*>(&AddFrame));
        PatchSlot(*result, 7, reinterpret_cast<void*>(&NextFrame));
#endif
    }
    return hr;
}

HRESULT WINAPI ActivationFactory(HSTRING class_id, REFIID iid, void** result) noexcept {
    InterlockedIncrement(&factory_calls);
    HRESULT hr = original_factory(class_id, iid, result);
    if (FAILED(hr) || !result || !*result) return hr;
    UINT32 length = 0;
    const wchar_t* name = WindowsGetStringRawBuffer(class_id, &length);
    constexpr wchar_t pool_name[] = L"Windows.Graphics.Capture.Direct3D11CaptureFramePool";
    if (length == _countof(pool_name) - 1 && wmemcmp(name, pool_name, length) == 0) {
        if (iid == __uuidof(capture::IDirect3D11CaptureFramePoolStatics) ||
            iid == __uuidof(capture::IDirect3D11CaptureFramePoolStatics2)) {
            PatchSlot(*result, 6, reinterpret_cast<void*>(&CreatePool));
        } else {
            // Also cover clients requesting an activation factory / IUnknown
            // before QueryInterface-ing to one of the two statics interfaces.
            auto unknown = static_cast<IUnknown*>(*result);
            capture::IDirect3D11CaptureFramePoolStatics* s1 = nullptr;
            capture::IDirect3D11CaptureFramePoolStatics2* s2 = nullptr;
            if (SUCCEEDED(unknown->QueryInterface(IID_PPV_ARGS(&s1)))) {
                PatchSlot(s1, 6, reinterpret_cast<void*>(&CreatePool));
                s1->Release();
            }
            if (SUCCEEDED(unknown->QueryInterface(IID_PPV_ARGS(&s2)))) {
                PatchSlot(s2, 6, reinterpret_cast<void*>(&CreatePool));
                s2->Release();
            }
        }
    }
    return hr;
}

bool IsReadableImageRange(const BYTE* image, const void* address, SIZE_T size) noexcept;

template <typename T>
const T* ReadImageValue(const BYTE* image, SIZE_T image_size, SIZE_T rva,
                        bool require_mapped_read = false) noexcept {
    if (!image || rva > image_size || sizeof(T) > image_size - rva) return nullptr;
    const BYTE* address = image + rva;
    if (require_mapped_read && !IsReadableImageRange(image, address, sizeof(T))) return nullptr;
    return reinterpret_cast<const T*>(address);
}

bool IsNamedImport(const BYTE* image, SIZE_T image_size, SIZE_T name_rva,
                   const char* expected, bool require_mapped_read) noexcept {
    if (!image || !expected || name_rva >= image_size || image_size - name_rva < sizeof(WORD) + 1)
        return false;
    if (require_mapped_read && !IsReadableImageRange(image, image + name_rva, sizeof(WORD))) return false;
    const char* name = reinterpret_cast<const char*>(image + name_rva + sizeof(WORD));
    const SIZE_T available = image_size - name_rva - sizeof(WORD);
    SIZE_T index = 0;
    for (; index < available; ++index) {
        const char value = name[index];
        if (value == '\0') return expected[index] == '\0';
        if (expected[index] == '\0' || value != expected[index]) return false;
    }
    return false;
}

bool HasTerminatedString(const BYTE* image, SIZE_T image_size, SIZE_T rva,
                         bool require_mapped_read) noexcept {
    if (!image || rva >= image_size) return false;
    SIZE_T cursor = rva;
    while (cursor < image_size) {
        SIZE_T readable_size = image_size - cursor;
        const BYTE* address = image + cursor;
        if (require_mapped_read) {
            MEMORY_BASIC_INFORMATION info{};
            if (VirtualQuery(address, &info, sizeof(info)) != sizeof(info) ||
                info.AllocationBase != image || info.State != MEM_COMMIT ||
                (info.Protect & (PAGE_GUARD | PAGE_NOACCESS)) != 0 ||
                (info.Protect & 0xff) == PAGE_EXECUTE)
                return false;
            const auto region_start = reinterpret_cast<ULONG_PTR>(info.BaseAddress);
            if (info.RegionSize > std::numeric_limits<ULONG_PTR>::max() - region_start) return false;
            const auto region_end = region_start + info.RegionSize;
            const auto current = reinterpret_cast<ULONG_PTR>(address);
            if (current < region_start || current >= region_end) return false;
            const SIZE_T region_remaining = static_cast<SIZE_T>(region_end - current);
            if (readable_size > region_remaining) readable_size = region_remaining;
            if (!IsReadableImageRange(image, address, readable_size)) return false;
        }
        if (readable_size == 0) return false;
        if (std::memchr(address, '\0', readable_size)) return true;
        cursor += readable_size;
    }
    return false;
}

bool IsReadableImageRange(const BYTE* image, const void* address, SIZE_T size) noexcept {
    if (!image || !address || size == 0) return false;
    const auto allocation_base = reinterpret_cast<const void*>(image);
    auto cursor = reinterpret_cast<ULONG_PTR>(address);
    if (size > std::numeric_limits<ULONG_PTR>::max() - cursor) return false;
    SIZE_T remaining = size;
    while (remaining) {
        MEMORY_BASIC_INFORMATION info{};
        if (VirtualQuery(reinterpret_cast<const void*>(cursor), &info, sizeof(info)) != sizeof(info) ||
            info.AllocationBase != allocation_base || info.State != MEM_COMMIT ||
            (info.Protect & (PAGE_GUARD | PAGE_NOACCESS)) != 0 ||
            (info.Protect & 0xff) == PAGE_EXECUTE)
            return false;
        const auto region_start = reinterpret_cast<ULONG_PTR>(info.BaseAddress);
        if (info.RegionSize > std::numeric_limits<ULONG_PTR>::max() - region_start) return false;
        const auto region_end = region_start + info.RegionSize;
        if (cursor < region_start || cursor >= region_end) return false;
        const SIZE_T chunk = static_cast<SIZE_T>(std::min<ULONG_PTR>(remaining, region_end - cursor));
        if (chunk == 0) return false;
        cursor += chunk;
        remaining -= chunk;
    }
    return true;
}

bool GetLoadedImageSize(HMODULE module, SIZE_T& image_size) noexcept {
    image_size = 0;
    if (!module) return false;
    auto base = reinterpret_cast<const BYTE*>(module);
    if (!IsReadableImageRange(base, base, sizeof(IMAGE_DOS_HEADER))) return false;
    const auto dos = reinterpret_cast<const IMAGE_DOS_HEADER*>(base);
    if (dos->e_magic != IMAGE_DOS_SIGNATURE || dos->e_lfanew < static_cast<LONG>(sizeof(IMAGE_DOS_HEADER)))
        return false;
    const SIZE_T nt_offset = static_cast<SIZE_T>(dos->e_lfanew);
    if (nt_offset > std::numeric_limits<SIZE_T>::max() - sizeof(IMAGE_NT_HEADERS64) ||
        !IsReadableImageRange(base, base + nt_offset, sizeof(IMAGE_NT_HEADERS64)))
        return false;
    const auto nt = reinterpret_cast<const IMAGE_NT_HEADERS64*>(base + nt_offset);
    if (nt->Signature != IMAGE_NT_SIGNATURE || nt->FileHeader.Machine != IMAGE_FILE_MACHINE_AMD64 ||
        nt->FileHeader.SizeOfOptionalHeader < sizeof(IMAGE_OPTIONAL_HEADER64) ||
        nt->OptionalHeader.Magic != IMAGE_NT_OPTIONAL_HDR64_MAGIC ||
        nt->OptionalHeader.SizeOfImage < nt_offset + sizeof(IMAGE_NT_HEADERS64))
        return false;
    const SIZE_T declared_size = nt->OptionalHeader.SizeOfImage;
    if (nt_offset > declared_size || sizeof(IMAGE_NT_HEADERS64) > declared_size - nt_offset)
        return false;
    image_size = declared_size;
    return true;
}

bool FindNamedIat(const BYTE* image, SIZE_T buffer_size, SIZE_T& thunk_offset,
                  const char* expected_import, const char* expected_module,
                  bool require_mapped_read = false) noexcept {
    if (!image || buffer_size < sizeof(IMAGE_DOS_HEADER)) return false;
    const auto dos = ReadImageValue<IMAGE_DOS_HEADER>(image, buffer_size, 0, require_mapped_read);
    if (!dos || dos->e_magic != IMAGE_DOS_SIGNATURE ||
        dos->e_lfanew < static_cast<LONG>(sizeof(IMAGE_DOS_HEADER))) return false;
    const SIZE_T nt_offset = static_cast<SIZE_T>(dos->e_lfanew);
    const auto nt = ReadImageValue<IMAGE_NT_HEADERS64>(image, buffer_size, nt_offset, require_mapped_read);
    if (!nt || nt->Signature != IMAGE_NT_SIGNATURE ||
        nt->FileHeader.Machine != IMAGE_FILE_MACHINE_AMD64 ||
        nt->FileHeader.SizeOfOptionalHeader < sizeof(IMAGE_OPTIONAL_HEADER64) ||
        nt->OptionalHeader.Magic != IMAGE_NT_OPTIONAL_HDR64_MAGIC) return false;

    const SIZE_T image_size = nt->OptionalHeader.SizeOfImage;
    if (image_size < nt_offset + sizeof(IMAGE_NT_HEADERS64) || image_size > buffer_size ||
        nt->OptionalHeader.NumberOfRvaAndSizes <= IMAGE_DIRECTORY_ENTRY_IMPORT) return false;
    const auto& import_directory = nt->OptionalHeader.DataDirectory[IMAGE_DIRECTORY_ENTRY_IMPORT];
    if (!import_directory.VirtualAddress || import_directory.Size < sizeof(IMAGE_IMPORT_DESCRIPTOR) ||
        import_directory.VirtualAddress > image_size ||
        import_directory.Size > image_size - import_directory.VirtualAddress) return false;

    const SIZE_T descriptor_count = import_directory.Size / sizeof(IMAGE_IMPORT_DESCRIPTOR);
    const SIZE_T descriptor_rva = import_directory.VirtualAddress;
    bool terminated = false;
    bool found = false;
    SIZE_T resolved_offset = 0;
    for (SIZE_T descriptor_index = 0; descriptor_index < descriptor_count; ++descriptor_index) {
        const SIZE_T current_rva = descriptor_rva + descriptor_index * sizeof(IMAGE_IMPORT_DESCRIPTOR);
        const auto descriptor = ReadImageValue<IMAGE_IMPORT_DESCRIPTOR>(image, image_size, current_rva, require_mapped_read);
        if (!descriptor) return false;
        if (descriptor->Name == 0) { terminated = true; break; }
        if (descriptor->Name >= image_size ||
            !HasTerminatedString(image, image_size, descriptor->Name, require_mapped_read) ||
            !descriptor->FirstThunk || descriptor->FirstThunk >= image_size) return false;
        const bool matching_module = !expected_module ||
            _stricmp(reinterpret_cast<const char*>(image + descriptor->Name), expected_module) == 0;
        // Without OriginalFirstThunk, FirstThunk contains resolved addresses
        // in a loaded image and cannot be used to recover import names.
        if (!descriptor->OriginalFirstThunk) continue;
        if (descriptor->OriginalFirstThunk >= image_size) return false;

        const SIZE_T max_names = (image_size - descriptor->OriginalFirstThunk) / sizeof(IMAGE_THUNK_DATA64);
        const SIZE_T max_addresses = (image_size - descriptor->FirstThunk) / sizeof(IMAGE_THUNK_DATA64);
        const SIZE_T max_thunks = (max_names < max_addresses) ? max_names : max_addresses;
        bool thunk_terminated = false;
        for (SIZE_T index = 0; index < max_thunks; ++index) {
            const SIZE_T name_rva = descriptor->OriginalFirstThunk + index * sizeof(IMAGE_THUNK_DATA64);
            const SIZE_T address_rva = descriptor->FirstThunk + index * sizeof(IMAGE_THUNK_DATA64);
            const auto names = ReadImageValue<IMAGE_THUNK_DATA64>(image, image_size, name_rva, require_mapped_read);
            const auto addresses = ReadImageValue<IMAGE_THUNK_DATA64>(image, image_size, address_rva, require_mapped_read);
            if (!names || !addresses) return false;
            if (names->u1.AddressOfData == 0) { thunk_terminated = true; break; }
            if (IMAGE_SNAP_BY_ORDINAL64(names->u1.Ordinal)) continue;
            if (names->u1.AddressOfData > std::numeric_limits<SIZE_T>::max()) return false;
            const SIZE_T import_name_rva = static_cast<SIZE_T>(names->u1.AddressOfData);
            if (import_name_rva > image_size || image_size - import_name_rva < sizeof(WORD) + 1 ||
                !HasTerminatedString(image, image_size, import_name_rva + sizeof(WORD), require_mapped_read)) return false;
            if (matching_module && IsNamedImport(image, image_size, import_name_rva, expected_import, require_mapped_read)) {
                if (addresses->u1.Function == 0) return false;
                if (found && resolved_offset != address_rva) return false;
                resolved_offset = address_rva;
                found = true;
            }
        }
        if (!thunk_terminated) return false;
    }
    if (!terminated || !found) return false;
    thunk_offset = resolved_offset;
    return true;
}

bool FindActivationFactoryIat(const BYTE* image, SIZE_T size, SIZE_T& offset,
                              bool require_mapped_read = false) noexcept {
    return FindNamedIat(image, size, offset, "RoGetActivationFactory", nullptr, require_mapped_read);
}

bool KeyboardHelperProfile(const BYTE* image, SIZE_T size, SIZE_T offset,
                           bool mapped = false) noexcept {
    // Mapped-image anchors were audited against the @oai/sky 0.7.5 helper.
    // Installation ownership also verifies the recorded helper/DLL hashes.
    // These mapped-image anchors enable only that audited keyboard path; they
    // never change existing capture support for unknown helper versions.
    constexpr BYTE constructor[] = {
        0x48,0x8d,0x44,0x24,0x20, 0xc7,0x00,0x01,0x00,0x00,0x00,
        0x66,0x89,0x48,0x08, 0x66,0x83,0x60,0x0a,0x00,
        0x44,0x89,0x40,0x0c, 0x83,0x60,0x10,0x00,
        0x48,0x83,0x60,0x18,0x00, 0xba,0x01,0x00,0x00,0x00,
        0x48,0x89,0xc1, 0xe8,0xf2,0xfa,0xff,0xff };
    if (size != 0x17e000 || offset != 0x1774d0) return false;
    const auto dos = ReadImageValue<IMAGE_DOS_HEADER>(image, size, 0, mapped);
    if (!dos || dos->e_magic != IMAGE_DOS_SIGNATURE || dos->e_lfanew < 0) return false;
    const auto nt = ReadImageValue<IMAGE_NT_HEADERS64>(image, size, static_cast<SIZE_T>(dos->e_lfanew), mapped);
    if (!nt || nt->Signature != IMAGE_NT_SIGNATURE || nt->FileHeader.Machine != IMAGE_FILE_MACHINE_AMD64 ||
        nt->FileHeader.TimeDateStamp != 0x6ab98f85 || nt->OptionalHeader.Magic != IMAGE_NT_OPTIONAL_HDR64_MAGIC ||
        nt->OptionalHeader.SizeOfImage != size || nt->OptionalHeader.AddressOfEntryPoint != 0x1410) return false;
    constexpr SIZE_T anchor = 0xafa57;
    if (anchor > size || sizeof(constructor) > size - anchor ||
        (mapped && !IsReadableImageRange(image, image + anchor, sizeof(constructor)))) return false;
    return std::memcmp(image + anchor, constructor, sizeof(constructor)) == 0;
}

UINT SendInputWithScans(UINT count, LPINPUT inputs, int input_size, HKL layout,
                       KeyboardMapFn mapper, KeyboardSendFn sender) noexcept {
    const DWORD incoming_error = GetLastError();
    if (!sender) return 0;
    if (!mapper || !inputs || input_size != sizeof(INPUT) || count == 0 || count > 4096)
        return sender(count, inputs, input_size);
    auto copy = new (std::nothrow) INPUT[count];
    if (!copy) {
        SetLastError(incoming_error);
        return sender(count, inputs, input_size);
    }
    std::memcpy(copy, inputs, sizeof(INPUT) * count);
    bool changed = false;
    for (UINT index = 0; index < count; ++index) {
        auto& item = copy[index];
        if (item.type != INPUT_KEYBOARD || item.ki.wVk == 0 || item.ki.wScan != 0 ||
            (item.ki.dwFlags & (KEYEVENTF_UNICODE | KEYEVENTF_SCANCODE)) != 0) continue;
        const UINT scan = mapper(item.ki.wVk, MAPVK_VK_TO_VSC_EX, layout);
        if ((scan & 0xff) == 0 || (scan & 0xffff0000) != 0 ||
            ((scan & 0xff00) != 0 && (scan & 0xff00) != 0xe000)) continue;
        item.ki.wScan = static_cast<WORD>(scan & 0xff);
        if ((scan & 0xff00) == 0xe000) item.ki.dwFlags |= KEYEVENTF_EXTENDEDKEY;
        changed = true;
    }
    SetLastError(incoming_error);
    const UINT sent = sender(count, changed ? copy : inputs, input_size);
    const DWORD error = GetLastError();
    delete[] copy;
    SetLastError(error);
    return sent;
}

HKL WINAPI ForegroundKeyboardLayout() noexcept {
    HWND foreground = GetForegroundWindow();
    DWORD thread = foreground ? GetWindowThreadProcessId(foreground, nullptr) : 0;
    return GetKeyboardLayout(thread);
}

UINT SendInputWithLookup(UINT count, LPINPUT inputs, int size, HKL(WINAPI* lookup)(),
                         KeyboardMapFn mapper, KeyboardSendFn sender) noexcept {
    const DWORD incoming_error = GetLastError();
    const HKL layout = lookup ? lookup() : nullptr;
    SetLastError(incoming_error);
    return SendInputWithScans(count, inputs, size, layout, mapper, sender);
}

UINT WINAPI SendInputWithKeyboardScans(UINT count, LPINPUT inputs, int size) noexcept {
    return SendInputWithLookup(count, inputs, size, &ForegroundKeyboardLayout, &MapVirtualKeyExW, original_send_input);
}

void HookKeyboardInput(BYTE* image, SIZE_T size) noexcept {
    SIZE_T offset = 0;
    if (!FindNamedIat(image, size, offset, "SendInput", "user32.dll", true) ||
        !KeyboardHelperProfile(image, size, offset, true)) return;
    auto slot = reinterpret_cast<void**>(image + offset);
    void* original = *slot;
    if (!original) return;
    DWORD protection = 0;
    if (!VirtualProtect(slot, sizeof(void*), PAGE_READWRITE, &protection)) return;
    original_send_input = reinterpret_cast<KeyboardSendFn>(original);
    InterlockedExchangePointer(slot, reinterpret_cast<void*>(&SendInputWithKeyboardScans));
    DWORD ignored = 0;
    if (!VirtualProtect(slot, sizeof(void*), protection, &ignored)) {
        InterlockedExchangePointer(slot, original);
        VirtualProtect(slot, sizeof(void*), protection, &ignored);
        original_send_input = nullptr;
        InterlockedIncrement(&hook_errors);
    }
}
}

void* BeginSubscriptionForTest(void* pool, FrameHandler* handler) noexcept {
    return BeginSubscription(pool, handler);
}

bool CommitSubscriptionForTest(void* subscription, EventRegistrationToken token) noexcept {
    return CommitSubscription(static_cast<Subscription*>(subscription), token);
}

void AbortSubscriptionForTest(void* subscription) noexcept {
    AbortSubscription(static_cast<Subscription*>(subscription));
}

void CancelSubscriptionsForTest(void* pool) noexcept {
    CancelSubscriptions(pool);
}

bool HookSession(capture::IGraphicsCaptureSession* session) noexcept {
    if (!session) return false;
    if (!PatchSlot(session, 0, reinterpret_cast<void*>(&SessionQuery))) return false;
#ifdef CAPTURE_COMPAT_TRACE
    PatchSlot(session, 6, reinterpret_cast<void*>(&StartCapture));
#endif
    InterlockedIncrement(&session_hooks);
    return true;
}

bool Initialize(HMODULE executable) noexcept {
    // Runs under loader lock: no COM, LoadLibrary, workers, heap allocations,
    // or file logging here. Only the target EXE's already-resolved IAT changes.
    auto base = reinterpret_cast<BYTE*>(executable);
    SIZE_T image_size = 0;
    SIZE_T thunk_offset = 0;
    if (!GetLoadedImageSize(executable, image_size) ||
        !FindActivationFactoryIat(base, image_size, thunk_offset, true)) return false;

    auto target = reinterpret_cast<void**>(base + thunk_offset);
    if (!IsReadableImageRange(base, target, sizeof(*target))) return false;
    void* resolved_factory = *target;
    if (!resolved_factory) return false;
    DWORD protection = 0;
    if (!VirtualProtect(target, sizeof(void*), PAGE_READWRITE, &protection)) return false;
    original_factory = reinterpret_cast<FactoryFn>(resolved_factory);
    InterlockedExchangePointer(target, reinterpret_cast<void*>(&ActivationFactory));
    DWORD ignored = 0;
    if (!VirtualProtect(target, sizeof(void*), protection, &ignored)) {
        InterlockedExchangePointer(target, resolved_factory);
        DWORD rollback_protection = 0;
        VirtualProtect(target, sizeof(void*), protection, &rollback_protection);
        original_factory = nullptr;
        InterlockedIncrement(&hook_errors);
        return false;
    }
    InterlockedIncrement(&iat_hooks);
    HookKeyboardInput(base, image_size);
    return true;
}

bool FindSendInputIatForTest(const void* image, size_t size, size_t& offset) noexcept {
    return FindNamedIat(static_cast<const BYTE*>(image), size, offset, "SendInput", "user32.dll");
}

bool IsKeyboardHelperProfileForTest(const void* image, size_t size, size_t offset) noexcept {
    return KeyboardHelperProfile(static_cast<const BYTE*>(image), size, offset);
}

UINT SendInputWithScansForTest(UINT count, LPINPUT inputs, int size, HKL layout,
                              KeyboardMapFn mapper, KeyboardSendFn sender) noexcept {
    return SendInputWithScans(count, inputs, size, layout, mapper, sender);
}

// Internal fake-lookup seam, not exported from the proxy DLL.
UINT SendInputWithLookupForTest(UINT count, LPINPUT inputs, int size, HKL(WINAPI* lookup)(),
                               KeyboardMapFn mapper, KeyboardSendFn sender) noexcept {
    return SendInputWithLookup(count, inputs, size, lookup, mapper, sender);
}

bool FindActivationFactoryIatForTest(const void* image, size_t image_size,
                                    size_t& thunk_offset, bool require_mapped_read) noexcept {
    return FindActivationFactoryIat(reinterpret_cast<const BYTE*>(image), image_size, thunk_offset,
                                    require_mapped_read);
}

void GetStatus(Status& result) noexcept {
    result = {sizeof(Status), InterlockedCompareExchange(&iat_hooks, 0, 0),
        InterlockedCompareExchange(&factory_calls, 0, 0), InterlockedCompareExchange(&pool_hooks, 0, 0),
        InterlockedCompareExchange(&session_hooks, 0, 0), InterlockedCompareExchange(&border_interfaces, 0, 0),
        InterlockedCompareExchange(&border_noops, 0, 0), InterlockedCompareExchange(&hook_errors, 0, 0)};
    GetDispatchStatus(result.live_handlers, result.dispatched_calls, result.dispatch_errors);
}
}
