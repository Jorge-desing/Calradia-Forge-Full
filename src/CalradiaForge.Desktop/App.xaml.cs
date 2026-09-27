using System;
using System.IO;
using System.Windows;
using CalradiaForge.Desktop.Services;

namespace CalradiaForge.Desktop
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            var readOnlyUia = string.Equals(Environment.GetEnvironmentVariable("CALRADIA_FORGE_UIA_READ_ONLY"), "1", StringComparison.Ordinal);
            try
            {
                var w = new MainWindow();
                // The opt-in UI Automation runner must never present a test window
                // on the user's desktop. Normal launches keep their regular behavior.
                if (readOnlyUia) NonActivatingWindowBehavior.ApplyOffscreenInspection(w);
                w.Show();
            }
            catch (Exception error)
            {
                var logPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CalradiaForge", "logs", "desktop-startup.log");
                try
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(logPath));
                    File.WriteAllText(logPath, DateTimeOffset.Now.ToString("O") + Environment.NewLine + error + Environment.NewLine);
                }
                catch { }
                // A modal error window would take foreground away from the user
                // during unattended UIA inspection. The failure remains in the log.
                if (!readOnlyUia)
                    MessageBox.Show("Calradia Forge Desktop could not start. Details were saved to:\n" + logPath + "\n\n" + error.Message,
                        "Calradia Forge", MessageBoxButton.OK, MessageBoxImage.Error);
                Shutdown(1);
            }
        }
    }
}
