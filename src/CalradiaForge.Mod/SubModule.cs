using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.MountAndBlade;
using TaleWorlds.InputSystem;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.ScreenSystem;
using TaleWorlds.Library;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("CalradiaForge.Tests")]

namespace CalradiaForge.Mod
{
    public sealed class SubModule : MBSubModuleBase
    {
        internal static Runtime CurrentRuntime { get; set; }
        Runtime runtime; GauntletLayer layer; ScreenBase owner; PanelViewModel vm;
        GauntletLayer extensionLayer; ScreenBase extensionOwner; TaleWorlds.Library.ViewModel extensionViewModel;
        string extensionPageOwner;
        string cachedHotkeyName;
        InputKey cachedHotkey = InputKey.F10;
        bool f10ProbeHeld;
        Widget keyboardControl;
        Widget navigationPaletteReturnFocus;
        bool navigationPaletteWasOpen;
        private static readonly Func<Widget, bool> IsForgeArgumentPredicate = w => w != null && (w.Id == "ForgeArgument" || w.Id == "ForgeCommandArgument");
        private static readonly Func<Widget, bool> IsSdkCatalogSearchPredicate = w => w is EditableTextWidget && w.Id == "ForgeSdkCatalogSearch";
        private static readonly Func<Widget, bool> IsNavigationPaletteSearchPredicate = w => w is EditableTextWidget && w.Id == "ForgeNavigationPaletteSearch";
        protected override void OnSubModuleLoad() 
        { 
            try 
            {
                AppDomain.CurrentDomain.UnhandledException += OnUnhandledException; 
                runtime=new Runtime {OpenPanel=Open,ClosePanel=Close}; 
                CurrentRuntime = runtime; 
                CalradiaForge.Sdk.ForgeUI.PageOpenRequested += OpenExtensionPage;
                CalradiaForge.Sdk.ForgeUI.PageCloseRequested += RequestCloseExtensionPage;
                CalradiaForge.Sdk.ForgeApi.UiPagesRemoved += OnUiPagesRemoved;
                CalradiaForge.Core.ForgeBootstrapper.InitializeGlobalPatches();
            }
            catch (Exception ex)
            {
                TaleWorlds.Library.Debug.Print("[CalradiaForge] FATAL ERROR on SubModuleLoad: " + ex.Message, 0, TaleWorlds.Library.Debug.DebugColor.Red);
            }
        }
        
        private void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            if (e.ExceptionObject is Exception ex)
            {
                try
                {
                    var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                    var dump = "{\n" +
                               $"  \"Timestamp\": \"{DateTime.Now:O}\",\n" +
                               $"  \"IsTerminating\": {(e.IsTerminating ? "true" : "false")},\n" +
                               $"  \"Exception\": \"{ex.ToString().Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "")}\"\n" +
                               "}";
                    var path = System.IO.Path.Combine(TaleWorlds.Engine.Utilities.GetBasePath(), "Modules", "CalradiaForge", $"crash_{timestamp}.cfcrash");
                    System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path));
                    System.IO.File.WriteAllText(path, dump);
                }
                catch { }
            }
        }

        protected override void OnBeforeInitialModuleScreenSetAsRoot(){runtime?.NotifyInitialScreenReady();}

        protected override void OnGameStart(TaleWorlds.Core.Game game, TaleWorlds.Core.IGameStarter gameStarterObject)
        {
            base.OnGameStart(game, gameStarterObject);
            if (gameStarterObject is TaleWorlds.CampaignSystem.CampaignGameStarter campaignStarter)
            {
                // Auto-register behaviors for all mods using CalradiaForge
                CalradiaForge.Core.CampaignExtensions.ForgeBehaviorLoader.RegisterAll(campaignStarter);
                campaignStarter.AddBehavior(new CalradiaForge.Mod.CampaignBehaviors.ClanCharacterProgressionBehavior());
                campaignStarter.AddBehavior(new CalradiaForge.Mod.CampaignBehaviors.AgentCognitiveMemoryBehavior());
                campaignStarter.AddBehavior(new CalradiaForge.Mod.DataExtensions.DataBehavior());
            }
        }

        public override void OnCampaignStart(TaleWorlds.Core.Game game,object starterObject)
        {
            runtime?.NotifyCampaignStarted();
            // Campaign behaviors registered in OnGameStart per Rule B



        }
        public override void OnGameLoaded(TaleWorlds.Core.Game game, object initializerObject)
        {
            base.OnGameLoaded(game, initializerObject);
            runtime?.NotifyGameLoaded();
        }
        protected override void OnApplicationTick(float dt)
        {
            if (runtime == null) return;
            var callbackStarted = Stopwatch.GetTimestamp();
            try
            {
                runtime.BindGameThread();
                var configuredHotkey = runtime.Config.Hotkey;
                if (!string.Equals(configuredHotkey, cachedHotkeyName, StringComparison.Ordinal))
                {
                    cachedHotkeyName = configuredHotkey;
                    if (!Enum.TryParse(configuredHotkey, out cachedHotkey)) cachedHotkey = InputKey.F10;
                    runtime.Register("CalradiaForge", "Info", "Panel hotkey polling armed: " + cachedHotkey + ".");
                }

                var hotkeyPressed = Input.IsKeyPressed(cachedHotkey);
                if (cachedHotkey == InputKey.F10)
                {
                    var f10Down = Input.IsKeyDown(InputKey.F10);
                    var f10Immediate = Input.IsKeyDownImmediate(InputKey.F10);
                    var f10Held = f10Down || f10Immediate;
                    hotkeyPressed = hotkeyPressed || (f10Held && !f10ProbeHeld);
                    f10ProbeHeld = f10Held;
                }
                else f10ProbeHeld = false;

                if (layer == null && extensionLayer == null)
                {
                    if (hotkeyPressed)
                    {
                        runtime.Register("CalradiaForge", "Info", "Panel hotkey detected: " + cachedHotkey + ".");
                        Open();
                        return;
                    }
                }
                else if (layer != null && extensionLayer == null && hotkeyPressed)
                {
                    runtime.Register("CalradiaForge", "Info", "Panel hotkey detected; closing the panel.");
                    Close();
                    return;
                }

                if (runtime.RefreshGameLanguage()) vm?.RefreshLanguage();
                runtime.Tick(dt);

                if (layer == null && extensionLayer == null) return;

                if (GameNetwork.IsMultiplayer) { Close(); return; }

                if (extensionLayer != null)
                {
                    if (Input.IsKeyPressed(InputKey.Escape)) CloseExtensionPage();
                    return;
                }

                // Mod panel is open: execute UI tick and handle full interactive controls.
                vm?.Tick(dt);
                if (ScreenManager.TopScreen != owner) { Close(); return; }
                SyncNavigationPaletteFocus();

                if (Input.IsKeyPressed(InputKey.Escape))
                {
                    if (vm != null && vm.IsNavigationPaletteOpen)
                    {
                        vm.CloseNavigationPalette();
                        SyncNavigationPaletteFocus();
                        return;
                    }
                    if (vm != null && vm.IsKeyHelpOpen)
                    {
                        vm?.ExecuteToggleKeyHelp();
                        return;
                    }
                    if (vm != null && vm.IsSdkCatalogOpen)
                    {
                        vm.ExecuteCloseSdkCatalog();
                        return;
                    }
                    if (vm != null && vm.IsHistoryOpen)
                    {
                        vm.ExecuteToggleHistory();
                        return;
                    }
                    Close();
                    return;
                }

                if (IsControlDown() && Input.IsKeyPressed(InputKey.P))
                {
                    vm?.ToggleNavigationPalette();
                    SyncNavigationPaletteFocus();
                    return;
                }

                if (vm != null && vm.IsNavigationPaletteOpen)
                {
                    if (IsControlDown() && Input.IsKeyPressed(InputKey.F))
                    {
                        FocusNavigationPaletteSearch();
                        return;
                    }
                    if (Input.IsKeyPressed(InputKey.Tab))
                    {
                        FocusNavigationPaletteSearch();
                        return;
                    }
                    if (Input.IsKeyPressed(InputKey.Up))
                    {
                        vm.ExecuteNavigationPalettePrevious();
                        return;
                    }
                    if (Input.IsKeyPressed(InputKey.Down))
                    {
                        vm.ExecuteNavigationPaletteNext();
                        return;
                    }
                    if (Input.IsKeyPressed(InputKey.Enter))
                    {
                        if (!IsControlDown()) vm.ExecuteNavigationPaletteSelect();
                        SyncNavigationPaletteFocus();
                        return;
                    }
                }

                if (keyboardControl != null && (layer.UIContext.EventManager.FocusedWidget != keyboardControl || !IsAvailable(keyboardControl)))
                    ClearKeyboardFocus();

                if (Input.IsKeyPressed(InputKey.Tab))
                    MoveKeyboardFocus(Input.IsKeyDown(InputKey.LeftShift) || Input.IsKeyDown(InputKey.RightShift) || Input.IsKeyDown(InputKey.LeftControl) || Input.IsKeyDown(InputKey.RightControl));

                if (Input.IsKeyPressed(InputKey.Enter) && !Input.IsKeyDown(InputKey.LeftControl) && !Input.IsKeyDown(InputKey.RightControl) && keyboardControl is ButtonWidget)
                {
                    var id = keyboardControl.Id;
                    layer.UIContext.EventManager.ClearFocus();
                    ClearKeyboardFocus();
                    vm?.ExecuteKeyboardControl(id);
                }

                var focusedId = layer.UIContext.EventManager.FocusedWidget?.Id;
                if (focusedId == "ForgeArgument" || focusedId == "ForgeCommandArgument")
                {
                    if (Input.IsKeyPressed(InputKey.Up)) vm?.ExecuteHistoryPrevious();
                    else if (Input.IsKeyPressed(InputKey.Down)) vm?.ExecuteHistoryNext();
                }

                if (Input.IsKeyDown(InputKey.LeftControl) || Input.IsKeyDown(InputKey.RightControl))
                {
                    if (Input.IsKeyPressed(InputKey.F))
                    {
                        FocusMainSearch();
                    }
                    else if (Input.IsKeyPressed(InputKey.Enter)) { layer.UIContext.EventManager.ClearFocus(); vm?.ExecuteRefresh(); }
                    else if (Input.IsKeyPressed(InputKey.L)) vm?.ExecuteClearOutput();
                    else if (Input.IsKeyPressed(InputKey.K)) vm?.ExecuteToggleKeyHelp();
                    else if (Input.IsKeyPressed(InputKey.W)) vm?.ExecuteToggleLiveWatch();
                    else if (Input.IsKeyPressed(InputKey.Up)) vm?.ExecuteHistoryPrevious();
                    else if (Input.IsKeyPressed(InputKey.Down)) vm?.ExecuteHistoryNext();
                    else if (Input.IsKeyPressed(InputKey.D1)) vm?.ExecuteCategoryOverview();
                    else if (Input.IsKeyPressed(InputKey.D2)) vm?.ExecuteCategoryInspector();
                    else if (Input.IsKeyPressed(InputKey.D3)) vm?.ExecuteCategoryToolkit();
                    else if (Input.IsKeyPressed(InputKey.D4)) vm?.ExecuteCategoryWeave();
                    else if (Input.IsKeyPressed(InputKey.D5)) vm?.ExecuteCategorySimulate();
                    else if (Input.IsKeyPressed(InputKey.D6)) vm?.ExecuteCategoryAudit();
                    else if (Input.IsKeyPressed(InputKey.D7)) vm?.ExecuteCategoryNovice();
                    else if (Input.IsKeyPressed(InputKey.D8)) vm?.ExecuteDependencies();
                    else if (Input.IsKeyPressed(InputKey.D9)) vm?.ExecuteExtensions();
                    else if (Input.IsKeyPressed(InputKey.Left)) vm?.ExecutePrevious();
                    else if (Input.IsKeyPressed(InputKey.Right)) vm?.ExecuteNext();
                }
            }
            catch (Exception e) { runtime.Register("CalradiaForge", "Error", e.ToString()); Close(); }
            finally { runtime?.RecordCallbackTime((Stopwatch.GetTimestamp() - callbackStarted) * 1000.0 / Stopwatch.Frequency); }
        }
        void ClearKeyboardFocus()
        {
            keyboardControl=null;vm?.SetKeyboardFocus("");
        }
        bool IsControlDown() => Input.IsKeyDown(InputKey.LeftControl) || Input.IsKeyDown(InputKey.RightControl);
        void SyncNavigationPaletteFocus()
        {
            if (vm == null || layer == null) return;

            var isOpen = vm.IsNavigationPaletteOpen;
            if (isOpen && !navigationPaletteWasOpen)
            {
                navigationPaletteReturnFocus = layer.UIContext.EventManager.FocusedWidget;
                ClearKeyboardFocus();
                FocusNavigationPaletteSearch();
            }
            else if (!isOpen && navigationPaletteWasOpen)
            {
                RestoreNavigationPaletteFocus();
            }

            if (vm.ConsumeNavigationPaletteFocusSearchRequest()) FocusMainSearch();
            navigationPaletteWasOpen = isOpen;
        }
        void RestoreNavigationPaletteFocus()
        {
            var previous = navigationPaletteReturnFocus;
            navigationPaletteReturnFocus = null;
            if (previous is ButtonWidget || previous is EditableTextWidget)
            {
                if (IsAvailable(previous))
                {
                    layer.UIContext.EventManager.FocusedWidget = previous;
                    SetKeyboardControl(previous);
                    return;
                }
            }

            layer.UIContext.EventManager.ClearFocus();
            ClearKeyboardFocus();
        }
        void FocusNavigationPaletteSearch() => FocusFirstWidget(IsNavigationPaletteSearchPredicate);
        void FocusMainSearch()
        {
            var predicate = vm?.IsSdkCatalogOpen == true ? IsSdkCatalogSearchPredicate : IsForgeArgumentPredicate;
            FocusFirstWidget(predicate);
        }
        void FocusFirstWidget(Func<Widget, bool> predicate)
        {
            if (layer == null) return;
            var field = layer.UIContext.Root.GetFirstInChildrenRecursive(predicate);
            if (field == null || !IsAvailable(field)) return;
            ClearKeyboardFocus();
            layer.UIContext.EventManager.FocusedWidget = field;
            SetKeyboardControl(field);
        }
        void SetKeyboardControl(Widget widget)
        {
            keyboardControl = widget is ButtonWidget || widget is EditableTextWidget ? widget : null;
            vm?.SetKeyboardFocus(GetKeyboardFocusLabel(widget));
        }
        string GetKeyboardFocusLabel(Widget widget)
        {
            if (widget is ButtonWidget button)
            {
                var label = FindFirstTextWidget(button);
                return label?.Text ?? button.Id ?? "";
            }
            if (widget is EditableTextWidget field)
            {
                switch (field.Id)
                {
                    case "ForgeAssemblyPath": return vm?.AssemblyPathLabel ?? "";
                    case "ForgeAssemblyVersion": return vm?.AssemblyVersionLabel ?? "";
                    case "ForgeSdkCatalogSearch": return vm?.SdkCatalogSearchPlaceholder ?? "";
                    case "ForgeNavigationPaletteSearch": return vm?.NavigationPaletteSearchPlaceholder ?? "";
                    default: return vm?.InputLabel ?? "";
                }
            }
            return "";
        }
        static bool IsAvailable(Widget widget)
        {
            for(var current=widget;current!=null;current=current.ParentWidget)
                if(!current.IsVisible || current.IsDisabled)return false;
            return true;
        }
        static void CollectFocusableControls(Widget widget, List<Widget> list)
        {
            if(widget==null || !widget.IsVisible || widget.IsDisabled)return;
            if((widget is ButtonWidget && (widget.Id??"").StartsWith("Forge",StringComparison.Ordinal)) ||
               widget is EditableTextWidget)
                list.Add(widget);
            for(int i=0;i<widget.ChildCount;i++)
                CollectFocusableControls(widget.GetChild(i),list);
        }
        static TextWidget FindFirstTextWidget(Widget widget)
        {
            if(widget==null)return null;
            if(widget is TextWidget textWidget)return textWidget;
            for(int i=0;i<widget.ChildCount;i++)
            {
                var found=FindFirstTextWidget(widget.GetChild(i));
                if(found!=null)return found;
            }
            return null;
        }
        void MoveKeyboardFocus(bool backwards)
        {
            var ui=layer.UIContext;
            var controls=new List<Widget>();
            CollectFocusableControls(ui.Root,controls);
            if(controls.Count==0)return;
            var index=controls.IndexOf(ui.EventManager.FocusedWidget);
            index=index<0?(backwards?controls.Count-1:0):(index+(backwards?-1:1)+controls.Count)%controls.Count;
            ClearKeyboardFocus();
            keyboardControl=controls[index];ui.EventManager.FocusedWidget=keyboardControl;
            SetKeyboardControl(keyboardControl);
        }
        void Open()
        {
            if(layer!=null)return;
            runtime.RefreshGameLanguage();
            owner=ScreenManager.TopScreen;if(owner==null)throw new InvalidOperationException("No active game screen for the panel");
            runtime.Register("CalradiaForge","Info","Opening panel on "+owner.GetType().FullName);
            runtime.Register("CalradiaForge","Info","Creating panel GauntletLayer.");
            vm=new PanelViewModel(runtime,Close);layer=new GauntletLayer("CalradiaForge",500,true);
            runtime.Register("CalradiaForge","Info","Panel GauntletLayer created.");
            try {
                layer.UIContext.BrushFactory.LoadBrushFile("CalradiaForge");
                layer.LoadMovie("CalradiaForge",vm);layer.IsFocusLayer=true;layer.InputRestrictions.SetInputRestrictions();owner.AddLayer(layer);
                runtime.Register("CalradiaForge","Info","Panel GauntletLayer attached to owner.");
                ScreenManager.TrySetFocus(layer);
                runtime.Register("CalradiaForge","Info","Panel movie loaded and layer attached");
            } catch {Close();throw;}
        }
        void OpenExtensionPage(string pageId)
        {
            if (runtime == null) throw new InvalidOperationException("Calradia Forge runtime is not ready.");
            if (!runtime.IsGameThread)
            {
                if (!runtime.TryPostToMainThread(() =>
                {
                    try { OpenExtensionPageOnGameThread(pageId); }
                    catch (Exception error) { runtime?.Register("CalradiaForge", "Error", "Extension page request failed: " + error.Message); }
                }))
                    throw new InvalidOperationException("The game-thread request queue is full; retry after the game updates.");
                return;
            }
            OpenExtensionPageOnGameThread(pageId);
        }
        void OpenExtensionPageOnGameThread(string pageId)
        {
            if (runtime == null) throw new InvalidOperationException("Calradia Forge runtime is not ready.");
            var registry = CalradiaForge.Sdk.ForgeApi.UI;
            var page = registry?.FindPage(pageId);
            if (page == null) throw new InvalidOperationException("No valid registered page has ID '" + pageId + "'.");
            var policyReason = CalradiaForge.Sdk.ForgeUiPolicy.GetUnavailableReason(
                page, runtime.CurrentContext, runtime.TestEngine.TestingEnabled, runtime.TestEngine.CampaignCopyConfirmed);
            if (policyReason != null) throw new InvalidOperationException(policyReason);
            if (!typeof(TaleWorlds.Library.ViewModel).IsAssignableFrom(page.ViewModelType))
                throw new InvalidOperationException("The registered page type must derive from TaleWorlds.Library.ViewModel.");
            if (owner == null || owner.IsFinalized || ScreenManager.TopScreen != owner)
                throw new InvalidOperationException("Open Forge from the active screen before opening an extension page.");
            CloseExtensionPage();
            var createdViewModel = page.CreateViewModel() as TaleWorlds.Library.ViewModel;
            if (createdViewModel == null) throw new InvalidOperationException("The extension ViewModel could not be constructed.");
            extensionOwner = owner;
            extensionPageOwner = page.Owner;
            extensionViewModel = createdViewModel;
            extensionLayer = new GauntletLayer("CalradiaForge.Extension." + page.Id, 510, true);
            try
            {
                var moduleBrushes = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(page.PrefabPath), "..", "Brushes", page.Owner + ".xml");
                if (System.IO.File.Exists(moduleBrushes)) extensionLayer.UIContext.BrushFactory.LoadBrushFile(page.Owner);
                extensionLayer.LoadMovie(page.Prefab, extensionViewModel);
                extensionLayer.IsFocusLayer = true;
                extensionLayer.InputRestrictions.SetInputRestrictions();
                extensionOwner.AddLayer(extensionLayer);
                ScreenManager.TrySetFocus(extensionLayer);
                runtime.Register(page.Owner, "Info", "Opened standalone Gauntlet page " + page.Id + " from " + page.PrefabPath);
            }
            catch
            {
                CloseExtensionPage();
                throw;
            }
        }
        void CloseExtensionPage()
        {
            if (extensionLayer == null && extensionViewModel == null) return;
            var closingLayer = extensionLayer;
            var closingOwner = extensionOwner;
            var closingViewModel = extensionViewModel;
            extensionLayer = null; extensionOwner = null; extensionViewModel = null; extensionPageOwner = null;
            try
            {
                if (closingLayer != null && !closingLayer.IsFinalized && closingOwner != null && !closingOwner.IsFinalized)
                {
                    closingLayer.InputRestrictions.ResetInputRestrictions();
                    ScreenManager.TryLoseFocus(closingLayer);
                    if (closingOwner.HasLayer(closingLayer)) closingOwner.RemoveLayer(closingLayer);
                }
            }
            finally { closingViewModel?.OnFinalize(); }
        }
        void RequestCloseExtensionPage()
        {
            var currentRuntime = runtime;
            if (currentRuntime == null) return;
            GameThreadActionDispatch.RunOrPost(currentRuntime.IsGameThread, currentRuntime.PostToMainThread, CloseExtensionPage);
        }
        void OnUiPagesRemoved(string moduleId)
        {
            var currentRuntime = runtime;
            if (currentRuntime == null) return;

            // Module unload callbacks may come from a different thread. Read the active
            // owner and release Gauntlet resources only after returning to the game thread.
            GameThreadActionDispatch.RunOrPost(currentRuntime.IsGameThread, currentRuntime.PostToMainThread, () =>
            {
                if (string.Equals(extensionPageOwner, moduleId, StringComparison.OrdinalIgnoreCase)) CloseExtensionPage();
            });
        }
        void Close()
        {
            CloseExtensionPage();
            if(layer==null)return;
            keyboardControl=null;
            navigationPaletteReturnFocus=null;
            navigationPaletteWasOpen=false;
            var closingLayer=layer;var closingOwner=owner;var closingViewModel=vm;
            // Detach our references first: removing a layer can reenter screen teardown.
            layer=null;owner=null;vm=null;
            try {
                // Screen shutdown already releases its layers. Touching their input/native
                // resources again during module unload can access a destroyed screen.
                if(!closingLayer.IsFinalized && closingOwner!=null && !closingOwner.IsFinalized) {
                    closingLayer.InputRestrictions.ResetInputRestrictions();
                    ScreenManager.TryLoseFocus(closingLayer);
                    if(closingOwner.HasLayer(closingLayer))closingOwner.RemoveLayer(closingLayer);
                }
            } finally {closingViewModel?.CancelPendingWork();closingViewModel?.OnFinalize();}
        }
        protected override void OnSubModuleUnloaded()
        {
            try
            {
                Close();
                CalradiaForge.Sdk.ForgeUI.PageOpenRequested -= OpenExtensionPage;
                CalradiaForge.Sdk.ForgeUI.PageCloseRequested -= RequestCloseExtensionPage;
                CalradiaForge.Sdk.ForgeApi.UiPagesRemoved -= OnUiPagesRemoved;
                CalradiaForge.Sdk.ForgeCampaignEvents.ClearSubscribers();
                CalradiaForge.Sdk.CampaignVariableInspector.ClearTrackedVariables();
                CalradiaForge.Sdk.CampaignVariableInspector.ClearSnapshotListeners();
                CalradiaForge.Sdk.ForgeData.ClearAll();
                CalradiaForge.Sdk.ForgeAgentMemory.ClearAll();
                CalradiaForge.Sdk.ForgeUI.Clear();
                CalradiaForge.Sdk.ForgeDetour.UnpatchAll();
            }
            finally
            {
                runtime?.Dispose();
                runtime = null;
                CurrentRuntime = null;
            }
        }
        public override void OnMissionBehaviorInitialize(Mission mission)
        {
            if(GameNetwork.IsMultiplayer)return;
            runtime?.NotifyMissionInitialized();
            mission.AddMissionBehavior(new EventObserver(runtime));
        }
    }
    internal sealed class EventObserver : MissionBehavior
    {
        readonly Runtime runtime;public EventObserver(Runtime r){runtime=r;}
        public override MissionBehaviorType BehaviorType=>MissionBehaviorType.Other;
        public override void OnAgentCreated(Agent agent)=>runtime?.NotifyAgentCreated(agent?.Index??-1);
        public override void OnAgentDeleted(Agent agent)=>runtime?.NotifyAgentRemoved(agent?.Index??-1);
        protected override void OnEndMission()
        {
            runtime?.NotifyMissionEnded();
            CalradiaForge.Sdk.ForgeAgentMemory.ClearAll();
            base.OnEndMission();
        }
    }
}
