using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;

namespace CalradiaForge.Desktop.Presentation
{
    internal enum DesktopToolKind { Analyzer, AssemblyEditor, Generator, Simulation, Live, Report, Reference }

    internal enum DesktopStudioKind : byte
    {
        Generic = 0,
        TroopTree,
        AudioMixer,
        Workshop,
        AgentMemory,
        CodeSecurity,
        ModuleHierarchy,
        KingdomDiplomacy,
        ComponentGenerator,
        CombatStudio,
        CaravanTrade,
        GauntletStudio,
        CampaignStudio,
        LiveSession,
        DeliveryStudio,
        DiagnosticsStudio
    }

    internal sealed class ToolDefinition : INotifyPropertyChanged
    {
        internal const string ApiDeprecationToolId = "ApiDeprecationChecker";

        static readonly string[] DefaultHotkeys =
        [
            "Ctrl+Enter (Ejecutar Orden)",
            "Ctrl+D (Split Deck Comparativo)",
            "Ctrl+P (Alternar Presets Canónicos)",
            "Ctrl+E (Exportar Evidencia JSON)",
            "Esc (Cancelar Orden en Curso)",
            "Ctrl+F (Filtrar Catálogo)"
        ];

        static readonly string[] AudioCommands = ["cf.reload_audio", "sound.play_event <sound_id>", "sound.stop_all"];
        static readonly string[] TroopCommands = ["cf.inspect_troop <troop_id>", "campaign.give_troops <troop_id> 5", "cf.reload_prefabs"];
        static readonly string[] MemoryCommands = ["cf.agent_memory_dump", "cf.inspect_agent <hero_id>", "cf.coala_status"];
        static readonly string[] EconomyCommands = ["cf.workshop_eval <town>", "campaign.add_gold_to_hero 1000", "campaign.print_settlement_economy <town>"];
        static readonly string[] AuditCommands = ["cf.audit_rules", "cf.verify_assembly", "cf.check_conflicts"];
        static readonly string[] ConflictCommands = ["cf.check_conflicts", "cf.list_modules", "cf.validate_manifest"];
        static readonly string[] DiplomacyCommands = ["cf.sim_dynasty", "campaign.lead_kingdom", "campaign.declare_war <f1> <f2>"];
        static readonly string[] LiveConsoleCommands = ["cf.connect", "cf.telemetry", "cf.forgeweave_events", "cf.dump_state"];

        static readonly string[] GroupDiagnosticsCommands = ["cf.audit_rules", "cf.verify_assembly", "cf.status"];
        static readonly string[] GroupLiveCommands = ["cf.connect", "cf.telemetry", "cf.dump_state"];
        static readonly string[] GroupAssetsCommands = ["cf.reload_assets", "cf.inspect_tpac", "sound.reload_sounds"];
        static readonly string[] GroupSimulationCommands = ["cf.sim_dynasty", "cf.inspect_troop", "cf.workshop_eval"];
        static readonly string[] GroupPoliticsCommands = ["campaign.lead_kingdom", "cf.sim_dynasty", "campaign.declare_war"];
        static readonly string[] GroupCampaignCommands = ["campaign.start_quest", "cf.inspect_agent", "cf.sim_dynasty"];
        static readonly string[] GroupEconomyCommands = ["cf.workshop_eval", "campaign.add_gold_to_hero", "campaign.print_settlement_economy"];
        static readonly string[] GroupCombatCommands = ["cf.inspect_troop", "campaign.give_troops", "mission.set_combat_ai"];
        static readonly string[] GroupGauntletCommands = ["cf.open_workbench", "cf.reload_prefabs", "ui.reload_all"];
        static readonly string[] GroupDeliveryCommands = ["cf.package_preflight", "cf.verify_hashes"];
        static readonly string[] GroupLearningCommands = ["cf.help", "cf.encyclopedia_search", "cf.list_commands"];
        static readonly string[] GroupDefaultCommands = ["cf.help", "cf.status"];

        readonly string title;
        readonly string group;
        readonly string iconKey;
        readonly string categoryBanner;
        readonly string categoryMotto;
        readonly string categoryAccentBrushKey;
        readonly string inputDomainBadge;
        readonly string inputFormatHint;
        readonly string securityPillText;
        readonly string cliSyntax;
        readonly IReadOnlyList<string> consoleCommands;

        public ToolDefinition(string id, string title, string category, DesktopToolKind kind, bool requiresInput, bool changesState, string pipeAction = null)
        {
            Id = id;
            this.title = title;
            Category = category;
            Kind = kind;
            RequiresInput = requiresInput;
            ChangesState = changesState;
            PipeAction = pipeAction;

            Studio = id switch
            {
                "TroopTreeVisualizer" or "PerkTreeCalculator" => DesktopStudioKind.TroopTree,
                "AudioFmodMixerInspector" => DesktopStudioKind.AudioMixer,
                "WorkshopEnterpriseSimulator" => DesktopStudioKind.Workshop,
                "SaveInspector" or "ForgeAgentMemoryInspector" or "AgentMemoryInspector" => DesktopStudioKind.AgentMemory,
                "SaveTypeDefinerAuditor" or "CampaignNamespaceGuard" or "AssemblyInspector" or "AssemblyVersionPatchLab" => DesktopStudioKind.CodeSecurity,
                "ModConflictMatrix" or "DependencySorter" => DesktopStudioKind.ModuleHierarchy,
                "DiplomaticMatrix" or "WarCasusBelliEngine" or "DynasticSuccessionEvaluator" or "SDK_ForgeDiplomacyEngine"
                or "SDK_ForgeKingdomManager" or "SDK_ForgeClanManager" or "SDK_ForgeRebellionSystem" or "SDK_ForgePolicyEnforcer"
                or "SDK_ForgeVassalRelations" or "SDK_ForgeMarriageArranger" or "SDK_ForgeHeirDesignator" or "SDK_ForgeElectionRigger"
                or "SDK_ForgeTreasonSystem" or "SDK_ForgeCivilWarTrigger" or "SDK_ForgeAllianceBuilder" or "SDK_ForgeTruceNegotiator"
                or "SDK_ForgeCasusBelli" or "SDK_ForgeSpyNetwork" or "SDK_ForgeAssassinationPlot" or "SDK_ForgeInfluenceMarket"
                or "SDK_ForgeRenownTracker" or "SDK_ForgeTitleGranter" or "SDK_ForgeFactionSplitter" or "SDK_ForgeNobleCourt" => DesktopStudioKind.KingdomDiplomacy,
                "SoundXmlSynthesizer" or "TroopXmlSynthesizer" or "ItemXmlSynthesizer" or "WeaponCraftingForge"
                or "BrushSynthesizer" or "XmlSnippetForge" or "LocalizationHashGenerator" or "LocalizationMatrix"
                or "HarmonyPatcher" or "HarmonyTranspiler" or "QuestDialogBuilder"
                or "NoviceBehavior" or "NoviceTroop" or "NoviceQuest" or "NoviceItem" or "NoviceSubmodule"
                or "NoviceChecklist" or "NoviceEvents" or "NoviceHint" or "NoviceWorkshop" or "NoviceParty"
                or "NoviceBuilding" or "NoviceCombatAi" => DesktopStudioKind.ComponentGenerator,
                "ItemBalanceAnalyzer" or "CombatAgentSpawner" or "SiegeNavmeshTactician"
                or "SDK_ForgeFormationController" or "SDK_ForgeSiegeEngineManager" or "SDK_ForgeDamageModifier"
                or "SDK_ForgeArmorPenetration" or "SDK_ForgeWeaponBreakage" or "SDK_ForgeMoraleShock" or "SDK_ForgeFleeLogic"
                or "SDK_ForgeCavalryCharge" or "SDK_ForgePikemanBrace" or "SDK_ForgeArcherVolley" or "SDK_ForgeFriendlyFireAvoidance"
                or "SDK_ForgeWeatherCombatMod" or "SDK_ForgeNightVisionPenalties" or "SDK_ForgeBleedEffect" or "SDK_ForgePoisonEffect"
                or "SDK_ForgeStunEffect" or "SDK_ForgeCleaveStrike" or "SDK_ForgeHeadshotBonus" or "SDK_ForgeDismountChance"
                or "SDK_ForgeShieldBash" or "SDK_ForgeExecutionMoves" or "SDK_ForgeAmbushTactics" or "SDK_ForgeLootingBehavior"
                or "SDK_ForgeBodyguardAssignment" or "SDK_ForgeDuellingSystem" or "SDK_ForgeTargetPriority" or "SDK_ForgeWeaponSwapLogic"
                or "SDK_ForgeFormationSpacing"
                or "TacticalCombatSimulator" or "CombatAiAuditor" or "SDK_ForgeCombatAi"
                or "ForgeFormationController" or "ForgeSiegeEngineManager" or "ForgeDamageModifier" or "ForgeArmorPenetration"
                or "ForgeMoraleShock" or "ForgeCavalryCharge" or "ForgeArcherVolley" or "ForgeAmbushTactics"
                or "ForgeCleaveStrike" or "ForgeShieldBash" => DesktopStudioKind.CombatStudio,
                "UnderworldCrimeSimulator"
                or "SDK_ForgeTradeManager" or "SDK_ForgeCaravanController" or "SDK_ForgeWorkshopManager" or "SDK_ForgeMarketFluctuation"
                or "SDK_ForgeTaxesController" or "SDK_ForgeSmugglingSystem" or "SDK_ForgeBlackMarket" or "SDK_ForgeLoanSystem"
                or "SDK_ForgeBankSystem" or "SDK_ForgeInvestmentTracker" or "SDK_ForgeResourceDepletion" or "SDK_ForgeInflationController"
                or "SDK_ForgeTradeRouteOptimizer" or "SDK_ForgeMerchantGuilds" or "SDK_ForgeCurrencyExchange" or "SDK_ForgePricePegging"
                or "SDK_ForgeBribeManager" or "SDK_ForgeEconomicCrisis" or "SDK_ForgeProsperityBooster" or "SDK_ForgeFamineSimulator"
                or "SDK_ForgeSupplyChainManager"
                or "CaravanTradeHub" or "MarketPriceSpreadInspector" or "SDK_ForgeEconomyTrade"
                or "ForgeCaravanController" or "ForgeMarketFluctuation" or "ForgeTaxesController" or "ForgeSmugglingSystem"
                or "ForgeLoanSystem" or "ForgeInflationController" or "ForgeTradeRouteOptimizer" or "ForgeBlackMarket"
                or "ForgeSupplyChainManager" => DesktopStudioKind.CaravanTrade,
                "GauntletInspector" or "GauntletLivePreview" or "GauntletEventPassChecker" or "SpritePackageAuditor"
                or "SDK_ForgeFloatingDamage" or "SDK_ForgeCustomCrosshair" or "SDK_ForgeMinimapOverlay" or "SDK_ForgeHealthBars"
                or "SDK_ForgeCombatCompass" or "SDK_ForgeAdvancedKillfeed" or "SDK_ForgeInventorySort" or "SDK_ForgePartyFilter"
                or "SDK_ForgeTroopTreeViewer" or "SDK_ForgeEncyclopediaExtender" or "SDK_ForgeDialogueOptionsUI" or "SDK_ForgeTradeProfitUI"
                or "SDK_ForgeKingdomOverviewUI" or "SDK_ForgeClanRolesUI" or "SDK_ForgeSiegeHUD" or "SDK_ForgeTournamentBracketUI"
                or "SDK_ForgeWeaponStatsUI" or "SDK_ForgeCharacterEditorExtra" or "SDK_ForgeMapBordersUI" or "SDK_ForgeArmyMoraleUI"
                or "SDK_ForgeGarrisonManagerUI" or "SDK_ForgeWorkshopStatsUI" or "SDK_ForgeSettlementIcons" or "SDK_ForgePrisonerRansomUI"
                or "SDK_ForgeRelationshipBarsUI" or "SDK_ForgeSkillTrackerUI" or "SDK_ForgeGoldTrackerUI" or "SDK_ForgeInfluenceGainUI"
                or "SDK_ForgeRenownGainUI" or "SDK_ForgeFoodConsumptionUI" => DesktopStudioKind.GauntletStudio,
                "SettlementCalculator" or "PartyCalculator" or "SDK_ForgeQuestManager" or "SDK_ForgeWeatherController"
                or "SDK_ForgeTimeManipulator" or "SDK_ForgeReligionSystem" or "SDK_ForgeTraitManager" or "SDK_ForgeBanditController"
                or "SDK_ForgeHideoutSpawner" or "SDK_ForgeMercenaryHiring" or "SDK_ForgeVillageHearthManager" or "SDK_ForgeLoyaltyModifier"
                or "SDK_ForgeSecurityModifier" or "SDK_ForgeWoundRateController" or "SDK_ForgeNotableSpawner" or "SDK_ForgeCaravanGuardManager"
                or "SDK_ForgeRandomEventTrigger" or "SDK_ForgePlagueSimulator" or "SDK_ForgeBanditInvasion" or "SDK_ForgeBountyHunting"
                or "SDK_ForgeSlaveTrade" or "SDK_ForgeTournamentGenerator" or "SDK_ForgeCustomSettlementBuilder" or "SDK_ForgeNavalTravel"
                or "SDK_ForgeCampingSystem" or "SDK_ForgeHuntingSystem" or "SDK_ForgeForagingSystem" or "SDK_ForgeCompanionSpawner" => DesktopStudioKind.CampaignStudio,
                "LiveConsole" or "ObjectInspector" or "SaveBinaryParser" or "MemoryProfiler"
                or "PartyInventory" or "MapPathfindingDebugger" or "MockEngine" => DesktopStudioKind.LiveSession,
                "ModPackager" or "RecentExports" or "SdkCheatSheet" or "ShortcutGuide"
                or "ConsoleReference" or "FbxAsciiPreflight" => DesktopStudioKind.DeliveryStudio,
                "XmlSchemaValidator" or "WatchdogParser" or "CrashAnalyzer"
                or "ApiDeprecationChecker" or "PackagingAuditor" or "MissionMeshGuard"
                or "ModIdAuditor" => DesktopStudioKind.DiagnosticsStudio,
                _ => DesktopStudioKind.Generic
            };

            group = category.StartsWith("Diagnostics", StringComparison.Ordinal) ? "Diagnostics" :
                category.StartsWith("Live", StringComparison.Ordinal) ? "Live session" :
                category.StartsWith("Asset", StringComparison.Ordinal) ? "Assets" :
                category.StartsWith("Politics", StringComparison.Ordinal) ? "Politics" :
                category.StartsWith("Simulation", StringComparison.Ordinal) ? "Simulation" :
                category.StartsWith("Delivery", StringComparison.Ordinal) ? "Delivery" :
                category.StartsWith("Learning", StringComparison.Ordinal) ? "Learning" :
                category.StartsWith("Campaign", StringComparison.Ordinal) ? "Campaign" :
                category.StartsWith("Economy", StringComparison.Ordinal) ? "Economy" :
                category.StartsWith("Combat", StringComparison.Ordinal) ? "Combat" :
                category.StartsWith("Gauntlet", StringComparison.Ordinal) ? "Gauntlet" : "Forge SDK";

            iconKey = ToolGroupViewModel.IconForGroup(group);

            categoryBanner = group switch
            {
                "Diagnostics" => "🛡️ IMPERIAL CODEX AUDIT & INTEGRITY SQUADRON",
                "Live session" => "📡 TACTICAL WAR ROOM & APM TELEMETRY MESH",
                "Assets" => "⚔️ ROYAL ARSENAL & GAUNTLET ASSET SYNTHESIZER",
                "Simulation" => "⚡ CALRADIA BATTLEFIELD & ECONOMY EQUILIBRIUM SIMULATOR",
                "Politics" => "👑 HIGH SENATE & IMPERIAL REICHSRAT DIPLOMATIC CHANCERY",
                "Campaign" => "📜 CAMPAIGN EXPEDITION & DYNASTIC CHRONICLES",
                "Economy" => "⚖️ GUILD MERCHANT & TRADE ARBITRAGE LEDGER",
                "Combat" => "🛡️ VANGUARD DRILL & TACTICAL FORMATION ACADEMY",
                "Gauntlet" => "🎨 IMPERIAL GAUNTLET INTERFACE & PREFAB WORKSHOP",
                "Delivery" => "📦 EXPEDITION PACKAGING & CIPHER VERIFICATION DEPOT",
                "Learning" => "📖 CODEX SCHOLASTIC ENCYCLOPEDIA & ARCHIVE EXTENDER",
                _ => "⚙️ CALRADIA FORGE CORE SDK & LOW-LEVEL ENGINE BRIDGES"
            };

            categoryMotto = group switch
            {
                "Diagnostics" => "Vigilantia et Aequitas (Vigilance & Equity)",
                "Live session" => "Vox Imperii in Tempore Reali (Imperial Voice in Real Time)",
                "Assets" => "Ars Bellica et Fabrica (Martial Craft & Manufacture)",
                "Simulation" => "Ratio Praevia Victoriae (Calculation Precedes Victory)",
                "Politics" => "Concordia Parvae Res Crescunt (Through Harmony Small Things Grow)",
                "Campaign" => "Iter Ad Gloriam (Journey to Glory)",
                "Economy" => "Pecunia Nervus Belli (Money is the Sinew of War)",
                "Combat" => "Virtus in Proelio (Valor in Battle)",
                "Gauntlet" => "Forma et Munus (Form and Function)",
                "Delivery" => "Fides et Integritas (Faith and Integrity)",
                "Learning" => "Scientia Potentia Est (Knowledge is Power)",
                _ => "Fundamentum Calradiae (Foundation of Calradia)"
            };

            categoryAccentBrushKey = group switch
            {
                "Diagnostics" => "BrassBrush",
                "Live session" => "EmberBrush",
                "Assets" => "DeepPineBrush",
                "Simulation" => "VerdigrisBrush",
                "Politics" => "BrassBrush",
                "Campaign" => "DeepPineBrush",
                "Economy" => "BrassBrush",
                "Combat" => "EmberBrush",
                "Gauntlet" => "VerdigrisBrush",
                "Delivery" => "DeepPineBrush",
                "Learning" => "BrassBrush",
                _ => "VerdigrisBrush"
            };

            inputDomainBadge = group switch
            {
                "Diagnostics" => "[TARGET ASSEMBLY / PE BINARY / MANIFEST]",
                "Live session" => "[PIPE TOPIC / APM EVENT PAYLOAD]",
                "Assets" => "[SOURCE ASSET / FBX MESH / SOUND WAV]",
                "Simulation" => "[SCENARIO BRANCH / BENCHMARK MODEL]",
                "Politics" => "[TARGET FACTION / SENATE POLICY / CLAN]",
                "Campaign" => "[SETTLEMENT / CLAN STRINGID / CHRONICLE]",
                "Economy" => "[MARKET NODE / TRADE ROUTE / WORKSHOP]",
                "Combat" => "[COMBAT LOADOUT / FORMATION DIRECTIVE]",
                "Gauntlet" => "[GAUNTLET PREFAB XML / MOVIE SCHEMA]",
                "Delivery" => "[ARCHIVE BATCH / ZONE STREAM PREFLIGHT]",
                "Learning" => "[ENCYCLOPEDIA EXTENDER / CODEX ENTRY]",
                _ => "[SDK TEST BENCH / CALRADIA FORGE ROUTE]"
            };

            inputFormatHint = group switch
            {
                "Diagnostics" => "Accepts *.dll, *.exe, SubModule.xml or project root directory",
                "Live session" => "Requires named pipe session (cf.forgeweave.* topic or command payload)",
                "Assets" => "Accepts *.fbx, *.wav, *.ogg, *.tpac, or ModuleData XML files",
                "Simulation" => "Accepts scenario presets, XML troop/economy diffs, or custom hero IDs",
                "Politics" => "Accepts faction ID (e.g. empire_s, vlandia) or policy proposal string",
                "Campaign" => "Accepts settlement ID (e.g. settlement_town_1) or hero StringId",
                "Economy" => "Accepts town name (e.g. Marunath, Epicrotea) or workshop enterprise type",
                "Combat" => "Accepts troop archetype (e.g. imperial_legionary) or combat balance spec",
                "Gauntlet" => "Accepts *.xml Gauntlet Prefab path or UI Brush resource identifier",
                "Delivery" => "Accepts target release directory or output distribution package path",
                "Learning" => "Accepts codex topic key, XML docstring symbol, or category name",
                _ => "Accepts bounded parameters, assembly paths, or test scenario inputs"
            };

            securityPillText = changesState
                ? "[ GUARDED ROUTE · CAMPAIGN STATE MUTATION REQUIRES TEST WORKFLOW ]"
                : "[ STRICT BOUNDED ROUTE · ZERO IN-GAME SIDE EFFECTS · THREAD ISOLATED ]";

            cliSyntax = $"CalradiaForge.Desktop.exe --tool {id}" + (requiresInput ? " --input <file_or_dir>" : " --auto");

            consoleCommands = id switch
            {
                "AudioFmodMixerInspector" or "SoundXmlSynthesizer" => AudioCommands,
                "TroopTreeVisualizer" or "TroopXmlSynthesizer" => TroopCommands,
                "SaveInspector" or "ForgeAgentMemoryInspector" or "AgentMemoryInspector" => MemoryCommands,
                "WorkshopEnterpriseSimulator" or "SettlementCalculator" => EconomyCommands,
                "SaveTypeDefinerAuditor" or "CampaignNamespaceGuard" or "ModRuleAuditor" => AuditCommands,
                "ModConflictMatrix" or "DependencySorter" => ConflictCommands,
                "DiplomaticMatrix" or "WarCasusBelliEngine" or "DynasticSuccessionEvaluator" => DiplomacyCommands,
                "LiveConsole" or "GauntletLivePreview" => LiveConsoleCommands,
                _ => group switch
                {
                    "Diagnostics" => GroupDiagnosticsCommands,
                    "Live session" => GroupLiveCommands,
                    "Assets" => GroupAssetsCommands,
                    "Simulation" => GroupSimulationCommands,
                    "Politics" => GroupPoliticsCommands,
                    "Campaign" => GroupCampaignCommands,
                    "Economy" => GroupEconomyCommands,
                    "Combat" => GroupCombatCommands,
                    "Gauntlet" => GroupGauntletCommands,
                    "Delivery" => GroupDeliveryCommands,
                    "Learning" => GroupLearningCommands,
                    _ => GroupDefaultCommands
                }
            };
        }
        public string Id { get; }
        public string Title => Id == ApiDeprecationToolId
            ? LocalizedText("Ui.ApiDeprecation.Name", title)
            : title;
        public string Category { get; }
        public DesktopToolKind Kind { get; }
        public bool RequiresInput { get; }
        public bool ChangesState { get; }
        public string PipeAction { get; }
        public string Group => group;
        public string IconKey => iconKey;
        public string AvailabilityReason => Id == ApiDeprecationToolId
            ? LocalizedText("Ui.ApiDeprecation.Availability", "Unavailable until a verified, versioned TaleWorlds API deprecation catalog is available.")
            : ChangesState
            ? "Guarded: enable the in-game test workflow on a copied campaign."
            : RequiresInput
                ? "Ready when a supported file or folder is selected."
                : Kind == DesktopToolKind.Live
                    ? "Ready after a compatible Forge session advertises this capability."
                    : "Ready for a bounded desktop operation.";
        public string KindLabel => Kind switch
        {
            DesktopToolKind.Analyzer => "ANALYZER",
            DesktopToolKind.AssemblyEditor => "COPY-ONLY EDITOR",
            DesktopToolKind.Generator => "GENERATOR",
            DesktopToolKind.Simulation => "SIMULATION",
            DesktopToolKind.Live => "LIVE SESSION",
            DesktopToolKind.Report => "REPORT",
            _ => "REFERENCE"
        };
        public string Purpose => Id == ApiDeprecationToolId
            ? LocalizedText("Ui.ApiDeprecation.Purpose", "Unavailable pending a verified, versioned TaleWorlds API deprecation catalog.")
            : Kind switch {
            DesktopToolKind.Analyzer => "Inspect supported local evidence with a bounded analyzer.",
            DesktopToolKind.AssemblyEditor => "Preview a version-metadata change and write only a separately backed-up copy.",
            DesktopToolKind.Generator => "Create an editable, explicitly labeled starting point.",
            DesktopToolKind.Simulation => "Prepare a bounded scenario; no game state is changed.",
            DesktopToolKind.Live => "Query an advertised capability from the active Forge session.",
            DesktopToolKind.Report => "Review or export retained desktop evidence.",
            _ => "Read a local reference without making a compatibility claim."
        };

        public string CategoryBanner => categoryBanner;

        public string CategoryMotto => categoryMotto;

        public string CategoryAccentBrushKey => categoryAccentBrushKey;

        public string InputDomainBadge => inputDomainBadge;

        public string InputFormatHint => inputFormatHint;

        public string SecurityPillText => securityPillText;

        public IReadOnlyList<string> ConsoleCommands => consoleCommands;

        public string CliSyntax => cliSyntax;

        public DesktopStudioKind Studio { get; }

        public bool HasVisualDashboard => Studio != DesktopStudioKind.Generic;

        public IReadOnlyList<string> ApplicableHotkeys => DefaultHotkeys;

        public string SectionInformation => Id == ApiDeprecationToolId
            ? LocalizedText("Ui.ApiDeprecation.Information", "This route does not classify API usage as deprecated. Analysis may be enabled only after a verified catalog identifies the exact TaleWorlds API version it covers.")
            : Id switch
            {
            "AudioFmodMixerInspector" => "Inspecciona la configuración acústica del motor TaleWorlds/FMOD, manifest de sonidos 2D/3D (module_sounds.xml), curvas de atenuación y categorías de mixer (ui, mission_combat, ambient). Modela la forma de onda acústica y el espectro en frecuencia con verificación estricta de clipping.",
            "TroopTreeVisualizer" => "Visualiza y simula árboles jerárquicos de progresión de tropas militares de Bannerlord (Tiers 1 a 6). Renderiza nodos DAG interactivos, equipamiento, estadísticas de combate (Vigor, Control, Resistencia, Tácticas) y calcula curvas comparativas de balance militar.",
            "WorkshopEnterpriseSimulator" => "Simulador macroeconómico de 30 días para talleres y empresas urbanas de Calradia (cervecerías, herrerías, prensas de aceite). Modela costos de materias primas, demanda local, rentabilidad diaria y medidor de riesgo de descontento/rebelión civil.",
            "SaveInspector" or "ForgeAgentMemoryInspector" or "AgentMemoryInspector" => "Auditor de arquitectura de memoria cognitiva CoALA para NPCs y agentes de Bannerlord. Analiza cuota global de agentes, memoria semántica con decadencia TTL, cola FIFO episódica y rutinas tácticas procedimentales.",
            "SaveTypeDefinerAuditor" => "Auditor de bajo nivel de metadatos CLR para SaveableTypeDefiner en assemblies del mod. Verifica por análisis estático que el base ID sea estrictamente >= 2.500.000 para prevenir colisiones catastróficas de serialización en partidas guardadas.",
            "CampaignNamespaceGuard" => "Guardián de integridad de espacios de nombres. Garantiza el cumplimiento de la Regla A (Anti-Shadowing de GEMINI.md), impidiendo que ningún tipo, carpeta o subespacio de nombres oculte TaleWorlds.CampaignSystem ni rompa Campaign.Current.",
            "ModConflictMatrix" => "Matriz topológica de resolución y detección de conflictos entre módulos de Mount & Blade II: Bannerlord. Analiza el orden de carga (Load Order DAG), sobrescrituras de XML, dependencias cíclicas y submódulos huérfanos.",
            "DiplomaticMatrix" => "Chancillería diplomática y barómetro geopolítico del Senado Imperial. Evalúa posturas entre facciones (Paz, Guerra, Tregua), desgaste de guerra (War Weariness), intenciones de casus belli y votos del senado en políticas dinásticas.",
            "SoundXmlSynthesizer" => "Forja y sintetizador de manifiestos de sonido (module_sounds.xml). Genera definiciones XML conformes al esquema de TaleWorlds con balance estéreo, categorías activas y compatibilidad de eventos 2D de interfaz y 3D posicionales.",
            "TroopXmlSynthesizer" => "Sintetizador de definiciones de personajes y tropas (NPCCharacters.xml). Genera árboles de tropas válidos con niveles de habilidad, plantillas de equipo (body, gloves, boots, weapons) y etiquetas de facción/cultura.",
            "GauntletInspector" or "GauntletLivePreview" => "Estudio táctico de Gauntlet UI y HUD. Modela la jerarquía de widgets, resolución de anclajes, presupuesto de draw calls, estilos de brochas y capas visuales del HUD in-game.",
            "SettlementCalculator" or "SDK_ForgeWeatherController" => "Estudio de expedición y equilibrio de campaña de Calradia. Modela el crecimiento de hogares, estabilidad de lealtad, guarniciones, seguridad cívica y variaciones meteorológicas.",
            "LiveConsole" or "MemoryProfiler" or "ObjectInspector" => "Estudio de telemetría y sesión interactiva en tiempo real (Live Session). Monitorea el bus de eventos de ForgeWeave por Named Pipes, latencia IPC de ida y vuelta, saturación del buffer en anillo y cuotas de memoria sin interferir con el hilo del juego.",
            "ModPackager" or "RecentExports" or "FbxAsciiPreflight" => "Estudio de empaquetado y entrega de producción (Delivery & Deployment). Ejecuta auditorías preflight de FastPackageEngine, verificación de firmas e integridad SHA-256, saneamiento de streams Zone.Identifier y generación paralela multihilo.",
            "XmlSchemaValidator" or "CrashAnalyzer" or "WatchdogParser" or "PackagingAuditor" or "MissionMeshGuard" or "ModIdAuditor" => "Estudio de diagnóstico e integridad estructural (Diagnostics & Integrity). Inspecciona la validez de esquemas XML, detección de excepciones y cuelgues en volcados .cfcrash, paridad de manifiestos SubModule.xml y barreras de memoria nativa sin alterar el estado del motor.",
                _ => $"{Purpose} Opera de forma estrictamente acotada (bounded) y aislada del hilo de renderizado, garantizando cero efectos colaterales persistentes en partidas guardadas ni en la instalación del juego."
            };

        public event PropertyChangedEventHandler PropertyChanged;

        internal void RefreshLocalizedText()
        {
            if (Id != ApiDeprecationToolId) return;
            RaisePropertyChanged(nameof(Title));
            RaisePropertyChanged(nameof(Purpose));
            RaisePropertyChanged(nameof(AvailabilityReason));
            RaisePropertyChanged(nameof(SectionInformation));
        }

        static string LocalizedText(string resourceKey, string fallback)
        {
            var application = Application.Current;
            if (application == null || !application.Dispatcher.CheckAccess()) return fallback;
            return application.TryFindResource(resourceKey) is string value && !string.IsNullOrWhiteSpace(value)
                ? value
                : fallback;
        }

        void RaisePropertyChanged(string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    /// <summary>Reviewed source of desktop routes. It never reads a live WPF tree or infers tool kinds from identifiers.</summary>
    internal sealed class ToolCatalog
    {
        internal static readonly string[] GroupOrder = new[]
        {
            "Diagnostics", "Live session", "Assets", "Simulation", "Politics", "Campaign",
            "Economy", "Combat", "Gauntlet", "Delivery", "Learning", "Forge SDK"
        };
        readonly ReadOnlyCollection<ToolDefinition> tools;
        readonly string[] categories;
        readonly Dictionary<string, ToolDefinition> byId;
        public ToolCatalog(IEnumerable<ToolDefinition> definitions = null)
        {
            var all = (definitions ?? DesktopToolDefinitions.All).ToArray();
            byId = new Dictionary<string, ToolDefinition>(all.Length, StringComparer.Ordinal);
            var catSet = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < all.Length; i++)
            {
                var tool = all[i];
                if (string.IsNullOrWhiteSpace(tool?.Id)) throw new ArgumentException("A desktop tool requires an identifier.", nameof(definitions));
                if (!byId.TryAdd(tool.Id, tool)) throw new InvalidOperationException("Duplicate desktop tool identifier: " + tool.Id);
                catSet.Add(tool.Category);
            }
            tools = Array.AsReadOnly(all);
            categories = catSet.ToArray();
        }
        public IReadOnlyList<ToolDefinition> Tools => tools;
        public IReadOnlyList<string> Categories => categories;
        public ToolDefinition Find(string id) => id != null && byId.TryGetValue(id, out var item) ? item : null;
        internal static int GroupRank(string group) => group switch
        {
            "Diagnostics" => 0,
            "Live session" => 1,
            "Assets" => 2,
            "Simulation" => 3,
            "Politics" => 4,
            "Campaign" => 5,
            "Economy" => 6,
            "Combat" => 7,
            "Gauntlet" => 8,
            "Delivery" => 9,
            "Learning" => 10,
            "Forge SDK" => 11,
            _ => 12
        };
    }

    internal static class DesktopToolDefinitions
    {
        static ToolDefinition T(string id, string title, string category, DesktopToolKind kind, bool requiresInput, bool changesState, string pipeAction = null) => new ToolDefinition(id, title, category, kind, requiresInput, changesState, pipeAction);
        internal static readonly ToolDefinition[] All = new[]
        {
            T("SubModuleValidator", "SubModule.xml Validator", "Diagnostics & Safety", DesktopToolKind.Analyzer, true, false, null),
            T("AssemblyInspector", ".NET Assembly Inspector", "Diagnostics & Safety", DesktopToolKind.Analyzer, true, false, null),
            T("AssemblyVersionPatchLab", "Assembly Version Patch Lab", "Diagnostics & Safety", DesktopToolKind.AssemblyEditor, true, false, null),
            T("ModConflictMatrix", "Mod Conflict Matrix", "Diagnostics Diagnostics & Safety", DesktopToolKind.Analyzer, true, false, null),
            T("XmlSchemaValidator", "Standalone XML Validator", "Diagnostics Diagnostics & Safety", DesktopToolKind.Analyzer, true, false, null),
            T("WatchdogParser", "TaleWorlds Watchdog Parser", "Diagnostics Diagnostics & Safety", DesktopToolKind.Analyzer, true, false, null),
            T("CrashAnalyzer", "Crash Analyzer", "Diagnostics Diagnostics & Safety", DesktopToolKind.Analyzer, true, false, null),
            T(ToolDefinition.ApiDeprecationToolId, "API Deprecation Analysis — Unavailable", "Diagnostics Diagnostics & Safety", DesktopToolKind.Analyzer, true, false, null),
            T("PackagingAuditor", "Mod Packaging Auditor", "Diagnostics Diagnostics & Safety", DesktopToolKind.Analyzer, true, false, null),
            T("MissionMeshGuard", "Mission Mesh Safety Guard", "Diagnostics Diagnostics & Safety", DesktopToolKind.Analyzer, true, false, null),
            T("SaveTypeDefinerAuditor", "SaveableTypeDefiner Auditor", "Diagnostics Diagnostics & Safety", DesktopToolKind.Analyzer, true, false, null),
            T("GauntletEventPassChecker", "Gauntlet Event-Pass Checker", "Diagnostics Diagnostics & Safety", DesktopToolKind.Analyzer, true, false, null),
            T("CampaignNamespaceGuard", "Campaign Namespace Guard", "Diagnostics Diagnostics & Safety", DesktopToolKind.Analyzer, true, false, null),
            T("ModIdAuditor", "Mod ID Collision Auditor", "Diagnostics Diagnostics & Safety", DesktopToolKind.Analyzer, true, false, null),
            T("AudioFmodMixerInspector", "Audio Manifest & Mixer Auditor", "Diagnostics Diagnostics & Safety", DesktopToolKind.Analyzer, true, false, null),
            T("SpritePackageAuditor", "Gauntlet Sprite Package Auditor", "Assets / UI Pipeline", DesktopToolKind.Analyzer, true, false, null),
            T("FbxAsciiPreflight", "FBX ASCII Preflight", "Assets / 3D Pipeline", DesktopToolKind.Analyzer, true, false, null),
            T("LiveConsole", "Live Console", "Live Live Inspection & Memory", DesktopToolKind.Live, false, false, "command"),
            T("ObjectInspector", "Object Inspector", "Live Live Inspection & Memory", DesktopToolKind.Live, false, false, "inspect"),
            T("SaveInspector", "Save Inspector", "Live Live Inspection & Memory", DesktopToolKind.Live, false, false, "summary"),
            T("SaveBinaryParser", "Save Game Binary Parser", "Live Live Inspection & Memory", DesktopToolKind.Live, false, false, "summary"),
            T("MemoryProfiler", "Siege Memory Profiler", "Live Live Inspection & Memory", DesktopToolKind.Live, false, false, "metrics"),
            T("GauntletInspector", "Gauntlet Widget Inspector", "Live Live Inspection & Memory", DesktopToolKind.Live, false, false, "inspect"),
            T("GauntletLivePreview", "Gauntlet UI Live Preview", "Live Live Inspection & Memory", DesktopToolKind.Live, false, false, "framework"),
            T("PartyInventory", "Party Inventory Inspector", "Live Live Inspection & Memory", DesktopToolKind.Live, false, false, "summary"),
            T("MapPathfindingDebugger", "Map Pathfinding Debugger", "Live Live Inspection & Memory", DesktopToolKind.Live, false, false, "summary"),
            T("SoundXmlSynthesizer", "Custom Audio & Sound Synthesizer", "Assets Asset & XML Synthesizers", DesktopToolKind.Generator, false, false, null),
            T("TroopXmlSynthesizer", "Troop & NPC Character Synthesizer", "Assets Asset & XML Synthesizers", DesktopToolKind.Generator, false, false, null),
            T("ItemXmlSynthesizer", "Item & Smithing Crafting Synthesizer", "Assets Asset & XML Synthesizers", DesktopToolKind.Generator, false, false, null),
            T("WeaponCraftingForge", "Weapon Crafting Forge", "Assets Asset & XML Synthesizers", DesktopToolKind.Generator, false, false, null),
            T("BrushSynthesizer", "Gauntlet Brush Synthesizer", "Assets Asset & XML Synthesizers", DesktopToolKind.Generator, false, false, null),
            T("XmlSnippetForge", "XML Snippet Forge", "Assets Asset & XML Synthesizers", DesktopToolKind.Generator, false, false, null),
            T("LocalizationHashGenerator", "Localization Hash Generator", "Assets Asset & XML Synthesizers", DesktopToolKind.Generator, false, false, null),
            T("LocalizationMatrix", "Localization Matrix", "Assets Asset & XML Synthesizers", DesktopToolKind.Generator, false, false, null),
            T("ItemBalanceAnalyzer", "Item Stat Balance Analyzer", "Simulation & Balance", DesktopToolKind.Simulation, false, false, null),
            T("TroopTreeVisualizer", "Troop Tree Visualizer", "Simulation & Balance", DesktopToolKind.Simulation, false, false, null),
            T("PerkTreeCalculator", "Hero Perk Tree Calculator", "Simulation & Balance", DesktopToolKind.Simulation, false, false, null),
            T("DiplomaticMatrix", "Kingdom Diplomatic Matrix", "Simulation & Balance", DesktopToolKind.Simulation, false, false, null),
            T("SettlementCalculator", "Economic Balance Calculator", "Simulation & Balance", DesktopToolKind.Simulation, false, false, null),
            T("PartyCalculator", "Party & Army Calculator", "Simulation & Balance", DesktopToolKind.Simulation, false, false, null),
            T("CombatAgentSpawner", "Combat Agent Spawner", "Simulation & Balance", DesktopToolKind.Simulation, false, true, null),
            T("MockEngine", "Mock Engine Simulator", "Simulation & Balance", DesktopToolKind.Simulation, false, false, null),
            T("WorkshopEnterpriseSimulator", "Workshop Economics Simulator", "Simulation & Balance", DesktopToolKind.Simulation, false, false, null),
            T("SiegeNavmeshTactician", "Siege Assault & Breach Tactician", "Simulation & Balance", DesktopToolKind.Simulation, false, false, null),
            T("WarCasusBelliEngine", "War Justification & Casus Belli Engine", "Simulation & Balance", DesktopToolKind.Simulation, false, false, null),
            T("DynasticSuccessionEvaluator", "Dynasty & Succession Evaluator", "Simulation & Balance", DesktopToolKind.Simulation, false, false, null),
            T("UnderworldCrimeSimulator", "Underworld & Crime Racket Simulator", "Simulation & Balance", DesktopToolKind.Simulation, false, false, null),
            T("ModPackager", "1-Click Mod Packager", "Delivery & Deployment", DesktopToolKind.Generator, true, false, null),
            T("DependencySorter", "Topological Module Sorter", "Delivery & Deployment", DesktopToolKind.Generator, true, false, null),
            T("HarmonyPatcher", "Harmony Patch Generator", "Delivery & Deployment", DesktopToolKind.Generator, false, false, null),
            T("HarmonyTranspiler", "Harmony Transpiler Analyzer", "Delivery & Deployment", DesktopToolKind.Generator, false, false, null),
            T("QuestDialogBuilder", "Quest & Dialogue Flow Builder", "Delivery & Deployment", DesktopToolKind.Generator, false, false, null),
            T("RecentExports", "Recent Exports", "Delivery & Deployment", DesktopToolKind.Report, false, false, null),
            T("SdkCheatSheet", "Architecture Cheat Sheet", "Delivery & Deployment", DesktopToolKind.Report, false, false, null),
            T("ShortcutGuide", "Keyboard Shortcut Guide", "Delivery & Deployment", DesktopToolKind.Report, false, false, null),
            T("ConsoleReference", "Offline Console Reference", "Delivery & Deployment", DesktopToolKind.Report, false, false, null),
            T("NoviceBehavior", "Behavior Scaffolder", "Learning / Novice Modder Hub", DesktopToolKind.Generator, false, false, null),
            T("NoviceTroop", "Troop Character Generator", "Learning / Novice Modder Hub", DesktopToolKind.Generator, false, false, null),
            T("NoviceQuest", "Quest & Dialogue Flow", "Learning / Novice Modder Hub", DesktopToolKind.Generator, false, false, null),
            T("NoviceItem", "Item & Equipment Synthesizer", "Learning / Novice Modder Hub", DesktopToolKind.Generator, false, false, null),
            T("NoviceSubmodule", "SubModule.xml Manifest", "Learning / Novice Modder Hub", DesktopToolKind.Generator, false, false, null),
            T("NoviceChecklist", "Mod Readiness Checklist", "Learning / Novice Modder Hub", DesktopToolKind.Generator, false, false, null),
            T("NoviceEvents", "Campaign Events Encyclopedia", "Learning / Novice Modder Hub", DesktopToolKind.Generator, false, false, null),
            T("NoviceHint", "Gauntlet Hint & Tooltip Forge", "Learning / Novice Modder Hub", DesktopToolKind.Generator, false, false, null),
            T("NoviceWorkshop", "Workshop & Enterprise Scaffolder", "Learning / Novice Modder Hub", DesktopToolKind.Generator, false, false, null),
            T("NoviceParty", "Bandit Clan & Hideout Spawner", "Learning / Novice Modder Hub", DesktopToolKind.Generator, false, false, null),
            T("NoviceBuilding", "Settlement Building Architect", "Learning / Novice Modder Hub", DesktopToolKind.Generator, false, false, null),
            T("NoviceCombatAi", "Combat Tactics & Shouts", "Learning / Novice Modder Hub", DesktopToolKind.Generator, false, false, null),
            T("SDK_ForgeKingdomManager", "ForgeKingdomManager", "Politics", DesktopToolKind.Generator, false, false, null),
            T("SDK_ForgeClanManager", "ForgeClanManager", "Politics", DesktopToolKind.Generator, false, false, null),
            T("SDK_ForgeRebellionSystem", "ForgeRebellionSystem", "Politics", DesktopToolKind.Generator, false, false, null),
            T("SDK_ForgePolicyEnforcer", "ForgePolicyEnforcer", "Politics", DesktopToolKind.Generator, false, false, null),
            T("SDK_ForgeVassalRelations", "ForgeVassalRelations", "Politics", DesktopToolKind.Generator, false, false, null),
            T("SDK_ForgeDiplomacyEngine", "ForgeDiplomacyEngine", "Politics", DesktopToolKind.Generator, false, false, null),
            T("SDK_ForgeMarriageArranger", "ForgeMarriageArranger", "Politics", DesktopToolKind.Generator, false, false, null),
            T("SDK_ForgeHeirDesignator", "ForgeHeirDesignator", "Politics", DesktopToolKind.Generator, false, false, null),
            T("SDK_ForgeElectionRigger", "ForgeElectionRigger", "Politics", DesktopToolKind.Generator, false, false, null),
            T("SDK_ForgeTreasonSystem", "ForgeTreasonSystem", "Politics", DesktopToolKind.Generator, false, false, null),
            T("SDK_ForgeCivilWarTrigger", "ForgeCivilWarTrigger", "Politics", DesktopToolKind.Generator, false, false, null),
            T("SDK_ForgeAllianceBuilder", "ForgeAllianceBuilder", "Politics", DesktopToolKind.Generator, false, false, null),
            T("SDK_ForgeTruceNegotiator", "ForgeTruceNegotiator", "Politics", DesktopToolKind.Generator, false, false, null),
            T("SDK_ForgeCasusBelli", "ForgeCasusBelli", "Politics", DesktopToolKind.Generator, false, false, null),
            T("SDK_ForgeSpyNetwork", "ForgeSpyNetwork", "Politics", DesktopToolKind.Generator, false, false, null),
            T("SDK_ForgeAssassinationPlot", "ForgeAssassinationPlot", "Politics", DesktopToolKind.Generator, false, false, null),
            T("SDK_ForgeInfluenceMarket", "ForgeInfluenceMarket", "Politics", DesktopToolKind.Generator, false, false, null),
            T("SDK_ForgeRenownTracker", "ForgeRenownTracker", "Politics", DesktopToolKind.Generator, false, false, null),
            T("SDK_ForgeTitleGranter", "ForgeTitleGranter", "Politics", DesktopToolKind.Generator, false, false, null),
            T("SDK_ForgeFactionSplitter", "ForgeFactionSplitter", "Politics", DesktopToolKind.Generator, false, false, null),
            T("SDK_ForgeNobleCourt", "ForgeNobleCourt", "Politics", DesktopToolKind.Generator, false, false, null),
            T("SDK_ForgeQuestManager", "ForgeQuestManager", "Campaign Campaign", DesktopToolKind.Generator, false, false, null),
            T("SDK_ForgeWeatherController", "ForgeWeatherController", "Campaign Campaign", DesktopToolKind.Generator, false, false, null),
            T("SDK_ForgeTimeManipulator", "ForgeTimeManipulator", "Campaign Campaign", DesktopToolKind.Generator, false, false, null),
            T("SDK_ForgeReligionSystem", "ForgeReligionSystem", "Campaign Campaign", DesktopToolKind.Generator, false, false, null),
            T("SDK_ForgeTraitManager", "ForgeTraitManager", "Campaign Campaign", DesktopToolKind.Generator, false, false, null),
            T("SDK_ForgeBanditController", "ForgeBanditController", "Campaign Campaign", DesktopToolKind.Generator, false, false, null),
            T("SDK_ForgeHideoutSpawner", "ForgeHideoutSpawner", "Campaign Campaign", DesktopToolKind.Generator, false, false, null),
            T("SDK_ForgeMercenaryHiring", "ForgeMercenaryHiring", "Campaign Campaign", DesktopToolKind.Generator, false, false, null),
            T("SDK_ForgeVillageHearthManager", "ForgeVillageHearthManager", "Campaign Campaign", DesktopToolKind.Generator, false, false, null),
            T("SDK_ForgeLoyaltyModifier", "ForgeLoyaltyModifier", "Campaign Campaign", DesktopToolKind.Generator, false, false, null),
            T("SDK_ForgeSecurityModifier", "ForgeSecurityModifier", "Campaign Campaign", DesktopToolKind.Generator, false, false, null),
            T("SDK_ForgeWoundRateController", "ForgeWoundRateController", "Campaign Campaign", DesktopToolKind.Generator, false, false, null),
            T("SDK_ForgeNotableSpawner", "ForgeNotableSpawner", "Campaign Campaign", DesktopToolKind.Generator, false, false, null),
            T("SDK_ForgeCaravanGuardManager", "ForgeCaravanGuardManager", "Campaign Campaign", DesktopToolKind.Generator, false, false, null),
            T("SDK_ForgeRandomEventTrigger", "ForgeRandomEventTrigger", "Campaign Campaign", DesktopToolKind.Generator, false, false, null),
            T("SDK_ForgePlagueSimulator", "ForgePlagueSimulator", "Campaign Campaign", DesktopToolKind.Generator, false, false, null),
            T("SDK_ForgeBanditInvasion", "ForgeBanditInvasion", "Campaign Campaign", DesktopToolKind.Generator, false, false, null),
            T("SDK_ForgeBountyHunting", "ForgeBountyHunting", "Campaign Campaign", DesktopToolKind.Generator, false, false, null),
            T("SDK_ForgeSlaveTrade", "ForgeSlaveTrade", "Campaign Campaign", DesktopToolKind.Generator, false, false, null),
            T("SDK_ForgeTournamentGenerator", "ForgeTournamentGenerator", "Campaign Campaign", DesktopToolKind.Generator, false, false, null),
            T("SDK_ForgeCustomSettlementBuilder", "ForgeCustomSettlementBuilder", "Campaign Campaign", DesktopToolKind.Generator, false, false, null),
            T("SDK_ForgeNavalTravel", "ForgeNavalTravel", "Campaign Campaign", DesktopToolKind.Generator, false, false, null),
            T("SDK_ForgeCampingSystem", "ForgeCampingSystem", "Campaign Campaign", DesktopToolKind.Generator, false, false, null),
            T("SDK_ForgeHuntingSystem", "ForgeHuntingSystem", "Campaign Campaign", DesktopToolKind.Generator, false, false, null),
            T("SDK_ForgeForagingSystem", "ForgeForagingSystem", "Campaign Campaign", DesktopToolKind.Generator, false, false, null),
            T("SDK_ForgeCompanionSpawner", "ForgeCompanionSpawner", "Campaign Campaign", DesktopToolKind.Generator, false, false, null),
            T("SDK_ForgeTradeManager", "ForgeTradeManager", "Economy Economy", DesktopToolKind.Generator, false, false, null),
            T("SDK_ForgeCaravanController", "ForgeCaravanController", "Economy Economy", DesktopToolKind.Generator, false, false, null),
            T("SDK_ForgeWorkshopManager", "ForgeWorkshopManager", "Economy Economy", DesktopToolKind.Generator, false, false, null),
            T("SDK_ForgeMarketFluctuation", "ForgeMarketFluctuation", "Economy Economy", DesktopToolKind.Generator, false, false, null),
            T("SDK_ForgeTaxesController", "ForgeTaxesController", "Economy Economy", DesktopToolKind.Generator, false, false, null),
            T("SDK_ForgeSmugglingSystem", "ForgeSmugglingSystem", "Economy Economy", DesktopToolKind.Generator, false, false, null),
            T("SDK_ForgeBlackMarket", "ForgeBlackMarket", "Economy Economy", DesktopToolKind.Generator, false, false, null),
            T("SDK_ForgeLoanSystem", "ForgeLoanSystem", "Economy Economy", DesktopToolKind.Generator, false, false, null),
            T("SDK_ForgeBankSystem", "ForgeBankSystem", "Economy Economy", DesktopToolKind.Generator, false, false, null),
            T("SDK_ForgeInvestmentTracker", "ForgeInvestmentTracker", "Economy Economy", DesktopToolKind.Generator, false, false, null),
            T("SDK_ForgeResourceDepletion", "ForgeResourceDepletion", "Economy Economy", DesktopToolKind.Generator, false, false, null),
            T("SDK_ForgeInflationController", "ForgeInflationController", "Economy Economy", DesktopToolKind.Generator, false, false, null),
            T("SDK_ForgeTradeRouteOptimizer", "ForgeTradeRouteOptimizer", "Economy Economy", DesktopToolKind.Generator, false, false, null),
            T("SDK_ForgeMerchantGuilds", "ForgeMerchantGuilds", "Economy Economy", DesktopToolKind.Generator, false, false, null),
            T("SDK_ForgeCurrencyExchange", "ForgeCurrencyExchange", "Economy Economy", DesktopToolKind.Generator, false, false, null),
            T("SDK_ForgePricePegging", "ForgePricePegging", "Economy Economy", DesktopToolKind.Generator, false, false, null),
            T("SDK_ForgeBribeManager", "ForgeBribeManager", "Economy Economy", DesktopToolKind.Generator, false, false, null),
            T("SDK_ForgeEconomicCrisis", "ForgeEconomicCrisis", "Economy Economy", DesktopToolKind.Generator, false, false, null),
            T("SDK_ForgeProsperityBooster", "ForgeProsperityBooster", "Economy Economy", DesktopToolKind.Generator, false, false, null),
            T("SDK_ForgeFamineSimulator", "ForgeFamineSimulator", "Economy Economy", DesktopToolKind.Generator, false, false, null),
            T("SDK_ForgeSupplyChainManager", "ForgeSupplyChainManager", "Economy Economy", DesktopToolKind.Generator, false, false, null),
            T("SDK_ForgeFormationController", "ForgeFormationController", "Combat Combat", DesktopToolKind.Generator, false, false, null),
            T("SDK_ForgeSiegeEngineManager", "ForgeSiegeEngineManager", "Combat Combat", DesktopToolKind.Generator, false, false, null),
            T("SDK_ForgeDamageModifier", "ForgeDamageModifier", "Combat Combat", DesktopToolKind.Generator, false, false, null),
            T("SDK_ForgeArmorPenetration", "ForgeArmorPenetration", "Combat Combat", DesktopToolKind.Generator, false, false, null),
            T("SDK_ForgeWeaponBreakage", "ForgeWeaponBreakage", "Combat Combat", DesktopToolKind.Generator, false, false, null),
            T("SDK_ForgeMoraleShock", "ForgeMoraleShock", "Combat Combat", DesktopToolKind.Generator, false, false, null),
            T("SDK_ForgeFleeLogic", "ForgeFleeLogic", "Combat Combat", DesktopToolKind.Generator, false, false, null),
            T("SDK_ForgeCavalryCharge", "ForgeCavalryCharge", "Combat Combat", DesktopToolKind.Generator, false, false, null),
            T("SDK_ForgePikemanBrace", "ForgePikemanBrace", "Combat Combat", DesktopToolKind.Generator, false, false, null),
            T("SDK_ForgeArcherVolley", "ForgeArcherVolley", "Combat Combat", DesktopToolKind.Generator, false, false, null),
            T("SDK_ForgeFriendlyFireAvoidance", "ForgeFriendlyFireAvoidance", "Combat Combat", DesktopToolKind.Generator, false, false, null),
            T("SDK_ForgeWeatherCombatMod", "ForgeWeatherCombatMod", "Combat Combat", DesktopToolKind.Generator, false, false, null),
            T("SDK_ForgeNightVisionPenalties", "ForgeNightVisionPenalties", "Combat Combat", DesktopToolKind.Generator, false, false, null),
            T("SDK_ForgeBleedEffect", "ForgeBleedEffect", "Combat Combat", DesktopToolKind.Generator, false, false, null),
            T("SDK_ForgePoisonEffect", "ForgePoisonEffect", "Combat Combat", DesktopToolKind.Generator, false, false, null),
            T("SDK_ForgeStunEffect", "ForgeStunEffect", "Combat Combat", DesktopToolKind.Generator, false, false, null),
            T("SDK_ForgeCleaveStrike", "ForgeCleaveStrike", "Combat Combat", DesktopToolKind.Generator, false, false, null),
            T("SDK_ForgeHeadshotBonus", "ForgeHeadshotBonus", "Combat Combat", DesktopToolKind.Generator, false, false, null),
            T("SDK_ForgeDismountChance", "ForgeDismountChance", "Combat Combat", DesktopToolKind.Generator, false, false, null),
            T("SDK_ForgeShieldBash", "ForgeShieldBash", "Combat Combat", DesktopToolKind.Generator, false, false, null),
            T("SDK_ForgeExecutionMoves", "ForgeExecutionMoves", "Combat Combat", DesktopToolKind.Generator, false, false, null),
            T("SDK_ForgeAmbushTactics", "ForgeAmbushTactics", "Combat Combat", DesktopToolKind.Generator, false, false, null),
            T("SDK_ForgeLootingBehavior", "ForgeLootingBehavior", "Combat Combat", DesktopToolKind.Generator, false, false, null),
            T("SDK_ForgeBodyguardAssignment", "ForgeBodyguardAssignment", "Combat Combat", DesktopToolKind.Generator, false, false, null),
            T("SDK_ForgeDuellingSystem", "ForgeDuellingSystem", "Combat Combat", DesktopToolKind.Generator, false, false, null),
            T("SDK_ForgeTargetPriority", "ForgeTargetPriority", "Combat Combat", DesktopToolKind.Generator, false, false, null),
            T("SDK_ForgeWeaponSwapLogic", "ForgeWeaponSwapLogic", "Combat Combat", DesktopToolKind.Generator, false, false, null),
            T("SDK_ForgeFormationSpacing", "ForgeFormationSpacing", "Combat Combat", DesktopToolKind.Generator, false, false, null),
            T("SDK_ForgeFloatingDamage", "ForgeFloatingDamage", "Gauntlet UI", DesktopToolKind.Generator, false, false, null),
            T("SDK_ForgeCustomCrosshair", "ForgeCustomCrosshair", "Gauntlet UI", DesktopToolKind.Generator, false, false, null),
            T("SDK_ForgeMinimapOverlay", "ForgeMinimapOverlay", "Gauntlet UI", DesktopToolKind.Generator, false, false, null),
            T("SDK_ForgeHealthBars", "ForgeHealthBars", "Gauntlet UI", DesktopToolKind.Generator, false, false, null),
            T("SDK_ForgeCombatCompass", "ForgeCombatCompass", "Gauntlet UI", DesktopToolKind.Generator, false, false, null),
            T("SDK_ForgeAdvancedKillfeed", "ForgeAdvancedKillfeed", "Gauntlet UI", DesktopToolKind.Generator, false, false, null),
            T("SDK_ForgeInventorySort", "ForgeInventorySort", "Gauntlet UI", DesktopToolKind.Generator, false, false, null),
            T("SDK_ForgePartyFilter", "ForgePartyFilter", "Gauntlet UI", DesktopToolKind.Generator, false, false, null),
            T("SDK_ForgeTroopTreeViewer", "ForgeTroopTreeViewer", "Gauntlet UI", DesktopToolKind.Generator, false, false, null),
            T("SDK_ForgeEncyclopediaExtender", "ForgeEncyclopediaExtender", "Gauntlet UI", DesktopToolKind.Generator, false, false, null),
            T("SDK_ForgeDialogueOptionsUI", "ForgeDialogueOptionsUI", "Gauntlet UI", DesktopToolKind.Generator, false, false, null),
            T("SDK_ForgeTradeProfitUI", "ForgeTradeProfitUI", "Gauntlet UI", DesktopToolKind.Generator, false, false, null),
            T("SDK_ForgeKingdomOverviewUI", "ForgeKingdomOverviewUI", "Gauntlet UI", DesktopToolKind.Generator, false, false, null),
            T("SDK_ForgeClanRolesUI", "ForgeClanRolesUI", "Gauntlet UI", DesktopToolKind.Generator, false, false, null),
            T("SDK_ForgeSiegeHUD", "ForgeSiegeHUD", "Gauntlet UI", DesktopToolKind.Generator, false, false, null),
            T("SDK_ForgeTournamentBracketUI", "ForgeTournamentBracketUI", "Gauntlet UI", DesktopToolKind.Generator, false, false, null),
            T("SDK_ForgeWeaponStatsUI", "ForgeWeaponStatsUI", "Gauntlet UI", DesktopToolKind.Generator, false, false, null),
            T("SDK_ForgeCharacterEditorExtra", "ForgeCharacterEditorExtra", "Gauntlet UI", DesktopToolKind.Generator, false, false, null),
            T("SDK_ForgeMapBordersUI", "ForgeMapBordersUI", "Gauntlet UI", DesktopToolKind.Generator, false, false, null),
            T("SDK_ForgeArmyMoraleUI", "ForgeArmyMoraleUI", "Gauntlet UI", DesktopToolKind.Generator, false, false, null),
            T("SDK_ForgeGarrisonManagerUI", "ForgeGarrisonManagerUI", "Gauntlet UI", DesktopToolKind.Generator, false, false, null),
            T("SDK_ForgeWorkshopStatsUI", "ForgeWorkshopStatsUI", "Gauntlet UI", DesktopToolKind.Generator, false, false, null),
            T("SDK_ForgeSettlementIcons", "ForgeSettlementIcons", "Gauntlet UI", DesktopToolKind.Generator, false, false, null),
            T("SDK_ForgePrisonerRansomUI", "ForgePrisonerRansomUI", "Gauntlet UI", DesktopToolKind.Generator, false, false, null),
            T("SDK_ForgeRelationshipBarsUI", "ForgeRelationshipBarsUI", "Gauntlet UI", DesktopToolKind.Generator, false, false, null),
            T("SDK_ForgeSkillTrackerUI", "ForgeSkillTrackerUI", "Gauntlet UI", DesktopToolKind.Generator, false, false, null),
            T("SDK_ForgeGoldTrackerUI", "ForgeGoldTrackerUI", "Gauntlet UI", DesktopToolKind.Generator, false, false, null),
            T("SDK_ForgeInfluenceGainUI", "ForgeInfluenceGainUI", "Gauntlet UI", DesktopToolKind.Generator, false, false, null),
            T("SDK_ForgeRenownGainUI", "ForgeRenownGainUI", "Gauntlet UI", DesktopToolKind.Generator, false, false, null),
            T("SDK_ForgeFoodConsumptionUI", "ForgeFoodConsumptionUI", "Gauntlet UI", DesktopToolKind.Generator, false, false, null),
        };
    }
}
