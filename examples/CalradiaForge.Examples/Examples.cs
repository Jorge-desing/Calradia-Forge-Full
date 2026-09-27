using System;
using System.Collections.Generic;
using System.Reflection;
using CalradiaForge.Core;
using CalradiaForge.Sdk;
using CalradiaForge.Mod;
using TaleWorlds.MountAndBlade;
using TaleWorlds.Library;

namespace CalradiaForge.Examples
{
    public sealed class SubModule : MBSubModuleBase
    {
        // The atomic helper cannot miss a connection that races module loading.
        protected override void OnSubModuleLoad() { ForgeApi.RegisterWhenAvailable(Register); }
        protected override void OnSubModuleUnloaded() { ForgeApi.UnregisterWhenAvailable(Register); ForgeApi.UnregisterUiPages("CalradiaForgeExamples"); }
        void Register(IForgeRegistry r)
        {
            ForgeApi.AutoRegister(Assembly.GetExecutingAssembly(), "CalradiaForgeExamples");
            ForgeApi.PatchBlueprints?.Register(new PatchBlueprintExample());
        }
    }
    // Campaign example: add one item and restore the original count even if verification fails.
    public sealed class InventoryExample : ITestCase
    {
        IGameLaboratory laboratory; int before; bool prepared;
        public Descriptor Descriptor => new Descriptor {Id="examples.inventory",Module="CalradiaForgeExamples",Name="Inventory",Context=Context.Campaign,ChangesState=true};
        public void Prepare(TestExecution e) {prepared=false; laboratory=(IGameLaboratory)e.Services.GetService(typeof(IGameLaboratory)); before=laboratory.GetGrainCount(); prepared=true; e.Steps.Add("grain.before="+before);}
        public void Execute(TestExecution e) {e.Cancellation.ThrowIfCancellationRequested(); laboratory.ChangeGrain(1); e.Steps.Add("grain.delta=1");}
        public void Verify(TestExecution e)=>e.Verify(laboratory.GetGrainCount()==before+1,"grain.after == before + 1");
        public void Cleanup(TestExecution e) {if(prepared) { laboratory.ChangeGrain(before-laboratory.GetGrainCount()); e.Steps.Add("grain.restored="+laboratory.GetGrainCount()); if(laboratory.GetGrainCount()!=before) throw new InvalidOperationException("Inventory cleanup failed"); prepared=false;}}
    }
    // Combat example: a temporary custom-battle agent, without changing campaign troops.
    public sealed class TroopExample : ITestCase
    {
        IGameLaboratory laboratory; object agent;
        public Descriptor Descriptor => new Descriptor {Id="examples.troops",Module="CalradiaForgeExamples",Name="Troops",Context=Context.Mission,ChangesState=true};
        public void Prepare(TestExecution e) {agent=null; laboratory=(IGameLaboratory)e.Services.GetService(typeof(IGameLaboratory)); e.Steps.Add("custom_battle.player_character; seed="+e.Seed);}
        public void Execute(TestExecution e) {e.Cancellation.ThrowIfCancellationRequested(); agent=laboratory.SpawnAgent(e.Seed);}
        public void Verify(TestExecution e)=>e.Verify(laboratory.AgentExists(agent),"spawned agent exists");
        public void Cleanup(TestExecution e) {laboratory?.RemoveAgent(agent); e.Steps.Add("agent.fadeout.requested"); agent=null;}
    }

    [ForgeUiPage("examples.readme", "ForgeExamplesPage", "CalradiaForgeExamples.Title", Context = Context.Any)]
    public sealed class ForgeExamplesPageViewModel : ViewModel
    {
        [DataSourceProperty] public string Title => "Calradia Forge UI example";
        [DataSourceProperty] public string Body => "This page is loaded as a separate Gauntlet movie from CalradiaForgeExamples/GUI/Prefabs.\n\nThe Forge SDK validated this ViewModel, its owner, the prefab file and the Command.Click binding. It does not patch another module's interface or sandbox extension code.";

        [ForgeUiCommand("close", "ExecuteClose", Context = Context.Any, ChangesState = false)]
        public void ExecuteClose() => ForgeUI.ClosePage();
    }
    // This declaration is intentionally inert. It exercises the SDK's target resolver without
    // bringing Harmony (or any other patch framework) into Forge's build or package.
    public sealed class PatchBlueprintExample : IPatchBlueprintProvider
    {
        public Descriptor Descriptor => new Descriptor {Id="examples.patch_blueprint",Module="CalradiaForgeExamples",Name="Patch blueprint example",Context=Context.Any,ChangesState=false};
        public IEnumerable<PatchBlueprint> Describe(PatchBlueprintRequest request)
        {
            request.Cancellation.ThrowIfCancellationRequested();
            yield return new PatchBlueprint {
                Id="examples.startup_observation",
                Name="Initial module screen hook",
                Hook=PatchHookKind.Prefix,
                Target=new MethodReference {
                    AssemblyName="TaleWorlds.MountAndBlade",
                    DeclaringType="TaleWorlds.MountAndBlade.MBSubModuleBase",
                    MemberName="OnBeforeInitialModuleScreenSetAsRoot",
                    ParameterTypes=new List<TypeReference>(),
                    IsStatic=false
                },
                PatchMethod=MethodReference.From(typeof(PatchBlueprintExample).GetMethod(nameof(ObserveStartup),BindingFlags.Public|BindingFlags.Static)),
                Rationale="A read-only declaration used to verify the loaded Bannerlord target."
            };
        }
        // Metadata only: Forge never invokes this method and it is not registered with Harmony.
        public static void ObserveStartup() { }
    }
    // ForgeWeave example: receives one official host event after Forge has finished connecting.
    // It deliberately consumes only copied scalar metadata and never reads or changes game state.
    public sealed class ForgeWeaveReadyExample : IForgeEventHandler
    {
        public ForgeEventSubscription Subscription => new ForgeEventSubscription {
            Descriptor=new Descriptor {Id="examples.forgeweave.ready",Module="CalradiaForgeExamples",Name="ForgeWeave ready observer",Context=Context.Any,ChangesState=false},
            Event=ForgeEventKind.ForgeReady,
            Priority=100,
            Access=ForgeEventAccess.Observe,
            // Declarative filters keep the handler focused on ForgeReady events from this suite.
            // Forge copies the pair when registering and applies it to live events and replay.
            Filter=new ForgeEventFilter {RequiredData=new Dictionary<string,string>{{"suite",SuiteInfo.Version}}},
            // This is a read-only host event, so it is the small safe Replay Lab example.
            // Writers must explicitly declare Live and still pass the host's existing gates.
            ReplayMode=ForgeReplayMode.Live
        };
        public void Handle(ForgeEvent @event)
        {
            @event.Cancellation.ThrowIfCancellationRequested();
            // The host has already bounded and copied this data. The example intentionally has
            // no side effect; ForgeWeave health records its invocation and duration.
            var suite=@event.Data.TryGetValue("suite",out var value)?value:"";
            if(suite.Length>128)throw new InvalidOperationException("Unexpected ForgeWeave suite metadata.");
        }
    }
}
