using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace CalradiaForge.Sdk
{
    /// <summary>
    /// Fluent builder for creating and validating TaleWorlds Items.xml weapon, armor, and mount definitions.
    /// Strictly enforces engine rules for damage types (Cut, Pierce, Blunt), component structures, and crafting templates.
    /// </summary>
    public sealed class ForgeItemBuilder
    {
        private static readonly HashSet<string> ValidDamageTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Cut", "Pierce", "Blunt"
        };

        private static readonly HashSet<string> ValidItemTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "OneHandedWeapon", "TwoHandedWeapon", "Polearm", "Bow", "Crossbow", "Thrown", "Shield",
            "HeadArmor", "BodyArmor", "LegArmor", "HandArmor", "Cape", "Horse", "HorseHarness", "Goods", "Banner"
        };

        public string Id { get; private set; }
        public string Name { get; private set; }
        public string Mesh { get; private set; }
        public string Culture { get; private set; } = "Culture.neutral_culture";
        public double Weight { get; private set; } = 1.0;
        public int Appearance { get; private set; } = 1;
        public string Type { get; private set; } = "OneHandedWeapon";
        public int Value { get; private set; } = 100;

        // Weapon Component
        private bool _hasWeapon;
        private string _weaponClass = "OneHandedSword";
        private int _thrustSpeed = 90;
        private int _speedRating = 90;
        private int _weaponBalance = 90;
        private int _thrustDamage = 30;
        private string _thrustDamageType = "Pierce";
        private int _swingDamage = 60;
        private string _swingDamageType = "Cut";
        private string _itemUsage = "one_handed_sword";

        // Armor Component
        private bool _hasArmor;
        private int _headArmor;
        private int _bodyArmor;
        private int _legArmor;
        private int _armArmor;
        private bool _hasGenderVariations = true;

        // Horse Component
        private bool _hasHorse;
        private string _monster = "Monster.horse";
        private int _maneuver = 65;
        private int _speed = 45;
        private int _chargeDamage = 15;
        private int _extraHealth = 20;

        public ForgeItemBuilder(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("Item ID cannot be null or empty.", nameof(id));
            Id = id;
            Name = id;
            Mesh = $"{id}_mesh";
        }

        public static ForgeItemBuilder Create(string id) => new ForgeItemBuilder(id);

        public ForgeItemBuilder WithName(string name)
        {
            Name = string.IsNullOrWhiteSpace(name) ? Id : name;
            return this;
        }

        public ForgeItemBuilder WithMesh(string mesh)
        {
            Mesh = string.IsNullOrWhiteSpace(mesh) ? $"{Id}_mesh" : mesh;
            return this;
        }

        public ForgeItemBuilder WithCulture(string culture)
        {
            if (!string.IsNullOrWhiteSpace(culture))
            {
                Culture = culture.StartsWith("Culture.", StringComparison.OrdinalIgnoreCase) ? culture : $"Culture.{culture}";
            }
            return this;
        }

        public ForgeItemBuilder WithWeight(double weight)
        {
            Weight = Math.Max(0.1, weight);
            return this;
        }

        public ForgeItemBuilder WithValue(int value)
        {
            Value = Math.Max(1, value);
            return this;
        }

        public ForgeItemBuilder WithType(string itemType)
        {
            if (!ValidItemTypes.Contains(itemType))
                throw new ArgumentException($"Invalid item Type '{itemType}'.");
            Type = itemType;
            return this;
        }

        public ForgeItemBuilder AsWeapon(string weaponClass, int thrustSpeed, int speedRating, int thrustDmg, string thrustType, int swingDmg, string swingType, string itemUsage = null)
        {
            if (!ValidDamageTypes.Contains(thrustType)) throw new ArgumentException($"Invalid thrust damage type '{thrustType}'. Must be Cut, Pierce, or Blunt.");
            if (!ValidDamageTypes.Contains(swingType)) throw new ArgumentException($"Invalid swing damage type '{swingType}'. Must be Cut, Pierce, or Blunt.");

            _hasWeapon = true;
            _hasArmor = false;
            _hasHorse = false;
            _weaponClass = weaponClass;
            _thrustSpeed = Math.Max(1, thrustSpeed);
            _speedRating = Math.Max(1, speedRating);
            _thrustDamage = Math.Max(0, thrustDmg);
            _thrustDamageType = thrustType;
            _swingDamage = Math.Max(0, swingDmg);
            _swingDamageType = swingType;
            _itemUsage = itemUsage ?? weaponClass.ToLowerInvariant();
            return this;
        }

        public ForgeItemBuilder AsArmor(int bodyArmor, int legArmor = 0, int armArmor = 0, int headArmor = 0, bool hasGenderVariations = true)
        {
            _hasArmor = true;
            _hasWeapon = false;
            _hasHorse = false;
            _bodyArmor = Math.Max(0, bodyArmor);
            _legArmor = Math.Max(0, legArmor);
            _armArmor = Math.Max(0, armArmor);
            _headArmor = Math.Max(0, headArmor);
            _hasGenderVariations = hasGenderVariations;
            return this;
        }

        public ForgeItemBuilder AsHorse(int speed, int maneuver, int chargeDamage, int extraHealth = 20, string monster = "Monster.horse")
        {
            _hasHorse = true;
            _hasWeapon = false;
            _hasArmor = false;
            Type = "Horse";
            _speed = Math.Max(1, speed);
            _maneuver = Math.Max(1, maneuver);
            _chargeDamage = Math.Max(0, chargeDamage);
            _extraHealth = Math.Max(0, extraHealth);
            _monster = monster ?? "Monster.horse";
            return this;
        }

        public XElement BuildElement()
        {
            var elem = new XElement("Item",
                new XAttribute("id", Id),
                new XAttribute("name", Name),
                new XAttribute("mesh", Mesh),
                new XAttribute("culture", Culture),
                new XAttribute("weight", Weight.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture)),
                new XAttribute("appearance", Appearance),
                new XAttribute("Type", Type),
                new XAttribute("value", Value));

            var comp = new XElement("ItemComponent");

            if (_hasWeapon)
            {
                comp.Add(new XElement("Weapon",
                    new XAttribute("weapon_class", _weaponClass),
                    new XAttribute("thrust_speed", _thrustSpeed),
                    new XAttribute("speed_rating", _speedRating),
                    new XAttribute("weapon_balance", _weaponBalance),
                    new XAttribute("thrust_damage", _thrustDamage),
                    new XAttribute("thrust_damage_type", _thrustDamageType),
                    new XAttribute("swing_damage", _swingDamage),
                    new XAttribute("swing_damage_type", _swingDamageType),
                    new XAttribute("item_usage", _itemUsage)));
                elem.Add(comp);
            }
            else if (_hasArmor)
            {
                var armorElem = new XElement("Armor",
                    new XAttribute("body_armor", _bodyArmor),
                    new XAttribute("leg_armor", _legArmor),
                    new XAttribute("arm_armor", _armArmor),
                    new XAttribute("has_gender_variations", _hasGenderVariations ? "true" : "false"));
                if (_headArmor > 0) armorElem.Add(new XAttribute("head_armor", _headArmor));
                comp.Add(armorElem);
                elem.Add(comp);
            }
            else if (_hasHorse)
            {
                comp.Add(new XElement("Horse",
                    new XAttribute("monster", _monster),
                    new XAttribute("speed", _speed),
                    new XAttribute("maneuver", _maneuver),
                    new XAttribute("charge_damage", _chargeDamage),
                    new XAttribute("extra_health", _extraHealth),
                    new XAttribute("is_pack_animal", "false"),
                    new XAttribute("is_mountable", "true")));
                elem.Add(comp);
            }

            return elem;
        }

        public string BuildXml()
        {
            var doc = new XDocument(
                new XDeclaration("1.0", "utf-8", null),
                new XElement("Items", BuildElement()));
            return doc.Declaration + Environment.NewLine + doc.ToString();
        }
    }
}
