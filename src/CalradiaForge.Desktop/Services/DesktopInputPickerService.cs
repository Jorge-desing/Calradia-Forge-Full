using System;
using System.Windows;
using Microsoft.Win32;
using CalradiaForge.Desktop.Presentation;

namespace CalradiaForge.Desktop.Services
{
    /// <summary>Opens native, local-only file and folder pickers for workbench inputs.</summary>
    internal sealed class DesktopInputPickerService
    {
        public string Pick(ToolDefinition tool, bool folder)
        {
            if (tool == null) throw new ArgumentNullException(nameof(tool));
            var owner = Application.Current?.MainWindow;
            var titleKey = folder ? "Ui.BrowseFolder" : "Ui.BrowseFile";
            var localizedTitle = Application.Current?.TryFindResource(titleKey)?.ToString();
            if (string.IsNullOrWhiteSpace(localizedTitle)) localizedTitle = tool.Title;
            if (folder)
            {
                var folderDialog = new OpenFolderDialog
                {
                    Title = localizedTitle,
                    Multiselect = false
                };
                return folderDialog.ShowDialog(owner) == true ? folderDialog.FolderName : null;
            }

            var fileDialog = new OpenFileDialog
            {
                Title = localizedTitle,
                Filter = FilterFor(tool),
                CheckFileExists = true,
                CheckPathExists = true,
                Multiselect = false
            };
            return fileDialog.ShowDialog(owner) == true ? fileDialog.FileName : null;
        }

        static string FilterFor(ToolDefinition tool) => tool.Id switch
        {
            "AssemblyInspector" => "Managed assemblies (*.dll;*.exe)|*.dll;*.exe|All files (*.*)|*.*",
            "AssemblyVersionPatchLab" => "Patch request (*.json)|*.json|All files (*.*)|*.*",
            "SpritePackageAuditor" => "Sprite resources (*.png;*.xml;*.tpac)|*.png;*.xml;*.tpac|All files (*.*)|*.*",
            "AudioFmodMixerInspector" or "SoundXmlSynthesizer" => "Audio & Sound XML (*.wav;*.ogg;*.xml)|*.wav;*.ogg;*.xml|All files (*.*)|*.*",
            "TroopTreeVisualizer" or "ItemBalanceAnalyzer" => "Game Data XML (*.xml;*.json)|*.xml;*.json|All files (*.*)|*.*",
            _ => "All files (*.*)|*.*"
        };
    }
}
