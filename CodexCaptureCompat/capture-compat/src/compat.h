#pragma once
#include <windows.h>
#include <roapi.h>
#include <windows.graphics.capture.h>
#include <cstddef>

namespace capture_compat {
namespace capture = ABI::Windows::Graphics::Capture;
using FrameHandler = ABI::Windows::Foundation::ITypedEventHandler<capture::Direct3D11CaptureFramePool*, IInspectable*>;

struct Status {
    DWORD size;
    LONG iat_hooks;
    LONG factory_calls;
    LONG pool_hooks;
    LONG session_hooks;
    LONG border_interfaces;
    LONG border_noops;
    LONG hook_errors;
    LONG live_handlers;
    LONG dispatched_calls;
    LONG dispatch_errors;
};

// The DLL and patched system modules must remain loaded until process exit.
bool Initialize(HMODULE executable) noexcept;
bool HookSession(capture::IGraphicsCaptureSession* session) noexcept;
void GetStatus(Status& result) noexcept;

// Internal parser seam used by the native unit tests. This is not exported
// from the proxy DLL and never modifies the supplied image.
bool FindActivationFactoryIatForTest(const void* image, size_t image_size,
                                    size_t& thunk_offset, bool require_mapped_read = false) noexcept;

// Internal subscription lifecycle seams. These are not DLL exports; they let
// native tests exercise the same pending/commit/cancel transitions as AddFrame.
void* BeginSubscriptionForTest(void* pool, FrameHandler* handler) noexcept;
bool CommitSubscriptionForTest(void* subscription, EventRegistrationToken token) noexcept;
void AbortSubscriptionForTest(void* subscription) noexcept;
void CancelSubscriptionsForTest(void* pool) noexcept;
}
