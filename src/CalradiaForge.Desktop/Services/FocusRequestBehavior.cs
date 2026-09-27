using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

namespace CalradiaForge.Desktop.Services
{
    /// <summary>Small dependency-free focus bridge for keyboard commands that target a WPF control.</summary>
    public static class FocusRequestBehavior
    {
        public static readonly DependencyProperty RequestProperty = DependencyProperty.RegisterAttached(
            "Request", typeof(int), typeof(FocusRequestBehavior), new PropertyMetadata(0, OnRequestChanged));

        public static void SetRequest(DependencyObject element, int value) => element.SetValue(RequestProperty, value);
        public static int GetRequest(DependencyObject element) => (int)element.GetValue(RequestProperty);

        static void OnRequestChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
        {
            if (!(dependencyObject is Control control) || control.IsKeyboardFocusWithin) return;
            // Queue the request at input priority so it runs after the command or mouse
            // event has finished changing focus. A normal-priority callback can run
            // during the same routed interaction and lose to WPF's later focus update.
            control.Dispatcher.BeginInvoke(DispatcherPriority.Input, new Action(() =>
            {
                if (!control.IsKeyboardFocusWithin && control.IsVisible && control.IsEnabled && control.Focusable)
                {
                    var owner = Window.GetWindow(control);
                    if (owner != null && !owner.ShowActivated)
                    {
                        // A deliberately non-activating host (for example, the off-screen
                        // render harness) cannot own native keyboard focus. Preserve the
                        // requested logical focus without activating its HWND.
                        var focusScope = FocusManager.GetFocusScope(control);
                        FocusManager.SetFocusedElement(focusScope, control);
                        return;
                    }

                    control.Focus();
                    Keyboard.Focus(control);
                }
            }));
        }
    }
}
