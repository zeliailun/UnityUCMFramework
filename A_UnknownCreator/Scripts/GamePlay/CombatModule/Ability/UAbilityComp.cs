using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnknownCreator.Modules
{
    public sealed class UAbilityComp : StateComp
    {
        private List<AbilityBase> abilityList = new();

        private Dictionary<long, AbilityBase> abilityById = new();

        public int abilityCount => abilityList.Count;

        public bool hasAbility
        => abilityCount > 0;

        public bool isCastPhase
        => castAbility != null && (isCastPoint || isCastBackswing);

        public bool isCastPoint { internal set; get; }

        public bool isCastBackswing { internal set; get; }

        private AbilityBase castAbility;

        private Unit self;

        private int maxAttempts = 3;

        public override void InitComp()
        {
            self = kv.GetValue<Unit>();

        }

        public override void ReleaseComp()
        {
            ClearAbility();
            castAbility = null;
            self = null;
        }

        public override void UpdateComp()
        {
            for (int i = 0; i < abilityList.Count; i++)
                abilityList[i]?.UpdateAbility();
        }


        public void AbilityPressed(string abName)
        {
            if (!Mgr.Cntlr.IsControllerTarget(self.ent)) return;
            GetAbility(abName)?.ExecuteAbilityPressed();
        }

        public void AbilityPressed(int id)
        {
            if (!Mgr.Cntlr.IsControllerTarget(self.ent) || !HasNonAbilityNullByIndex(id)) return;
            abilityList[id].ExecuteAbilityPressed();
        }

        public void AbilityReleased(string abName)
        {
            if (!Mgr.Cntlr.IsControllerTarget(self.ent)) return;

            GetAbility(abName)?.ExecuteAbilityReleased();
        }

        public void AbilityReleased(int id)
        {

            if (!Mgr.Cntlr.IsControllerTarget(self.ent) || !HasNonAbilityNullByIndex(id)) return;

            abilityList[id].ExecuteAbilityReleased();
        }

        public void TriggerAbilityOnImmediate(string abName)
        {
            GetAbility(abName)?.ExecuteAbilityOnImmediate();
        }

        public void TriggerAbilityOnImmediate(int id)
        {
            if (HasNonAbilityNullByIndex(id))
                abilityList[id]?.ExecuteAbilityOnImmediate();
        }

        public void TriggerAbilityOnPosition(string abName, Vector3 pos)
        {
            GetAbility(abName)?.ExecuteAbilityOnPosition(pos);
        }

        public void TriggerAbilityOnPosition(int id, Vector3 pos)
        {
            if (HasNonAbilityNullByIndex(id))
                abilityList[id]?.ExecuteAbilityOnPosition(pos);
        }

        public void TriggerAbilityOnTarget(string abName, Unit target)
        {
            if (target == null)
            {
                UCMDebug.Log("施法技能目标不能为空");
                return;
            }
            GetAbility(abName)?.ExecuteAbilityOnTarget(target);
        }

        public void TriggerAbilityOnTarget(int id, Unit target)
        {
            if (HasNonAbilityNullByIndex(id))
                abilityList[id]?.ExecuteAbilityOnTarget(target);
        }

        public void InterruptAbility()
        {
            InterruptCastPoint(castAbility);
            InterruptCastBackswing(castAbility);
        }

        public void InterruptAbility(bool isPointOrBackswing)
        {
            if (isPointOrBackswing)
                InterruptCastPoint(castAbility);
            else
                InterruptCastBackswing(castAbility);
        }

        public void InterruptAbility(AbilityBase ab)
        {
            if (!ab.Equals(castAbility)) return;
            InterruptCastPoint(ab);
            InterruptCastBackswing(ab);
        }

        public AbilityBase ReplaceAbilityNull(string abName, string cfgName, int index)
        {
            if (!HasAbilityNullByIndex(index)) return null;

            return ReplaceAbility(abName, cfgName, index);
        }

        public AbilityBase ReplaceAbilityNull(string abName, string cfgName)
        {
            AbilityBase oldAb;
            for (int i = 0; i < abilityCount; i++)
            {
                oldAb = abilityList[i];
                if (oldAb != null && oldAb.isNullAbility)
                    return ReplaceAbility(abName, cfgName, i);
            }
            return null;
        }

        public void ReplaceAbility(string abName, string cfgName, string targetAbility)
        {
            if (targetAbility == AbilityGlobals.AbilityNull ||
                targetAbility == nameof(AbilityNull)) return;

            AbilityBase oldAb;
            for (int i = 0; i < abilityCount; i++)
            {
                oldAb = abilityList[i];
                if (oldAb != null && oldAb.abName == targetAbility)
                {
                    ReplaceAbility(abName, cfgName, oldAb.index);
                    return;
                }
            }
        }


        public AbilityBase ReplaceAbility(string abName, string cfgName, int index)
        {
            var nullAb = abilityList[index];
            Mgr.Event.Send<AbilityBase>(nullAb, UCMGameEvents.OnRemoveAbility);
            Mgr.RPool.Release(nullAb);
            abilityById.Remove(nullAb.abilityID);
            AbilityBase newAb = (AbilityBase)Mgr.RPool.Load(Type.GetType(abName));
            newAb.InitAbility(self, index, abName, cfgName);
            abilityList[index] = newAb;
            abilityById.Add(newAb.abilityID, newAb);
            newAb.OnCreated();
            if (newAb.isRelease) return null;
            newAb.UpdateAbility();
            Mgr.Event.Send<AbilityBase>(newAb, UCMGameEvents.OnAbilityAdded);

            return newAb;
        }

        public AbilityBase AddAbility(string abName, string cfgName)
        {
            if (abName == nameof(AbilityNull))
                abName = AbilityGlobals.AbilityNull;

            AbilityBase newAb = (AbilityBase)Mgr.RPool.Load(Type.GetType(abName));
            newAb.InitAbility(self, abilityCount, abName, cfgName);
            abilityList.Add(newAb);
            abilityById.Add(newAb.abilityID, newAb);
            newAb.OnCreated();
            if (!newAb.isRelease)
            {
                newAb.UpdateAbility();
                Mgr.Event.Send<AbilityBase>(newAb, UCMGameEvents.OnAbilityAdded);
                return newAb;
            }
            return null;
        }

        public AbilityBase AddAbility(string abName)
        {
            return AddAbility(abName, abName);
        }

        public T AddAbility<T>(string cfgName) where T : AbilityBase, new()
        => (T)AddAbility(typeof(T).Name, cfgName);

        public void RemoveAbilityType<T>()
        => RemoveAbility(typeof(T).Name);

        public void RemoveAbility(int index)
        {
            if (!IsRemoveAbility(index)) return;
            var old = abilityList[index];
            InterruptAbility(old);
            Mgr.Event.Send<AbilityBase>(old, UCMGameEvents.OnRemoveAbility);
            Mgr.RPool.Release(old);
            abilityById.Remove(old.abilityID);
            AbilityBase generic = (AbilityBase)Mgr.RPool.Load(Type.GetType(AbilityGlobals.AbilityNull));
            generic.InitAbility(self, index, AbilityGlobals.AbilityNull, nameof(AbilityNull));
            abilityList[index] = generic;
            abilityById.Add(generic.abilityID, generic);
            generic.OnCreated();
            generic.UpdateAbility();
        }

        public void RemoveAbility(string abName)
        {
            AbilityBase ab;
            for (int i = abilityCount - 1; i >= 0; i--)
            {
                ab = abilityList[i];
                if (ab != null && ab.abName == abName)
                {
                    RemoveAbility(i);
                    return;
                }
            }
        }

        public AbilityBase GetAbility(int index)
        => HasNonAbilityNullByIndex(index) ? abilityList[index] : null;

        public AbilityBase GetAbility(string abName)
        {
            for (int i = 0; i < abilityList.Count; i++)
            {
                if (abilityList[i] is not null && abilityList[i].abName == abName) return abilityList[i];
            }
            return null;
        }

        public void GetAllAbility(Action<AbilityBase> ab)
        {
            foreach (var value in abilityList)
            {
                if (value != null)
                    ab?.Invoke(value);
            }
        }


        public int GetAbilityIndex(string abName)
        {
            for (int i = 0; i < abilityList.Count; i++)
            {
                if (abilityList[i] != null && abilityList[i].abName == abName) return i;
            }
            return -1;
        }

        public int GetAbilityIndex(AbilityBase ability)
        {
            return abilityList.IndexOf(ability);
        }

        public AbilityBase GetAbilityByID(long id)
        {
            return abilityById.TryGetValue(id, out var ab) ? ab : null;
        }

        public bool HasAbilityByID(long id)
        {
            return abilityById.TryGetValue(id, out var ab) ? true : false;
        }

        public bool HasAbility(string abName)
        {
            return GetAbility(abName) != null;
        }

        public bool HasAbilityNullByIndex(int id)
        => HasAbilityByIndex(id) && abilityList[id].isNullAbility;

        public bool HasNonAbilityNullByIndex(int id)
        => HasAbilityByIndex(id) && !abilityList[id].isNullAbility;

        private bool HasAbilityByIndex(int id)
        => hasAbility &&
           id < abilityCount &&
           abilityList[id] != null;

        private bool IsRemoveAbility(int id)
        => HasNonAbilityNullByIndex(id) &&
            !abilityList[id].isRelease &&
           !abilityList[id].isNullAbility;

        public bool IsValid(AbilityBase ability)
        {
            return ability != null &&
                   !ability.isRelease &&
                   !Mgr.RPool.HasObject(ability) &&
                   GetAbility(ability.abName) != null;
        }

        private void InterruptCastPoint(AbilityBase ab)
        {
            if (isCastPoint)
            {
                ab.OnCastInterrupt();
                ab.ExecuteAbilityInterrupt(true);
            }
        }

        private void InterruptCastBackswing(AbilityBase ab)
        {
            if (isCastBackswing)
            {
                ab.ExecuteAbilityInterrupt(false);
            }
        }



        private void ClearAbility()
        {
            int attemptCount = 0;
            abilityById.Clear();
            AbilityBase ab;
            while (abilityCount > 0)
            {
                attemptCount++;

                for (int i = abilityCount - 1; i >= 0; i--)
                {
                    ab = abilityList[i];
                    if (ab != null)
                    {
                        InterruptAbility(ab);
                        if (abilityList.Remove(ab))
                            Mgr.RPool.Release(ab);
                    }
                }

                if (attemptCount > maxAttempts)
                {
                    UCMDebug.LogWarning("技能生成可能触发了死循环");
                    break;
                }
            }
        }




        internal void SetCastAbility(AbilityBase ab)
        {
            castAbility = ab;
        }

        internal void SetCastPoint(bool value)
        {
            isCastPoint = value;
        }

        internal void SetCastBackswing(bool value)
        {
            isCastBackswing = value;
        }


    }
}