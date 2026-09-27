using System;
using System.Collections.Concurrent;
using System.IO;

namespace CalradiaForge.Sdk
{
    /// <summary>Describes the outcome of a safe settings save.</summary>
    public enum ModSettingsSaveState
    {
        Saved,
        SerializerUnavailable,
        SerializationFailed,
        StorageFailed
    }

    /// <summary>Immutable, non-sensitive outcome of a settings save attempt.</summary>
    public sealed class ModSettingsSaveResult
    {
        internal ModSettingsSaveResult(ModSettingsSaveState state, string failureReason, Exception error)
        {
            State = state;
            FailureReason = failureReason;
            Error = error;
        }

        /// <summary>Gets the final save state.</summary>
        public ModSettingsSaveState State { get; }

        /// <summary>Gets whether the file and cache were committed successfully.</summary>
        public bool Succeeded => State == ModSettingsSaveState.Saved;

        /// <summary>Gets a bounded diagnostic without settings content or filesystem paths.</summary>
        public string FailureReason { get; }

        internal Exception Error { get; }
    }

    /// <summary>
    /// Provides dynamic configuration management without relying on MCM.
    /// </summary>
    public static class ModSettings
    {
        // Windows treats settings file names case-insensitively. Cache and operation identity
        // use the same rule so IDs such as "MyMod" and "mymod" cannot race over one file.
        private static readonly ConcurrentDictionary<string, object> SettingsCache =
            new ConcurrentDictionary<string, object>(StringComparer.OrdinalIgnoreCase);
        private static readonly ConcurrentDictionary<string, object> FileLocks =
            new ConcurrentDictionary<string, object>(StringComparer.OrdinalIgnoreCase);
        private static readonly ConcurrentDictionary<string, long> SettingsVersions =
            new ConcurrentDictionary<string, long>(StringComparer.OrdinalIgnoreCase);

        public static Func<object, string> DefaultSerializer { get; set; }
        public static Func<string, Type, object> DefaultDeserializer { get; set; }

        public static T Register<T>(string modId, T defaultSettings) where T : class
        {
            if (string.IsNullOrWhiteSpace(modId)) throw new ArgumentException("Mod ID cannot be null or empty.", nameof(modId));
            if (defaultSettings == null) throw new ArgumentNullException(nameof(defaultSettings));

            var path = GetSettingsPath(modId);
            var fileLock = FileLocks.GetOrAdd(modId, _ => new object());
            long startingVersion;
            lock (fileLock)
            {
                startingVersion = GetVersionLocked(modId);
            }

            var settings = defaultSettings;
            string temporaryPath = null;
            var fileExists = File.Exists(path);
            if (fileExists)
            {
                var deserializer = DefaultDeserializer;
                if (deserializer != null)
                {
                    try
                    {
                        var contents = File.ReadAllText(path);
                        settings = (deserializer(contents, typeof(T)) as T) ?? defaultSettings;
                    }
                    catch (Exception ex)
                    {
                        ForgeLogger.LogError($"Failed to load settings for {modId}. Using defaults.", ex);
                    }
                }
            }
            else
            {
                var preparationFailure = PrepareSave(modId, settings, path, out temporaryPath);
                if (preparationFailure != null && preparationFailure.State != ModSettingsSaveState.SerializerUnavailable)
                {
                    LogSaveFailure(modId, preparationFailure);
                }
            }

            try
            {
                lock (fileLock)
                {
                    // A serializer/deserializer may reenter ModSettings. If a nested operation
                    // committed while this registration was loading, never overwrite its cache.
                    if (GetVersionLocked(modId) != startingVersion)
                    {
                        object currentSettings;
                        return SettingsCache.TryGetValue(modId, out currentSettings)
                            ? currentSettings as T ?? defaultSettings
                            : defaultSettings;
                    }

                    if (!fileExists && temporaryPath != null)
                    {
                        try
                        {
                            // Do not overwrite a file created by another process after the
                            // initial existence check. File.Move fails safely on a collision.
                            File.Move(temporaryPath, path);
                            temporaryPath = null;
                        }
                        catch (Exception ex)
                        {
                            LogSaveFailure(modId, new ModSettingsSaveResult(
                                ModSettingsSaveState.StorageFailed,
                                "Settings storage failed (" + ex.GetType().Name + ").",
                                ex));
                        }
                    }

                    SettingsCache[modId] = settings;
                    IncrementVersionLocked(modId);
                    return settings;
                }
            }
            finally
            {
                DeleteTemporaryFile(temporaryPath);
            }
        }

        public static T Get<T>(string modId) where T : class
        {
            object value;
            return SettingsCache.TryGetValue(modId, out value) ? value as T : null;
        }

        public static void Save<T>(string modId, T settings) where T : class
        {
            var result = TrySave(modId, settings);
            if (!result.Succeeded) LogSaveFailure(modId, result);
        }

        /// <summary>
        /// Saves settings through a same-directory temporary file and commits the file before
        /// updating the in-memory cache. Existing files remain intact when serialization or
        /// storage fails. User-provided serializers run outside the per-mod commit lock.
        /// </summary>
        /// <param name="modId">A single file-name component identifying the owning mod.</param>
        /// <param name="settings">The settings object to serialize.</param>
        /// <returns>A typed outcome that does not expose serialized settings or filesystem paths.</returns>
        public static ModSettingsSaveResult TrySave<T>(string modId, T settings) where T : class
        {
            var path = GetSettingsPath(modId);
            if (settings == null) throw new ArgumentNullException(nameof(settings));
            string temporaryPath;
            var preparationFailure = PrepareSave(modId, settings, path, out temporaryPath);
            if (preparationFailure != null) return preparationFailure;

            try
            {
                var fileLock = FileLocks.GetOrAdd(modId, _ => new object());
                lock (fileLock)
                {
                    try
                    {
                        if (File.Exists(path))
                        {
                            File.Replace(temporaryPath, path, null);
                        }
                        else
                        {
                            File.Move(temporaryPath, path);
                        }

                        temporaryPath = null;
                        SettingsCache[modId] = settings;
                        IncrementVersionLocked(modId);
                        return new ModSettingsSaveResult(ModSettingsSaveState.Saved, null, null);
                    }
                    catch (Exception ex)
                    {
                        return new ModSettingsSaveResult(
                            ModSettingsSaveState.StorageFailed,
                            "Settings storage failed (" + ex.GetType().Name + ").",
                            ex);
                    }
                }
            }
            finally
            {
                DeleteTemporaryFile(temporaryPath);
            }
        }

        // Returns null when preparation succeeded. The destination is not touched here, and
        // serialization executes outside the lock so consumers cannot deadlock other accesses.
        private static ModSettingsSaveResult PrepareSave<T>(string modId, T settings, string path, out string temporaryPath)
            where T : class
        {
            temporaryPath = null;
            var serializer = DefaultSerializer;
            if (serializer == null)
            {
                return new ModSettingsSaveResult(
                    ModSettingsSaveState.SerializerUnavailable,
                    "No settings serializer is configured.",
                    null);
            }

            string content;
            try
            {
                content = serializer(settings);
                if (content == null)
                {
                    throw new InvalidOperationException("The settings serializer returned null.");
                }
            }
            catch (Exception ex)
            {
                return new ModSettingsSaveResult(
                    ModSettingsSaveState.SerializationFailed,
                    "Settings serialization failed (" + ex.GetType().Name + ").",
                    ex);
            }

            try
            {
                var directory = Path.GetDirectoryName(path);
                Directory.CreateDirectory(directory);
                temporaryPath = Path.Combine(
                    directory,
                    Path.GetFileName(path) + "." + Guid.NewGuid().ToString("N") + ".tmp");
                File.WriteAllText(temporaryPath, content);
                return null;
            }
            catch (Exception ex)
            {
                DeleteTemporaryFile(temporaryPath);
                temporaryPath = null;
                return new ModSettingsSaveResult(
                    ModSettingsSaveState.StorageFailed,
                    "Settings storage failed (" + ex.GetType().Name + ").",
                    ex);
            }
        }

        private static void DeleteTemporaryFile(string path)
        {
            if (path == null) return;
            try
            {
                if (File.Exists(path)) File.Delete(path);
            }
            catch
            {
                // A failed best-effort cleanup must not hide the original save outcome.
            }
        }

        private static long GetVersionLocked(string modId)
        {
            long version;
            return SettingsVersions.TryGetValue(modId, out version) ? version : 0L;
        }

        private static void IncrementVersionLocked(string modId)
        {
            SettingsVersions[modId] = GetVersionLocked(modId) + 1L;
        }

        private static void LogSaveFailure(string modId, ModSettingsSaveResult result)
        {
            var error = result.Error ?? new InvalidOperationException(result.FailureReason);
            ForgeLogger.LogError($"Failed to save settings for {modId}: {result.FailureReason}", error);
        }

        private static string GetSettingsPath(string modId)
        {
            ValidateModId(modId);

            var settingsDirectory = Path.GetFullPath(Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "Mount and Blade II Bannerlord",
                "Configs",
                "ModSettings"));
            var settingsPath = Path.GetFullPath(Path.Combine(settingsDirectory, $"{modId}.json"));
            var directoryPrefix = settingsDirectory.EndsWith(Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal)
                ? settingsDirectory
                : settingsDirectory + Path.DirectorySeparatorChar;

            if (!settingsPath.StartsWith(directoryPrefix, StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException("Mod ID must resolve to a file inside the ModSettings directory.", nameof(modId));
            }

            return settingsPath;
        }

        private static void ValidateModId(string modId)
        {
            if (string.IsNullOrWhiteSpace(modId))
            {
                throw new ArgumentException("Mod ID cannot be null or empty.", nameof(modId));
            }

            // Mod IDs are file-name stems here, never paths. Check both separators explicitly
            // so this remains safe even if the code is exercised on a non-Windows host.
            const string invalidFileNameCharacters = "\\/:*?\"<>|";
            for (var index = 0; index < modId.Length; index++)
            {
                var character = modId[index];
                if (character < 32 || invalidFileNameCharacters.IndexOf(character) >= 0)
                {
                    throw new ArgumentException("Mod ID must be a single valid file-name component.", nameof(modId));
                }
            }
        }
    }
}
