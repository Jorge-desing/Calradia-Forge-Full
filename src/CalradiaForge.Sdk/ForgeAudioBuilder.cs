using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace CalradiaForge.Sdk
{
    /// <summary>
    /// Fluent builder for creating and validating TaleWorlds module_sounds.xml audio declarations.
    /// Enforces valid mixer sound categories (ui, mission_combat, ambient, voice) and audio file extensions (.ogg, .wav).
    /// </summary>
    public sealed class ForgeAudioBuilder
    {
        private static readonly HashSet<string> ValidCategories = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "ui", "mission_combat", "ambient", "voice"
        };

        private readonly List<(string Name, bool Is2D, string Category, string Path)> _sounds =
            new List<(string, bool, string, string)>();

        public static ForgeAudioBuilder Create() => new ForgeAudioBuilder();

        public ForgeAudioBuilder Add2DSound(string name, string path, string category = "ui")
        {
            return AddSound(name, true, category, path);
        }

        public ForgeAudioBuilder Add3DSound(string name, string path, string category = "mission_combat")
        {
            return AddSound(name, false, category, path);
        }

        public ForgeAudioBuilder AddSound(string name, bool is2D, string category, string path)
        {
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Sound name cannot be null or empty.");
            if (string.IsNullOrWhiteSpace(path)) throw new ArgumentException("Audio file path cannot be null or empty.");
            if (!path.EndsWith(".ogg", StringComparison.OrdinalIgnoreCase) && !path.EndsWith(".wav", StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException($"Audio file '{path}' must have a .ogg or .wav extension.");
            if (!ValidCategories.Contains(category))
                throw new ArgumentException($"Invalid sound category '{category}'. Valid categories: ui, mission_combat, ambient, voice.");

            _sounds.Add((name, is2D, category.ToLowerInvariant(), path));
            return this;
        }

        public int Count => _sounds.Count;

        public (bool isValid, List<string> errors) Validate()
        {
            var errors = new List<string>();
            if (_sounds.Count == 0)
            {
                errors.Add("Audio manifest contains no sound entries.");
            }
            var seenNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var s in _sounds)
            {
                if (!seenNames.Add(s.Name))
                {
                    errors.Add($"Duplicate sound name '{s.Name}' detected in audio manifest.");
                }
                if (!ValidCategories.Contains(s.Category))
                    errors.Add($"Sound '{s.Name}' has invalid category '{s.Category}'. Valid categories: ui, mission_combat, ambient, voice.");
                if (!s.Path.EndsWith(".ogg", StringComparison.OrdinalIgnoreCase) && !s.Path.EndsWith(".wav", StringComparison.OrdinalIgnoreCase))
                    errors.Add($"Sound '{s.Name}' has invalid path '{s.Path}'. Must end with .ogg or .wav.");
            }
            return (errors.Count == 0, errors);
        }

        public XElement BuildElement()
        {
            var root = new XElement("module_sounds");
            for (int i = 0; i < _sounds.Count; i++)
            {
                var s = _sounds[i];
                root.Add(new XElement("module_sound",
                    new XAttribute("name", s.Name),
                    new XAttribute("is_2d", s.Is2D ? "true" : "false"),
                    new XAttribute("sound_category", s.Category),
                    new XAttribute("path", s.Path)));
            }
            return root;
        }

        public string BuildXml()
        {
            var doc = new XDocument(
                new XDeclaration("1.0", "utf-8", null),
                BuildElement());
            return doc.Declaration + Environment.NewLine + doc.ToString();
        }
    }
}
