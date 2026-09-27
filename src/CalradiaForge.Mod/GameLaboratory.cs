using System;
using System.Collections.Generic;
using System.Linq;
using CalradiaForge.Sdk;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using TaleWorlds.ObjectSystem;

namespace CalradiaForge.Mod
{
    // Game service shared by SDK examples. Never persisted in campaign saves.
    public interface IGameLaboratory
    {
        int GetGrainCount(); void ChangeGrain(int delta);
        object SpawnAgent(int seed); bool AgentExists(object agent); void RemoveAgent(object agent);
    }
    internal sealed class GameLaboratory : IGameLaboratory
    {
        readonly Dictionary<Agent,Tuple<Mission,int>> pendingRemoval=new Dictionary<Agent,Tuple<Mission,int>>();
        public Action<string> Record {get;set;}
        ItemRoster InventoryExample => MobileParty.MainParty?.ItemRoster ?? throw new InvalidOperationException("No main party");
        ItemObject Grain => MBObjectManager.Instance.GetObject<ItemObject>("grain") ?? throw new InvalidOperationException("No grain item");
        public int GetGrainCount()=>InventoryExample.GetItemNumber(Grain);
        public void ChangeGrain(int delta)=>InventoryExample.AddToCounts(Grain,delta);
        public object SpawnAgent(int seed)
        {
            var m=Mission.Current;
            // Custom battles only; never campaign missions or multiplayer.
            if(Campaign.Current!=null || m?.MainAgent==null || m.PlayerTeam==null) throw new InvalidOperationException("Open a custom battle with an active player agent");
            var troop=m.MainAgent.Character;
            var position=m.MainAgent.Position; var direction=m.MainAgent.LookDirection.AsVec2;
            // Custom battles use BasicCharacterObject; campaign origins cast it to CharacterObject.
            var data=new AgentBuildData(troop).Team(m.PlayerTeam).InitialPosition(in position).InitialDirection(in direction).EquipmentSeed(seed).NoHorses(true).Controller(AgentControllerType.AI).TroopOrigin(new BasicBattleAgentOrigin(troop));
            return m.SpawnAgent(data);
        }
        public bool AgentExists(object a)=>a is Agent agent && Mission.Current!=null && Mission.Current.Agents.Contains(agent);
        public void RemoveAgent(object a) {
            if(a is Agent agent && AgentExists(a)) {
                agent.FadeOut(true,true);
                if(pendingRemoval.Count<100)pendingRemoval[agent]=Tuple.Create(Mission.Current,agent.Index);
                Record?.Invoke("Agent "+agent.Index+": fade-out requested.");
            }
        }
        public void ObserveCleanup() {
            if(pendingRemoval.Count==0)return;
            foreach(var entry in pendingRemoval.ToArray()) {
                if(entry.Value.Item1!=Mission.Current) {
                    Record?.Invoke("Mission changed before agent removal could be verified.");
                    pendingRemoval.Remove(entry.Key);
                } else if(!entry.Value.Item1.Agents.Contains(entry.Key)) {
                    Record?.Invoke("Agent "+entry.Value.Item2+": removal verified on a later game tick.");
                    pendingRemoval.Remove(entry.Key);
                }
            }
        }
    }
}
