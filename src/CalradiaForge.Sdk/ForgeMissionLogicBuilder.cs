using System;
using System.Text;

namespace CalradiaForge.Sdk
{
    /// <summary>
    /// Scaffolds Bannerlord MissionLogic implementations adhering strictly to bannerlord_mission_lifecycle rules:
    /// 1. Defers mesh, skeleton, and physics manipulations away from OnInit() into OnMissionTick() with an _isInitialized guard.
    /// 2. Handles Unanimity (IsAgentInteractionAllowed) and First-Wins (MissionEnded, OnEndMissionRequest) hooks safely.
    /// </summary>
    public sealed class ForgeMissionLogicBuilder
    {
        public string Namespace { get; private set; } = "MyMod.Missions";
        public string ClassName { get; private set; } = "CustomMissionLogic";
        public bool IncludeAgentInteractionHook { get; private set; } = true;
        public bool IncludeMissionEndHook { get; private set; } = false;

        public static ForgeMissionLogicBuilder Create(string className)
        {
            if (string.IsNullOrWhiteSpace(className))
                throw new ArgumentException("Class name cannot be empty.", nameof(className));
            if (className.Equals("Campaign", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Class name cannot be 'Campaign' as it shadows TaleWorlds.CampaignSystem.Campaign (GEMINI.md).");

            return new ForgeMissionLogicBuilder
            {
                ClassName = className
            };
        }

        public ForgeMissionLogicBuilder WithNamespace(string ns)
        {
            if (!string.IsNullOrWhiteSpace(ns))
            {
                var parts = ns.Split('.');
                for (int i = 0; i < parts.Length; i++)
                {
                    if (parts[i].Equals("Campaign", StringComparison.OrdinalIgnoreCase))
                        throw new InvalidOperationException($"Namespace segment '{parts[i]}' violates GEMINI.md anti-shadowing rule. Do not use 'Campaign'.");
                }
                Namespace = ns;
            }
            return this;
        }

        public ForgeMissionLogicBuilder WithAgentInteractionHook(bool include)
        {
            IncludeAgentInteractionHook = include;
            return this;
        }

        public ForgeMissionLogicBuilder WithMissionEndHook(bool include)
        {
            IncludeMissionEndHook = include;
            return this;
        }

        public string BuildCSharpCode()
        {
            var sb = new StringBuilder();
            sb.AppendLine("using System;");
            sb.AppendLine("using TaleWorlds.Core;");
            sb.AppendLine("using TaleWorlds.Engine;");
            sb.AppendLine("using TaleWorlds.Library;");
            sb.AppendLine("using TaleWorlds.MountAndBlade;");
            sb.AppendLine();
            sb.AppendLine($"namespace {Namespace}");
            sb.AppendLine("{");
            sb.AppendLine("    /// <summary>");
            sb.AppendLine($"    /// Mission logic implementation for {ClassName}.");
            sb.AppendLine("    /// Adheres to TaleWorlds mission lifecycle constraints:");
            sb.AppendLine("    /// Skeletons and meshes MUST NOT be manipulated during OnInit(); deferred to OnMissionTick.");
            sb.AppendLine("    /// </summary>");
            sb.AppendLine($"    public class {ClassName} : MissionLogic");
            sb.AppendLine("    {");
            sb.AppendLine("        private bool _isInitialized;");
            sb.AppendLine();
            sb.AppendLine("        public override void OnBehaviorInitialize()");
            sb.AppendLine("        {");
            sb.AppendLine("            base.OnBehaviorInitialize();");
            sb.AppendLine("            // Register non-scene data structures and basic event listeners here.");
            sb.AppendLine("        }");
            sb.AppendLine();
            sb.AppendLine("        public override void OnMissionTick(float dt)");
            sb.AppendLine("        {");
            sb.AppendLine("            base.OnMissionTick(dt);");
            sb.AppendLine();
            sb.AppendLine("            // CRITICAL ENGINE RULE: Meshes, skeletons, and physics components cannot");
            sb.AppendLine("            // be created or attached in OnInit(). We defer to the first OnMissionTick call.");
            sb.AppendLine("            if (!_isInitialized)");
            sb.AppendLine("            {");
            sb.AppendLine("                InitializeMissionSceneComponents();");
            sb.AppendLine("                _isInitialized = true;");
            sb.AppendLine("            }");
            sb.AppendLine("        }");
            sb.AppendLine();
            sb.AppendLine("        private void InitializeMissionSceneComponents()");
            sb.AppendLine("        {");
            sb.AppendLine("            // Safe scene component, mesh attachment, and agent visual initialization");
            sb.AppendLine("        }");

            if (IncludeAgentInteractionHook)
            {
                sb.AppendLine();
                sb.AppendLine("        // UNANIMITY HOOK: All active MissionLogics must return true for interaction to succeed.");
                sb.AppendLine("        // Never return false blindly as it globally blocks agent interaction.");
                sb.AppendLine("        public override bool IsAgentInteractionAllowed()");
                sb.AppendLine("        {");
                sb.AppendLine("            return true;");
                sb.AppendLine("        }");
            }

            if (IncludeMissionEndHook)
            {
                sb.AppendLine();
                sb.AppendLine("        // FIRST-WINS HOOK: The first MissionLogic returning true will immediately conclude the mission.");
                sb.AppendLine("        // Only return true when your mod explicitly intends to terminate the combat/scene.");
                sb.AppendLine("        public override bool MissionEnded(ref MissionResult missionResult)");
                sb.AppendLine("        {");
                sb.AppendLine("            return false;");
                sb.AppendLine("        }");
            }

            sb.AppendLine("    }");
            sb.AppendLine("}");

            return sb.ToString();
        }
    }
}
