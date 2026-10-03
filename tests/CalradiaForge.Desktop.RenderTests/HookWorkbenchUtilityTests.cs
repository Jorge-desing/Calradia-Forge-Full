using System;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Media;
using System.Collections.Generic;
using CalradiaForge.Desktop.Presentation;
using CalradiaForge.Desktop.Services;

internal static class HookWorkbenchUtilityTests
{
    public static void Run()
    {
        const string json = """
            {"session":"test","eligible":true,"hooks":[
            {"id":"one","owner":"Alpha","target":"Type.One","state":"Registered","hasPrefix":true,"hasFinalizer":true,"hasTranspiler":true},
            {"id":"two","owner":"Beta","target":"Type.Two","state":"Registered","hasPostfix":true}]}
            """;
        Require(HookWorkbenchViewModel.TryReadSnapshotEnvelope(json, out var inventory, out var error), error);
        Require(inventory.Hooks[0].HookKinds.Contains("Finalizer") && inventory.Hooks[0].HookKinds.Contains("Transpiler"), "Extended patch kinds missing.");
        Require(inventory.Hooks[1].HasFinalizer == null && inventory.Hooks[1].HasTranspiler == null, "Older host metadata must remain unknown.");
        Require(inventory.Hooks[0].Copy().HasTranspiler == true, "Copy lost extended metadata.");
        using var session = new DesktopSessionService(new DesktopMetricsService());
        using var vm = new HookWorkbenchViewModel(session);
        typeof(HookWorkbenchViewModel).GetMethod("ReplaceHooks", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(vm, [inventory.Hooks]);
        vm.Hooks[1].IsSelected = true;
        vm.OwnerFilter = "ALPHA";
        Require(vm.FilteredHooks.Count == 1 && vm.FilteredHooks[0].Id == "one", "Owner filter must ignore case.");
        Require(vm.SelectedCount == 1 && vm.Hooks[1].IsSelected, "Filtering must preserve registered selection.");
        Require(vm.HiddenSelectedCount == 1 && vm.SelectionMessage == "1 selected; 1 hidden by filters. Operations include every selected hook.",
            "Hidden selected hooks must be explicitly disclosed before operations.");
        vm.TargetFilter = "Two";
        Require(!vm.HasFilteredHooks, "Independent filters must combine.");
        Require(vm.EmptyInventoryMessage == "No registered hooks match the current filters.", "Filtered empty state must not claim the inventory is empty.");
        vm.OwnerFilter = "";
        Require(vm.FilteredHooks.Single().Id == "two", "Target filter failed.");
        Require(vm.HiddenSelectedCount == 0 && vm.SelectionMessage.StartsWith("1 selected; 0 hidden", StringComparison.Ordinal),
            "Restoring a selected hook to view must update the hidden selection count.");
        vm.TargetFilter = "";
        vm.TypeFilter = "transpiler";
        Require(vm.FilteredHooks.Single().Id == "one", "Type filter must include Transpiler.");
        Require(!vm.VerifySelectedCommand.CanExecute(null), "Disconnected Verify must be disabled.");
        Require(vm.CanChangeSelection, "Selection must remain editable before a plan is created.");
        Require(HookWorkbenchViewModel.SelectionMatchesPlan(["one"], ["one"]), "The exact selected-ID set must match its plan.");
        Require(!HookWorkbenchViewModel.SelectionMatchesPlan(["two"], ["one"]), "Confirmation must reject a selection that differs from the plan IDs.");
        Require(!HookWorkbenchViewModel.SelectionMatchesPlan(["one", "two"], ["one"]), "Confirmation must reject additional selected IDs.");
        vm.Hooks[1].IsSelected = false;
        vm.Hooks[0].IsSelected = true;

        var expiry = DateTimeOffset.UtcNow.AddSeconds(40).ToString("O");
        var plan = JsonSerializer.Serialize(new { operation = "apply", session = "test", token = "one-use",
            requiresConfirmation = true, expiresAtUtc = expiry, selected = new[] { new { id = "one", owner = "Alpha", target = "Type.One",
                state = "Registered", hasPrefix = true, hasFinalizer = false, hasTranspiler = true } } });
        Require(!HookWorkbenchViewModel.TryReadPlan(plan, "apply", "test", ["one"], inventory.Hooks, out _, out _, out _),
            "A plan with changed Finalizer metadata must be rejected.");
        const string verify = """
            {"operation":"verify","session":"test","tokenConsumed":false,"succeeded":true,"partial":false,"cancelled":false,
             "notAttemptedIds":[],"results":[{"id":"one","state":"Registered","succeeded":true,"verified":true,"verificationDetail":"Not installed"}]}
            """;
        Require(HookWorkbenchViewModel.TryReadCommit(verify, "verify", "test", ["one"], out var result, out error), error);
        Require(!result.TokenConsumed && result.Results.Single().Verified, "Read-only verification result was changed.");
        Require(!HookWorkbenchViewModel.TryReadCommit(verify, "verify", "other-session", ["one"], out _, out _), "Wrong-session verification must fail.");
        Require(!HookWorkbenchViewModel.TryReadCommit(verify, "verify", "test", ["two"], out _, out _), "Unselected verification IDs must fail.");
        var longId = new string('i', 200);
        var longOwner = new string('o', 200);
        var longTarget = "Namespace.Type." + new string('t', 300);
        for (var index = 0; index < 16; index++)
            vm.PlannedHooks.Add(new HookWorkbenchRow(longId + index, longOwner, longTarget, true, false,
                null, [], [], "Registered", ""));
        typeof(HookWorkbenchViewModel).GetField("pendingPlan", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(vm,
            new HookPlan("apply", "test", "preview-only", DateTimeOffset.UtcNow.AddSeconds(40), true,
                ["one"]));
        Require(!vm.CanChangeSelection, "A pending plan must lock selection changes.");
        var control = new HookWorkbenchControl { DataContext = vm };
        control.Measure(new Size(960, 600));
        control.Arrange(new Rect(0, 0, 960, 600));
        control.UpdateLayout();
        Require(control.DesiredSize.Width <= 960 && control.DesiredSize.Height <= 600,
            "Hook utility controls exceed the bounded workbench viewport.");
        var elements = new List<DependencyObject> { control };
        for (var index = 0; index < elements.Count; index++)
            for (var child = 0; child < VisualTreeHelper.GetChildrenCount(elements[index]); child++)
                elements.Add(VisualTreeHelper.GetChild(elements[index], child));
        var viewport = elements.OfType<ScrollViewer>().Single(item => AutomationProperties.GetAutomationId(item) == "HookPlanPreviewViewport");
        Require(viewport.ActualHeight <= 160 && viewport.ExtentHeight > viewport.ViewportHeight &&
            viewport.HorizontalScrollBarVisibility == ScrollBarVisibility.Disabled,
            "Long multi-hook plans must use the bounded vertical preview viewport.");
        foreach (var text in new[] { longId + "0", longOwner, longTarget })
            Require(elements.OfType<TextBlock>().Any(item => item.Text == text && item.TextWrapping == TextWrapping.Wrap),
                "Preview must retain and wrap complete ID, owner, and target metadata.");
        var registeredSelection = elements.OfType<CheckBox>().SingleOrDefault(item => AutomationProperties.GetName(item) == "one");
        Require(registeredSelection != null && !registeredSelection.IsEnabled,
            "The registered-hook selector must be disabled while its plan is pending.");
        typeof(HookWorkbenchViewModel).GetMethod("ClearPendingPlan", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(vm, null);
        control.UpdateLayout();
        Require(vm.CanChangeSelection && registeredSelection.IsEnabled,
            "Clearing a pending plan must notify the view and re-enable registered-hook selection.");
        typeof(HookWorkbenchViewModel).GetMethod("ReplaceHooks", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(vm, [Array.Empty<HookWorkbenchRow>()]);
        Require(vm.EmptyInventoryMessage == "No registered hooks are available in this session.", "An empty inventory must retain its explicit empty state.");
    }

    static void Require(bool value, string detail)
    {
        if (!value) throw new InvalidOperationException(detail);
    }
}
