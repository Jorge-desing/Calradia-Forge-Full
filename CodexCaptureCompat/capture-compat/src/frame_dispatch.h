#pragma once
#include "compat.h"

namespace capture_compat {
inline HRESULT ThreadpoolSetupFailure(DWORD error) noexcept {
    return error == ERROR_SUCCESS ? E_FAIL : HRESULT_FROM_WIN32(error);
}
// E_NOINTERFACE means the caller must retain native delegate delivery.
HRESULT MakeDeferredHandler(FrameHandler* original, FrameHandler** result) noexcept;
void CancelDeferredHandler(FrameHandler* handler) noexcept;
HRESULT InvokeHandlerSafely(FrameHandler* handler,
                            capture::IDirect3D11CaptureFramePool* sender,
                            IInspectable* args) noexcept;
void GetDispatchStatus(LONG& live, LONG& calls, LONG& errors) noexcept;
LONG GetWorkItemCreationCount() noexcept;
}
