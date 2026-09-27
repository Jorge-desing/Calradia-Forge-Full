using System;
using System.IO;
using System.Text.Json;

namespace CalradiaForge.Desktop.Services
{
    internal sealed class DesktopPreferenceState
    {
        public string ThemeId { get; set; } = "war-table";
        public string LanguageCode { get; set; } = "en";
    }

    internal sealed class DesktopPreferenceLoad
    {
        public DesktopPreferenceState State { get; set; } = new();
        public bool UsedFallback { get; set; }
        public string Message { get; set; }
    }

    /// <summary>Persists only Desktop presentation preferences; it never writes game saves or campaign state.</summary>
    internal sealed class DesktopPreferenceService
    {
        readonly string path = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CalradiaForge", "desktop-preferences.json");

        public string Path => path;

        public DesktopPreferenceLoad Load()
        {
            if (!File.Exists(path)) return new();
            try
            {
                var state = JsonSerializer.Deserialize<DesktopPreferenceState>(File.ReadAllText(path));
                if (state == null) throw new InvalidDataException("Preference object was empty.");
                state.ThemeId = string.IsNullOrWhiteSpace(state.ThemeId) ? "war-table" : state.ThemeId;
                state.LanguageCode = string.IsNullOrWhiteSpace(state.LanguageCode) ? "en" : state.LanguageCode;
                return new() { State = state };
            }
            catch (Exception error)
            {
                return new() {
                    UsedFallback = true,
                    Message = "Desktop preferences were invalid and were reset. " + error.Message
                };
            }
        }

        public bool Save(DesktopPreferenceState state, out string error)
        {
            error = null;
            string temporary = null;
            try
            {
                Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path));
                temporary = path + ".tmp-" + Guid.NewGuid().ToString("N");
                File.WriteAllText(temporary, JsonSerializer.Serialize(state, new JsonSerializerOptions { WriteIndented = true }));
                File.Move(temporary, path, true);
                return true;
            }
            catch (Exception exception)
            {
                if (temporary != null) try { File.Delete(temporary); } catch { }
                error = "Desktop preferences could not be saved. " + exception.Message;
                return false;
            }
        }
    }
}
