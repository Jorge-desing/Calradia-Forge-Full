#if NET472
using System;
using System.IO;
using Newtonsoft.Json;
using TaleWorlds.Engine;

namespace CalradiaForge.Core.Configuration
{
    /// <summary>
    /// Utility to effortlessly load and save JSON configuration files for your mod.
    /// Files are stored in Documents\Mount and Blade II Bannerlord\Configs\ModConfigs\
    /// </summary>
    public static class ForgeConfig
    {
        private static string GetConfigDirectory()
        {
            string docsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            string configDir = System.IO.Path.Combine(docsPath, "Mount and Blade II Bannerlord", "Configs", "ModConfigs");
            if (!Directory.Exists(configDir)) Directory.CreateDirectory(configDir);
            return configDir;
        }

        public static T Load<T>(string modName) where T : new()
        {
            string path = System.IO.Path.Combine(GetConfigDirectory(), $"{modName}.json");
            if (File.Exists(path))
            {
                try
                {
                    string json = File.ReadAllText(path);
                    return JsonConvert.DeserializeObject<T>(json) ?? new T();
                }
                catch (Exception ex)
                {
                    Diagnostics.ForgeLogger.PrintError($"Failed to load config for {modName}: {ex.Message}");
                    return new T();
                }
            }
            
            // Create default
            var config = new T();
            Save(modName, config);
            return config;
        }

        public static void Save<T>(string modName, T config)
        {
            try
            {
                string path = System.IO.Path.Combine(GetConfigDirectory(), $"{modName}.json");
                string json = JsonConvert.SerializeObject(config, Formatting.Indented);
                File.WriteAllText(path, json);
            }
            catch (Exception ex)
            {
                Diagnostics.ForgeLogger.PrintError($"Failed to save config for {modName}: {ex.Message}");
            }
        }
    }
}
#endif
