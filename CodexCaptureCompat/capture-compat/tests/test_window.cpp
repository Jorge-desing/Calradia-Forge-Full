#include <windows.h>

namespace {
constexpr int kStatusId = 101;
constexpr int kButtonId = 102;
constexpr int kInputId = 103;
constexpr int kCtrlKCommand = 104;
constexpr int kShiftF6Command = 105;
constexpr int kAltF8Command = 106;

HWND g_status = nullptr;
HWND g_input = nullptr;
HWND g_button = nullptr;

void SetFixtureStatus(const wchar_t* value) {
    if (g_status) SetWindowTextW(g_status, value);
}

LRESULT CALLBACK TestWindowProc(HWND window, UINT message, WPARAM w, LPARAM l) {
    if (message == WM_CREATE) {
        const auto* create = reinterpret_cast<LPCREATESTRUCTW>(l);
        g_status = CreateWindowExW(0, L"STATIC", L"Fixture ready; no project action is connected.",
            WS_CHILD | WS_VISIBLE | SS_LEFT, 24, 32, 560, 26, window,
            reinterpret_cast<HMENU>(static_cast<INT_PTR>(kStatusId)), create->hInstance, nullptr);
        g_input = CreateWindowExW(WS_EX_CLIENTEDGE, L"EDIT", L"",
            WS_CHILD | WS_VISIBLE | WS_TABSTOP | ES_AUTOHSCROLL, 24, 76, 560, 32, window,
            reinterpret_cast<HMENU>(static_cast<INT_PTR>(kInputId)), create->hInstance, nullptr);
        g_button = CreateWindowExW(0, L"BUTTON", L"Safe fixture control",
            WS_CHILD | WS_VISIBLE | WS_TABSTOP | BS_PUSHBUTTON, 24, 124, 200, 36, window,
            reinterpret_cast<HMENU>(static_cast<INT_PTR>(kButtonId)), create->hInstance, nullptr);
        if (!g_status || !g_input || !g_button) return -1;
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
    }
    if (message == WM_DESTROY) { PostQuitMessage(0); return 0; }
    if (message == WM_TIMER) { DestroyWindow(window); return 0; }
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

int WINAPI wWinMain(HINSTANCE instance, HINSTANCE, PWSTR, int) {
    WNDCLASSW klass{};
    klass.lpfnWndProc = TestWindowProc; klass.hInstance = instance;
    klass.lpszClassName = L"CodexCaptureTestWindow";
    if (!RegisterClassW(&klass)) return 1;
    HWND window = CreateWindowExW(0, klass.lpszClassName, L"Codex Computer Use fixture",
        WS_OVERLAPPEDWINDOW, 80, 80, 640, 400, nullptr, nullptr, instance, nullptr);
    if (!window) return 1;
    const ACCEL entries[] = {
        { static_cast<BYTE>(FVIRTKEY | FCONTROL), 'K', kCtrlKCommand },
        { static_cast<BYTE>(FVIRTKEY | FSHIFT), VK_F6, kShiftF6Command },
        { static_cast<BYTE>(FVIRTKEY | FALT), VK_F8, kAltF8Command },
    };
    HACCEL accelerators = CreateAcceleratorTableW(const_cast<LPACCEL>(entries), ARRAYSIZE(entries));
    if (!accelerators) { DestroyWindow(window); return 1; }

    SetTimer(window, 1, 600000, nullptr);
    ShowWindow(window, SW_SHOWNORMAL);
    UpdateWindow(window);
    SetFocus(g_input);
    MSG message{};
    while (GetMessageW(&message, nullptr, 0, 0) > 0) {
        if (!TranslateAcceleratorW(window, accelerators, &message) &&
            !IsDialogMessageW(window, &message)) {
            TranslateMessage(&message);
            DispatchMessageW(&message);
        }
    }
    DestroyAcceleratorTable(accelerators);
    return 0;
}
