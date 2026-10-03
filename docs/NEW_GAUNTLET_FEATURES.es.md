# Nuevas funciones de interfaz Gauntlet para Calradia Forge

## Función 1: HUD de análisis de combate en tiempo real

### Propósito
Mostrar estadísticas de combate en vivo directamente en la pantalla del juego durante las batallas para proporcionar información táctica a los comandantes.

### Implementación

#### Extensión del ViewModel
```csharp
using TaleWorlds.Library;
using TaleWorlds.ScreenSystem;
using CalradiaForge.Sdk;

namespace CalradiaForge.UI.ViewModels
{
    public class BattleAnalyticsViewModel : ViewModel
    {
        private MBBindingList<BattleStatItemVM> _battleStats = new MBBindingList<BattleStatItemVM>();
        private string _battlePhase = "Preparation";
        private int _alliedCasualties = 0;
        private int _enemyCasualties = 0;
        private float _moraleScore = 100f;
        private bool _isAnalyticsVisible = false;

        [DataSourceProperty]
        public string BattlePhase
        {
            get => _battlePhase;
            set
            {
                if (value != _battlePhase)
                {
                    _battlePhase = value;
                    OnPropertyChangedWithValue(value);
                }
            }
        }

        [DataSourceProperty]
        public int AlliedCasualties
        {
            get => _alliedCasualties;
            set
            {
                if (value != _alliedCasualties)
                {
                    _alliedCasualties = value;
                    OnPropertyChangedWithValue(value);
                }
            }
        }

        [DataSourceProperty]
        public int EnemyCasualties
        {
            get => _enemyCasualties;
            set
            {
                if (value != _enemyCasualties)
                {
                    _enemyCasualties = value;
                    OnPropertyChangedWithValue(value);
                }
            }
        }

        [DataSourceProperty]
        public float MoraleScore
        {
            get => _moraleScore;
            set
            {
                if (value != _moraleScore)
                {
                    _moraleScore = value;
                    OnPropertyChangedWithValue(value);
                }
            }
        }

        [DataSourceProperty]
        public bool IsAnalyticsVisible
        {
            get => _isAnalyticsVisible;
            set
            {
                if (value != _isAnalyticsVisible)
                {
                    _isAnalyticsVisible = value;
                    OnPropertyChangedWithValue(value);
                }
            }
        }

        [DataSourceProperty]
        public MBBindingList<BattleStatItemVM> BattleStats
        {
            get => _battleStats;
            set
            {
                if (value != _battleStats)
                {
                    _battleStats = value;
                    OnPropertyChangedWithValue(value);
                }
            }
        }

        public void ExecuteToggleAnalytics()
        {
            IsAnalyticsVisible = !IsAnalyticsVisible;
        }

        public void UpdateBattleStats(Mission mission)
        {
            if (mission == null) return;

            // Update combat statistics using ForgeCombatTactics
            var tactics = new ForgeCombatTactics();
            var stats = tactics.CalculateBattleStatistics(mission);

            AlliedCasualties = stats.AlliedCasualties;
            EnemyCasualties = stats.EnemyCasualties;
            MoraleScore = stats.MoraleScore;
            BattlePhase = stats.CurrentPhase;

            // Update formation-specific stats
            _battleStats.Clear();
            foreach (var formationStat in stats.FormationStats)
            {
                _battleStats.Add(new BattleStatItemVM(
                    formationStat.FormationName,
                    formationStat.Strength,
                    formationStat.Casualties,
                    formationStat.Effectiveness
                ));
            }
        }
    }

    public class BattleStatItemVM : ViewModel
    {
        private string _formationName;
        private int _strength;
        private int _casualties;
        private float _effectiveness;

        public BattleStatItemVM(string formationName, int strength, int casualties, float effectiveness)
        {
            _formationName = formationName;
            _strength = strength;
            _casualties = casualties;
            _effectiveness = effectiveness;
        }

        [DataSourceProperty]
        public string FormationName
        {
            get => _formationName;
            set
            {
                if (value != _formationName)
                {
                    _formationName = value;
                    OnPropertyChangedWithValue(value);
                }
            }
        }

        [DataSourceProperty]
        public int Strength
        {
            get => _strength;
            set
            {
                if (value != _strength)
                {
                    _strength = value;
                    OnPropertyChangedWithValue(value);
                }
            }
        }

        [DataSourceProperty]
        public int Casualties
        {
            get => _casualties;
            set
            {
                if (value != _casualties)
                {
                    _casualties = value;
                    OnPropertyChangedWithValue(value);
                }
            }
        }

        [DataSourceProperty]
        public float Effectiveness
        {
            get => _effectiveness;
            set
            {
                if (value != _effectiveness)
                {
                    _effectiveness = value;
                    OnPropertyChangedWithValue(value);
                }
            }
        }
    }
}
```

#### Prefab XML de Gauntlet
```xml
<Prefab>
  <Window>
    <Widget Id="BattleAnalyticsHUD" WidthSizePolicy="Fixed" SuggestedWidth="400"
            HeightSizePolicy="Fixed" SuggestedHeight="300"
            HorizontalAlignment="Right" VerticalAlignment="Top"
            Sprite="StdAssets\Background\paper_panel_dark"
            DoNotAcceptEvents="true" DoNotPassEventsToChildren="true"
            Visibility="@IsAnalyticsVisible">
      <Children>
        <!-- Header -->
        <ListPanel WidthSizePolicy="StretchToParent" HeightSizePolicy="CoverChildren"
                   MarginTop="10" MarginLeft="15" MarginRight="15">
          <Children>
            <TextWidget WidthSizePolicy="CoverChildren" HeightSizePolicy="CoverChildren"
                        Text="{=forge_battle_analytics}Battle Analytics" 
                        Brush="CalradiaForge.Gold" Brush.FontSize="20" />
            <TextWidget WidthSizePolicy="CoverChildren" HeightSizePolicy="CoverChildren"
                        Text="@BattlePhase" 
                        Brush="CalradiaForge.Text" Brush.FontSize="14" MarginLeft="10"/>
          </Children>
        </ListPanel>

        <!-- Casualties Overview -->
        <ListPanel WidthSizePolicy="StretchToParent" HeightSizePolicy="CoverChildren"
                   MarginTop="40" MarginLeft="15" MarginRight="15">
          <Children>
            <TextWidget WidthSizePolicy="CoverChildren" HeightSizePolicy="CoverChildren"
                        Text="{=forge_allied_casualties}Allied: @AlliedCasualties" 
                        Brush="CalradiaForge.Text" Brush.FontSize="14"/>
            <TextWidget WidthSizePolicy="CoverChildren" HeightSizePolicy="CoverChildren"
                        Text="{=forge_enemy_casualties}Enemy: @EnemyCasualties" 
                        Brush="CalradiaForge.Text" Brush.FontSize="14" MarginLeft="10"/>
            <TextWidget WidthSizePolicy="CoverChildren" HeightSizePolicy="CoverChildren"
                        Text="{=forge_morale}Morale: @MoraleScore" 
                        Brush="CalradiaForge.Text" Brush.FontSize="14" MarginLeft="10"/>
          </Children>
        </ListPanel>

        <!-- Formation Stats -->
        <ScrollablePanel WidthSizePolicy="StretchToParent" HeightSizePolicy="StretchToParent"
                         MarginTop="80" MarginBottom="10" MarginLeft="15" MarginRight="15"
                         ClipRect="Clip" InnerPanel="Clip\ListContainer" VerticalScrollbar="ScrollBar">
          <Children>
            <Widget Id="Clip" WidthSizePolicy="StretchToParent" HeightSizePolicy="StretchToParent" ClipContents="true">
              <Children>
                <ListPanel Id="ListContainer" WidthSizePolicy="StretchToParent" HeightSizePolicy="CoverChildren"
                           StackLayout.LayoutMethod="VerticalBottomToTop">
                  <ItemTemplate>
                    <ButtonWidget WidthSizePolicy="StretchToParent" HeightSizePolicy="Fixed" SuggestedHeight="35"
                                  Brush="CalradiaForge.RowButton" IsDisabled="true">
                      <Children>
                        <TextWidget WidthSizePolicy="CoverChildren" HeightSizePolicy="CoverChildren"
                                    HorizontalAlignment="Left" VerticalAlignment="Center" MarginLeft="8"
                                    Text="@FormationName" Brush="CalradiaForge.Text" Brush.FontSize="12"/>
                        <TextWidget WidthSizePolicy="Fixed" SuggestedWidth="60" HeightSizePolicy="CoverChildren"
                                    HorizontalAlignment="Right" VerticalAlignment="Center" MarginRight="8"
                                    Text="@Strength" Brush="CalradiaForge.Text" Brush.FontSize="12"/>
                        <TextWidget WidthSizePolicy="Fixed" SuggestedWidth="50" HeightSizePolicy="CoverChildren"
                                    HorizontalAlignment="Right" VerticalAlignment="Center" MarginRight="8"
                                    Text="@Casualties" Brush="CalradiaForge.Text" Brush.FontSize="12"/>
                        <TextWidget WidthSizePolicy="Fixed" SuggestedWidth="50" HeightSizePolicy="CoverChildren"
                                    HorizontalAlignment="Right" VerticalAlignment="Center" MarginRight="8"
                                    Text="@Effectiveness" Brush="CalradiaForge.Text" Brush.FontSize="12"/>
                      </Children>
                    </ButtonWidget>
                  </ItemTemplate>
                </ListPanel>
              </Children>
            </Widget>
            <ScrollbarWidget Id="ScrollBar" WidthSizePolicy="Fixed" SuggestedWidth="8"
                             HeightSizePolicy="StretchToParent" HorizontalAlignment="Right" />
          </Children>
        </ScrollablePanel>

        <!-- Toggle Button -->
        <ButtonWidget WidthSizePolicy="Fixed" SuggestedWidth="120" HeightSizePolicy="Fixed" SuggestedHeight="30"
                      HorizontalAlignment="Right" VerticalAlignment="Bottom" MarginRight="15" MarginBottom="5"
                      Brush="CalradiaForge.ActionBtn" Command.Click="ExecuteToggleAnalytics">
          <Children>
            <TextWidget WidthSizePolicy="CoverChildren" HeightSizePolicy="CoverChildren"
                        HorizontalAlignment="Center" VerticalAlignment="Center"
                        Text="{=forge_toggle}Toggle" Brush="CalradiaForge.Text" />
          </Children>
        </ButtonWidget>
      </Children>
    </Widget>
  </Window>
</Prefab>
```

#### Integración con MissionBehavior
```csharp
using TaleWorlds.MountAndBlade;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.ScreenSystem;

namespace CalradiaForge.Mod
{
    public class BattleAnalyticsBehavior : MissionBehavior
    {
        private GauntletLayer _gauntletLayer;
        private BattleAnalyticsViewModel _viewModel;
        private IGauntletMovie _movie;

        public override MissionBehaviorType BehaviorType => MissionBehaviorType.Other;

        protected override void OnMissionStart()
        {
            base.OnMissionStart();
            
            _viewModel = new BattleAnalyticsViewModel();
            _gauntletLayer = new GauntletLayer(150) { IsFocusLayer = false };
            _gauntletLayer.InputRestrictions.SetInputRestrictions(false);
            
            _movie = _gauntletLayer.LoadMovie("BattleAnalyticsHUD", _viewModel);
            Mission.Current.AddMissionBehavior(this);
            MissionScreen.AddLayer(_gauntletLayer);
        }

        protected override void OnMissionTick(float dt)
        {
            base.OnMissionTick(dt);
            
            // Update analytics every second
            if (Mission.Current != null)
            {
                _viewModel.UpdateBattleStats(Mission.Current);
            }
        }

        protected override void OnMissionEnd()
        {
            base.OnMissionEnd();
            
            if (_gauntletLayer != null)
            {
                MissionScreen.RemoveLayer(_gauntletLayer);
                _gauntletLayer.ReleaseMovie(_movie);
                _gauntletLayer = null;
            }
            _viewModel = null;
        }
    }
}
```

## Función 2: Sistema dinámico de notificaciones de misiones

### Propósito
Proporcionar notificaciones de misiones detalladas e interactivas, con seguimiento del progreso y botones de acción inmediata.

### Extensión del ViewModel
```csharp
namespace CalradiaForge.UI.ViewModels
{
    public class QuestNotificationViewModel : ViewModel
    {
        private MBBindingList<QuestNotificationItemVM> _activeQuests = new MBBindingList<QuestNotificationItemVM>();
        private bool _isNotificationPanelVisible = false;
        private string _selectedQuestId = "";

        [DataSourceProperty]
        public bool IsNotificationPanelVisible
        {
            get => _isNotificationPanelVisible;
            set
            {
                if (value != _isNotificationPanelVisible)
                {
                    _isNotificationPanelVisible = value;
                    OnPropertyChangedWithValue(value);
                }
            }
        }

        [DataSourceProperty]
        public MBBindingList<QuestNotificationItemVM> ActiveQuests
        {
            get => _activeQuests;
            set
            {
                if (value != _activeQuests)
                {
                    _activeQuests = value;
                    OnPropertyChangedWithValue(value);
                }
            }
        }

        public void ExecuteToggleNotificationPanel()
        {
            IsNotificationPanelVisible = !IsNotificationPanelVisible;
        }

        public void ExecuteTrackQuest(string questId)
        {
            _selectedQuestId = questId;
            // Set quest as tracked in the campaign
            ForgeQuestBuilder.SetTrackedQuest(questId);
        }

        public void ExecuteOpenQuestLog()
        {
            // Open the vanilla quest log screen
            // Campaign.Current.EncyclopediaManager.GoToURL("Quests");
        }

        public void AddQuestNotification(string questId, string title, string description, float progress)
        {
            _activeQuests.Add(new QuestNotificationItemVM(questId, title, description, progress));
        }

        public void UpdateQuestProgress(string questId, float progress)
        {
            var quest = _activeQuests.FirstOrDefault(q => q.QuestId == questId);
            if (quest != null)
            {
                quest.Progress = progress;
            }
        }
    }

    public class QuestNotificationItemVM : ViewModel
    {
        private string _questId;
        private string _title;
        private string _description;
        private float _progress;

        public QuestNotificationItemVM(string questId, string title, string description, float progress)
        {
            _questId = questId;
            _title = title;
            _description = description;
            _progress = progress;
        }

        [DataSourceProperty]
        public string QuestId
        {
            get => _questId;
            set
            {
                if (value != _questId)
                {
                    _questId = value;
                    OnPropertyChangedWithValue(value);
                }
            }
        }

        [DataSourceProperty]
        public string Title
        {
            get => _title;
            set
            {
                if (value != _title)
                {
                    _title = value;
                    OnPropertyChangedWithValue(value);
                }
            }
        }

        [DataSourceProperty]
        public string Description
        {
            get => _description;
            set
            {
                if (value != _description)
                {
                    _description = value;
                    OnPropertyChangedWithValue(value);
                }
            }
        }

        [DataSourceProperty]
        public float Progress
        {
            get => _progress;
            set
            {
                if (value != _progress)
                {
                    _progress = value;
                    OnPropertyChangedWithValue(value);
                }
            }
        }
    }
}
```

#### Prefab XML de Gauntlet
```xml
<Prefab>
  <Window>
    <Widget Id="QuestNotificationPanel" WidthSizePolicy="Fixed" SuggestedWidth="350"
            HeightSizePolicy="Fixed" SuggestedHeight="450"
            HorizontalAlignment="Left" VerticalAlignment="Top"
            Sprite="StdAssets\Background\paper_panel_dark"
            DoNotAcceptEvents="true" DoNotPassEventsToChildren="true"
            Visibility="@IsNotificationPanelVisible">
      <Children>
        <!-- Header -->
        <ListPanel WidthSizePolicy="StretchToParent" HeightSizePolicy="CoverChildren"
                   MarginTop="12" MarginLeft="15" MarginRight="15">
          <Children>
            <TextWidget WidthSizePolicy="CoverChildren" HeightSizePolicy="CoverChildren"
                        Text="{=forge_active_quests}Active Quests" 
                        Brush="CalradiaForge.Gold" Brush.FontSize="18" />
            <ButtonWidget WidthSizePolicy="Fixed" SuggestedWidth="100" HeightSizePolicy="Fixed" SuggestedHeight="25"
                          HorizontalAlignment="Right" VerticalAlignment="Center"
                          Brush="CalradiaForge.ActionBtn" Command.Click="ExecuteOpenQuestLog">
              <Children>
                <TextWidget WidthSizePolicy="CoverChildren" HeightSizePolicy="CoverChildren"
                            HorizontalAlignment="Center" VerticalAlignment="Center"
                            Text="{=forge_quest_log}Quest Log" Brush="CalradiaForge.Text" Brush.FontSize="12"/>
              </Children>
            </ButtonWidget>
          </Children>
        </ListPanel>

        <!-- Quest List -->
        <ScrollablePanel WidthSizePolicy="StretchToParent" HeightSizePolicy="StretchToParent"
                         MarginTop="50" MarginBottom="60" MarginLeft="15" MarginRight="15"
                         ClipRect="Clip" InnerPanel="Clip\ListContainer" VerticalScrollbar="ScrollBar">
          <Children>
            <Widget Id="Clip" WidthSizePolicy="StretchToParent" HeightSizePolicy="StretchToParent" ClipContents="true">
              <Children>
                <ListPanel Id="ListContainer" WidthSizePolicy="StretchToParent" HeightSizePolicy="CoverChildren"
                           StackLayout.LayoutMethod="VerticalBottomToTop">
                  <ItemTemplate>
                    <ButtonWidget WidthSizePolicy="StretchToParent" HeightSizePolicy="Fixed" SuggestedHeight="70"
                                  Brush="CalradiaForge.QuestCard">
                      <Children>
                        <ListPanel WidthSizePolicy="StretchToParent" HeightSizePolicy="CoverChildren"
                                   MarginLeft="10" MarginRight="10" MarginTop="5" MarginBottom="5">
                          <Children>
                            <TextWidget WidthSizePolicy="CoverChildren" HeightSizePolicy="CoverChildren"
                                        Text="@Title" Brush="CalradiaForge.Gold" Brush.FontSize="14"/>
                            <TextWidget WidthSizePolicy="CoverChildren" HeightSizePolicy="CoverChildren"
                                        Text="@Description" Brush="CalradiaForge.Text" Brush.FontSize="11" 
                                        TextWrap="true" MaxTextWidth="300"/>
                            <ListPanel WidthSizePolicy="StretchToParent" HeightSizePolicy="CoverChildren">
                              <Children>
                                <TextWidget WidthSizePolicy="CoverChildren" HeightSizePolicy="CoverChildren"
                                            Text="{=forge_progress}Progress:" Brush="CalradiaForge.Text" Brush.FontSize="10"/>
                                <Widget WidthSizePolicy="StretchToParent" HeightSizePolicy="Fixed" SuggestedHeight="8"
                                        Sprite="StdAssets\UI\Components\bar_fill" 
                                        ScaledWidthFactor="@Progress" HorizontalAlignment="Stretch"/>
                              </Children>
                            </ListPanel>
                            <ButtonWidget WidthSizePolicy="Fixed" SuggestedWidth="80" HeightSizePolicy="Fixed" SuggestedHeight="20"
                                          HorizontalAlignment="Right" VerticalAlignment="Bottom"
                                          Brush="CalradiaForge.SmallBtn" 
                                          Command.Click="ExecuteTrackQuest">
                              <Children>
                                <TextWidget WidthSizePolicy="CoverChildren" HeightSizePolicy="CoverChildren"
                                            HorizontalAlignment="Center" VerticalAlignment="Center"
                                            Text="{=forge_track}Track" Brush="CalradiaForge.Text" Brush.FontSize="10"/>
                              </Children>
                            </ButtonWidget>
                          </Children>
                        </ListPanel>
                      </Children>
                    </ButtonWidget>
                  </ItemTemplate>
                </ListPanel>
              </Children>
            </Widget>
            <ScrollbarWidget Id="ScrollBar" WidthSizePolicy="Fixed" SuggestedWidth="8"
                             HeightSizePolicy="StretchToParent" HorizontalAlignment="Right" />
          </Children>
        </ScrollablePanel>

        <!-- Toggle Button -->
        <ButtonWidget WidthSizePolicy="Fixed" SuggestedWidth="30" HeightSizePolicy="Fixed" SuggestedHeight="30"
                      HorizontalAlignment="Right" VerticalAlignment="Bottom" MarginRight="10" MarginBottom="10"
                      Brush="CalradiaForge.IconBtn" Command.Click="ExecuteToggleNotificationPanel">
          <Children>
            <TextWidget WidthSizePolicy="CoverChildren" HeightSizePolicy="CoverChildren"
                        HorizontalAlignment="Center" VerticalAlignment="Center"
                        Text="📜" Brush="CalradiaForge.Text" Brush.FontSize="16"/>
          </Children>
        </ButtonWidget>
      </Children>
    </Widget>
  </Window>
</Prefab>
```

## Función 3: Panel de resumen de recursos de asentamientos

### Propósito
Mostrar información completa de recursos de los asentamientos —incluidos alimentos, milicia, prosperidad y lealtad— directamente en el mapa de campaña.

### Extensión del ViewModel
```csharp
namespace CalradiaForge.UI.ViewModels
{
    public class SettlementResourceViewModel : ViewModel
    {
        private string _settlementName = "";
        private float _foodStocks = 0f;
        private float _militia = 0f;
        private float _prosperity = 0f;
        private float _loyalty = 0f;
        private float _hearth = 0f;
        private bool _isResourcePanelVisible = false;
        private MBBindingList<ResourceItemVM> _resources = new MBBindingList<ResourceItemVM>();

        [DataSourceProperty]
        public string SettlementName
        {
            get => _settlementName;
            set
            {
                if (value != _settlementName)
                {
                    _settlementName = value;
                    OnPropertyChangedWithValue(value);
                }
            }
        }

        [DataSourceProperty]
        public float FoodStocks
        {
            get => _foodStocks;
            set
            {
                if (value != _foodStocks)
                {
                    _foodStocks = value;
                    OnPropertyChangedWithValue(value);
                }
            }
        }

        [DataSourceProperty]
        public float Militia
        {
            get => _militia;
            set
            {
                if (value != _militia)
                {
                    _militia = value;
                    OnPropertyChangedWithValue(value);
                }
            }
        }

        [DataSourceProperty]
        public float Prosperity
        {
            get => _prosperity;
            set
            {
                if (value != _prosperity)
                {
                    _prosperity = value;
                    OnPropertyChangedWithValue(value);
                }
            }
        }

        [DataSourceProperty]
        public float Loyalty
        {
            get => _loyalty;
            set
            {
                if (value != _loyalty)
                {
                    _loyalty = value;
                    OnPropertyChangedWithValue(value);
                }
            }
        }

        [DataSourceProperty]
        public float Hearth
        {
            get => _hearth;
            set
            {
                if (value != _hearth)
                {
                    _hearth = value;
                    OnPropertyChangedWithValue(value);
                }
            }
        }

        [DataSourceProperty]
        public bool IsResourcePanelVisible
        {
            get => _isResourcePanelVisible;
            set
            {
                if (value != _isResourcePanelVisible)
                {
                    _isResourcePanelVisible = value;
                    OnPropertyChangedWithValue(value);
                }
            }
        }

        [DataSourceProperty]
        public MBBindingList<ResourceItemVM> Resources
        {
            get => _resources;
            set
            {
                if (value != _resources)
                {
                    _resources = value;
                    OnPropertyChangedWithValue(value);
                }
            }
        }

        public void ExecuteToggleResourcePanel()
        {
            IsResourcePanelVisible = !IsResourcePanelVisible;
        }

        public void UpdateSettlementInfo(Settlement settlement)
        {
            if (settlement == null || !settlement.IsTown) return;

            SettlementName = settlement.Name.ToString();
            FoodStocks = settlement.Town.FoodStocks;
            Militia = settlement.Town.Militia;
            Prosperity = settlement.Prosperity;
            Loyalty = settlement.Town.Loyalty;
            Hearth = settlement.Town.Hearth;

            // Update detailed resources using ForgeSettlementSystem
            var settlementSystem = new ForgeSettlementSystem();
            var resourceData = settlementSystem.GetDetailedResources(settlement);

            _resources.Clear();
            foreach (var resource in resourceData)
            {
                _resources.Add(new ResourceItemVM(resource.Name, resource.Amount, resource.ChangeRate));
            }
        }
    }

    public class ResourceItemVM : ViewModel
    {
        private string _name;
        private float _amount;
        private float _changeRate;

        public ResourceItemVM(string name, float amount, float changeRate)
        {
            _name = name;
            _amount = amount;
            _changeRate = changeRate;
        }

        [DataSourceProperty]
        public string Name
        {
            get => _name;
            set
            {
                if (value != _name)
                {
                    _name = value;
                    OnPropertyChangedWithValue(value);
                }
            }
        }

        [DataSourceProperty]
        public float Amount
        {
            get => _amount;
            set
            {
                if (value != _amount)
                {
                    _amount = value;
                    OnPropertyChangedWithValue(value);
                }
            }
        }

        [DataSourceProperty]
        public float ChangeRate
        {
            get => _changeRate;
            set
            {
                if (value != _changeRate)
                {
                    _changeRate = value;
                    OnPropertyChangedWithValue(value);
                }
            }
        }
    }
}
```

#### Integración con CampaignBehavior
```csharp
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Settlements;

namespace CalradiaForge.Mod.CampaignBehaviors
{
    public class SettlementResourceBehavior : CampaignBehaviorBase
    {
        private SettlementResourceViewModel _resourceViewModel;

        public override void RegisterEvents()
        {
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
            CampaignEvents.HourlyTickSettlementEvent.AddNonSerializedListener(this, OnHourlyTickSettlement);
        }

        private void OnSessionLaunched(CampaignGameStarter starter)
        {
            // Initialize the resource panel ViewModel
            _resourceViewModel = new SettlementResourceViewModel();
        }

        private void OnHourlyTickSettlement(Settlement settlement)
        {
            if (settlement == null || !settlement.IsTown) return;
            
            // Update resource information for the player's current settlement
            if (settlement == MobileParty.MainParty.CurrentSettlement)
            {
                _resourceViewModel?.UpdateSettlementInfo(settlement);
            }
        }

        public SettlementResourceViewModel GetResourceViewModel()
        {
            return _resourceViewModel;
        }
    }
}
```

## Integración con el PanelViewModel existente

```csharp
// Add to existing PanelViewModel.cs
private SettlementResourceViewModel _settlementResourceViewModel;
private BattleAnalyticsViewModel _battleAnalyticsViewModel;
private QuestNotificationViewModel _questNotificationViewModel;

public PanelViewModel(Runtime r, Action c) 
{ 
    runtime = r; 
    close = c; 
    Labels(); 
    InitializeTools(); 
    ExecuteSummary();
    
    // Initialize new feature ViewModels
    _settlementResourceViewModel = new SettlementResourceViewModel();
    _battleAnalyticsViewModel = new BattleAnalyticsViewModel();
    _questNotificationViewModel = new QuestNotificationViewModel();
}

[DataSourceProperty]
public bool ShowSettlementResources => _settlementResourceViewModel?.IsResourcePanelVisible ?? false;

[DataSourceProperty]
public bool ShowBattleAnalytics => _battleAnalyticsViewModel?.IsAnalyticsVisible ?? false;

[DataSourceProperty]
public bool ShowQuestNotifications => _questNotificationViewModel?.IsNotificationPanelVisible ?? false;

public void ExecuteToggleSettlementResources()
{
    _settlementResourceViewModel?.ExecuteToggleResourcePanel();
}

public void ExecuteToggleBattleAnalytics()
{
    _battleAnalyticsViewModel?.ExecuteToggleAnalytics();
}

public void ExecuteToggleQuestNotifications()
{
    _questNotificationViewModel?.ExecuteToggleNotificationPanel();
}
```

## Integración de teclas rápidas

Añadir a `SubModule.cs` en `OnApplicationTick`:

```csharp
protected override void OnApplicationTick(float dt)
{
    // ... existing code ...
    
    // New hotkeys for features
    if(layer!=null && Input.IsKeyPressed(InputKey.F11)) 
    {
        vm?.ExecuteToggleSettlementResources();
    }
    if(layer!=null && Input.IsKeyPressed(InputKey.F12)) 
    {
        vm?.ExecuteToggleBattleAnalytics();
    }
    if(layer!=null && Input.IsKeyPressed(InputKey.F9)) 
    {
        vm?.ExecuteToggleQuestNotifications();
    }
}
```

## Cadenas de localización

Añadir a los archivos de localización:

```xml
<String id="forge_battle_analytics" text="Battle Analytics" />
<String id="forge_allied_casualties" text="Allied: " />
<String id="forge_enemy_casualties" text="Enemy: " />
<String id="forge_morale" text="Morale: " />
<String id="forge_toggle" text="Toggle" />
<String id="forge_active_quests" text="Active Quests" />
<String id="forge_quest_log" text="Quest Log" />
<String id="forge_progress" text="Progress:" />
<String id="forge_track" text="Track" />
```

## Resumen

Estas funciones nuevas de la interfaz Gauntlet proporcionan:

1. **HUD de análisis de combate en tiempo real** - Estadísticas de combate en vivo durante las batallas
2. **Sistema dinámico de notificaciones de misiones** - Notificaciones de misiones con seguimiento del progreso
3. **Panel de resumen de recursos de asentamientos** - Información completa sobre los recursos

Todas las funciones siguen los patrones establecidos para Gauntlet:
- ViewModel con atributos `[DataSourceProperty]`
- `MBBindingList` para colecciones dinámicas
- Prefabs XML con nombres de widgets adecuados
- Binding de comandos para las interacciones del usuario
- Integración con los servicios SDK existentes
- Administración adecuada del ciclo de vida
