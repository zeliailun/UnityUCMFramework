using System;
using System.Collections.Generic;

namespace UnknownCreator.Modules
{
    public sealed class UTalentComp : StateComp
    {
        private Dictionary<string, AbilityBase> talentDict = new();

        private List<AbilityBase> talentList = new();

        private Unit self;

        public override void InitComp()
        {
            self = kv.GetValue<Unit>();
        }

        public override void ReleaseComp()
        {
            ClearAllTalent();
            self = null;
        }

        public void AddTalent(string talentName)
        {
            if (talentName == AbilityGlobals.AbilityNull) return;

            AbilityBase newAb = (AbilityBase)Mgr.RPool.Load(Type.GetType(talentName));
            if (newAb.abilityCfg.isTalent)
            {
                Mgr.RPool.Release(newAb);
                return;
            }

            newAb.InitAbility(self, -1, talentName, talentName);
            talentDict.Add(talentName, newAb);
            talentList.Add(newAb);
            newAb.OnCreated();
            if (!newAb.isRelease)
            {
                newAb.UpdateAbility();
                Mgr.Event.Send<EvtTalentChanged>(new(self, newAb), UCMGameEvents.OnTalentAdded);
            }
        }

        public void RemoveTalent(string talentName)
        {
            if (!talentDict.Remove(talentName, out var result))
                return;
            talentList.Remove(result);
            Mgr.Event.Send<EvtTalentChanged>(new(self, result), UCMGameEvents.OnTalentRemove);
            Mgr.RPool.Release(result);
        }

        public AbilityBase GetTalent(string talentName)
         => talentDict.TryGetValue(talentName, out var result) ? result : null;

        public bool HasTalent(string talentName)
        => talentDict.TryGetValue(talentName, out _);


        public void ClearAllTalent()
        {
            for (int i = talentList.Count - 1; i >= 0; i--)
                Mgr.RPool.Release(talentList[i]);
            talentList.Clear();
        }
    }
}