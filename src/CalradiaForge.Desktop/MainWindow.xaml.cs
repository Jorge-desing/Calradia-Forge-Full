using System.Windows;
using System.Windows.Input;
using CalradiaForge.Desktop.Presentation;
using CalradiaForge.Desktop.Services;

namespace CalradiaForge.Desktop
{
    /// <summary>Window composition and lifecycle only. Commands, navigation, and services live in the workbench shell.</summary>
    public partial class MainWindow : Window
    {
        readonly DesktopShellViewModel shell;

        public MainWindow()
        {
            InitializeComponent();
            var metrics = new DesktopMetricsService();
            var theme = new DesktopThemeService();
            var preferences = new DesktopPreferenceService();
            shell = new DesktopShellViewModel(new ToolCatalog(), new DesktopWorkspaceService(metrics), metrics, new DesktopLocalizationService(), theme, preferences, new DesktopInputPickerService().Pick, clipboardWriter: TryCopyText);
            if (string.Equals(System.Environment.GetEnvironmentVariable("CALRADIA_FORGE_UIA_READ_ONLY"), "1", System.StringComparison.Ordinal))
                shell.IsContextDossierOpen = true;
            DataContext = shell;
        }

        void Window_MinimizeClick(object sender, RoutedEventArgs e) => SystemCommands.MinimizeWindow(this);

        void Window_MaximizeClick(object sender, RoutedEventArgs e) => SystemCommands.MaximizeWindow(this);

        void Window_RestoreClick(object sender, RoutedEventArgs e) => SystemCommands.RestoreWindow(this);

        void Window_CloseClick(object sender, RoutedEventArgs e) => SystemCommands.CloseWindow(this);

        static bool TryCopyText(string text)
        {
            try
            {
                Clipboard.SetText(text ?? string.Empty);
                return true;
            }
            catch
            {
                return false;
            }
        }

        void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.System && e.SystemKey == Key.F4 && (Keyboard.Modifiers & ModifierKeys.Alt) != 0)
            {
                e.Handled = true;
                SystemCommands.CloseWindow(this);
            }
        }

        void Window_Closed(object sender, System.EventArgs e) => shell?.Dispose();
    }
}
