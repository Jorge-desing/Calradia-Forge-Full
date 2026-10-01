using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace CalradiaForge.Mod
{
    internal sealed class CampaignRuleBuilderDraft
    {
        [JsonProperty("version")]
        public int Version { get; set; } = CampaignRuleBuilderPersistence.CurrentVersion;
        [JsonProperty("rules")]
        public List<CampaignRuleBuilderRule> Rules { get; set; } = new List<CampaignRuleBuilderRule>();
    }

    internal sealed class CampaignRuleBuilderRule
    {
        [JsonProperty("id")]
        public string Id { get; set; }
        [JsonProperty("event")]
        public string EventId { get; set; }
        [JsonProperty("action")]
        public string ActionId { get; set; }
        [JsonProperty("target")]
        public string TargetId { get; set; }
        [JsonProperty("amount")]
        public int Amount { get; set; }
        [JsonProperty("groupA")]
        public List<CampaignRuleBuilderCondition> GroupA { get; set; } = new List<CampaignRuleBuilderCondition>();
        [JsonProperty("groupB")]
        public List<CampaignRuleBuilderCondition> GroupB { get; set; } = new List<CampaignRuleBuilderCondition>();
    }

    internal sealed class CampaignRuleBuilderCondition
    {
        [JsonProperty("kind")]
        public string Kind { get; set; }
        [JsonProperty("operator")]
        public string Operator { get; set; } = "eq";
        [JsonProperty("value")]
        public string Value { get; set; } = string.Empty;
    }

    internal enum CampaignRuleBuilderLoadStatus { Missing, Loaded, Invalid, TooLarge, UnknownVersion, Unavailable }

    internal sealed class CampaignRuleBuilderLoadResult
    {
        internal CampaignRuleBuilderLoadStatus Status { get; set; }
        internal CampaignRuleBuilderDraft Draft { get; set; }
    }

    internal static class CampaignRuleBuilderKinds
    {
        internal const int MaximumRules = 8;
        internal const int MaximumGroups = 2;
        internal const int MaximumConditionsPerGroup = 3;
        internal const int MaximumAmount = 100000;
        internal static readonly string[] Events = { "WeeklyTickEvent", "DailyTickHeroEvent", "DailyTickSettlementEvent", "HeroComesOfAgeEvent", "HeroGainedSkill", "OnHeroJoinedPartyEvent", "OnClanCreatedEvent" };
        internal static readonly string[] Actions = { "gold", "influence", "renown", "relation" };
        internal static readonly string[] Targets = { "player", "event" };
        internal static readonly string[] Conditions = { "always", "hero_is_player", "hero_is_alive", "hero_level_at_least", "clan_is_player", "settlement_is_town", "skill_gain_at_least", "party_is_main" };

        internal static CampaignRuleBuilderRule CreateRule()
        {
            return new CampaignRuleBuilderRule
            {
                Id = "rule_" + Guid.NewGuid().ToString("N").Substring(0, 12),
                EventId = Events[0], ActionId = Actions[0], TargetId = Targets[0], Amount = 100
            };
        }

        internal static string[] GetCompatibleConditions(string eventId)
        {
            if (!Events.Contains(eventId, StringComparer.Ordinal)) return new string[0];
            var result = new List<string> { "always" };
            if (HasHero(eventId)) result.AddRange(new[] { "hero_is_player", "hero_is_alive", "hero_level_at_least", "clan_is_player" });
            if (eventId == "OnClanCreatedEvent") result.Add("clan_is_player");
            if (eventId == "DailyTickSettlementEvent") result.Add("settlement_is_town");
            if (eventId == "HeroGainedSkill") result.Add("skill_gain_at_least");
            if (eventId == "OnHeroJoinedPartyEvent") result.Add("party_is_main");
            return result.ToArray();
        }

        internal static string[] GetCompatibleTargets(string eventId, string actionId)
        {
            if (!Events.Contains(eventId, StringComparer.Ordinal) || !Actions.Contains(actionId, StringComparer.Ordinal)) return new string[0];
            if (actionId == "relation") return HasHero(eventId) ? new[] { "event" } : new string[0];
            if (actionId == "gold") return HasHero(eventId) ? new[] { "player", "event" } : new[] { "player" };
            return HasHero(eventId) || eventId == "OnClanCreatedEvent" ? new[] { "player", "event" } : new[] { "player" };
        }

        internal static bool IsCompatibleCondition(string eventId, string kind) => GetCompatibleConditions(eventId).Contains(kind, StringComparer.Ordinal);
        internal static bool IsCompatibleTarget(string eventId, string actionId, string targetId) => GetCompatibleTargets(eventId, actionId).Contains(targetId, StringComparer.Ordinal);
        internal static bool HasHero(string eventId) => eventId == "DailyTickHeroEvent" || eventId == "HeroComesOfAgeEvent" || eventId == "HeroGainedSkill" || eventId == "OnHeroJoinedPartyEvent";

        internal static bool TryNormalize(CampaignRuleBuilderDraft input, out CampaignRuleBuilderDraft normalized, out string error)
        {
            normalized = null;
            error = null;
            if (input == null || input.Version != CampaignRuleBuilderPersistence.CurrentVersion) { error = "Unsupported or empty rule draft."; return false; }
            if (input.Rules == null || input.Rules.Count > MaximumRules) { error = "A draft can contain at most eight rules."; return false; }
            var ids = new HashSet<string>(StringComparer.Ordinal);
            var copy = new CampaignRuleBuilderDraft();
            foreach (var rule in input.Rules)
            {
                if (rule == null || !IsValidId(rule.Id) || !ids.Add(rule.Id) || !Events.Contains(rule.EventId, StringComparer.Ordinal) || !Actions.Contains(rule.ActionId, StringComparer.Ordinal) || !IsCompatibleTarget(rule.EventId, rule.ActionId, rule.TargetId))
                { error = "A rule has an invalid ID, event, action, or target."; return false; }
                bool validAmount = rule.ActionId == "gold" ? rule.Amount >= 1 && rule.Amount <= MaximumAmount
                    : rule.ActionId == "influence" ? rule.Amount >= -1000 && rule.Amount <= 1000 && rule.Amount != 0
                    : rule.ActionId == "renown" ? rule.Amount >= 1 && rule.Amount <= 1000
                    : rule.Amount >= -100 && rule.Amount <= 100 && rule.Amount != 0;
                if (!validAmount)
                { error = "Rule amount is outside the supported range."; return false; }
                if (!TryNormalizeGroup(rule.EventId, rule.GroupA, out var groupA, out error) || !TryNormalizeGroup(rule.EventId, rule.GroupB, out var groupB, out error)) return false;
                if (groupA.Count == 0 && groupB.Count > 0) { error = "The second condition group requires a first group."; return false; }
                copy.Rules.Add(new CampaignRuleBuilderRule { Id = rule.Id, EventId = rule.EventId, ActionId = rule.ActionId, TargetId = rule.TargetId, Amount = rule.Amount, GroupA = groupA, GroupB = groupB });
            }
            normalized = copy;
            return true;
        }

        private static bool TryNormalizeGroup(string eventId, List<CampaignRuleBuilderCondition> group, out List<CampaignRuleBuilderCondition> result, out string error)
        {
            result = new List<CampaignRuleBuilderCondition>(); error = null;
            if (group == null || group.Count > MaximumConditionsPerGroup) { error = "Each group accepts at most three conditions."; return false; }
            foreach (var condition in group)
            {
                if (condition == null || !IsCompatibleCondition(eventId, condition.Kind)) { error = "A condition is incompatible with its event."; return false; }
                bool numeric = condition.Kind == "hero_level_at_least" || condition.Kind == "skill_gain_at_least";
                if (numeric)
                {
                    if (condition.Operator != "gte" || !int.TryParse(condition.Value, out var number) || number < 1 || number > 1000)
                    { error = "A numeric condition requires a value from 1 to 1000."; return false; }
                    result.Add(new CampaignRuleBuilderCondition { Kind = condition.Kind, Operator = "gte", Value = number.ToString(System.Globalization.CultureInfo.InvariantCulture) });
                }
                else
                {
                    if (condition.Operator != "eq" || !string.IsNullOrEmpty(condition.Value)) { error = "A boolean condition cannot have a value."; return false; }
                    result.Add(new CampaignRuleBuilderCondition { Kind = condition.Kind, Operator = "eq", Value = string.Empty });
                }
            }
            return true;
        }

        private static bool IsValidId(string id)
        {
            if (id == null || id.Length != 17 || !id.StartsWith("rule_", StringComparison.Ordinal)) return false;
            for (int i = 5; i < id.Length; i++) if (!Uri.IsHexDigit(id[i]) || char.IsUpper(id[i])) return false;
            return true;
        }
    }

    internal static class CampaignRuleBuilderPersistence
    {
        internal const int CurrentVersion = 1;
        internal const int MaximumFileBytes = 64 * 1024;
        private static readonly object SaveGate = new object();

        internal static CampaignRuleBuilderLoadResult Load(string path)
        {
            var result = new CampaignRuleBuilderLoadResult { Status = CampaignRuleBuilderLoadStatus.Missing, Draft = new CampaignRuleBuilderDraft() };
            if (string.IsNullOrWhiteSpace(path)) { result.Status = CampaignRuleBuilderLoadStatus.Unavailable; return result; }
            try
            {
                if (!File.Exists(path)) return result;
                byte[] bytes;
                using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    if (stream.Length > MaximumFileBytes) { result.Status = CampaignRuleBuilderLoadStatus.TooLarge; return result; }
                    bytes = new byte[(int)stream.Length];
                    int offset = 0;
                    while (offset < bytes.Length) { int read = stream.Read(bytes, offset, bytes.Length - offset); if (read <= 0) throw new EndOfStreamException(); offset += read; }
                }
                string json = new UTF8Encoding(false, true).GetString(bytes);
                var root = JObject.Parse(json);
                if (root["version"] == null || root["rules"] == null) { result.Status = CampaignRuleBuilderLoadStatus.Invalid; return result; }
                var loaded = root.ToObject<CampaignRuleBuilderDraft>();
                if (loaded == null) { result.Status = CampaignRuleBuilderLoadStatus.Invalid; return result; }
                if (loaded.Version != CurrentVersion) { result.Status = CampaignRuleBuilderLoadStatus.UnknownVersion; return result; }
                if (!CampaignRuleBuilderKinds.TryNormalize(loaded, out var normalized, out _)) { result.Status = CampaignRuleBuilderLoadStatus.Invalid; return result; }
                result.Status = CampaignRuleBuilderLoadStatus.Loaded; result.Draft = normalized; return result;
            }
            catch (DecoderFallbackException) { result.Status = CampaignRuleBuilderLoadStatus.Invalid; }
            catch (JsonException) { result.Status = CampaignRuleBuilderLoadStatus.Invalid; }
            catch { result.Status = CampaignRuleBuilderLoadStatus.Unavailable; }
            return result;
        }

        internal static bool TrySave(string path, CampaignRuleBuilderDraft draft, out string error)
        {
            error = null;
            if (string.IsNullOrWhiteSpace(path)) { error = "The draft path is unavailable."; return false; }
            if (!CampaignRuleBuilderKinds.TryNormalize(draft, out var normalized, out error)) return false;
            byte[] bytes = new UTF8Encoding(false).GetBytes(JsonConvert.SerializeObject(normalized, Formatting.None));
            if (bytes.Length > MaximumFileBytes) { error = "The draft exceeds 64 KiB."; return false; }
            try
            {
                lock (SaveGate)
                {
                    string directory = Path.GetDirectoryName(path);
                    if (string.IsNullOrWhiteSpace(directory)) throw new IOException("The draft directory is unavailable.");
                    Directory.CreateDirectory(directory);
                    string temporary = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
                    try
                    {
                        using (var stream = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None)) { stream.Write(bytes, 0, bytes.Length); stream.Flush(true); }
                        if (File.Exists(path)) File.Replace(temporary, path, null); else File.Move(temporary, path);
                    }
                    finally { try { if (File.Exists(temporary)) File.Delete(temporary); } catch { } }
                }
                return true;
            }
            catch { error = "The draft could not be saved."; return false; }
        }
    }

    internal sealed class CampaignRuleBuilderPackage
    {
        internal string BehaviorCode { get; set; }
        internal string Integration { get; set; }
        internal string ValidationReport { get; set; }
        internal string FullText { get; set; }
    }

    internal static class CampaignRuleBuilderGenerator
    {
        internal static List<string> Validate(CampaignRuleBuilderDraft draft)
        {
            var errors = new List<string>();
            if (!CampaignRuleBuilderKinds.TryNormalize(draft, out var normalized, out var error)) errors.Add(error);
            else if (normalized.Rules.Count == 0) errors.Add("Add at least one rule before generating.");
            return errors;
        }

        internal static bool TryGenerate(CampaignRuleBuilderDraft draft, out CampaignRuleBuilderPackage package, out List<string> errors)
        {
            package = null; errors = Validate(draft);
            if (errors.Count > 0) return false;
            CampaignRuleBuilderKinds.TryNormalize(draft, out var normalized, out _);
            var code = BuildCode(normalized);
            ValidateCodeContract(normalized, code, errors);
            if (errors.Count > 0) return false;
            const string integration = "// SubModule.OnGameStart: register exactly once for a campaign game.\nif (gameStarterObject is CampaignGameStarter campaignStarter)\n    campaignStarter.AddBehavior(new GeneratedCampaignRulesBehavior());\n// Do not also add AutoRegisterBehavior to the generated class.\n// The designer only emits text; it does not register or execute these rules.";
            var eventIds = normalized.Rules.Select(r => r.EventId).Distinct(StringComparer.Ordinal).ToArray();
            string report = "Validated " + normalized.Rules.Count + " rule(s), " + eventIds.Length + " unique event subscription(s). Each action runs once for every matching event occurrence. Conditions use AND within groups and OR between groups.";
            if (eventIds.Contains("DailyTickHeroEvent", StringComparer.Ordinal))
                report += " DailyTickHeroEvent evaluates the rule for the event's hero; actions run only when conditions match, so player-targeted rewards may repeat across matching hero events.";
            if (eventIds.Contains("DailyTickSettlementEvent", StringComparer.Ordinal))
                report += " DailyTickSettlementEvent evaluates the rule for the event's settlement; actions run only when conditions match, so player-targeted rewards may repeat across matching settlement events.";
            report += " No game action ran in the designer.";
            package = new CampaignRuleBuilderPackage { BehaviorCode = code, Integration = integration, ValidationReport = report, FullText = "=== GeneratedCampaignRulesBehavior.cs ===\n" + code + "\n=== SubModule integration ===\n" + integration + "\n=== Validation ===\n" + report };
            return true;
        }

        private static void ValidateCodeContract(CampaignRuleBuilderDraft draft, string code, List<string> errors)
        {
            foreach (string eventId in draft.Rules.Select(r => r.EventId).Distinct(StringComparer.Ordinal))
            {
                string subscription = "CampaignEvents." + eventId + ".AddNonSerializedListener(this, " + HandlerName(eventId) + ");";
                if (Count(code, subscription) != 1 || Count(code, "private void " + HandlerName(eventId) + "(") != 1)
                    errors.Add("Generated event handler contract failed for " + eventId + ".");
            }
            if (!code.Contains("public override void SyncData(IDataStore dataStore) { }") ||
                code.Contains("SaveableTypeDefiner") || code.Contains("dataStore.SyncData(") ||
                !code.Contains("if (_executing || Campaign.Current == null) return;") ||
                !code.Contains("finally { _executing = false; }"))
                errors.Add("Generated stateless and reentrancy contract failed.");
            foreach (var rule in draft.Rules)
                if (Count(code, "// " + rule.Id) != 1)
                    errors.Add("Generated rule ID contract failed for " + rule.Id + ".");
        }

        private static int Count(string source, string needle) => source.Split(new[] { needle }, StringSplitOptions.None).Length - 1;

        private static string BuildCode(CampaignRuleBuilderDraft draft)
        {
            var b = new StringBuilder();
            b.AppendLine("using TaleWorlds.CampaignSystem;");
            b.AppendLine("using TaleWorlds.CampaignSystem.Actions;");
            b.AppendLine("using TaleWorlds.CampaignSystem.Party;");
            b.AppendLine("using TaleWorlds.CampaignSystem.Settlements;");
            b.AppendLine("using TaleWorlds.Core;");
            b.AppendLine();
            b.AppendLine("namespace YOUR_MOD_NAMESPACE.CampaignBehaviors");
            b.AppendLine("{");
            b.AppendLine("    public sealed class GeneratedCampaignRulesBehavior : CampaignBehaviorBase");
            b.AppendLine("    {");
            b.AppendLine("        private bool _executing;");
            b.AppendLine("        public override void RegisterEvents()");
            b.AppendLine("        {");
            foreach (string eventId in CampaignRuleBuilderKinds.Events)
                if (draft.Rules.Any(r => r.EventId == eventId)) b.AppendLine("            CampaignEvents." + eventId + ".AddNonSerializedListener(this, " + HandlerName(eventId) + ");");
            b.AppendLine("        }");
            b.AppendLine("        public override void SyncData(IDataStore dataStore) { } // Stateless: no save data.");
            foreach (string eventId in CampaignRuleBuilderKinds.Events)
            {
                var rules = draft.Rules.Where(r => r.EventId == eventId).ToList();
                if (rules.Count == 0) continue;
                b.AppendLine();
                b.AppendLine("        private void " + HandlerName(eventId) + "(" + Parameters(eventId) + ")");
                b.AppendLine("        {");
                b.AppendLine("            if (_executing || Campaign.Current == null) return;");
                b.AppendLine("            _executing = true;");
                b.AppendLine("            try");
                b.AppendLine("            {");
                foreach (var rule in rules)
                {
                    b.AppendLine("                // " + rule.Id);
                    b.AppendLine("                if (" + Guard(eventId, rule) + ")");
                    b.AppendLine("                {");
                    b.AppendLine("                    " + Action(rule, eventId));
                    b.AppendLine("                }");
                }
                b.AppendLine("            }");
                b.AppendLine("            finally { _executing = false; }");
                b.AppendLine("        }");
            }
            b.AppendLine("    }"); b.AppendLine("}");
            return b.ToString();
        }

        private static string HandlerName(string eventId) => "Handle" + eventId.Replace("Event", "");
        private static string Parameters(string eventId)
        {
            switch (eventId)
            {
                case "DailyTickHeroEvent": case "HeroComesOfAgeEvent": return "Hero hero";
                case "DailyTickSettlementEvent": return "Settlement settlement";
                case "HeroGainedSkill": return "Hero hero, SkillObject skill, int changeAmount, bool shouldNotify";
                case "OnHeroJoinedPartyEvent": return "Hero hero, MobileParty mobileParty";
                case "OnClanCreatedEvent": return "Clan clan, bool isPlayerClan";
                default: return string.Empty;
            }
        }

        private static string Guard(string eventId, CampaignRuleBuilderRule rule)
        {
            var checks = new List<string>();
            if (CampaignRuleBuilderKinds.HasHero(eventId)) checks.Add("hero != null");
            if (eventId == "DailyTickSettlementEvent") checks.Add("settlement != null");
            if (eventId == "OnClanCreatedEvent") checks.Add("clan != null");
            if (eventId == "OnHeroJoinedPartyEvent") checks.Add("mobileParty != null");
            if (rule.TargetId == "player")
            {
                checks.Add("Hero.MainHero != null");
                if (rule.ActionId == "influence" || rule.ActionId == "renown") checks.Add("Hero.MainHero.Clan != null");
            }
            if (rule.TargetId == "event" && (rule.ActionId == "influence" || rule.ActionId == "renown") && CampaignRuleBuilderKinds.HasHero(eventId)) checks.Add("hero.Clan != null");
            if (rule.TargetId == "event" && rule.ActionId == "renown" && eventId == "OnClanCreatedEvent") checks.Add("clan.Leader != null");
            if (rule.ActionId == "relation") checks.Add("Hero.MainHero != null && hero != Hero.MainHero");
            if (rule.GroupA.Count > 0)
            {
                string first = string.Join(" && ", rule.GroupA.Select(c => Condition(c, eventId)));
                string second = rule.GroupB.Count > 0 ? " || (" + string.Join(" && ", rule.GroupB.Select(c => Condition(c, eventId))) + ")" : string.Empty;
                checks.Add("((" + first + ")" + second + ")");
            }
            return string.Join(" && ", checks);
        }

        private static string Condition(CampaignRuleBuilderCondition c, string eventId)
        {
            switch (c.Kind)
            {
                case "hero_is_player": return "hero == Hero.MainHero";
                case "hero_is_alive": return "hero.IsAlive";
                case "hero_level_at_least": return "hero.Level >= " + c.Value;
                case "clan_is_player": return eventId == "OnClanCreatedEvent" ? "isPlayerClan" : "hero.Clan == Clan.PlayerClan";
                case "settlement_is_town": return "settlement.IsTown";
                case "skill_gain_at_least": return "skill != null && changeAmount >= " + c.Value;
                case "party_is_main": return "mobileParty == MobileParty.MainParty";
                default: return "true";
            }
        }

        private static string Action(CampaignRuleBuilderRule rule, string eventId)
        {
            string hero = rule.TargetId == "player" ? "Hero.MainHero" : "hero";
            string clan = rule.TargetId == "player" ? "Hero.MainHero.Clan" : eventId == "OnClanCreatedEvent" ? "clan" : "hero.Clan";
            string amount = rule.Amount.ToString(System.Globalization.CultureInfo.InvariantCulture);
            switch (rule.ActionId)
            {
                case "gold": return "GiveGoldAction.ApplyBetweenCharacters(null, " + hero + ", " + amount + ");";
                case "influence": return "ChangeClanInfluenceAction.Apply(" + clan + ", " + amount + "f);";
                case "renown": return "GainRenownAction.Apply(" + (rule.TargetId == "player" ? "Hero.MainHero" : eventId == "OnClanCreatedEvent" ? "clan.Leader" : "hero") + ", " + amount + "f);";
                default: return "ChangeRelationAction.ApplyPlayerRelation(hero, " + amount + ", true, true);";
            }
        }
    }
}
