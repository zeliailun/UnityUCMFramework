using System.Collections.Generic;

namespace UnknownCreator.Modules
{
    public sealed class UTalentComp : StateComp
    {
        private Dictionary<string, List<TalentInfo>> abilityDict = new();
        private Dictionary<string, TalentInfo> talentDict = new();

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

        public void AddTalent(TalentInfo info)
        {
            if (HasTalent(info.talentName))
                return;

            if (info.isIndieTalent)
            {
                self.buffC.AddPermanentBuff(info.talentName, null, self);
            }
            else
            {
                if (abilityDict.TryGetValue(info.abilityName, out var value))
                    value.Add(info);
                else
                    abilityDict[info.abilityName] = new() { info };
            }

            talentDict[info.talentName] = info;
            Mgr.Event.Send<EvtTalentChanged>(new(owner: self, info.talentName), CombatEvtGlobals.OnTalentAdded);
        }

        public void RemoveTalent(string talentName)
        {
            if (!talentDict.Remove(talentName, out var result))
                return;

            if (result.isIndieTalent)
            {
                self.buffC.RemoveBuff(talentName);
            }
            else
            {
                var list = abilityDict[result.abilityName];
                for (int i = list.Count - 1; i >= 0; i--)
                {
                    if (list[i].talentName == talentName)
                    {
                        abilityDict[result.abilityName].RemoveAt(i);
                        break;
                    }
                }
                if (list.Count == 0) abilityDict.Remove(result.abilityName);
            }

            Mgr.Event.Send<EvtTalentChanged>(new(self, talentName), CombatEvtGlobals.OnTalentRemoved);
        }

        public bool HasTalent(string talentName)
        => talentDict.TryGetValue(talentName, out _);

        public bool HasAbility(string abilityName)
        => abilityDict.TryGetValue(abilityName, out _);

        public void ClearAbilityTalent(string abilityName)
        {
            if (abilityDict.TryGetValue(abilityName, out var value))
            {
                for (int i = 0; i < value.Count; i++)
                    talentDict.Remove(value[i].talentName);
            }
            abilityDict.Clear();
        }

        public void ClearAllTalent()
        {
            abilityDict.Clear();
            talentDict.Clear();
        }
    }
}