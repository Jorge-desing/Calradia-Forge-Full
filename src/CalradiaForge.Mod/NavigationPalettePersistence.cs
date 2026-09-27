using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Newtonsoft.Json;

namespace CalradiaForge.Mod
{
    internal sealed class NavigationPaletteSavedState
    {
        public NavigationPaletteSavedState() { }

        [JsonProperty("version")]
        public int Version { get; set; } = NavigationPalettePersistence.CurrentVersion;

        [JsonProperty("favorites")]
        public List<string> Favorites { get; set; } = new List<string>();

        [JsonProperty("recents")]
        public List<string> Recents { get; set; } = new List<string>();
    }

    internal static class NavigationPalettePersistence
    {
        internal const int CurrentVersion = 1;
        internal const int MaximumFavorites = 32;
        internal const int MaximumRecents = 10;
        private const long MaximumFileBytes = 16 * 1024;
        private static readonly object SaveGate = new object();

        internal static NavigationPaletteSavedState Load(string path)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
                    return EmptyState();

                string json;
                using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    if (stream.Length > MaximumFileBytes)
                        return EmptyState();

                    using (var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true))
                        json = reader.ReadToEnd();
                }

                var loaded = JsonConvert.DeserializeObject<NavigationPaletteSavedState>(json);
                if (loaded == null || loaded.Version != CurrentVersion)
                    return EmptyState();

                loaded.Favorites = Normalize(loaded.Favorites, MaximumFavorites);
                loaded.Recents = Normalize(loaded.Recents, MaximumRecents);
                return loaded;
            }
            catch
            {
                // A damaged or inaccessible preference file must not block the in-game UI.
                return EmptyState();
            }
        }

        internal static void Save(string path, IEnumerable<string> favorites, IEnumerable<string> recents)
        {
            if (string.IsNullOrWhiteSpace(path))
                return;

            var snapshot = new NavigationPaletteSavedState
            {
                Version = CurrentVersion,
                Favorites = Normalize(favorites, MaximumFavorites),
                Recents = Normalize(recents, MaximumRecents)
            };

            try
            {
                lock (SaveGate)
                    WriteAtomically(path, snapshot);
            }
            catch
            {
                // The current session keeps its in-memory preferences if disk persistence fails.
            }
        }

        private static void WriteAtomically(string path, NavigationPaletteSavedState state)
        {
            var directory = Path.GetDirectoryName(path);
            if (string.IsNullOrWhiteSpace(directory))
                return;

            Directory.CreateDirectory(directory);
            var temporaryPath = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
            try
            {
                var json = JsonConvert.SerializeObject(state, Formatting.Indented);
                File.WriteAllText(temporaryPath, json, new UTF8Encoding(false));
                if (File.Exists(path))
                    File.Replace(temporaryPath, path, null);
                else
                    File.Move(temporaryPath, path);
            }
            finally
            {
                try
                {
                    if (File.Exists(temporaryPath))
                        File.Delete(temporaryPath);
                }
                catch
                {
                    // A leftover temporary file is harmless and will not be read as state.
                }
            }
        }

        private static NavigationPaletteSavedState EmptyState()
        {
            return new NavigationPaletteSavedState
            {
                Version = CurrentVersion,
                Favorites = new List<string>(),
                Recents = new List<string>()
            };
        }

        private static List<string> Normalize(IEnumerable<string> values, int maximum)
        {
            var normalized = new List<string>();
            if (values == null)
                return normalized;

            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (var value in values)
            {
                if (string.IsNullOrWhiteSpace(value) || !seen.Add(value))
                    continue;

                normalized.Add(value);
                if (normalized.Count >= maximum)
                    break;
            }

            return normalized;
        }
    }
}
