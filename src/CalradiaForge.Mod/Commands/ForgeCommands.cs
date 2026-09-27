using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using TaleWorlds.Library;
using CalradiaForge.Sdk;
using CalradiaForge.Sdk.Patcher;
using CalradiaForge.Core;
using CalradiaForge.Core.Diagnostics;
using CalradiaForge.Mod;

namespace CalradiaForge.Mod.Commands
{
    /// <summary>
    /// Native Bannerlord Console Commands for Calradia Forge.
    /// Access these in-game by pressing ALT + ~ (tilde) to open the developer console.
    /// </summary>
    public static class ForgeCommands
    {
        [CommandLineFunctionality.CommandLineArgumentFunction("help", "cf")]
        public static string Help(List<string> args)
        {
            return "Calradia Forge Commands:\n" +
                   "cf.help - Shows this list\n" +
                   "cf.patches - Lists all currently active memory patches\n" +
                   "cf.revert_all - Emergency undo of all Forge patches\n" +
                   "cf.verify_integrity - Checks if other mods broke our patches\n" +
                   "cf.test_log - Tests the ForgeLogger subsystem\n" +
                   "cf.diplomacy.calc_war_score <military> <gold> <activeWars> <tributeRecv> <tributePaid>\n" +
                   "cf.diplomacy.calc_peace_tribute <casInflicted> <casSuffered> <settleTaken> <settleLost>\n" +
                   "cf.casusbelli.eval <attacker> <target> [attStr] [tgtStr] [borderSettles] [hasPact] [raids] [relation]\n" +
                   "cf.settlement.audit_rebellion <loyalty> <militia> <garrison>\n" +
                   "cf.settlement.security_delta <garrison> <militia> <rackets> <banditLair>\n" +
                   "cf.settlement.loyalty_delta <cultureMismatch> <govCultureMatch> <foodSurplus> <taxes> <corruption>\n" +
                   "cf.underworld.calc_alley <thugs> <prosperity> <security>\n" +
                   "cf.underworld.smuggling <purchase> <destination> <tariffRate> <bribe>\n" +
                   "cf.character.calc_learning_rate <attr> <focus> <skill>\n" +
                   "cf.character.succession_score <age> <leadership> <martial> <relation>\n" +
                   "cf.character.perk_role <perkId> <role>\n" +
                   "cf.combat.calc_morale_shock <casualties> <total> <isFlanked> <isCommanderDead>\n" +
                   "cf.siege.breach_chance <trebuchet> <catapult> <wallTier>\n" +
                   "cf.siege.tactics <settlement> [attackers] [defenders] [hasRam] [towers] [breaches]\n" +
                   "cf.trade.price <basePrice> <supply> <demand>\n" +
                   "cf.trade.verify_transfer <currentCount> <requestedAmount>\n" +
                   "cf.trade.workshop <rawCost> <outputPrice> <volume> <wageOverhead>\n" +
                   "cf.forgeweave.status - Displays ForgeWeave extension framework status\n" +
                   "cf.forgeweave.handlers [module] - Lists registered extension handlers\n" +
                   "cf.forgeweave.handler <id> - Inspects detailed telemetry for a handler\n" +
                   "cf.forgeweave.unquarantine <id|all|module:name> - Resets handler quarantine\n" +
                   "cf.forgeweave.replay <sequence> - Replays a retained host event by sequence ID\n" +
                   "cf.forgeweave.journal [limit] - Shows recent ForgeWeave event dispatches\n" +
                   "cf.forgeweave.clear [journal|replays|all] - Clears event journal or replays\n" +
                   "cf.forgeweave.publish <topic> [k1=v1...] - Publishes a custom ForgeWeave event\n" +
                   "cf.rules.audit <moduleFolderPath>\n";
        }

        [CommandLineFunctionality.CommandLineArgumentFunction("patches", "cf")]
        public static string ListPatches(List<string> args)
        {
            var patches = ForgePatcher.GetAppliedPatches();
            if (patches.Count == 0) return "No active patches found.";

            string result = $"Active Patches ({patches.Count}):\n";
            foreach (var p in patches)
            {
                string target = p.Original != null ? $"{p.Original.DeclaringType?.Name}.{p.Original.Name}" : "UnknownTarget";
                string replacement = p.Replacement != null ? $"{p.Replacement.DeclaringType?.Name}.{p.Replacement.Name}" : "UnknownReplacement";
                result += $"- {target} -> {replacement} (Module: {p.SourceModule})\n";
            }
            return result;
        }

        [CommandLineFunctionality.CommandLineArgumentFunction("revert_all", "cf")]
        public static string RevertAll(List<string> args)
        {
            try
            {
                int count = ForgePatcher.GetAppliedPatches().Count;
                ForgePatcher.RevertAll();
                return $"Successfully reverted {count} patches to their original game state.";
            }
            catch (Exception ex)
            {
                return "Error reverting patches: " + ex.Message;
            }
        }

        [CommandLineFunctionality.CommandLineArgumentFunction("verify_integrity", "cf")]
        public static string VerifyIntegrity(List<string> args)
        {
            var broken = ForgePatcher.VerifyIntegrity();
            if (broken.Count == 0) return "Integrity check passed: All patches are intact.";

            string result = $"WARNING: {broken.Count} patches have been overwritten by other frameworks!\n";
            foreach (var p in broken)
            {
                result += $"- Broken: {p.Original?.DeclaringType?.Name}.{p.Original?.Name}\n";
            }
            return result;
        }

        [CommandLineFunctionality.CommandLineArgumentFunction("test_log", "cf")]
        public static string TestLog(List<string> args)
        {
            CalradiaForge.Core.Diagnostics.ForgeLogger.PrintSuccess("This is a success message from ForgeLogger!");
            CalradiaForge.Core.Diagnostics.ForgeLogger.PrintWarning("This is a warning message from ForgeLogger!");
            CalradiaForge.Core.Diagnostics.ForgeLogger.PrintError("This is an error message from ForgeLogger!");
            return "Test logs emitted to the in-game message screen.";
        }

        [CommandLineFunctionality.CommandLineArgumentFunction("calc_war_score", "cf.diplomacy")]
        public static string CalcWarScore(List<string> args)
        {
            if (args.Count < 5) return "Usage: cf.diplomacy.calc_war_score <military> <gold> <activeWars> <tributeRecv> <tributePaid>";
            int.TryParse(args[0], out int mil);
            int.TryParse(args[1], out int gold);
            int.TryParse(args[2], out int wars);
            int.TryParse(args[3], out int recv);
            int.TryParse(args[4], out int paid);

            float score = ForgeApi.Diplomacy.CalculateWarScore(mil, gold, wars, recv, paid);
            return $"AI War Declaration Score: {score:F1} (Higher indicates stronger incentive to declare war)";
        }

        [CommandLineFunctionality.CommandLineArgumentFunction("calc_peace_tribute", "cf.diplomacy")]
        public static string CalcPeaceTribute(List<string> args)
        {
            if (args.Count < 4) return "Usage: cf.diplomacy.calc_peace_tribute <casInflicted> <casSuffered> <settleTaken> <settleLost>";
            int.TryParse(args[0], out int casInf);
            int.TryParse(args[1], out int casSuf);
            int.TryParse(args[2], out int setTak);
            int.TryParse(args[3], out int setLos);

            int tribute = ForgeApi.Diplomacy.CalculatePeaceTribute(casInf, casSuf, setTak, setLos);
            return $"Calculated Daily Peace Tribute: {tribute} denars/day (Positive: enemy pays us, Negative: we pay enemy)";
        }

        [CommandLineFunctionality.CommandLineArgumentFunction("audit_rebellion", "cf.settlement")]
        public static string AuditRebellion(List<string> args)
        {
            if (args.Count < 3) return "Usage: cf.settlement.audit_rebellion <loyalty> <militia> <garrison>";
            float.TryParse(args[0], out float loyalty);
            int.TryParse(args[1], out int militia);
            int.TryParse(args[2], out int garrison);

            var (isCritical, dangerIndex, status) = ForgeApi.Settlements.EvaluateRebellionRisk(loyalty, militia, garrison);
            return $"Rebellion Risk Audit: Status={status}, DangerIndex={dangerIndex:F1}%, Critical={isCritical}";
        }

        [CommandLineFunctionality.CommandLineArgumentFunction("calc_alley", "cf.underworld")]
        public static string CalcAlley(List<string> args)
        {
            if (args.Count < 3) return "Usage: cf.underworld.calc_alley <thugs> <prosperity> <security>";
            int.TryParse(args[0], out int thugs);
            int.TryParse(args[1], out int prosperity);
            float.TryParse(args[2], out float security);

            var (gold, crime) = ForgeApi.Underworld.CalculateAlleyDailyYield(thugs, prosperity, security);
            return $"Alley Racket Yield: +{gold} denars/day, +{crime:F2} daily crime rating";
        }

        [CommandLineFunctionality.CommandLineArgumentFunction("calc_learning_rate", "cf.character")]
        public static string CalcLearningRate(List<string> args)
        {
            if (args.Count < 3) return "Usage: cf.character.calc_learning_rate <attr> <focus> <skill>";
            int.TryParse(args[0], out int attr);
            int.TryParse(args[1], out int focus);
            int.TryParse(args[2], out int skill);

            float rate = ForgeApi.Progression.CalculateLearningRate(attr, focus, skill);
            return $"Skill Learning Rate: {rate:F2}x progression multiplier";
        }

        [CommandLineFunctionality.CommandLineArgumentFunction("calc_morale_shock", "cf.combat")]
        public static string CalcMoraleShock(List<string> args)
        {
            if (args.Count < 4) return "Usage: cf.combat.calc_morale_shock <casualties> <total> <isFlanked> <isCommanderDead>";
            int.TryParse(args[0], out int cas);
            int.TryParse(args[1], out int total);
            bool.TryParse(args[2], out bool isFlanked);
            bool.TryParse(args[3], out bool isCommDead);

            float shock = ForgeApi.Combat.CalculateMoraleShock(cas, total, isFlanked, isCommDead);
            return $"Tactical Morale Shock: -{shock:F1} morale loss inflicted";
        }

        [CommandLineFunctionality.CommandLineArgumentFunction("breach_chance", "cf.siege")]
        public static string BreachChance(List<string> args)
        {
            if (args.Count < 3) return "Usage: cf.siege.breach_chance <trebuchet> <catapult> <wallTier>";
            int.TryParse(args[0], out int treb);
            int.TryParse(args[1], out int cat);
            int.TryParse(args[2], out int tier);

            var (prob, remainingHp) = ForgeCombatTactics.CalculateSiegeWallBreachProbability(treb, cat, tier);
            return $"Siege Breach Progress: Probability={prob * 100f:F1}%, WallRemainingHp={remainingHp:F0}";
        }

        [CommandLineFunctionality.CommandLineArgumentFunction("price", "cf.trade")]
        public static string Price(List<string> args)
        {
            if (args.Count < 3) return "Usage: cf.trade.price <basePrice> <supply> <demand>";
            int.TryParse(args[0], out int basePrice);
            int.TryParse(args[1], out int supply);
            int.TryParse(args[2], out int demand);

            int price = ForgeApi.Trade.CalculatePriceBySupplyDemand(basePrice, supply, demand);
            return $"Dynamic Market Price: {price} denars (Base: {basePrice}, Supply: {supply}, Demand: {demand})";
        }

        [CommandLineFunctionality.CommandLineArgumentFunction("verify_transfer", "cf.trade")]
        public static string VerifyTransfer(List<string> args)
        {
            if (args.Count < 2) return "Usage: cf.trade.verify_transfer <currentCount> <requestedAmount>";
            int.TryParse(args[0], out int current);
            int.TryParse(args[1], out int req);

            var (isValid, allowed, reason) = ForgeApi.Trade.ValidateItemTransfer(current, req);
            return $"Underflow Check: Valid={isValid}, AllowedTransfer={allowed}, Reason: {reason}";
        }

        [CommandLineFunctionality.CommandLineArgumentFunction("audit", "cf.rules")]
        public static string AuditRules(List<string> args)
        {
            string path = args.Count > 0 ? string.Join(" ", args) : ".";
            var result = ModRuleAuditor.Audit(path);

            string summary = $"Rule Audit Results for '{path}': Passed={result.Passed}, Total Findings={result.Findings.Count}\n";
            foreach (var f in result.Findings)
            {
                summary += $"[{f.Severity}] {f.RuleId}: {f.Description}\n -> Rec: {f.Recommendation}\n";
            }
            return summary;
        }

        [CommandLineFunctionality.CommandLineArgumentFunction("eval", "cf.casusbelli")]
        public static string EvalCasusBelli(List<string> args)
        {
            if (args == null || args.Count < 2) return "Usage: cf.casusbelli.eval <attacker> <target> [attStr] [tgtStr] [borderSettles] [hasPact] [raids] [relation]";
            string attacker = args[0];
            string target = args[1];
            int attStr = args.Count > 2 && int.TryParse(args[2], out int aStr) ? aStr : 5000;
            int tgtStr = args.Count > 3 && int.TryParse(args[3], out int tStr) ? tStr : 3500;
            int borders = args.Count > 4 && int.TryParse(args[4], out int b) ? b : 3;
            bool hasPact = args.Count > 5 && bool.TryParse(args[5], out bool p) ? p : false;
            int raids = args.Count > 6 && int.TryParse(args[6], out int r) ? r : 1;
            int relation = args.Count > 7 && int.TryParse(args[7], out int rel) ? rel : -15;

            var analysis = ForgeCasusBelliEngine.EvaluateWarJustification(attacker, target, attStr, tgtStr, borders, hasPact, raids, relation);
            return analysis.Summary;
        }

        [CommandLineFunctionality.CommandLineArgumentFunction("tactics", "cf.siege")]
        public static string SiegeTactics(List<string> args)
        {
            if (args == null || args.Count < 1) return "Usage: cf.siege.tactics <settlement> [attackers] [defenders] [hasRam] [towers] [breaches]";
            string settlement = args[0];
            int attackers = args.Count > 1 && int.TryParse(args[1], out int att) ? att : 500;
            int defenders = args.Count > 2 && int.TryParse(args[2], out int def) ? def : 250;
            bool hasRam = args.Count > 3 && bool.TryParse(args[3], out bool ram) ? ram : true;
            int towers = args.Count > 4 && int.TryParse(args[4], out int tow) ? tow : 1;
            int breaches = args.Count > 5 && int.TryParse(args[5], out int br) ? br : 0;

            var analysis = ForgeSiegeTactician.AnalyzeAssaultTactics(settlement, attackers, defenders, hasRam, towers, breaches);
            return $"Siege Assault Analysis for {analysis.SettlementName}:\n" +
                   $"• Force: {analysis.AttackerStrength} vs {analysis.DefenderStrength} | Breakthrough: {(analysis.BreakthroughProbability * 100):F1}%\n" +
                   $"• Casualties: Attacker {(analysis.AttackerExpectedCasualtyRate * 100):F1}% | Defender {(analysis.DefenderExpectedCasualtyRate * 100):F1}%\n" +
                   $"• Tactical Advice: {analysis.TacticalAdvice}\n" +
                   $"• Navmeshes: {string.Join(", ", analysis.NavmeshRequirements)}";
        }

        [CommandLineFunctionality.CommandLineArgumentFunction("workshop", "cf.trade")]
        public static string CalcWorkshop(List<string> args)
        {
            if (args == null || args.Count < 4) return "Usage: cf.trade.workshop <rawCost> <outputPrice> <volume> <wageOverhead>";
            int.TryParse(args[0], out int rawCost);
            int.TryParse(args[1], out int outPrice);
            int.TryParse(args[2], out int volume);
            int.TryParse(args[3], out int wage);

            int netDaily = ForgeApi.Trade.CalculateWorkshopDailyNet(rawCost, outPrice, volume, wage);
            return $"Workshop Economics: RawCost={rawCost}, OutPrice={outPrice}, Volume={volume}, WageOverhead={wage} -> Daily Net: {netDaily:+0;-0} denars/day";
        }

        [CommandLineFunctionality.CommandLineArgumentFunction("smuggling", "cf.underworld")]
        public static string CalcSmuggling(List<string> args)
        {
            if (args == null || args.Count < 4) return "Usage: cf.underworld.smuggling <purchase> <destination> <tariffRate> <bribe>";
            int.TryParse(args[0], out int purchase);
            int.TryParse(args[1], out int destination);
            float.TryParse(args[2], out float tariff);
            float.TryParse(args[3], out float bribe);

            var (netProfit, risk) = ForgeApi.Underworld.CalculateSmugglingMargin(purchase, destination, tariff, bribe);
            return $"Smuggling Run Margin: Purchase={purchase}, Dest={destination}, Tariff={tariff:P0}, Bribe={bribe} -> Net Profit: +{netProfit} denars/unit | Risk Factor: {risk:P1}";
        }

        [CommandLineFunctionality.CommandLineArgumentFunction("succession_score", "cf.character")]
        public static string CalcSuccessionScore(List<string> args)
        {
            if (args == null || args.Count < 4) return "Usage: cf.character.succession_score <age> <leadership> <martial> <relation>";
            int.TryParse(args[0], out int age);
            int.TryParse(args[1], out int leadership);
            int.TryParse(args[2], out int martial);
            int.TryParse(args[3], out int relation);

            float score = ForgeProgressionSystem.EvaluateDynasticSuccession(age, leadership, martial, relation);
            return $"Dynastic Succession Score: Age={age}, Lead={leadership}, Martial={martial}, Rel={relation} -> Fitness Score: {score:F1} pts";
        }

        [CommandLineFunctionality.CommandLineArgumentFunction("perk_role", "cf.character")]
        public static string CheckPerkRole(List<string> args)
        {
            if (args == null || args.Count < 2) return "Usage: cf.character.perk_role <perkId> <role>";
            string perkId = args[0];
            string role = args[1];

            bool applies = ForgeApi.Progression.DoesPerkApplyToRole(perkId, role);
            return $"Perk Role Evaluation: Perk '{perkId}' for Role '{role}' -> Applies: {applies}";
        }

        [CommandLineFunctionality.CommandLineArgumentFunction("security_delta", "cf.settlement")]
        public static string CalcSecurityDelta(List<string> args)
        {
            if (args == null || args.Count < 4) return "Usage: cf.settlement.security_delta <garrison> <militia> <rackets> <banditLair>";
            int.TryParse(args[0], out int garrison);
            int.TryParse(args[1], out int militia);
            int.TryParse(args[2], out int rackets);
            bool.TryParse(args[3], out bool banditLair);

            float delta = ForgeApi.Settlements.CalculateDailySecurityDelta(garrison, militia, rackets, banditLair);
            return $"Settlement Daily Security Delta: {delta:+0.00;-0.00}/day (Garrison={garrison}, Militia={militia}, Rackets={rackets}, BanditLair={banditLair})";
        }

        [CommandLineFunctionality.CommandLineArgumentFunction("loyalty_delta", "cf.settlement")]
        public static string CalcLoyaltyDelta(List<string> args)
        {
            if (args == null || args.Count < 5) return "Usage: cf.settlement.loyalty_delta <cultureMismatch> <govCultureMatch> <foodSurplus> <taxes> <corruption>";
            bool.TryParse(args[0], out bool mismatch);
            bool.TryParse(args[1], out bool govMatch);
            int.TryParse(args[2], out int food);
            int.TryParse(args[3], out int taxes);
            int.TryParse(args[4], out int corrupt);

            float delta = ForgeApi.Settlements.CalculateDailyLoyaltyDelta(mismatch, govMatch, food, taxes, corrupt);
            return $"Settlement Daily Loyalty Delta: {delta:+0.00;-0.00}/day (Mismatch={mismatch}, GovMatch={govMatch}, FoodSurplus={food}, Taxes={taxes}, Corruption={corrupt})";
        }

        [CommandLineFunctionality.CommandLineArgumentFunction("status", "cf.forgeweave")]
        public static string ForgeWeaveStatus(List<string> args)
        {
            var runtime = SubModule.CurrentRuntime;
            if (runtime == null) return "Calradia Forge runtime is not active in this session.";
            var snapshot = runtime.TestEngine.CaptureForgeWeave();
            return $"ForgeWeave Status: {snapshot.Status}\n" +
                   $"• Handlers: {snapshot.HandlerCount} Total | {snapshot.ReadyHandlerCount} Ready | {snapshot.BlockedHandlerCount} Blocked | {snapshot.QuarantinedHandlerCount} Quarantined\n" +
                   $"• Telemetry: {snapshot.DispatchCount} Dispatches | {snapshot.InvocationCount} Invocations | {snapshot.FailureCount} Failures | {snapshot.BudgetExceededCount} Overruns\n" +
                   $"• Timing: Mean {snapshot.MeanMilliseconds:F2} ms | Max {snapshot.MaxMilliseconds:F2} ms\n" +
                   $"• Replay Lab: {snapshot.ReplayRecordCount} Retained | {snapshot.ReplayAttemptCount} Attempts | {snapshot.ReplaySuccessCount} Success | {snapshot.ReplayRejectedCount} Rejected";
        }

        [CommandLineFunctionality.CommandLineArgumentFunction("handlers", "cf.forgeweave")]
        public static string ForgeWeaveHandlers(List<string> args)
        {
            var runtime = SubModule.CurrentRuntime;
            if (runtime == null) return "Calradia Forge runtime is not active in this session.";
            var snapshot = runtime.TestEngine.CaptureForgeWeave();
            string filterModule = args != null && args.Count > 0 ? args[0].Trim() : null;
            var list = snapshot.Handlers ?? new List<ForgeWeaveHandlerHealth>();
            if (!string.IsNullOrEmpty(filterModule))
                list = list.Where(h => (h.Module ?? "").IndexOf(filterModule, StringComparison.OrdinalIgnoreCase) >= 0).ToList();

            if (list.Count == 0) return "No ForgeWeave handlers found" + (filterModule != null ? $" matching '{filterModule}'." : ".");

            string res = $"ForgeWeave Handlers ({list.Count}):\n";
            foreach (var h in list)
            {
                res += $"[{h.Status}] {h.Id} ({h.Module}) -> Event: {h.Event}, Priority: {h.Priority}, Calls: {h.InvocationCount}, Fails: {h.FailureCount}, Mean: {h.MeanMilliseconds:F2}ms, Min: {h.MinMilliseconds:F2}ms, Max: {h.MaxMilliseconds:F2}ms\n";
            }
            return res;
        }

        [CommandLineFunctionality.CommandLineArgumentFunction("handler", "cf.forgeweave")]
        public static string ForgeWeaveHandler(List<string> args)
        {
            if (args == null || args.Count < 1) return "Usage: cf.forgeweave.handler <id>";
            var runtime = SubModule.CurrentRuntime;
            if (runtime == null) return "Calradia Forge runtime is not active in this session.";
            string id = args[0].Trim();
            var h = runtime.TestEngine.GetForgeWeaveHandlerHealth(id);
            if (h == null) return $"ForgeWeave handler '{id}' not found.";

            string info = $"ForgeWeave Handler '{h.Id}':\n" +
                          $"• Module: {h.Module} | Name: {h.Name}\n" +
                          $"• Event: {h.Event}" + (!string.IsNullOrEmpty(h.Topic) ? $" (Topic: {h.Topic})" : "") + $" | Context: {h.Context} | Access: {h.Access} | ReplayMode: {h.ReplayMode}\n" +
                          $"• Status: {h.Status} (Circuit: {h.CircuitState}) | Priority: {h.Priority} | FailureLimit: {h.FailureLimit}\n" +
                          $"• Execution: {h.InvocationCount} Calls | {h.FailureCount} Failures | {h.BudgetExceededCount} Overruns\n" +
                          $"• Timing: Min {h.MinMilliseconds:F2} ms | Mean {h.MeanMilliseconds:F2} ms | Max {h.MaxMilliseconds:F2} ms | Last {h.LastMilliseconds:F2} ms\n" +
                          $"• Latency APM: P50: {h.P50Milliseconds:F2} ms | P95: {h.P95Milliseconds:F2} ms | P99: {h.P99Milliseconds:F2} ms\n" +
                          $"• Histogram: <1ms: {h.BucketUnder1Ms} | 1-5ms: {h.Bucket1To5Ms} | 5-20ms: {h.Bucket5To20Ms} | >20ms: {h.BucketOver20Ms}\n" +
                          $"• Last Outcome: {h.LastOutcome}\n";
            if (!string.IsNullOrEmpty(h.LastError)) info += $"• Last Error: {h.LastError}\n";
            if (!string.IsNullOrEmpty(h.BlockingReason)) info += $"• Blocking Reason: {h.BlockingReason}\n";
            if (h.Filter?.RequiredData != null && h.Filter.RequiredData.Count > 0)
                info += $"• Required Filter: {string.Join(", ", h.Filter.RequiredData.Select(p => p.Key + "=" + p.Value))}\n";
            if (h.Filter?.ExcludedData != null && h.Filter.ExcludedData.Count > 0)
                info += $"• Excluded Filter: {string.Join(", ", h.Filter.ExcludedData.Select(p => p.Key + "!=" + p.Value))}\n";
            if (h.Before != null && h.Before.Count > 0) info += $"• Before: {string.Join(", ", h.Before)}\n";
            if (h.After != null && h.After.Count > 0) info += $"• After: {string.Join(", ", h.After)}\n";
            return info;
        }

        [CommandLineFunctionality.CommandLineArgumentFunction("unquarantine", "cf.forgeweave")]
        public static string ForgeWeaveUnquarantine(List<string> args)
        {
            if (args == null || args.Count < 1) return "Usage: cf.forgeweave.unquarantine <id|all|module:name>";
            var runtime = SubModule.CurrentRuntime;
            if (runtime == null) return "Calradia Forge runtime is not active in this session.";
            string target = args[0].Trim();
            if (string.Equals(target, "all", StringComparison.OrdinalIgnoreCase))
            {
                int count = runtime.TestEngine.ResetAllForgeWeaveQuarantines();
                return $"Quarantine reset for {count} handler(s).";
            }
            if (target.StartsWith("module:", StringComparison.OrdinalIgnoreCase))
            {
                string modName = target.Substring(7).Trim();
                int count = runtime.TestEngine.ResetModuleForgeWeaveQuarantines(modName);
                return $"Quarantine reset for {count} handler(s) in module '{modName}'.";
            }
            bool ok = runtime.TestEngine.ResetForgeWeaveQuarantine(target);
            return ok ? $"Quarantine reset for handler '{target}'." : $"Handler '{target}' not found or not quarantined.";
        }

        [CommandLineFunctionality.CommandLineArgumentFunction("replay", "cf.forgeweave")]
        public static string ForgeWeaveReplay(List<string> args)
        {
            if (args == null || args.Count < 1) return "Usage: cf.forgeweave.replay <sequence>";
            var runtime = SubModule.CurrentRuntime;
            if (runtime == null) return "Calradia Forge runtime is not active in this session.";
            if (!long.TryParse(args[0].Trim(), out long sequence) || sequence < 1) return "Invalid sequence ID. Must be a positive integer.";

            var replay = runtime.TestEngine.ReplayForgeEvent(sequence, runtime, CancellationToken.None);
            string res = $"Replay Result for Sequence #{replay.SourceSequence}:\n" +
                         $"• Status: {replay.Status} (Replayed: {replay.Replayed})\n" +
                         $"• Timing: {replay.Milliseconds:F2} ms\n" +
                         $"• Handlers: {replay.InvokedCount} Invoked | {replay.SkippedCount} Skipped | {replay.FailureCount} Failures | {replay.BudgetExceededCount} Overruns";
            if (!string.IsNullOrEmpty(replay.RejectionReason)) res += $"\n• Rejection Reason: {replay.RejectionReason}";
            return res;
        }

        [CommandLineFunctionality.CommandLineArgumentFunction("journal", "cf.forgeweave")]
        public static string ForgeWeaveJournal(List<string> args)
        {
            var runtime = SubModule.CurrentRuntime;
            if (runtime == null) return "Calradia Forge runtime is not active in this session.";
            int limit = args != null && args.Count > 0 && int.TryParse(args[0], out int lim) ? lim : 10;
            var snapshot = runtime.TestEngine.CaptureForgeWeave();
            var entries = (snapshot.RecentDispatches ?? new List<ForgeWeaveDispatchRecord>()).Take(limit).ToList();
            if (entries.Count == 0) return "ForgeWeave dispatch journal is empty.";

            string res = $"Recent ForgeWeave Dispatches ({entries.Count}):\n";
            foreach (var d in entries)
            {
                string replayMark = d.IsReplay ? $" [Replay of #{d.SourceSequence}]" : "";
                res += $"• #{d.Sequence} {d.Event} ({d.Context}){replayMark} -> {d.Status} ({d.Milliseconds:F2}ms, Invoked={d.InvokedCount}, Skipped={d.SkippedCount}, Failed={d.FailureCount})\n";
            }
            return res;
        }

        [CommandLineFunctionality.CommandLineArgumentFunction("clear", "cf.forgeweave")]
        public static string ForgeWeaveClear(List<string> args)
        {
            var runtime = SubModule.CurrentRuntime;
            if (runtime == null) return "Calradia Forge runtime is not active in this session.";
            string target = args != null && args.Count > 0 ? args[0].Trim().ToLowerInvariant() : "all";
            if (target == "journal")
            {
                runtime.TestEngine.ClearForgeWeaveJournal();
                return "ForgeWeave dispatch journal cleared.";
            }
            if (target == "replays")
            {
                runtime.TestEngine.ClearForgeWeaveReplays();
                return "ForgeWeave retained replays cleared.";
            }
            runtime.TestEngine.ClearForgeWeaveJournal();
            runtime.TestEngine.ClearForgeWeaveReplays();
            return "ForgeWeave journal and replays cleared.";
        }

        [CommandLineFunctionality.CommandLineArgumentFunction("publish", "cf.forgeweave")]
        public static string ForgeWeavePublish(List<string> args)
        {
            if (args == null || args.Count < 1) return "Usage: cf.forgeweave.publish <topic> [k1=v1 k2=v2...]";
            string topic = args[0].Trim();
            var data = new Dictionary<string, string>();
            for (int i = 1; i < args.Count; i++)
            {
                var part = args[i];
                int eq = part.IndexOf('=');
                if (eq > 0)
                {
                    string k = part.Substring(0, eq).Trim();
                    string v = part.Substring(eq + 1).Trim();
                    if (!string.IsNullOrEmpty(k)) data[k] = v;
                }
            }
            bool published = ForgeApi.PublishCustomEvent(topic, data);
            return published
                ? $"Published custom event to topic '{topic}' with {data.Count} data pair(s)."
                : $"Event '{topic}' published, but no active handlers accepted or matched the event.";
        }
    }
}
