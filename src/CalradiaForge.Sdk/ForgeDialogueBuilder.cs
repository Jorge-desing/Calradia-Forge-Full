using System;
using System.Collections.Generic;
using System.Linq;

namespace CalradiaForge.Sdk
{
    public enum DialogueSpeaker
    {
        Player,
        Npc
    }

    public sealed class ForgeDialogueLine
    {
        public string Id { get; }
        public DialogueSpeaker Speaker { get; }
        public string InputToken { get; }
        public string OutputToken { get; }
        public string Text { get; }
        public int Priority { get; }
        public Func<bool> Condition { get; }
        public Action Consequence { get; }

        public ForgeDialogueLine(
            string id,
            DialogueSpeaker speaker,
            string inputToken,
            string outputToken,
            string text,
            int priority = 110,
            Func<bool> condition = null,
            Action consequence = null)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("Dialogue line ID cannot be empty.", nameof(id));
            if (string.IsNullOrWhiteSpace(inputToken)) throw new ArgumentException("InputToken cannot be empty.", nameof(inputToken));
            if (string.IsNullOrWhiteSpace(outputToken)) throw new ArgumentException("OutputToken cannot be empty.", nameof(outputToken));

            Id = id.Trim();
            Speaker = speaker;
            InputToken = inputToken.Trim();
            OutputToken = outputToken.Trim();
            Text = text ?? string.Empty;
            Priority = priority;
            Condition = condition;
            Consequence = consequence;
        }
    }

    /// <summary>
    /// Fluent Dialogue Tree Builder and Token State Machine Validator.
    /// Enforces Bannerlord dialogue rules (token chaining, root tokens, priority 110, terminal close_window/hero_main_options).
    /// </summary>
    public sealed class ForgeDialogueBuilder
    {
        private readonly string _dialogueId;
        private readonly List<ForgeDialogueLine> _lines = new List<ForgeDialogueLine>();
        private string _currentToken;

        public static readonly string[] StandardRootTokens = { "start", "hero_main_options", "lord_talk_ask_something_2" };
        public static readonly string[] StandardTerminalTokens = { "close_window", "hero_main_options" };

        public ForgeDialogueBuilder(string dialogueId, string rootToken = "hero_main_options")
        {
            if (string.IsNullOrWhiteSpace(dialogueId)) throw new ArgumentException("Dialogue ID cannot be empty.", nameof(dialogueId));
            _dialogueId = dialogueId.Trim();
            _currentToken = string.IsNullOrWhiteSpace(rootToken) ? "hero_main_options" : rootToken.Trim();
        }

        public static ForgeDialogueBuilder Create(string dialogueId, string rootToken = "hero_main_options")
        {
            return new ForgeDialogueBuilder(dialogueId, rootToken);
        }

        public ForgeDialogueBuilder PlayerLine(
            string lineId,
            string text,
            string nextToken,
            int priority = 110,
            Func<bool> condition = null,
            Action consequence = null)
        {
            _lines.Add(new ForgeDialogueLine(lineId, DialogueSpeaker.Player, _currentToken, nextToken, text, priority, condition, consequence));
            _currentToken = nextToken;
            return this;
        }

        public ForgeDialogueBuilder NpcReply(
            string lineId,
            string text,
            string nextToken,
            int priority = 110,
            Func<bool> condition = null,
            Action consequence = null)
        {
            _lines.Add(new ForgeDialogueLine(lineId, DialogueSpeaker.Npc, _currentToken, nextToken, text, priority, condition, consequence));
            _currentToken = nextToken;
            return this;
        }

        public ForgeDialogueBuilder Branch(
            string lineId,
            DialogueSpeaker speaker,
            string inputToken,
            string text,
            string nextToken,
            int priority = 110,
            Func<bool> condition = null,
            Action consequence = null)
        {
            _lines.Add(new ForgeDialogueLine(lineId, speaker, inputToken, nextToken, text, priority, condition, consequence));
            return this;
        }

        /// <summary>
        /// Validates that all tokens are chained properly without orphan tokens or unreachable dead ends.
        /// </summary>
        public List<string> Validate()
        {
            var errors = new List<string>();

            if (_lines.Count == 0)
            {
                errors.Add($"Dialogue '{_dialogueId}' contains no lines.");
                return errors;
            }

            // Check for duplicate line IDs
            var lineIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var line in _lines)
            {
                if (!lineIds.Add(line.Id))
                {
                    errors.Add($"Duplicate dialogue line ID '{line.Id}' in dialogue '{_dialogueId}'.");
                }
            }

            // Check that output tokens lead to another input token or standard terminal tokens
            var inputTokens = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < _lines.Count; i++)
            {
                inputTokens.Add(_lines[i].InputToken);
            }
            foreach (var root in StandardRootTokens) inputTokens.Add(root);
            foreach (var terminal in StandardTerminalTokens) inputTokens.Add(terminal);

            foreach (var line in _lines)
            {
                if (!inputTokens.Contains(line.OutputToken))
                {
                    errors.Add(
                        $"Dialogue line '{line.Id}' has output token '{line.OutputToken}' which is neither a terminal token ('close_window', 'hero_main_options') nor matched by any line's input token.");
                }
            }

            return errors;
        }

        public IReadOnlyList<ForgeDialogueLine> Build()
        {
            var validationErrors = Validate();
            if (validationErrors.Count > 0)
            {
                throw new InvalidOperationException(
                    $"Dialogue validation failed for '{_dialogueId}':\n" + string.Join("\n", validationErrors));
            }
            return _lines.AsReadOnly();
        }
    }
}
