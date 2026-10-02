using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.CodeDom.Compiler;
using System.Xml;
using Microsoft.CSharp;
using CalradiaForge.Mod;

namespace CalradiaForge.Tests
{
    internal static class CampaignRuleBuilderTests
    {
        internal static void Run(Action<string, Action> test)
        {
            test("Campaign rule builder bounds and stable IDs", TestBoundsAndIds);
            test("Campaign rule builder validates event data and targets", TestCompatibility);
            test("Campaign rule builder validates action amounts", TestActionAmounts);
            test("Campaign rule builder action normalization updates amount text", TestActionNormalizationUpdatesAmountText);
            test("Campaign rule builder generates grouped stateless handlers", TestGeneration);
            test("Campaign rule builder generated behavior compiles against installed game DLLs", TestGeneratedCodeCompiles);
            test("Campaign rule builder draft loads and saves safely", TestPersistence);
            test("Campaign rule builder restores its draft after the Gauntlet view model is constructed", TestRouteLoadsDraftAfterConstruction);
            test("Campaign rule builder restores the exact draft after explicit save and a new view model", TestViewModelSaveRestoresDraftAfterNewInstance);
            test("Campaign rule builder first visit stays empty until Add rule", TestMissingDraftStartsEmptyUntilAdd);
            test("Campaign rule builder saved empty drafts stay empty", TestEmptyDraftPersistsWithoutImplicitRule);
            test("Campaign rule builder IDs survive reorder and removal", TestRuleIdentitySurvivesReorderAndRemoval);
            test("Campaign rule builder preview and copy remain local and complete", TestUiSurfaceContracts);
        }

        private static void TestBoundsAndIds()
        {
            var rules = Enumerable.Range(0, 8).Select(_ => CampaignRuleBuilderKinds.CreateRule()).ToList();
            var ids = rules.Select(r => r.Id).ToArray();
            if (ids.Distinct(StringComparer.Ordinal).Count() != 8) throw new Exception("Rule IDs must be unique.");
            rules.Reverse();
            if (!CampaignRuleBuilderKinds.TryNormalize(new CampaignRuleBuilderDraft { Rules = rules }, out var normalized, out var error)) throw new Exception(error);
            if (normalized.Rules[0].Id != ids[7]) throw new Exception("Reordering changed a rule ID.");
            rules.Add(CampaignRuleBuilderKinds.CreateRule());
            if (CampaignRuleBuilderKinds.TryNormalize(new CampaignRuleBuilderDraft { Rules = rules }, out _, out _)) throw new Exception("More than eight rules accepted.");
            rules.RemoveAt(8);
            rules[0].GroupA = Enumerable.Range(0, 4).Select(_ => new CampaignRuleBuilderCondition { Kind = "always" }).ToList();
            if (CampaignRuleBuilderKinds.TryNormalize(new CampaignRuleBuilderDraft { Rules = rules }, out _, out _)) throw new Exception("More than three conditions accepted.");
            rules[0].GroupA.Clear();
            rules[0].GroupB.Add(new CampaignRuleBuilderCondition { Kind = "always" });
            if (CampaignRuleBuilderKinds.TryNormalize(new CampaignRuleBuilderDraft { Rules = rules }, out _, out _)) throw new Exception("A second group without a first group was accepted.");
            rules[0].GroupB.Clear();
            rules[1].Id = rules[0].Id;
            if (CampaignRuleBuilderKinds.TryNormalize(new CampaignRuleBuilderDraft { Rules = rules }, out _, out _)) throw new Exception("Duplicate rule ID accepted.");
            rules[1].Id = "rule_ABCDEF123456";
            if (CampaignRuleBuilderKinds.TryNormalize(new CampaignRuleBuilderDraft { Rules = rules }, out _, out _)) throw new Exception("Malformed rule ID accepted.");
        }

        private static void TestCompatibility()
        {
            if (CampaignRuleBuilderKinds.Events.Length != 7 || CampaignRuleBuilderKinds.Actions.Length != 4) throw new Exception("Incomplete catalog.");
            if (CampaignRuleBuilderKinds.IsCompatibleTarget("WeeklyTickEvent", "relation", "event")) throw new Exception("Weekly event has no hero.");
            if (!CampaignRuleBuilderKinds.IsCompatibleTarget("HeroGainedSkill", "relation", "event")) throw new Exception("Hero event should support relation action.");
            if (CampaignRuleBuilderKinds.IsCompatibleCondition("WeeklyTickEvent", "skill_gain_at_least")) throw new Exception("Skill condition has no weekly data.");
            var rule = CampaignRuleBuilderKinds.CreateRule();
            rule.EventId = "HeroGainedSkill";
            rule.ActionId = "relation";
            rule.TargetId = "event";
            rule.GroupA.Add(new CampaignRuleBuilderCondition { Kind = "skill_gain_at_least", Operator = "gte", Value = "5" });
            rule.GroupB.Add(new CampaignRuleBuilderCondition { Kind = "hero_is_alive" });
            if (!CampaignRuleBuilderKinds.TryNormalize(new CampaignRuleBuilderDraft { Rules = new List<CampaignRuleBuilderRule> { rule } }, out _, out var error)) throw new Exception(error);
            rule.GroupB[0].Kind = "settlement_is_town";
            if (CampaignRuleBuilderKinds.TryNormalize(new CampaignRuleBuilderDraft { Rules = new List<CampaignRuleBuilderRule> { rule } }, out _, out _)) throw new Exception("Incompatible condition accepted.");
            rule.GroupB[0].Kind = "hero_is_alive";
            rule.Amount = 101;
            if (CampaignRuleBuilderKinds.TryNormalize(new CampaignRuleBuilderDraft { Rules = new List<CampaignRuleBuilderRule> { rule } }, out _, out _)) throw new Exception("Out-of-range relation change accepted.");
        }

        private static void TestActionAmounts()
        {
            var rule = CampaignRuleBuilderKinds.CreateRule();
            AssertAmount(rule, "gold", "player", 1, true);
            AssertAmount(rule, "gold", "player", 100000, true);
            AssertAmount(rule, "gold", "player", 0, false);
            AssertAmount(rule, "gold", "player", 100001, false);
            AssertAmount(rule, "influence", "player", -1000, true);
            AssertAmount(rule, "influence", "player", 1000, true);
            AssertAmount(rule, "influence", "player", 0, false);
            AssertAmount(rule, "influence", "player", -1001, false);
            AssertAmount(rule, "renown", "player", 1, true);
            AssertAmount(rule, "renown", "player", 1000, true);
            AssertAmount(rule, "renown", "player", -1, false);
            AssertAmount(rule, "renown", "player", 1001, false);
            rule.EventId = "DailyTickHeroEvent";
            AssertAmount(rule, "relation", "event", -100, true);
            AssertAmount(rule, "relation", "event", 100, true);
            AssertAmount(rule, "relation", "event", 0, false);
            AssertAmount(rule, "relation", "event", 101, false);
        }

        private static void AssertAmount(CampaignRuleBuilderRule rule, string actionId, string targetId, int amount, bool expected)
        {
            rule.ActionId = actionId;
            rule.TargetId = targetId;
            rule.Amount = amount;
            bool accepted = CampaignRuleBuilderKinds.TryNormalize(new CampaignRuleBuilderDraft { Rules = new List<CampaignRuleBuilderRule> { rule } }, out _, out _);
            if (accepted != expected) throw new Exception("Unexpected amount validation for " + actionId + ": " + amount + ".");
        }

        private static void TestActionNormalizationUpdatesAmountText()
        {
            string directory = Path.Combine(Path.GetTempPath(), "forge-rule-builder-amount-" + Guid.NewGuid().ToString("N"));
            string path = Path.Combine(directory, "campaign-rule-builder.json");
            try
            {
                using (var runtime = new Runtime())
                {
                    var viewModel = new PanelViewModel(runtime, delegate { }, path);
                    viewModel.ExecuteNoviceCampaignRuleBuilder();
                    viewModel.ExecuteCampaignRuleBuilderAdd();
                    viewModel.ExecuteCampaignRuleBuilderCycleAction();
                    if (viewModel.CampaignRuleBuilderSelectedActionLabel != "Change influence") throw new Exception("The explicitly added rule did not advance to the influence action.");
                    viewModel.CampaignRuleBuilderAmountText = "-25";
                    if (viewModel.CampaignRuleBuilderAmountText != "-25") throw new Exception("The influence amount edit was not retained.");

                    viewModel.ExecuteCampaignRuleBuilderCycleAction();
                    viewModel.ExecuteCampaignRuleBuilderCycleAction();
                    if (viewModel.CampaignRuleBuilderSelectedActionLabel != "Grant gold" || viewModel.CampaignRuleBuilderAmountText != "100")
                        throw new Exception("Normalizing a negative amount for gold did not refresh the visible amount field.");

                    viewModel.ExecuteCampaignRuleBuilderSave();
                    var loaded = CampaignRuleBuilderPersistence.Load(path);
                    if (loaded.Status != CampaignRuleBuilderLoadStatus.Loaded || loaded.Draft.Rules.Count != 1 || loaded.Draft.Rules[0].Amount != 100)
                        throw new Exception("The normalized amount in the UI did not match the saved rule.");
                }
            }
            finally { if (Directory.Exists(directory)) Directory.Delete(directory, true); }
        }

        private static void TestGeneration()
        {
            var first = CampaignRuleBuilderKinds.CreateRule();
            first.EventId = "HeroGainedSkill";
            first.ActionId = "relation";
            first.TargetId = "event";
            first.GroupA.Add(new CampaignRuleBuilderCondition { Kind = "hero_is_alive" });
            first.GroupA.Add(new CampaignRuleBuilderCondition { Kind = "skill_gain_at_least", Operator = "gte", Value = "10" });
            first.GroupB.Add(new CampaignRuleBuilderCondition { Kind = "hero_is_player" });
            var second = CampaignRuleBuilderKinds.CreateRule();
            second.EventId = "HeroGainedSkill";
            second.ActionId = "gold";
            second.TargetId = "player";
            if (!CampaignRuleBuilderGenerator.TryGenerate(new CampaignRuleBuilderDraft { Rules = new List<CampaignRuleBuilderRule> { first, second } }, out var package, out var errors)) throw new Exception(string.Join("; ", errors));
            var code = package.BehaviorCode;
            if (Count(code, "HeroGainedSkill.AddNonSerializedListener") != 1) throw new Exception("Expected one event subscription.");
            if (!code.Contains("skill != null && changeAmount >= 10") || !code.Contains(" || (hero == Hero.MainHero)")) throw new Exception("AND/OR conditions missing.");
            if (!code.Contains("finally { _executing = false; }") || !code.Contains("public override void SyncData(IDataStore dataStore) { }") || code.Contains("SaveableTypeDefiner")) throw new Exception("Stateless reentrancy contract missing.");
            if (!package.FullText.Contains(package.BehaviorCode) || !package.FullText.Contains(package.Integration) || !package.FullText.Contains(package.ValidationReport)) throw new Exception("Incomplete package copy.");
            var dailyHeroRule = CampaignRuleBuilderKinds.CreateRule();
            dailyHeroRule.EventId = "DailyTickHeroEvent";
            var dailySettlementRule = CampaignRuleBuilderKinds.CreateRule();
            dailySettlementRule.EventId = "DailyTickSettlementEvent";
            if (!CampaignRuleBuilderGenerator.TryGenerate(new CampaignRuleBuilderDraft { Rules = new List<CampaignRuleBuilderRule> { dailyHeroRule, dailySettlementRule } }, out var cadencePackage, out errors))
                throw new Exception(string.Join("; ", errors));
            if (!cadencePackage.ValidationReport.Contains("Each action runs once for every matching event occurrence.") ||
                !cadencePackage.ValidationReport.Contains("DailyTickHeroEvent evaluates the rule for the event's hero") ||
                !cadencePackage.ValidationReport.Contains("DailyTickSettlementEvent evaluates the rule for the event's settlement"))
                throw new Exception("Generated validation must explain per-occurrence and per-entity event cadence.");
            var clanRule = CampaignRuleBuilderKinds.CreateRule();
            clanRule.EventId = "OnClanCreatedEvent";
            clanRule.ActionId = "influence";
            clanRule.TargetId = "event";
            clanRule.GroupA.Add(new CampaignRuleBuilderCondition { Kind = "clan_is_player" });
            if (!CampaignRuleBuilderGenerator.TryGenerate(new CampaignRuleBuilderDraft { Rules = new List<CampaignRuleBuilderRule> { clanRule } }, out var clanPackage, out errors)) throw new Exception(string.Join("; ", errors));
            if (!clanPackage.BehaviorCode.Contains("if (clan != null && ((isPlayerClan)))")) throw new Exception("Clan creation condition must use the event's isPlayerClan value.");
        }

        private static void TestPersistence()
        {
            string directory = Path.Combine(Path.GetTempPath(), "forge-rule-builder-tests-" + Guid.NewGuid().ToString("N"));
            string path = Path.Combine(directory, "campaign-rule-builder.json");
            try
            {
                var missing = CampaignRuleBuilderPersistence.Load(path);
                if (missing.Status != CampaignRuleBuilderLoadStatus.Missing) throw new Exception("Missing status expected.");
                var draft = new CampaignRuleBuilderDraft { Rules = new List<CampaignRuleBuilderRule> { CampaignRuleBuilderKinds.CreateRule() } };
                if (!CampaignRuleBuilderPersistence.TrySave(path, draft, out var error)) throw new Exception(error);
                if (CampaignRuleBuilderPersistence.Load(path).Status != CampaignRuleBuilderLoadStatus.Loaded) throw new Exception("Saved draft did not load.");
                AssertLoadPreserves(path, "{ invalid", CampaignRuleBuilderLoadStatus.Invalid);
                AssertLoadPreserves(path, "{}", CampaignRuleBuilderLoadStatus.Invalid);
                AssertLoadPreserves(path, "{\"version\":99,\"rules\":[]}", CampaignRuleBuilderLoadStatus.UnknownVersion);
                AssertLoadPreserves(path, new string('x', CampaignRuleBuilderPersistence.MaximumFileBytes + 1), CampaignRuleBuilderLoadStatus.TooLarge);
                if (!CampaignRuleBuilderPersistence.TrySave(path, draft, out error)) throw new Exception(error);
                if (CampaignRuleBuilderPersistence.Load(path).Status != CampaignRuleBuilderLoadStatus.Loaded) throw new Exception("Explicit save did not replace the invalid draft.");
            }
            finally { if (Directory.Exists(directory)) Directory.Delete(directory, true); }
        }

        private static void TestRouteLoadsDraftAfterConstruction()
        {
            string directory = Path.Combine(Path.GetTempPath(), "forge-rule-builder-viewmodel-" + Guid.NewGuid().ToString("N"));
            string path = Path.Combine(directory, "campaign-rule-builder.json");
            try
            {
                var rule = CampaignRuleBuilderKinds.CreateRule();
                rule.EventId = "DailyTickHeroEvent";
                var draft = new CampaignRuleBuilderDraft { Rules = new List<CampaignRuleBuilderRule> { rule } };
                if (!CampaignRuleBuilderPersistence.TrySave(path, draft, out var error)) throw new Exception(error);

                using (var runtime = new Runtime())
                {
                    var viewModel = new PanelViewModel(runtime, delegate { }, path);
                    if (viewModel.CampaignRuleBuilderRules.Count != 0) throw new Exception("Draft loading must wait until the route is opened after the Gauntlet view model is bound.");
                    viewModel.ExecuteNoviceCampaignRuleBuilder();
                    if (viewModel.CampaignRuleBuilderRules.Count != 1 || viewModel.CampaignRuleBuilderIsEmpty) throw new Exception("Opening the route did not restore the saved rule list.");
                    if (viewModel.CampaignRuleBuilderRules[0].Id != rule.Id || viewModel.CampaignRuleBuilderStatusLabel != "Rule draft loaded." || viewModel.CampaignRuleBuilderDraftLoadStatusLabel != "Rule draft loaded.") throw new Exception("The restored row or load status is incorrect.");
                    if (viewModel.CampaignRuleBuilderSelectedEventLabel != rule.EventId || viewModel.CampaignRuleBuilderAmountText != "100") throw new Exception("Opening the route did not restore the selected rule's event and amount.");

                    viewModel.ExecuteCampaignRuleBuilderCycleEvent();
                    if (viewModel.CampaignRuleBuilderRules.Count != 1 || viewModel.CampaignRuleBuilderRules[0].Id != rule.Id) throw new Exception("Cycling the event must edit the selected draft rule without adding a row.");
                    if (viewModel.CampaignRuleBuilderSelectedEventLabel == rule.EventId || viewModel.CampaignRuleBuilderStatusLabel != "Unsaved draft edits.") throw new Exception("Cycling the event must mark only the in-memory draft as edited.");
                    if (viewModel.CampaignRuleBuilderDraftLoadStatusLabel != "Rule draft loaded.") throw new Exception("Editing the loaded rule must not erase the persistent load result.");
                    if (!File.ReadAllText(path).Contains("DailyTickHeroEvent")) throw new Exception("Editing a rule must not save it without an explicit Save draft command.");
                }
            }
            finally { if (Directory.Exists(directory)) Directory.Delete(directory, true); }
        }

        private static void TestMissingDraftStartsEmptyUntilAdd()
        {
            string directory = Path.Combine(Path.GetTempPath(), "forge-rule-builder-empty-first-" + Guid.NewGuid().ToString("N"));
            string path = Path.Combine(directory, "campaign-rule-builder.json");
            try
            {
                using (var runtime = new Runtime())
                {
                    var viewModel = new PanelViewModel(runtime, delegate { }, path);
                    viewModel.ExecuteNoviceCampaignRuleBuilder();
                    if (!viewModel.CampaignRuleBuilderIsEmpty || viewModel.CampaignRuleBuilderRules.Count != 0 ||
                        viewModel.CampaignRuleBuilderStatusLabel != "No saved rule draft." ||
                        viewModel.CampaignRuleBuilderDraftLoadStatusLabel != "No saved rule draft." ||
                        viewModel.CampaignRuleBuilderSelectedEventLabel.Length != 0 ||
                        viewModel.CampaignRuleBuilderCanGenerate || !viewModel.CampaignRuleBuilderCannotGenerate ||
                        !viewModel.CampaignRuleBuilderCannotSelect)
                        throw new Exception("A first visit without a saved file must open with zero rules and no selection.");
                    if (File.Exists(path)) throw new Exception("Opening an empty editor must not create a saved draft file.");

                    viewModel.ExecuteCampaignRuleBuilderCycleEvent();
                    if (viewModel.CampaignRuleBuilderRules.Count != 0 || viewModel.CampaignRuleBuilderSelectedEventLabel.Length != 0 || File.Exists(path))
                        throw new Exception("The event control must not create or edit a rule when the list is empty.");

                    viewModel.ExecuteCampaignRuleBuilderValidate();
                    if (viewModel.CampaignRuleBuilderStatusLabel != "Add at least one rule before generating.")
                        throw new Exception("Validation must reject an empty rule list.");
                    viewModel.ExecuteCampaignRuleBuilderGenerate();
                    if (viewModel.IsCampaignRuleBuilderPackageVisible || viewModel.CampaignRuleBuilderCanCopy ||
                        viewModel.CampaignRuleBuilderStatusLabel != "Add at least one rule before generating.")
                        throw new Exception("An empty rule list must not produce a package or enable copying.");
                    viewModel.ExecuteCampaignRuleBuilderCopy();
                    if (viewModel.IsCampaignRuleBuilderPackageVisible || viewModel.CampaignRuleBuilderCanCopy)
                        throw new Exception("Copy package must remain a no-op until generation succeeds.");

                    viewModel.ExecuteCampaignRuleBuilderAdd();
                    if (viewModel.CampaignRuleBuilderIsEmpty || viewModel.CampaignRuleBuilderRules.Count != 1 ||
                        !viewModel.CampaignRuleBuilderCanGenerate || viewModel.CampaignRuleBuilderCannotGenerate ||
                        viewModel.CampaignRuleBuilderDraftLoadStatusLabel != "No saved rule draft.")
                        throw new Exception("Add rule must explicitly create the first editable rule.");
                    string firstRuleId = viewModel.CampaignRuleBuilderRules[0].Id;
                    string firstRuleEvent = viewModel.CampaignRuleBuilderSelectedEventLabel;
                    if (viewModel.CampaignRuleBuilderRules[0].RowIdentityLabel != firstRuleId || File.Exists(path))
                        throw new Exception("An explicitly added rule must show its stable ID and remain unsaved.");

                    viewModel.ExecuteCampaignRuleBuilderCycleEvent();
                    if (viewModel.CampaignRuleBuilderRules.Count != 1 || viewModel.CampaignRuleBuilderRules[0].Id != firstRuleId ||
                        viewModel.CampaignRuleBuilderSelectedEventLabel == firstRuleEvent ||
                        viewModel.CampaignRuleBuilderStatusLabel != "Unsaved draft edits.")
                        throw new Exception("Changing the event after Add must edit the selected row without adding or saving another rule.");

                    viewModel.CampaignRuleBuilderAmountText = "0";
                    viewModel.ExecuteCampaignRuleBuilderSave();
                    if (File.Exists(path))
                        throw new Exception("A rejected save must not create a draft file.");
                    viewModel.CampaignRuleBuilderAmountText = "100";
                    viewModel.ExecuteCampaignRuleBuilderSave();
                    var saved = CampaignRuleBuilderPersistence.Load(path);
                    if (!File.Exists(path) || saved.Status != CampaignRuleBuilderLoadStatus.Loaded || saved.Draft.Rules.Count != 1 ||
                        saved.Draft.Rules[0].Id != firstRuleId || viewModel.CampaignRuleBuilderDraftLoadStatusLabel != "Rule draft saved." ||
                        viewModel.CampaignRuleBuilderRules[0].RowIdentityLabel != firstRuleId)
                        throw new Exception("Only explicit Save draft should persist the rule added by the user.");
                }
            }
            finally { if (Directory.Exists(directory)) Directory.Delete(directory, true); }
        }

        private static void TestEmptyDraftPersistsWithoutImplicitRule()
        {
            string directory = Path.Combine(Path.GetTempPath(), "forge-rule-builder-empty-draft-" + Guid.NewGuid().ToString("N"));
            string path = Path.Combine(directory, "campaign-rule-builder.json");
            try
            {
                var emptyDraft = new CampaignRuleBuilderDraft { Rules = new List<CampaignRuleBuilderRule>() };
                if (!CampaignRuleBuilderPersistence.TrySave(path, emptyDraft, out var error)) throw new Exception(error);
                byte[] savedBytes = File.ReadAllBytes(path);
                var loaded = CampaignRuleBuilderPersistence.Load(path);
                if (loaded.Status != CampaignRuleBuilderLoadStatus.Loaded || loaded.Draft.Rules.Count != 0)
                    throw new Exception("An explicitly saved empty draft must be recognized as loaded and stay empty.");

                using (var runtime = new Runtime())
                {
                    var viewModel = new PanelViewModel(runtime, delegate { }, path);
                    viewModel.ExecuteNoviceCampaignRuleBuilder();
                    if (!viewModel.CampaignRuleBuilderIsEmpty || viewModel.CampaignRuleBuilderRules.Count != 0 ||
                        viewModel.CampaignRuleBuilderStatusLabel != "Rule draft loaded." ||
                        viewModel.CampaignRuleBuilderDraftLoadStatusLabel != "Rule draft loaded." ||
                        viewModel.CampaignRuleBuilderCanGenerate || !viewModel.CampaignRuleBuilderCannotGenerate)
                        throw new Exception("Reopening a saved empty draft must not insert an unsaved rule.");
                    viewModel.ExecuteCampaignRuleBuilderGenerate();
                    if (viewModel.IsCampaignRuleBuilderPackageVisible || viewModel.CampaignRuleBuilderCanCopy)
                        throw new Exception("A saved empty draft must not produce a package.");
                    if (!File.ReadAllBytes(path).SequenceEqual(savedBytes))
                        throw new Exception("Loading or attempting to generate an empty draft must not rewrite its file.");
                }
            }
            finally { if (Directory.Exists(directory)) Directory.Delete(directory, true); }
        }

        private static void TestRuleIdentitySurvivesReorderAndRemoval()
        {
            string directory = Path.Combine(Path.GetTempPath(), "forge-rule-builder-order-" + Guid.NewGuid().ToString("N"));
            string path = Path.Combine(directory, "campaign-rule-builder.json");
            try
            {
                using (var runtime = new Runtime())
                {
                    var viewModel = new PanelViewModel(runtime, delegate { }, path);
                    viewModel.ExecuteNoviceCampaignRuleBuilder();
                    if (viewModel.CampaignRuleBuilderRules.Count != 0) throw new Exception("A missing draft must remain empty before an explicit add.");
                    viewModel.ExecuteCampaignRuleBuilderAdd();
                    string firstId = viewModel.CampaignRuleBuilderRules[0].Id;
                    viewModel.ExecuteCampaignRuleBuilderAdd();
                    string secondId = viewModel.CampaignRuleBuilderRules[1].Id;
                    viewModel.ExecuteCampaignRuleBuilderMoveUp();
                    if (viewModel.CampaignRuleBuilderRules[0].Id != secondId ||
                        viewModel.CampaignRuleBuilderRules[0].RowIdentityLabel != secondId ||
                        viewModel.CampaignRuleBuilderRules[1].Id != firstId ||
                        viewModel.CampaignRuleBuilderRules[1].RowIdentityLabel != firstId)
                        throw new Exception("Reordering must preserve each explicitly created rule's stable ID.");

                    viewModel.ExecuteCampaignRuleBuilderSelectNext();
                    viewModel.ExecuteCampaignRuleBuilderRemove();
                    if (viewModel.CampaignRuleBuilderRules.Count != 1 ||
                        viewModel.CampaignRuleBuilderRules[0].Id != secondId ||
                        viewModel.CampaignRuleBuilderRules[0].RowIdentityLabel != secondId || File.Exists(path))
                        throw new Exception("Removing a selected rule must leave the other unsaved rule unchanged.");

                    viewModel.ExecuteCampaignRuleBuilderRemove();
                    if (!viewModel.CampaignRuleBuilderIsEmpty || viewModel.CampaignRuleBuilderCanGenerate || File.Exists(path))
                        throw new Exception("Removing the final rule must restore the empty unsaved state without writing a file.");
                    viewModel.ExecuteCampaignRuleBuilderAdd();
                    string newRuleId = viewModel.CampaignRuleBuilderRules[0].Id;
                    if (newRuleId == firstId || newRuleId == secondId || viewModel.CampaignRuleBuilderRules[0].RowIdentityLabel != newRuleId)
                        throw new Exception("A new rule must receive its own stable identity.");
                    viewModel.ExecuteCampaignRuleBuilderSave();
                    var saved = CampaignRuleBuilderPersistence.Load(path);
                    if (saved.Status != CampaignRuleBuilderLoadStatus.Loaded || saved.Draft.Rules.Count != 1 ||
                        saved.Draft.Rules[0].Id != newRuleId || viewModel.CampaignRuleBuilderRules[0].RowIdentityLabel != newRuleId)
                        throw new Exception("Saving after removal must persist only the remaining explicitly added rule.");
                }
            }
            finally { if (Directory.Exists(directory)) Directory.Delete(directory, true); }
        }

        private static void TestViewModelSaveRestoresDraftAfterNewInstance()
        {
            string directory = Path.Combine(Path.GetTempPath(), "forge-rule-builder-reopen-" + Guid.NewGuid().ToString("N"));
            string path = Path.Combine(directory, "campaign-rule-builder.json");
            string firstRuleId = string.Empty;
            string secondRuleId = string.Empty;
            byte[] savedBytes = null;
            try
            {
                using (var firstRuntime = new Runtime())
                {
                    var firstViewModel = new PanelViewModel(firstRuntime, delegate { }, path);
                    firstViewModel.ExecuteNoviceCampaignRuleBuilder();
                    if (firstViewModel.CampaignRuleBuilderRules.Count != 0 || firstViewModel.CampaignRuleBuilderDraftLoadStatusLabel != "No saved rule draft.")
                        throw new Exception("A missing draft must begin with an empty list.");

                    firstViewModel.ExecuteCampaignRuleBuilderAdd();
                    firstRuleId = firstViewModel.CampaignRuleBuilderRules[0].Id;
                    firstViewModel.ExecuteCampaignRuleBuilderCycleEvent();
                    if (firstViewModel.CampaignRuleBuilderSelectedEventLabel != "DailyTickHeroEvent")
                        throw new Exception("Changing the event did not update the explicitly added row in place.");
                    if (!firstViewModel.CampaignRuleBuilderPreviewLabel.Contains("Daily per-hero event:"))
                        throw new Exception("The preview must disclose the selected event's per-hero daily cadence.");

                    firstViewModel.ExecuteCampaignRuleBuilderAdd();
                    if (firstViewModel.CampaignRuleBuilderRules.Count != 2) throw new Exception("Add rule did not create a second row.");
                    secondRuleId = firstViewModel.CampaignRuleBuilderRules[1].Id;
                    firstViewModel.ExecuteCampaignRuleBuilderCycleAction();
                    if (firstViewModel.CampaignRuleBuilderSelectedActionLabel != "Change influence")
                        throw new Exception("The second row did not change to the influence action.");
                    firstViewModel.CampaignRuleBuilderAmountText = "-25";
                    firstViewModel.ExecuteCampaignRuleBuilderMoveUp();
                    if (firstViewModel.CampaignRuleBuilderRules[0].Id != secondRuleId || firstViewModel.CampaignRuleBuilderRules[1].Id != firstRuleId)
                        throw new Exception("Reordering changed or lost a rule ID before save.");

                    firstViewModel.ExecuteCampaignRuleBuilderSelectNext();
                    if (firstViewModel.CampaignRuleBuilderRules[1].Id != firstRuleId)
                        throw new Exception("The first added rule could not be selected after reordering.");
                    firstViewModel.ExecuteCampaignRuleBuilderAddConditionA();
                    var levelCondition = firstViewModel.CampaignRuleBuilderConditionsA[0];
                    levelCondition.ExecuteCycleKind();
                    levelCondition.ExecuteCycleKind();
                    levelCondition.ExecuteCycleKind();
                    if (!levelCondition.IsNumeric) throw new Exception("The numeric hero-level condition was not selected.");
                    levelCondition.Value = "20";
                    firstViewModel.ExecuteCampaignRuleBuilderAddConditionB();
                    firstViewModel.CampaignRuleBuilderConditionsB[0].ExecuteCycleKind();

                    firstViewModel.ExecuteCampaignRuleBuilderSave();
                    var persisted = CampaignRuleBuilderPersistence.Load(path);
                    if (persisted.Status != CampaignRuleBuilderLoadStatus.Loaded || persisted.Draft.Rules.Count != 2 ||
                        persisted.Draft.Rules[0].Id != secondRuleId || persisted.Draft.Rules[0].EventId != "WeeklyTickEvent" ||
                        persisted.Draft.Rules[0].ActionId != "influence" || persisted.Draft.Rules[0].Amount != -25 ||
                        persisted.Draft.Rules[1].Id != firstRuleId || persisted.Draft.Rules[1].EventId != "DailyTickHeroEvent" ||
                        persisted.Draft.Rules[1].GroupA.Count != 1 || persisted.Draft.Rules[1].GroupA[0].Kind != "hero_level_at_least" ||
                        persisted.Draft.Rules[1].GroupA[0].Operator != "gte" || persisted.Draft.Rules[1].GroupA[0].Value != "20" ||
                        persisted.Draft.Rules[1].GroupB.Count != 1 || persisted.Draft.Rules[1].GroupB[0].Kind != "hero_is_player")
                        throw new Exception("Save draft did not persist the edited rule fields, IDs, order, and AND/OR conditions.");
                    if (firstViewModel.CampaignRuleBuilderDraftLoadStatusLabel != "Rule draft saved.")
                        throw new Exception("The first editor did not report an explicit save.");
                    savedBytes = File.ReadAllBytes(path);
                }

                using (var secondRuntime = new Runtime())
                {
                    var secondViewModel = new PanelViewModel(secondRuntime, delegate { }, path);
                    if (secondViewModel.CampaignRuleBuilderRules.Count != 0)
                        throw new Exception("The new editor must defer draft loading until its Gauntlet route opens.");

                    secondViewModel.ExecuteNoviceCampaignRuleBuilder();
                    if (secondViewModel.CampaignRuleBuilderRules.Count != 2 || secondViewModel.CampaignRuleBuilderIsEmpty)
                        throw new Exception("Reopening the route did not restore both explicitly saved rules.");
                    if (secondViewModel.CampaignRuleBuilderRules[0].Id != secondRuleId || secondViewModel.CampaignRuleBuilderRules[1].Id != firstRuleId)
                        throw new Exception("Reopening changed the saved rule IDs or order.");
                    if (secondViewModel.CampaignRuleBuilderStatusLabel != "Rule draft loaded." ||
                        secondViewModel.CampaignRuleBuilderDraftLoadStatusLabel != "Rule draft loaded.")
                        throw new Exception("A newly opened editor must distinguish a loaded draft from an unsaved rule edit.");
                    if (secondViewModel.CampaignRuleBuilderSelectedEventLabel != "WeeklyTickEvent" ||
                        secondViewModel.CampaignRuleBuilderSelectedActionLabel != "Change influence" ||
                        secondViewModel.CampaignRuleBuilderSelectedTargetLabel != "Player" ||
                        secondViewModel.CampaignRuleBuilderAmountText != "-25")
                        throw new Exception("The selected first rule's exact values were not restored.");

                    secondViewModel.ExecuteCampaignRuleBuilderSelectNext();
                    if (secondViewModel.CampaignRuleBuilderSelectedEventLabel != "DailyTickHeroEvent" ||
                        secondViewModel.CampaignRuleBuilderSelectedActionLabel != "Grant gold" ||
                        secondViewModel.CampaignRuleBuilderSelectedTargetLabel != "Player" ||
                        secondViewModel.CampaignRuleBuilderAmountText != "100")
                        throw new Exception("The second rule's exact values were not restored.");
                    if (secondViewModel.CampaignRuleBuilderConditionsA.Count != 1 || secondViewModel.CampaignRuleBuilderConditionsB.Count != 1 ||
                        !secondViewModel.CampaignRuleBuilderConditionsA[0].IsNumeric || secondViewModel.CampaignRuleBuilderConditionsA[0].Value != "20" ||
                        secondViewModel.CampaignRuleBuilderConditionsA[0].Operator != "gte" ||
                        secondViewModel.CampaignRuleBuilderConditionsB[0].IsNumeric || secondViewModel.CampaignRuleBuilderConditionsB[0].Summary != "Hero is player")
                        throw new Exception("The reopened view model did not restore the selected rule's full AND/OR condition groups.");
                    secondViewModel.ExecuteCampaignRuleBuilderSelectPrevious();
                    if (secondViewModel.CampaignRuleBuilderConditionsA.Count != 0 || secondViewModel.CampaignRuleBuilderConditionsB.Count != 0)
                        throw new Exception("Switching to the rule without conditions did not refresh both condition groups.");
                    secondViewModel.ExecuteCampaignRuleBuilderSelectNext();
                    if (secondViewModel.CampaignRuleBuilderConditionsA.Count != 1 || secondViewModel.CampaignRuleBuilderConditionsB.Count != 1 ||
                        secondViewModel.CampaignRuleBuilderConditionsA[0].Value != "20" || secondViewModel.CampaignRuleBuilderConditionsB[0].Summary != "Hero is player")
                        throw new Exception("Selecting the conditioned rule again did not restore both condition groups.");
                    if (!File.ReadAllBytes(path).SequenceEqual(savedBytes))
                        throw new Exception("Loading a saved draft must not rewrite the user's file.");
                }
            }
            finally { if (Directory.Exists(directory)) Directory.Delete(directory, true); }
        }

        private static void AssertLoadPreserves(string path, string text, CampaignRuleBuilderLoadStatus expected)
        {
            File.WriteAllText(path, text);
            if (CampaignRuleBuilderPersistence.Load(path).Status != expected) throw new Exception("Unexpected draft load status: " + expected + ".");
            if (File.ReadAllText(path) != text) throw new Exception("Loading rewrote an invalid draft.");
        }

        private static void TestGeneratedCodeCompiles()
        {
            string gameBin = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Steam", "steamapps", "common", "Mount & Blade II Bannerlord", "bin", "Win64_Shipping_Client");
            string campaignDll = Path.Combine(gameBin, "TaleWorlds.CampaignSystem.dll");
            if (!File.Exists(campaignDll)) throw new Exception("Installed Bannerlord DLLs are required for generated-code compilation.");
            var combinations = new List<CampaignRuleBuilderRule>();
            foreach (string eventId in CampaignRuleBuilderKinds.Events)
            {
                foreach (string actionId in CampaignRuleBuilderKinds.Actions)
                {
                    foreach (string targetId in CampaignRuleBuilderKinds.GetCompatibleTargets(eventId, actionId))
                    {
                        var rule = CampaignRuleBuilderKinds.CreateRule();
                        rule.EventId = eventId;
                        rule.ActionId = actionId;
                        rule.TargetId = targetId;
                        combinations.Add(rule);
                    }
                }
            }
            using (var provider = new CSharpCodeProvider(new Dictionary<string, string> { ["CompilerVersion"] = "v4.0" }))
            {
                for (int start = 0; start < combinations.Count; start += CampaignRuleBuilderKinds.MaximumRules)
                {
                    var draft = new CampaignRuleBuilderDraft { Rules = combinations.Skip(start).Take(CampaignRuleBuilderKinds.MaximumRules).ToList() };
                    if (!CampaignRuleBuilderGenerator.TryGenerate(draft, out var package, out var errors)) throw new Exception(string.Join("; ", errors));
                    var parameters = new CompilerParameters { GenerateExecutable = false, GenerateInMemory = true, TreatWarningsAsErrors = false };
                    foreach (string name in new[] { "TaleWorlds.CampaignSystem.dll", "TaleWorlds.Core.dll", "TaleWorlds.Library.dll", "TaleWorlds.MountAndBlade.dll", "TaleWorlds.ObjectSystem.dll" })
                        parameters.ReferencedAssemblies.Add(Path.Combine(gameBin, name));
                    string netstandardFacade = Path.Combine(gameBin, "mono", "lib", "mono", "4.7.2-api", "Facades", "netstandard.dll");
                    if (!File.Exists(netstandardFacade)) throw new Exception("The installed Bannerlord net472 netstandard facade is required for generated-code compilation.");
                    parameters.ReferencedAssemblies.Add(netstandardFacade);
                    var result = provider.CompileAssemblyFromSource(parameters, package.BehaviorCode);
                    var failures = result.Errors.Cast<CompilerError>().Where(e => !e.IsWarning).Select(e => e.ToString()).ToArray();
                    if (failures.Length > 0) throw new Exception("Combination batch " + start + ": " + string.Join(Environment.NewLine, failures));
                }
            }
        }

        private static void TestUiSurfaceContracts()
        {
            string panel = File.ReadAllText("src/CalradiaForge.Mod/PanelViewModel.CampaignRules.cs");
            string shell = File.ReadAllText("src/CalradiaForge.Mod/PanelViewModel.cs");
            string input = File.ReadAllText("src/CalradiaForge.Mod/SubModule.cs");
            string prefab = File.ReadAllText("modules/CalradiaForge/GUI/Prefabs/CalradiaForge.xml");
            var prefabDocument = new XmlDocument();
            prefabDocument.LoadXml(prefab);
            XmlNode rowIdentity = prefabDocument.SelectSingleNode("//*[@Text='@RowIdentityLabel']");
            XmlNode eventCycle = prefabDocument.SelectSingleNode("//*[@Id='ForgeCampaignRuleEventCycle']");
            XmlNode eventDisplayLabel = prefabDocument.SelectSingleNode("//*[@Text='@CampaignRuleBuilderSelectedEventDisplayLabel']");
            XmlNode eventHint = prefabDocument.SelectSingleNode("//*[@Text='@CampaignRuleBuilderEventChangeHintLabel']");
            XmlNode eventSurface = prefabDocument.SelectSingleNode("//*[@Id='CampaignRuleEvent']");
            XmlNode generateButton = prefabDocument.SelectSingleNode("//*[@Id='ForgeCampaignRuleGenerate']");
            XmlNode emptyState = prefabDocument.SelectSingleNode("//*[@Id='ForgeCampaignRuleEmpty']");
            XmlNode orderPrevious = prefabDocument.SelectSingleNode("//*[@Id='ForgeCampaignRulePrevious']");
            XmlNode orderRemove = prefabDocument.SelectSingleNode("//*[@Id='ForgeCampaignRuleRemove']");
            string copy = Slice(panel, "public void ExecuteCampaignRuleBuilderCopy()", "private bool ValidateCampaignRuleBuilderAmounts");
            string clipboard = Slice(shell, "public void ExecuteClipboard()", "public void RefreshLanguage()");
            string preview = Slice(panel, "private string BuildCampaignRulePreview()", "internal sealed class CampaignRuleItemVM");
            if (!copy.Contains("if (!CampaignRuleBuilderCanCopy) return;") || !copy.Contains("ExecuteClipboard();") ||
                !clipboard.Contains("Send(\"clipboard\", full);") || clipboard.Contains("Send(\"clipboard\", Content);") ||
                !panel.Contains("CampaignRuleBuilderOutputText => CampaignRuleBuilderCanCopy ? full : string.Empty"))
                throw new Exception("Copy must dispatch the complete generated package independently of its visible page.");
            if (!preview.Contains("SampleCampaignRuleGroup") || !preview.Contains("Preview only. No campaign action ran.") ||
                preview.Contains("GiveGoldAction") || preview.Contains("ChangeClanInfluenceAction") ||
                preview.Contains("GainRenownAction") || preview.Contains("ChangeRelationAction"))
                throw new Exception("Sample preview must evaluate only local draft data and never invoke campaign actions.");
            if (!prefab.Contains("Id=\"ForgeCampaignRuleRow\"") || !input.Contains("ExecuteCampaignRuleBuilderSelectIndex(rowIndex)"))
                throw new Exception("Every ordered rule row must be reachable through Tab and Enter.");
            if (!prefab.Contains("Id=\"ForgeCampaignRuleAdd\"") || !prefab.Contains("Command.Click=\"ExecuteCampaignRuleBuilderAdd\"") ||
                !prefab.Contains("Text=\"@CampaignRuleBuilderRulesLabel\"") || prefab.Contains("Text=\"@CampaignRuleBuilderCatalogLabel\"") ||
                !prefab.Contains("Id=\"ForgeCampaignRuleDraftLoadState\"") || !prefab.Contains("Text=\"@CampaignRuleBuilderDraftLoadStatusLabel\"") ||
                !prefab.Contains("Text=\"@RowIdentityLabel\"") ||
                !prefab.Contains("Id=\"ForgeCampaignRuleEventCycle\"") || !prefab.Contains("Command.Click=\"ExecuteCampaignRuleBuilderCycleEvent\"") ||
                !prefab.Contains("Text=\"@CampaignRuleBuilderEventLabel\"") ||
                !prefab.Contains("Text=\"@CampaignRuleBuilderSelectedEventDisplayLabel\"") ||
                !prefab.Contains("Text=\"@CampaignRuleBuilderEventChangeHintLabel\"") ||
                !panel.Contains("CampaignRuleBuilderEventChangeHintLabel => T(\"Changing the event clears both condition groups.\")") ||
                !panel.Contains("[DataSourceProperty] public string RowIdentityLabel => Id;") ||
                !panel.Contains("[DataSourceProperty] public bool CampaignRuleBuilderCannotGenerate => !CampaignRuleBuilderCanGenerate;") ||
                !panel.Contains("[DataSourceProperty] public bool CampaignRuleBuilderCannotSelect => !CampaignRuleBuilderHasSelection;"))
                throw new Exception("The explicitly created rule identity and Generate availability must be bound by the ViewModel.");
            if (rowIdentity == null || eventCycle == null || eventHint == null || eventSurface == null || generateButton == null || emptyState == null || orderPrevious == null || orderRemove == null ||
                eventCycle.Attributes["Command.Click"]?.Value != "ExecuteCampaignRuleBuilderCycleEvent" ||
                generateButton.Attributes["IsDisabled"]?.Value != "@CampaignRuleBuilderCannotGenerate" ||
                emptyState.Attributes["IsVisible"]?.Value != "@CampaignRuleBuilderIsEmpty" ||
                orderPrevious.Attributes["IsDisabled"]?.Value != "@CampaignRuleBuilderCannotSelect" ||
                orderRemove.Attributes["IsDisabled"]?.Value != "@CampaignRuleBuilderCannotSelect" ||
                eventDisplayLabel?.ParentNode?.ParentNode != eventCycle ||
                eventSurface.Attributes["SuggestedHeight"]?.Value != "87" ||
                eventHint.Attributes["Text"]?.Value != "@CampaignRuleBuilderEventChangeHintLabel")
                throw new Exception("The parsed Gauntlet prefab must bind empty state, disabled generation, rule identity, and event-loss warning correctly.");
        }

        private static string Slice(string source, string start, string end)
        {
            int first = source.IndexOf(start, StringComparison.Ordinal);
            int last = first < 0 ? -1 : source.IndexOf(end, first + start.Length, StringComparison.Ordinal);
            if (first < 0 || last < 0) throw new Exception("UI contract marker missing: " + start);
            return source.Substring(first, last - first);
        }

        private static int Count(string source, string needle) => source.Split(new[] { needle }, StringSplitOptions.None).Length - 1;
    }
}
