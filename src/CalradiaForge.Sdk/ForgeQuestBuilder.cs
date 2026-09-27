using System;
using System.Collections.Generic;
using System.Text;

namespace CalradiaForge.Sdk
{
    /// <summary>
    /// Scaffolds Bannerlord QuestBase and dialogue flow implementations.
    /// Strictly complies with the double SetDialogs() rule (constructor and InitializeQuestOnGameLoad)
    /// and save system isolation with unique base ID allocation (>= 2,500,000).
    /// </summary>
    public sealed class ForgeQuestBuilder
    {
        public string Namespace { get; private set; } = "MyMod.QuestBehaviors";
        public string ClassName { get; private set; } = "CustomEscortQuest";
        public string QuestId { get; private set; } = "custom_escort_quest";
        public string QuestTitle { get; private set; } = "Caravan Escort Duty";
        public int SaveableTypeId { get; private set; } = 2500000;

        private readonly List<(string Type, string Name, int FieldId)> _fields = new List<(string, string, int)>();

        public static ForgeQuestBuilder Create(string questId, string className)
        {
            if (string.IsNullOrWhiteSpace(className)) throw new ArgumentException("Class name cannot be empty.", nameof(className));
            if (className.Equals("Campaign", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Class name cannot be 'Campaign' as it shadows TaleWorlds.CampaignSystem.Campaign (GEMINI.md).");
            return new ForgeQuestBuilder
            {
                QuestId = questId,
                ClassName = className
            };
        }

        public ForgeQuestBuilder WithNamespace(string ns)
        {
            if (!string.IsNullOrWhiteSpace(ns))
            {
                var parts = ns.Split('.');
                foreach (var part in parts)
                {
                    if (part.Equals("Campaign", StringComparison.OrdinalIgnoreCase))
                        throw new InvalidOperationException($"Namespace segment '{part}' violates GEMINI.md anti-shadowing rule. Do not use 'Campaign'.");
                }
                Namespace = ns;
            }
            return this;
        }

        public ForgeQuestBuilder WithTitle(string title)
        {
            if (!string.IsNullOrWhiteSpace(title)) QuestTitle = title;
            return this;
        }

        public ForgeQuestBuilder WithSaveableTypeId(int typeId)
        {
            if (typeId < 2500000)
                throw new ArgumentOutOfRangeException(nameof(typeId), "SaveableTypeId must be >= 2,500,000 to prevent collisions with TaleWorlds and other mods.");
            SaveableTypeId = typeId;
            return this;
        }

        public ForgeQuestBuilder AddSaveableField(string type, string name, int fieldId)
        {
            _fields.Add((type, name, fieldId));
            return this;
        }

        public string BuildCSharpCode()
        {
            var sb = new StringBuilder();
            sb.AppendLine("using System;");
            sb.AppendLine("using System.Collections.Generic;");
            sb.AppendLine("using TaleWorlds.CampaignSystem;");
            sb.AppendLine("using TaleWorlds.CampaignSystem.Actions;");
            sb.AppendLine("using TaleWorlds.CampaignSystem.Conversation;");
            sb.AppendLine("using TaleWorlds.CampaignSystem.Party;");
            sb.AppendLine("using TaleWorlds.CampaignSystem.Settlements;");
            sb.AppendLine("using TaleWorlds.Core;");
            sb.AppendLine("using TaleWorlds.Localization;");
            sb.AppendLine("using TaleWorlds.SaveSystem;");
            sb.AppendLine();
            sb.AppendLine($"namespace {Namespace}");
            sb.AppendLine("{");
            sb.AppendLine($"    public class {ClassName} : QuestBase");
            sb.AppendLine("    {");

            foreach (var field in _fields)
            {
                sb.AppendLine($"        [SaveableField({field.FieldId})]");
                sb.AppendLine($"        private {field.Type} {field.Name};");
                sb.AppendLine();
            }

            sb.AppendLine($"        public override TextObject Title => new TextObject(\"{{={QuestId}_title}}{QuestTitle}\");");
            sb.AppendLine("        public override bool IsRemainingTimeHidden => false;");
            sb.AppendLine();
            sb.AppendLine($"        public {ClassName}(string questId, Hero questGiver, CampaignTime duration, int rewardGold)");
            sb.AppendLine("            : base(questId, questGiver, duration, rewardGold)");
            sb.AppendLine("        {");
            sb.AppendLine("            // CRITICAL: Double SetDialogs() rule (1 of 2: On construction)");
            sb.AppendLine("            SetDialogs();");
            sb.AppendLine("            InitializeQuestOnCreation();");
            sb.AppendLine("        }");
            sb.AppendLine();
            sb.AppendLine("        public override void InitializeQuestOnGameLoad()");
            sb.AppendLine("        {");
            sb.AppendLine("            // CRITICAL: Double SetDialogs() rule (2 of 2: On save restoration)");
            sb.AppendLine("            SetDialogs();");
            sb.AppendLine("        }");
            sb.AppendLine();
            sb.AppendLine("        protected override void SetDialogs()");
            sb.AppendLine("        {");
            sb.AppendLine($"            OfferDialogFlow = DialogFlow.CreateDialogFlow(\"{QuestId}_offer\", 110)");
            sb.AppendLine($"                .NpcLine(\"Greetings. I have a matter requiring an capable commander.\")");
            sb.AppendLine($"                    .Condition(() => Hero.OneToOneConversationHero == QuestGiver)");
            sb.AppendLine($"                .PlayerLine(\"I can assist you.\")");
            sb.AppendLine($"                .NpcLine(\"Splendid. May fortune favor your blade.\")");
            sb.AppendLine($"                    .Consequence(() => StartQuest());");
            sb.AppendLine("        }");
            sb.AppendLine();
            sb.AppendLine("        protected override void OnStartQuest() { }");
            sb.AppendLine("        protected override void OnCompleteQuest() { }");
            sb.AppendLine("        protected override void RegisterEvents() { }");
            sb.AppendLine("        protected override void HourlyTick() { }");
            sb.AppendLine("    }");
            sb.AppendLine();
            sb.AppendLine($"    public class {ClassName}TypeDefiner : SaveableTypeDefiner");
            sb.AppendLine("    {");
            sb.AppendLine($"        public {ClassName}TypeDefiner() : base({SaveableTypeId}) {{ }}");
            sb.AppendLine();
            sb.AppendLine("        protected override void DefineClassTypes()");
            sb.AppendLine("        {");
            sb.AppendLine($"            AddClassDefinition(typeof({ClassName}), 1);");
            sb.AppendLine("        }");
            sb.AppendLine("    }");
            sb.AppendLine("}");
            return sb.ToString();
        }
    }
}
