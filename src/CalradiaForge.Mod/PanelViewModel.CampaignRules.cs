using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using TaleWorlds.Library;

namespace CalradiaForge.Mod
{
    internal sealed partial class PanelViewModel
    {
        private readonly MBBindingList<CampaignRuleItemVM> _campaignRuleBuilderRules = new MBBindingList<CampaignRuleItemVM>();
        private readonly MBBindingList<CampaignRuleConditionItemVM> _campaignRuleBuilderConditionsA = new MBBindingList<CampaignRuleConditionItemVM>();
        private readonly MBBindingList<CampaignRuleConditionItemVM> _campaignRuleBuilderConditionsB = new MBBindingList<CampaignRuleConditionItemVM>();
        private readonly Dictionary<string, string> _campaignRuleBuilderAmountEdits = new Dictionary<string, string>(StringComparer.Ordinal);
        private CampaignRuleBuilderDraft _campaignRuleBuilderDraft = new CampaignRuleBuilderDraft();
        private string _campaignRuleBuilderDraftPath;
        private string _selectedCampaignRuleBuilderRuleId = string.Empty;
        private string _campaignRuleBuilderStarterRuleId = string.Empty;
        private string _campaignRuleBuilderStatusKey = "No saved rule draft.";
        private string _campaignRuleBuilderDraftLoadStatusKey = "No saved rule draft.";
        private bool _campaignRuleBuilderDraftLoaded;
        private bool _isCampaignRuleBuilderPackageVisible;

        public void ExecuteNoviceCampaignRuleBuilder() => SelectSection("novice-campaign-rule-builder");

        [DataSourceProperty] public bool IsCampaignRuleBuilderActive => current == "novice-campaign-rule-builder";
        [DataSourceProperty] public bool IsCampaignRuleBuilderWorkspaceVisible => IsCampaignRuleBuilderActive && !_isCampaignRuleBuilderPackageVisible;
        [DataSourceProperty] public bool IsCampaignRuleBuilderPackageVisible => IsCampaignRuleBuilderActive && _isCampaignRuleBuilderPackageVisible;
        [DataSourceProperty] public MBBindingList<CampaignRuleItemVM> CampaignRuleBuilderRules => _campaignRuleBuilderRules;
        [DataSourceProperty] public MBBindingList<CampaignRuleConditionItemVM> CampaignRuleBuilderConditionsA => _campaignRuleBuilderConditionsA;
        [DataSourceProperty] public MBBindingList<CampaignRuleConditionItemVM> CampaignRuleBuilderConditionsB => _campaignRuleBuilderConditionsB;
        [DataSourceProperty] public bool CampaignRuleBuilderIsEmpty => _campaignRuleBuilderRules.Count == 0;
        [DataSourceProperty] public bool CampaignRuleBuilderHasSelection => SelectedCampaignRule != null;
        [DataSourceProperty] public bool CampaignRuleBuilderCanAdd => _campaignRuleBuilderRules.Count < CampaignRuleBuilderKinds.MaximumRules;
        [DataSourceProperty] public bool CampaignRuleBuilderAtCapacity => !CampaignRuleBuilderCanAdd;
        [DataSourceProperty] public bool CampaignRuleBuilderCanCopy => IsCampaignRuleBuilderPackageVisible && !string.IsNullOrWhiteSpace(full);
        [DataSourceProperty] public bool CampaignRuleBuilderCannotCopy => !CampaignRuleBuilderCanCopy;
        [DataSourceProperty] public string CampaignRuleBuilderOutputText => CampaignRuleBuilderCanCopy ? full : string.Empty;
        [DataSourceProperty] public string CampaignRuleBuilderCountLabel => _campaignRuleBuilderRules.Count.ToString(CultureInfo.InvariantCulture) + " / " + CampaignRuleBuilderKinds.MaximumRules.ToString(CultureInfo.InvariantCulture);
        [DataSourceProperty] public string CampaignRuleBuilderStatusLabel => T(_campaignRuleBuilderStatusKey);
        [DataSourceProperty] public string CampaignRuleBuilderDraftLoadStatusLabel => T(_campaignRuleBuilderDraftLoadStatusKey);
        [DataSourceProperty] public string CampaignRuleBuilderTitleLabel => T("Campaign Rule Builder");
        [DataSourceProperty] public string CampaignRuleBuilderEmptyLabel => T("Add a rule to begin. The preview uses sample data only.");
        [DataSourceProperty] public string CampaignRuleBuilderRulesLabel => T("Rule order");
        [DataSourceProperty] public string CampaignRuleBuilderPropertiesLabel => T("Selected rule");
        [DataSourceProperty] public string CampaignRuleBuilderEventLabel => T("When · event (click to change)");
        [DataSourceProperty] public string CampaignRuleBuilderEventChangeHintLabel => T("Changing the event clears both condition groups.");
        [DataSourceProperty] public string CampaignRuleBuilderActionLabel => T("Then · action");
        [DataSourceProperty] public string CampaignRuleBuilderTargetLabel => T("Target");
        [DataSourceProperty] public string CampaignRuleBuilderAmountLabel => T("Amount");
        [DataSourceProperty] public string CampaignRuleBuilderGroupALabel => T("If · group A (all)");
        [DataSourceProperty] public string CampaignRuleBuilderGroupBLabel => T("Or · group B (all)");
        [DataSourceProperty] public string CampaignRuleBuilderAddConditionLabel => T("Add condition");
        [DataSourceProperty] public string CampaignRuleBuilderCycleKindLabel => T("Change condition");
        [DataSourceProperty] public string CampaignRuleBuilderCycleOperatorLabel => T("Operator");
        [DataSourceProperty] public string CampaignRuleBuilderRemoveConditionLabel => T("Remove condition");
        [DataSourceProperty] public string CampaignRuleBuilderAddRuleLabel => T("Add rule");
        [DataSourceProperty] public string CampaignRuleBuilderPreviousLabel => T("Previous rule");
        [DataSourceProperty] public string CampaignRuleBuilderNextLabel => T("Next rule");
        [DataSourceProperty] public string CampaignRuleBuilderMoveUpLabel => T("Move up");
        [DataSourceProperty] public string CampaignRuleBuilderMoveDownLabel => T("Move down");
        [DataSourceProperty] public string CampaignRuleBuilderRemoveRuleLabel => T("Remove rule");
        [DataSourceProperty] public string CampaignRuleBuilderSaveLabel => T("Save draft");
        [DataSourceProperty] public string CampaignRuleBuilderValidateLabel => T("Validate");
        [DataSourceProperty] public string CampaignRuleBuilderGenerateLabel => T("Generate");
        [DataSourceProperty] public string CampaignRuleBuilderCopyLabel => T("Copy package");
        [DataSourceProperty] public string CampaignRuleBuilderPreviewHeadingLabel => T("Sample preview");
        [DataSourceProperty] public string CampaignRuleBuilderConditionValueLabel => T("Threshold (1-1000)");
        [DataSourceProperty] public string CampaignRuleBuilderPackageHeadingLabel => T("Generated package");
        [DataSourceProperty] public string CampaignRuleBuilderSelectedEventLabel => SelectedCampaignRule?.EventId ?? string.Empty;
        [DataSourceProperty] public string CampaignRuleBuilderSelectedEventDisplayLabel => SelectedCampaignRule == null ? string.Empty : "‹ " + SelectedCampaignRule.EventId + " ›";
        [DataSourceProperty] public string CampaignRuleBuilderSelectedActionLabel => LocalizeCampaignRuleAction(SelectedCampaignRule?.ActionId);
        [DataSourceProperty] public string CampaignRuleBuilderSelectedTargetLabel => LocalizeCampaignRuleTarget(SelectedCampaignRule?.TargetId);
        [DataSourceProperty] public string CampaignRuleBuilderPreviewLabel => BuildCampaignRulePreview();
        [DataSourceProperty]
        public string CampaignRuleBuilderAmountText
        {
            get
            {
                var selected = SelectedCampaignRule;
                if (selected == null) return string.Empty;
                return _campaignRuleBuilderAmountEdits.TryGetValue(selected.Id, out var edit)
                    ? edit : selected.Amount.ToString(CultureInfo.InvariantCulture);
            }
            set
            {
                var selected = SelectedCampaignRule;
                if (selected == null) return;
                string edit = value ?? string.Empty;
                if (edit.Length > 8) edit = edit.Substring(0, 8);
                _campaignRuleBuilderAmountEdits[selected.Id] = edit;
                if (int.TryParse(edit, NumberStyles.Integer, CultureInfo.InvariantCulture, out var amount))
                    selected.Amount = amount;
                OnPropertyChangedWithValue(edit, nameof(CampaignRuleBuilderAmountText));
                MarkCampaignRuleBuilderEdited();
            }
        }

        private CampaignRuleBuilderRule SelectedCampaignRule => _campaignRuleBuilderDraft.Rules.FirstOrDefault(rule => string.Equals(rule.Id, _selectedCampaignRuleBuilderRuleId, StringComparison.Ordinal));

        private void InitializeCampaignRuleBuilder(string pathOverride)
        {
            if (!string.IsNullOrWhiteSpace(pathOverride))
            {
                _campaignRuleBuilderDraftPath = pathOverride;
                return;
            }

            try
            {
                string local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                _campaignRuleBuilderDraftPath = string.IsNullOrWhiteSpace(local) ? null : Path.Combine(local, "CalradiaForge", "campaign-rule-builder.json");
            }
            catch { _campaignRuleBuilderDraftPath = null; }
        }

        private void EnsureCampaignRuleBuilderDraftLoaded()
        {
            if (_campaignRuleBuilderDraftLoaded) return;
            _campaignRuleBuilderDraftLoaded = true;
            var loaded = CampaignRuleBuilderPersistence.Load(_campaignRuleBuilderDraftPath);
            _campaignRuleBuilderDraft = loaded.Draft ?? new CampaignRuleBuilderDraft();
            bool createdStarterRule = loaded.Status == CampaignRuleBuilderLoadStatus.Missing;
            if (createdStarterRule)
            {
                var starterRule = CampaignRuleBuilderKinds.CreateRule();
                _campaignRuleBuilderStarterRuleId = starterRule.Id;
                _campaignRuleBuilderDraft.Rules.Add(starterRule);
            }
            switch (loaded.Status)
            {
                case CampaignRuleBuilderLoadStatus.Loaded: _campaignRuleBuilderDraftLoadStatusKey = "Rule draft loaded."; break;
                case CampaignRuleBuilderLoadStatus.Invalid: _campaignRuleBuilderDraftLoadStatusKey = "Damaged draft; preserved until save."; break;
                case CampaignRuleBuilderLoadStatus.TooLarge: _campaignRuleBuilderDraftLoadStatusKey = "Draft over 64 KiB; preserved until save."; break;
                case CampaignRuleBuilderLoadStatus.UnknownVersion: _campaignRuleBuilderDraftLoadStatusKey = "Unsupported draft version; preserved until save."; break;
                case CampaignRuleBuilderLoadStatus.Unavailable: _campaignRuleBuilderDraftLoadStatusKey = "Unreadable draft; preserved until save."; break;
                case CampaignRuleBuilderLoadStatus.Missing: _campaignRuleBuilderDraftLoadStatusKey = "Unsaved starter rule."; break;
                default: _campaignRuleBuilderDraftLoadStatusKey = "No saved rule draft."; break;
            }
            _campaignRuleBuilderStatusKey = createdStarterRule ? "Unsaved starter rule." : _campaignRuleBuilderDraftLoadStatusKey;
            RebuildCampaignRuleBuilderRules();
            foreach (var name in new[] { nameof(CampaignRuleBuilderRules), nameof(CampaignRuleBuilderIsEmpty), nameof(CampaignRuleBuilderCanAdd), nameof(CampaignRuleBuilderAtCapacity), nameof(CampaignRuleBuilderCountLabel), nameof(CampaignRuleBuilderStatusLabel), nameof(CampaignRuleBuilderDraftLoadStatusLabel) }) OnPropertyChanged(name);
        }

        private void RebuildCampaignRuleBuilderRules()
        {
            _campaignRuleBuilderRules.Clear();
            foreach (var rule in _campaignRuleBuilderDraft.Rules)
                _campaignRuleBuilderRules.Add(new CampaignRuleItemVM(this, rule));
            if (!_campaignRuleBuilderDraft.Rules.Any(rule => rule.Id == _selectedCampaignRuleBuilderRuleId))
                _selectedCampaignRuleBuilderRuleId = _campaignRuleBuilderDraft.Rules.FirstOrDefault()?.Id ?? string.Empty;
            RefreshCampaignRuleBuilderSelection();
        }

        internal void SelectCampaignRule(string id)
        {
            if (!_campaignRuleBuilderDraft.Rules.Any(rule => string.Equals(rule.Id, id, StringComparison.Ordinal))) return;
            _selectedCampaignRuleBuilderRuleId = id;
            RefreshCampaignRuleBuilderSelection();
        }

        private void RefreshCampaignRuleBuilderSelection()
        {
            foreach (var row in _campaignRuleBuilderRules) row.RefreshSelection(_selectedCampaignRuleBuilderRuleId);
            _campaignRuleBuilderConditionsA.Clear();
            _campaignRuleBuilderConditionsB.Clear();
            var rule = SelectedCampaignRule;
            if (rule != null)
            {
                for (int i = 0; i < rule.GroupA.Count; i++) _campaignRuleBuilderConditionsA.Add(new CampaignRuleConditionItemVM(this, rule.Id, false, rule.GroupA[i]));
                for (int i = 0; i < rule.GroupB.Count; i++) _campaignRuleBuilderConditionsB.Add(new CampaignRuleConditionItemVM(this, rule.Id, true, rule.GroupB[i]));
            }
            foreach (var name in new[] { nameof(CampaignRuleBuilderHasSelection), nameof(CampaignRuleBuilderConditionsA), nameof(CampaignRuleBuilderConditionsB), nameof(CampaignRuleBuilderSelectedEventLabel), nameof(CampaignRuleBuilderSelectedEventDisplayLabel), nameof(CampaignRuleBuilderSelectedActionLabel), nameof(CampaignRuleBuilderSelectedTargetLabel), nameof(CampaignRuleBuilderAmountText), nameof(CampaignRuleBuilderPreviewLabel) }) OnPropertyChanged(name);
        }

        public void ExecuteCampaignRuleBuilderAdd()
        {
            if (!IsCampaignRuleBuilderActive || !CampaignRuleBuilderCanAdd) return;
            var rule = CampaignRuleBuilderKinds.CreateRule();
            _campaignRuleBuilderDraft.Rules.Add(rule);
            _campaignRuleBuilderRules.Add(new CampaignRuleItemVM(this, rule));
            _selectedCampaignRuleBuilderRuleId = rule.Id;
            RefreshCampaignRuleBuilderSelection();
            MarkCampaignRuleBuilderEdited();
        }

        public void ExecuteCampaignRuleBuilderSelectPrevious() => SelectAdjacentCampaignRule(-1);
        public void ExecuteCampaignRuleBuilderSelectNext() => SelectAdjacentCampaignRule(1);
        internal void ExecuteCampaignRuleBuilderSelectIndex(int index)
        {
            if (!IsCampaignRuleBuilderActive || index < 0 || index >= _campaignRuleBuilderDraft.Rules.Count) return;
            SelectCampaignRule(_campaignRuleBuilderDraft.Rules[index].Id);
        }
        private void SelectAdjacentCampaignRule(int offset)
        {
            int index = _campaignRuleBuilderDraft.Rules.FindIndex(rule => rule.Id == _selectedCampaignRuleBuilderRuleId);
            if (index < 0) return;
            index = Math.Max(0, Math.Min(_campaignRuleBuilderDraft.Rules.Count - 1, index + offset));
            SelectCampaignRule(_campaignRuleBuilderDraft.Rules[index].Id);
        }

        public void ExecuteCampaignRuleBuilderMoveUp() => MoveCampaignRule(-1);
        public void ExecuteCampaignRuleBuilderMoveDown() => MoveCampaignRule(1);
        private void MoveCampaignRule(int offset)
        {
            int index = _campaignRuleBuilderDraft.Rules.FindIndex(rule => rule.Id == _selectedCampaignRuleBuilderRuleId);
            int destination = index + offset;
            if (index < 0 || destination < 0 || destination >= _campaignRuleBuilderDraft.Rules.Count) return;
            var rule = _campaignRuleBuilderDraft.Rules[index];
            _campaignRuleBuilderDraft.Rules.RemoveAt(index);
            _campaignRuleBuilderDraft.Rules.Insert(destination, rule);
            var row = _campaignRuleBuilderRules[index];
            _campaignRuleBuilderRules.RemoveAt(index);
            _campaignRuleBuilderRules.Insert(destination, row);
            MarkCampaignRuleBuilderEdited();
        }

        public void ExecuteCampaignRuleBuilderRemove()
        {
            int index = _campaignRuleBuilderDraft.Rules.FindIndex(rule => rule.Id == _selectedCampaignRuleBuilderRuleId);
            if (index < 0) return;
            bool removedStarter = string.Equals(_campaignRuleBuilderDraft.Rules[index].Id, _campaignRuleBuilderStarterRuleId, StringComparison.Ordinal);
            _campaignRuleBuilderAmountEdits.Remove(_selectedCampaignRuleBuilderRuleId);
            _campaignRuleBuilderDraft.Rules.RemoveAt(index);
            _campaignRuleBuilderRules.RemoveAt(index);
            if (removedStarter)
            {
                _campaignRuleBuilderStarterRuleId = string.Empty;
                _campaignRuleBuilderDraftLoadStatusKey = "No saved rule draft.";
                OnPropertyChanged(nameof(CampaignRuleBuilderDraftLoadStatusLabel));
            }
            _selectedCampaignRuleBuilderRuleId = _campaignRuleBuilderDraft.Rules.Count == 0 ? string.Empty : _campaignRuleBuilderDraft.Rules[Math.Min(index, _campaignRuleBuilderDraft.Rules.Count - 1)].Id;
            RefreshCampaignRuleBuilderSelection();
            MarkCampaignRuleBuilderEdited();
        }

        public void ExecuteCampaignRuleBuilderCycleEvent()
        {
            var rule = SelectedCampaignRule;
            if (rule == null) return;
            rule.EventId = NextCampaignRuleOption(CampaignRuleBuilderKinds.Events, rule.EventId);
            rule.GroupA.Clear();
            rule.GroupB.Clear();
            NormalizeCampaignRuleActionTarget(rule);
            RefreshCampaignRuleBuilderSelection();
            RefreshCampaignRuleBuilderRows();
            MarkCampaignRuleBuilderEdited();
        }

        public void ExecuteCampaignRuleBuilderCycleAction()
        {
            var rule = SelectedCampaignRule;
            if (rule == null) return;
            int start = Array.IndexOf(CampaignRuleBuilderKinds.Actions, rule.ActionId);
            for (int i = 1; i <= CampaignRuleBuilderKinds.Actions.Length; i++)
            {
                string candidate = CampaignRuleBuilderKinds.Actions[(start + i + CampaignRuleBuilderKinds.Actions.Length) % CampaignRuleBuilderKinds.Actions.Length];
                if (CampaignRuleBuilderKinds.GetCompatibleTargets(rule.EventId, candidate).Length == 0) continue;
                rule.ActionId = candidate;
                break;
            }
            NormalizeCampaignRuleActionTarget(rule);
            RefreshCampaignRuleBuilderSelection();
            RefreshCampaignRuleBuilderRows();
            MarkCampaignRuleBuilderEdited();
        }

        public void ExecuteCampaignRuleBuilderCycleTarget()
        {
            var rule = SelectedCampaignRule;
            if (rule == null) return;
            rule.TargetId = NextCampaignRuleOption(CampaignRuleBuilderKinds.GetCompatibleTargets(rule.EventId, rule.ActionId), rule.TargetId);
            RefreshCampaignRuleBuilderSelection();
            RefreshCampaignRuleBuilderRows();
            MarkCampaignRuleBuilderEdited();
        }

        private static string NextCampaignRuleOption(string[] options, string currentOption)
        {
            if (options == null || options.Length == 0) return currentOption;
            int index = Array.IndexOf(options, currentOption);
            return options[(index + 1) % options.Length];
        }

        private void NormalizeCampaignRuleActionTarget(CampaignRuleBuilderRule rule)
        {
            var targets = CampaignRuleBuilderKinds.GetCompatibleTargets(rule.EventId, rule.ActionId);
            if (targets.Length > 0 && !targets.Contains(rule.TargetId, StringComparer.Ordinal)) rule.TargetId = targets[0];
            if (rule.ActionId == "gold" && rule.Amount < 0)
            {
                rule.Amount = 100;
                _campaignRuleBuilderAmountEdits.Remove(rule.Id);
            }
        }

        public void ExecuteCampaignRuleBuilderAddConditionA() => AddCampaignRuleCondition(false);
        public void ExecuteCampaignRuleBuilderAddConditionB() => AddCampaignRuleCondition(true);
        private void AddCampaignRuleCondition(bool secondGroup)
        {
            var rule = SelectedCampaignRule;
            if (rule == null) return;
            var group = secondGroup ? rule.GroupB : rule.GroupA;
            if (group.Count >= CampaignRuleBuilderKinds.MaximumConditionsPerGroup) return;
            if (secondGroup && rule.GroupA.Count == 0)
            {
                SetCampaignRuleBuilderStatus("Add a condition to group A before group B.");
                return;
            }
            var condition = new CampaignRuleBuilderCondition { Kind = "always", Operator = "eq", Value = string.Empty };
            group.Add(condition);
            (secondGroup ? _campaignRuleBuilderConditionsB : _campaignRuleBuilderConditionsA).Add(new CampaignRuleConditionItemVM(this, rule.Id, secondGroup, condition));
            MarkCampaignRuleBuilderEdited();
        }

        internal void CycleCampaignRuleCondition(string ruleId, bool secondGroup, CampaignRuleBuilderCondition condition)
        {
            var rule = SelectedCampaignRule;
            if (rule == null || rule.Id != ruleId) return;
            var options = CampaignRuleBuilderKinds.GetCompatibleConditions(rule.EventId);
            condition.Kind = NextCampaignRuleOption(options, condition.Kind);
            bool numeric = condition.Kind == "hero_level_at_least" || condition.Kind == "skill_gain_at_least";
            condition.Operator = numeric ? "gte" : "eq";
            condition.Value = numeric ? "1" : string.Empty;
            RefreshCampaignRuleBuilderSelection();
            MarkCampaignRuleBuilderEdited();
        }

        internal void RemoveCampaignRuleCondition(string ruleId, bool secondGroup, CampaignRuleBuilderCondition condition)
        {
            var rule = SelectedCampaignRule;
            if (rule == null || rule.Id != ruleId) return;
            var group = secondGroup ? rule.GroupB : rule.GroupA;
            if (!group.Remove(condition)) return;
            if (rule.GroupA.Count == 0) rule.GroupB.Clear();
            RefreshCampaignRuleBuilderSelection();
            MarkCampaignRuleBuilderEdited();
        }

        internal void ExecuteCampaignRuleBuilderConditionKeyboard(bool secondGroup, bool remove, int index)
        {
            var rule = SelectedCampaignRule;
            if (rule == null) return;
            var group = secondGroup ? rule.GroupB : rule.GroupA;
            if (index < 0 || index >= group.Count) return;
            var condition = group[index];
            if (remove) RemoveCampaignRuleCondition(rule.Id, secondGroup, condition);
            else CycleCampaignRuleCondition(rule.Id, secondGroup, condition);
        }

        internal void CampaignRuleConditionChanged() => MarkCampaignRuleBuilderEdited();

        public void ExecuteCampaignRuleBuilderSave()
        {
            if (!ValidateCampaignRuleBuilderAmounts(out var error)) { SetCampaignRuleBuilderStatus(error); return; }
            if (!CampaignRuleBuilderPersistence.TrySave(_campaignRuleBuilderDraftPath, _campaignRuleBuilderDraft, out error))
            {
                SetCampaignRuleBuilderStatus(error ?? "The rule draft could not be saved.");
                return;
            }
            _campaignRuleBuilderStarterRuleId = string.Empty;
            foreach (var row in _campaignRuleBuilderRules) row.RefreshIdentityLabel();
            _campaignRuleBuilderDraftLoadStatusKey = "Rule draft saved.";
            OnPropertyChanged(nameof(CampaignRuleBuilderDraftLoadStatusLabel));
            SetCampaignRuleBuilderStatus("Rule draft saved.");
        }

        public void ExecuteCampaignRuleBuilderValidate()
        {
            if (!ValidateCampaignRuleBuilderAmounts(out var amountError)) { SetCampaignRuleBuilderStatus(amountError); return; }
            var errors = CampaignRuleBuilderGenerator.Validate(_campaignRuleBuilderDraft);
            SetCampaignRuleBuilderStatus(errors.Count == 0 ? "Rules validated. No game action ran." : errors[0]);
        }

        public void ExecuteCampaignRuleBuilderGenerate()
        {
            if (!ValidateCampaignRuleBuilderAmounts(out var amountError)) { SetCampaignRuleBuilderStatus(amountError); return; }
            if (!CampaignRuleBuilderGenerator.TryGenerate(_campaignRuleBuilderDraft, out var package, out var errors))
            {
                SetCampaignRuleBuilderStatus(errors.Count > 0 ? errors[0] : "Rule package could not be generated.");
                return;
            }
            full = package.FullText;
            page = 0;
            overview = false;
            _isCampaignRuleBuilderPackageVisible = true;
            Render();
            SetCampaignRuleBuilderStatus("Rule package validated. No game action ran.");
            NotifyCampaignRuleBuilderVisibility();
        }

        public void ExecuteCampaignRuleBuilderCopy()
        {
            if (!CampaignRuleBuilderCanCopy) return;
            ExecuteClipboard();
        }

        private bool ValidateCampaignRuleBuilderAmounts(out string error)
        {
            foreach (var entry in _campaignRuleBuilderAmountEdits)
            {
                if (!int.TryParse(entry.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var amount)
                    || amount == 0 || amount < -CampaignRuleBuilderKinds.MaximumAmount || amount > CampaignRuleBuilderKinds.MaximumAmount)
                {
                    error = "Enter a nonzero amount within the supported range.";
                    return false;
                }
            }
            error = null;
            return true;
        }

        private void MarkCampaignRuleBuilderEdited()
        {
            if (_isCampaignRuleBuilderPackageVisible)
            {
                _isCampaignRuleBuilderPackageVisible = false;
                full = string.Empty;
                page = 0;
                Render();
            }
            SetCampaignRuleBuilderStatus("Unsaved draft edits.");
            OnPropertyChanged(nameof(CampaignRuleBuilderIsEmpty));
            OnPropertyChanged(nameof(CampaignRuleBuilderCanAdd));
            OnPropertyChanged(nameof(CampaignRuleBuilderAtCapacity));
            OnPropertyChanged(nameof(CampaignRuleBuilderCountLabel));
            OnPropertyChanged(nameof(CampaignRuleBuilderPreviewLabel));
            NotifyCampaignRuleBuilderVisibility();
        }

        private void SetCampaignRuleBuilderStatus(string status)
        {
            _campaignRuleBuilderStatusKey = status ?? string.Empty;
            OnPropertyChanged(nameof(CampaignRuleBuilderStatusLabel));
        }

        private void RefreshCampaignRuleBuilderRows()
        {
            foreach (var row in _campaignRuleBuilderRules)
            {
                row.RefreshSummary();
                row.RefreshIdentityLabel();
            }
        }

        internal string CampaignRuleBuilderRowIdentityLabel(string ruleId) =>
            string.Equals(ruleId, _campaignRuleBuilderStarterRuleId, StringComparison.Ordinal)
                ? CampaignRuleBuilderDraftLoadStatusLabel : ruleId;

        private void NotifyCampaignRuleBuilderVisibility()
        {
            foreach (var name in new[] { nameof(IsCampaignRuleBuilderActive), nameof(IsCampaignRuleBuilderWorkspaceVisible), nameof(IsCampaignRuleBuilderPackageVisible), nameof(CampaignRuleBuilderCanCopy), nameof(CampaignRuleBuilderCannotCopy), nameof(CampaignRuleBuilderOutputText), nameof(IsEvidenceFrameVisible), nameof(IsComposerPaginationVisible), nameof(IsEvidenceToggleVisible), nameof(IsNormalInputVisible), nameof(IsRegularActionDeckVisible), nameof(ShowCommandDeck), nameof(OutputHeading), nameof(IsPlaybookVisible), nameof(WorkspaceRightMargin) }) OnPropertyChanged(name);
        }

        private void NotifyCampaignRuleBuilderLabels()
        {
            foreach (var name in new[] { nameof(NoviceCampaignRuleBuilderLabel), nameof(NoviceCampaignRuleBuilderHint), nameof(CampaignRuleBuilderTitleLabel), nameof(CampaignRuleBuilderEmptyLabel), nameof(CampaignRuleBuilderRulesLabel), nameof(CampaignRuleBuilderPropertiesLabel), nameof(CampaignRuleBuilderEventLabel), nameof(CampaignRuleBuilderEventChangeHintLabel), nameof(CampaignRuleBuilderActionLabel), nameof(CampaignRuleBuilderTargetLabel), nameof(CampaignRuleBuilderAmountLabel), nameof(CampaignRuleBuilderGroupALabel), nameof(CampaignRuleBuilderGroupBLabel), nameof(CampaignRuleBuilderAddConditionLabel), nameof(CampaignRuleBuilderCycleKindLabel), nameof(CampaignRuleBuilderCycleOperatorLabel), nameof(CampaignRuleBuilderRemoveConditionLabel), nameof(CampaignRuleBuilderAddRuleLabel), nameof(CampaignRuleBuilderPreviousLabel), nameof(CampaignRuleBuilderNextLabel), nameof(CampaignRuleBuilderMoveUpLabel), nameof(CampaignRuleBuilderMoveDownLabel), nameof(CampaignRuleBuilderRemoveRuleLabel), nameof(CampaignRuleBuilderSaveLabel), nameof(CampaignRuleBuilderValidateLabel), nameof(CampaignRuleBuilderGenerateLabel), nameof(CampaignRuleBuilderCopyLabel), nameof(CampaignRuleBuilderPreviewHeadingLabel), nameof(CampaignRuleBuilderConditionValueLabel), nameof(CampaignRuleBuilderPackageHeadingLabel), nameof(CampaignRuleBuilderStatusLabel), nameof(CampaignRuleBuilderDraftLoadStatusLabel), nameof(CampaignRuleBuilderSelectedActionLabel), nameof(CampaignRuleBuilderSelectedTargetLabel), nameof(CampaignRuleBuilderPreviewLabel) }) OnPropertyChanged(name);
            RefreshCampaignRuleBuilderRows();
            RefreshCampaignRuleBuilderSelection();
        }

        internal string LocalizeCampaignRuleAction(string id)
        {
            switch (id)
            {
                case "gold": return T("Grant gold");
                case "influence": return T("Change influence");
                case "renown": return T("Grant renown");
                case "relation": return T("Change player relation");
                default: return string.Empty;
            }
        }

        internal string LocalizeCampaignRuleTarget(string id) => id == "event" ? T("Event entity") : id == "player" ? T("Player") : string.Empty;

        internal string LocalizeCampaignRuleCondition(string id)
        {
            switch (id)
            {
                case "always": return T("Always");
                case "hero_is_player": return T("Hero is player");
                case "hero_is_alive": return T("Hero is alive");
                case "hero_level_at_least": return T("Hero level at least");
                case "clan_is_player": return T("Clan belongs to player");
                case "settlement_is_town": return T("Settlement is a town");
                case "skill_gain_at_least": return T("Skill gain at least");
                case "party_is_main": return T("Party is main party");
                default: return id ?? string.Empty;
            }
        }

        private string BuildCampaignRulePreview()
        {
            var rule = SelectedCampaignRule;
            if (rule == null) return T("Select a rule to preview it.");
            bool groupA = SampleCampaignRuleGroup(rule.GroupA);
            bool groupB = rule.GroupB.Count > 0 && SampleCampaignRuleGroup(rule.GroupB);
            bool runs = rule.GroupA.Count == 0 || groupA || groupB;
            string outcome = runs ? T("Sample outcome: action would run.") : T("Sample outcome: action would be skipped.");
            string occurrenceNotice = string.Empty;
            if (rule.EventId == "DailyTickHeroEvent") occurrenceNotice = "\n" + T("Daily per-hero event: this rule is evaluated for the event's hero. Actions run only when conditions match; player rewards may repeat for each matching hero.");
            else if (rule.EventId == "DailyTickSettlementEvent") occurrenceNotice = "\n" + T("Daily per-settlement event: this rule is evaluated for the event's settlement. Actions run only when conditions match; player rewards may repeat for each matching settlement.");
            return T("Sample facts: non-player hero, alive, level 20, non-player clan, town, skill gain 5, non-main party.") + "\n"
                + outcome + "\n" + LocalizeCampaignRuleAction(rule.ActionId) + " · " + LocalizeCampaignRuleTarget(rule.TargetId) + " · " + rule.Amount.ToString(CultureInfo.InvariantCulture)
                + occurrenceNotice + "\n" + T("Preview only. No campaign action ran.");
        }

        private static bool SampleCampaignRuleGroup(List<CampaignRuleBuilderCondition> group)
        {
            foreach (var condition in group)
            {
                switch (condition.Kind)
                {
                    case "always":
                    case "hero_is_alive":
                    case "settlement_is_town":
                        break;
                    case "hero_is_player":
                    case "clan_is_player":
                    case "party_is_main":
                        return false;
                    case "hero_level_at_least":
                        if (!int.TryParse(condition.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var threshold) || 20 < threshold) return false;
                        break;
                    case "skill_gain_at_least":
                        if (!int.TryParse(condition.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var skillThreshold) || 5 < skillThreshold) return false;
                        break;
                    default:
                        return false;
                }
            }
            return true;
        }
    }

    internal sealed class CampaignRuleItemVM : ViewModel
    {
        private readonly PanelViewModel _parent;
        private readonly CampaignRuleBuilderRule _model;
        private bool _isSelected;
        internal CampaignRuleItemVM(PanelViewModel parent, CampaignRuleBuilderRule model) { _parent = parent; _model = model; }
        [DataSourceProperty] public string Id => _model.Id;
        [DataSourceProperty] public string Summary => _model.EventId + " · " + _parent.LocalizeCampaignRuleAction(_model.ActionId);
        [DataSourceProperty] public string RowIdentityLabel => _parent.CampaignRuleBuilderRowIdentityLabel(Id);
        [DataSourceProperty] public bool IsSelected { get => _isSelected; set { if (_isSelected == value) return; _isSelected = value; OnPropertyChangedWithValue(value, nameof(IsSelected)); } }
        internal void RefreshSelection(string selectedId) => IsSelected = string.Equals(Id, selectedId, StringComparison.Ordinal);
        internal void RefreshSummary() => OnPropertyChanged(nameof(Summary));
        internal void RefreshIdentityLabel() => OnPropertyChanged(nameof(RowIdentityLabel));
        public void ExecuteSelect() => _parent.SelectCampaignRule(Id);
    }

    internal sealed class CampaignRuleConditionItemVM : ViewModel
    {
        private readonly PanelViewModel _parent;
        private readonly string _ruleId;
        private readonly bool _secondGroup;
        private readonly CampaignRuleBuilderCondition _model;
        internal CampaignRuleConditionItemVM(PanelViewModel parent, string ruleId, bool secondGroup, CampaignRuleBuilderCondition model)
        { _parent = parent; _ruleId = ruleId; _secondGroup = secondGroup; _model = model; }
        [DataSourceProperty] public string Summary => _parent.LocalizeCampaignRuleCondition(_model.Kind) + (IsNumeric ? " ≥ " + _model.Value : string.Empty);
        [DataSourceProperty] public string CycleKindLabel => _parent.CampaignRuleBuilderCycleKindLabel;
        [DataSourceProperty] public string RemoveLabel => _parent.CampaignRuleBuilderRemoveConditionLabel;
        [DataSourceProperty] public bool IsNumeric => _model.Kind == "hero_level_at_least" || _model.Kind == "skill_gain_at_least";
        [DataSourceProperty] public string Operator => _model.Operator;
        [DataSourceProperty]
        public string Value
        {
            get => _model.Value ?? string.Empty;
            set
            {
                if (!IsNumeric) return;
                string next = value ?? string.Empty;
                if (next.Length > 4) next = next.Substring(0, 4);
                if (string.Equals(next, _model.Value, StringComparison.Ordinal)) return;
                _model.Value = next;
                OnPropertyChangedWithValue(next, nameof(Value));
                OnPropertyChanged(nameof(Summary));
                _parent.CampaignRuleConditionChanged();
            }
        }
        public void ExecuteCycleKind() => _parent.CycleCampaignRuleCondition(_ruleId, _secondGroup, _model);
        public void ExecuteCycleOperator() { }
        public void ExecuteRemove() => _parent.RemoveCampaignRuleCondition(_ruleId, _secondGroup, _model);
    }
}
