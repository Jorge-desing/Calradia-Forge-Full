using System.Windows.Controls;

namespace CalradiaForge.Desktop.Presentation
{
    public partial class ToolDossierControl : UserControl
    {
        public ToolDossierControl() => InitializeComponent();

        void CopyCommandClick(object sender, System.Windows.RoutedEventArgs e)
        {
            if (sender is System.Windows.FrameworkElement element && element.Tag is string text && !string.IsNullOrEmpty(text))
            {
                try { System.Windows.Clipboard.SetText(text); } catch { }
            }
        }
    }
}
