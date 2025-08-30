using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnknownCreator.Modules
{
    public sealed class UAbilityComp : StateComp
    {
        private List<AbilityBase> abList = new();

        public int abilityCount => abList.Count;

        public bool hasAbility
        => abilityCount > 0;

        public bool isCastPhase
        => castAbility != null;

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
            for (int i = 0; i < abList.Count; i++)
                abList[i]?.UpdateAbility();
        }

        public void AbilityPressed(string abName)
        {
            if (!Mgr.Cntlr.IsControllerTarget(self.ent)) return;
            GetAbility(abName)?.ExecuteAbilityPressed();
        }

        public void AbilityPressed(int id)
        {
            if (!Mgr.Cntlr.IsControllerTarget(self.ent) || !HasNonAbilityNullByIndex(id)) return;
            abList[id]?.ExecuteAbilityPressed();
        }

        public void AbilityReleased(string abName)
        {
            if (!Mgr.Cntlr.IsControllerTarget(self.ent)) return;

            GetAbility(abName)?.ExecuteAbilityReleased();
        }

        public void AbilityReleased(int id)
        {

            if (!Mgr.Cntlr.IsControllerTarget(self.ent) || !HasNonAbilityNullByIndex(id)) return;

            abList[id]?.ExecuteAbilityReleased();
        }

        public void TriggerAbilityOnImmediate(string abName)
        {
            GetAbility(abName)?.ExecuteAbilityOnImmediate();
        }

        public void TriggerAbilityOnImmediate(int id)
        {
            if (HasNonAbilityNullByIndex(id))
                abList[id]?.ExecuteAbilityOnImmediate();
        }

        public void TriggerAbilityOnPosition(string abName, Vector3 pos)
        {
            GetAbility(abName)?.ExecuteAbilityOnPosition(pos);
        }

        public void TriggerAbilityOnPosition(int id, Vector3 pos)
        {
            if (HasNonAbilityNullByIndex(id))
                abList[id]?.ExecuteAbilityOnPosition(pos);
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
                abList[id]?.ExecuteAbilityOnTarget(target);
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
                oldAb = abList[i];
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
                oldAb = abList[i];
                if (oldAb != null && oldAb.abName == targetAbility)
                {
                    ReplaceAbility(abName, cfgName, oldAb.index);
                    return;
                }
            }
        }

        public AbilityBase AddAbility(string abName, string cfgName)
        {
            if (abName == nameof(AbilityNull))
                abName = AbilityGlobals.AbilityNull;

            AbilityBase newAb = (AbilityBase)Mgr.RPool.Load(Type.GetType(abName));
            newAb.InitAbility(self, abilityCount, abName, cfgName);
            abList.Add(newAb);
            newAb.OnCreated();
            if (!newAb.isRelease)
            {
                newAb.UpdateAbility();
                Mgr.Event.Send<AbilityBase>(newAb, CombatEvtGlobals.OnAbilityAdded);
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
            InterruptAbility(abList[index]);
            Mgr.Event.Send<AbilityBase>(abList[index], CombatEvtGlobals.OnRemoveAbility);
            Mgr.RPool.Release(abList[index]);
            AbilityBase generic = (AbilityBase)Mgr.RPool.Load(Type.GetType(AbilityGlobals.AbilityNull));
            generic.InitAbility(self, index, AbilityGlobals.AbilityNull, nameof(AbilityNull));
            abList[index] = generic;
            generic.OnCreated();
            generic?.UpdateAbility();
        }

        public void RemoveAbility(string abName)
        {
            AbilityBase ab;
            for (int i = abilityCount - 1; i >= 0; i--)
            {
                ab = abList[i];
                if (ab != null && ab.abName == abName)
                {
                    RemoveAbility(i);
                    return;
                }
            }
        }

        public AbilityBase GetAbility(int index)
        => HasNonAbilityNullByIndex(index) ? abList[index] : null;

        public AbilityBase GetAbility(string abName)
        {
            for (int i = 0; i < abList.Count; i++)
            {
                if (abList[i] is not null && abList[i].abName == abName) return abList[i];
            }
            return null;
        }

        public void GetAllAbility(Action<AbilityBase> ab)
        {
            foreach (var value in abList)
            {
                if (value != null)
                    ab?.Invoke(value);
            }
        }


        public int GetAbilityIndex(string abName)
        {
            for (int i = 0; i < abList.Count; i++)
            {
                if (abList[i] != null && abList[i].abName == abName) return i;
            }
            return -1;
        }

        public int GetAbilityIndex(AbilityBase ability)
        {
            return abList.IndexOf(ability);
        }

        public bool HasAbility(string abName)
        {
            return GetAbility(abName) != null;
        }

        public bool HasAbilityNullByIndex(int id)
        => HasAbilityByIndex(id) && abList[id].isNullAbility;

        public bool HasNonAbilityNullByIndex(int id)
        => HasAbilityByIndex(id) && !abList[id].isNullAbility;

        private bool HasAbilityByIndex(int id)
        => hasAbility &&
           id < abilityCount &&
           abList[id] != null;

        private bool IsRemoveAbility(int id)
        => HasNonAbilityNullByIndex(id) &&
            !abList[id].isRelease &&
           !abList[id].isNullAbility;

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

        public AbilityBase ReplaceAbility(string abName, string cfgName, int index)
        {
            var nullAb = abList[index];
            Mgr.Event.Send<AbilityBase>(nullAb, CombatEvtGlobals.OnRemoveAbility);
            Mgr.RPool.Release(nullAb);

            AbilityBase newAb = (AbilityBase)Mgr.RPool.Load(Type.GetType(abName));
            newAb.InitAbility(self, index, abName, cfgName);
            abList[index] = newAb;
            newAb.OnCreated();
            if (newAb.isRelease) return null;
            newAb.UpdateAbility();
            Mgr.Event.Send<AbilityBase>(newAb, CombatEvtGlobals.OnAbilityAdded);

            return newAb;
        }

        private void ClearAbility()
        {
            int attemptCount = 0;
            AbilityBase ab;
            while (abilityCount > 0)
            {
                attemptCount++;

                for (int i = abilityCount - 1; i >= 0; i--)
                {
                    ab = abList[i];
                    if (ab != null)
                    {
                        InterruptAbility(ab);
                        if (abList.Remove(ab))
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