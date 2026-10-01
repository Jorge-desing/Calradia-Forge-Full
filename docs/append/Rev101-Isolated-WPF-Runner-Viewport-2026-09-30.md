# Rev101 — Isolated WPF runner viewport

**Date:** 30 September 2026
**Version:** Calradia Forge 25.2.0; unchanged
**Scope:** Desktop render fixture and hosted CI recovery.

## Observed problem

GitHub CI on commit 118fd2c compiled successfully and passed Desktop 65/65, but failed the fixed 1360×820 DIP viewport assertion. The documentation and ledger workflow passed. WPF caches native window tracking limits; a runner display smaller than the fixture viewport can constrain the test window.

## Correction

The isolated render fixture expands only its own HWND maximum tracking bounds through WM_GETMINMAXINFO before WPF processes that message. Application window code, minimum dimensions, native display settings and interactive windows remain unchanged. A regression sends smaller 1024×768 bounds to that exact fixture HWND and checks the resulting bounds before exercising the fixed logical viewport. Diagnostics now report the actual dimensions on failure.

Reference: [WPF Window implementation](https://github.com/dotnet/wpf/blob/main/src/Microsoft.DotNet.Wpf/src/PresentationFramework/System/Windows/Window.cs) and [Microsoft WM_GETMINMAXINFO](https://learn.microsoft.com/en-us/windows/win32/winmsg/wm-getminmaxinfo).

## Validation and limits

The Desktop BAT rebuilt successfully with zero warnings and errors, passed Desktop 65/65 and WPF render 293/293, retaining 182 render/layout passes. The additional case verifies small native tracking bounds without changing the display. Remote CI on this correction is pending until push. Hosted render measurements are fixture timings, not application latency. No game, campaign, battle, distribution package or product-version change occurred. Previous ledger revisions remain unchanged.
