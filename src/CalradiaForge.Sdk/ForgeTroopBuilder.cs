using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace CalradiaForge.Sdk
{
    /// <summary>
    /// Fluent builder for creating and validating TaleWorlds NPCCharacters.xml troop and character definitions.
    /// Strictly enforces engine constraints such as integer age, equipment slot validation, and upgrade tree prefixes.
    /// </summary>
    public sealed class ForgeTroopBuilder
    {
        private static readonly HashSet<string> ValidSlots = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Item0", "Item1", "Item2", "Item3", "Head", "Cape", "Body", "Gloves", "Leg", "Horse", "HorseHarness"
        };

        private static readonly HashSet<string> ValidGroups = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Infantry", "Ranged", "Cavalry", "HorseArcher"
        };

        public string Id { get; private set; }
        public string Name { get; private set; }
        public int Age { get; private set; } = 25;
        public int Level { get; private set; } = 1;
        public string Occupation { get; private set; } = "Soldier";
        public string Culture { get; private set; } = "Culture.neutral_culture";
        public string DefaultGroup { get; private set; } = "Infantry";
        public bool IsHero { get; private set; } = false;
        public bool IsFemale { get; private set; } = false;
        public string FaceKeyTemplate { get; private set; }

        private readonly Dictionary<string, int> _skills = new Dictionary<string, int>();
        private readonly List<Dictionary<string, string>> _battleSets = new List<Dictionary<string, string>>();
        private readonly Dictionary<string, string> _civilianSet = new Dictionary<string, string>();
        private readonly List<string> _upgradeTargets = new List<string>();

        public ForgeTroopBuilder(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("Troop ID cannot be null or empty.", nameof(id));
            Id = id;
            Name = id;
            _battleSets.Add(new Dictionary<string, string>());
        }

        public static ForgeTroopBuilder Create(string id) => new ForgeTroopBuilder(id);

        public ForgeTroopBuilder WithName(string name)
        {
            Name = name ?? Id;
            return this;
        }

        public ForgeTroopBuilder WithAge(int age)
        {
            if (age < 0 || age > 128) throw new ArgumentOutOfRangeException(nameof(age), "Age must be an integer between 0 and 128.");
            Age = age;
            return this;
        }

        public ForgeTroopBuilder WithLevel(int level)
        {
            Level = Math.Max(1, level);
            return this;
        }

        public ForgeTroopBuilder WithOccupation(string occupation)
        {
            Occupation = occupation ?? "Soldier";
            return this;
        }

        public ForgeTroopBuilder WithCulture(string culture)
        {
            if (!string.IsNullOrWhiteSpace(culture))
            {
                Culture = culture.StartsWith("Culture.", StringComparison.OrdinalIgnoreCase) ? culture : $"Culture.{culture}";
            }
            return this;
        }

        public ForgeTroopBuilder WithDefaultGroup(string group)
        {
            if (!ValidGroups.Contains(group))
                throw new ArgumentException($"Invalid default formation group '{group}'. Must be Infantry, Ranged, Cavalry, or HorseArcher.");
            DefaultGroup = group;
            return this;
        }

        public ForgeTroopBuilder SetHero(bool isHero)
        {
            IsHero = isHero;
            return this;
        }

        public ForgeTroopBuilder SetFemale(bool isFemale)
        {
            IsFemale = isFemale;
            return this;
        }

        public ForgeTroopBuilder WithFaceKeyTemplate(string template)
        {
            FaceKeyTemplate = template;
            return this;
        }

        public ForgeTroopBuilder AddSkill(string skillId, int value)
        {
            if (string.IsNullOrWhiteSpace(skillId)) throw new ArgumentException("Skill ID cannot be null or empty.");
            _skills[skillId.Trim()] = Math.Max(0, value);
            return this;
        }

        public ForgeTroopBuilder AddBattleEquipment(string slot, string itemId, int variationIndex = 0)
        {
            if (string.IsNullOrWhiteSpace(itemId)) throw new ArgumentException("Item ID cannot be null or empty.", nameof(itemId));
            if (!ValidSlots.Contains(slot))
                throw new ArgumentException($"Invalid equipment slot '{slot}'. Valid slots: {string.Join(", ", ValidSlots)}");

            while (_battleSets.Count <= variationIndex)
            {
                _battleSets.Add(new Dictionary<string, string>());
            }

            string trimmedItem = itemId.Trim();
            string formattedItem = trimmedItem.StartsWith("Item.", StringComparison.OrdinalIgnoreCase) ? trimmedItem : $"Item.{trimmedItem}";
            _battleSets[variationIndex][slot] = formattedItem;
            return this;
        }

        public ForgeTroopBuilder AddCivilianEquipment(string slot, string itemId)
        {
            if (string.IsNullOrWhiteSpace(itemId)) throw new ArgumentException("Item ID cannot be null or empty.", nameof(itemId));
            if (!ValidSlots.Contains(slot))
                throw new ArgumentException($"Invalid equipment slot '{slot}'. Valid slots: {string.Join(", ", ValidSlots)}");

            string trimmedItem = itemId.Trim();
            string formattedItem = trimmedItem.StartsWith("Item.", StringComparison.OrdinalIgnoreCase) ? trimmedItem : $"Item.{trimmedItem}";
            _civilianSet[slot] = formattedItem;
            return this;
        }

        public ForgeTroopBuilder AddUpgradeTarget(string targetId)
        {
            if (string.IsNullOrWhiteSpace(targetId)) throw new ArgumentException("Upgrade target ID cannot be null or empty.");
            if (_upgradeTargets.Count >= 2) throw new InvalidOperationException("Troop cannot have more than 2 branching upgrade targets in Bannerlord.");

            string formattedTarget = targetId.StartsWith("NPCCharacter.", StringComparison.OrdinalIgnoreCase)
                ? targetId
                : $"NPCCharacter.{targetId}";

            if (formattedTarget.Equals($"NPCCharacter.{Id}", StringComparison.OrdinalIgnoreCase) || formattedTarget.Equals(Id, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException($"Troop '{Id}' cannot have itself as an upgrade target (cyclic upgrade loop).");

            if (!_upgradeTargets.Contains(formattedTarget))
            {
                _upgradeTargets.Add(formattedTarget);
            }
            return this;
        }

        public XElement BuildElement()
        {
            var elem = new XElement("NPCCharacter",
                new XAttribute("id", Id),
                new XAttribute("name", Name),
                new XAttribute("age", Age),
                new XAttribute("level", Level),
                new XAttribute("occupation", Occupation),
                new XAttribute("culture", Culture),
                new XAttribute("default_group", DefaultGroup),
                new XAttribute("is_hero", IsHero ? "true" : "false"),
                new XAttribute("is_female", IsFemale ? "true" : "false"));

            if (!string.IsNullOrWhiteSpace(FaceKeyTemplate))
            {
                elem.Add(new XElement("face",
                    new XElement("face_key_template", new XAttribute("value", FaceKeyTemplate))));
            }

            if (_skills.Count > 0)
            {
                var skillsElem = new XElement("skills");
                foreach (var kvp in _skills)
                {
                    skillsElem.Add(new XElement("skill", new XAttribute("id", kvp.Key), new XAttribute("value", kvp.Value)));
                }
                elem.Add(skillsElem);
            }

            var equipmentsElem = new XElement("Equipments");
            foreach (var set in _battleSets)
            {
                if (set.Count > 0)
                {
                    var setElem = new XElement("EquipmentSet");
                    foreach (var item in set)
                    {
                        setElem.Add(new XElement("equipment", new XAttribute("slot", item.Key), new XAttribute("id", item.Value)));
                    }
                    equipmentsElem.Add(setElem);
                }
            }

            // Civilian Set
            var civElem = new XElement("EquipmentSet", new XAttribute("civilian", "true"));
            if (_civilianSet.Count > 0)
            {
                foreach (var item in _civilianSet)
                {
                    civElem.Add(new XElement("equipment", new XAttribute("slot", item.Key), new XAttribute("id", item.Value)));
                }
            }
            equipmentsElem.Add(civElem);
            elem.Add(equipmentsElem);

            if (_upgradeTargets.Count > 0)
            {
                var upgradeElem = new XElement("upgrade_targets");
                foreach (var target in _upgradeTargets)
                {
                    upgradeElem.Add(new XElement("upgrade_target", new XAttribute("id", target)));
                }
                elem.Add(upgradeElem);
            }

            return elem;
        }

        public string BuildXml()
        {
            var doc = new XDocument(
                new XDeclaration("1.0", "utf-8", null),
                new XElement("NPCCharacters", BuildElement()));
            return doc.Declaration + Environment.NewLine + doc.ToString();
        }
    }
}
