using System.Collections.Generic;
using UnityEngine;

namespace UnknownCreator.Modules
{
    public sealed class DamageMgr : IDamageMgr
    {
        private Dictionary<EntityId, IHealth> hurtDict = new();

        //private DamageMGR() { }

        void IDearMgr.WorkWork()
        {
            hurtDict ??= new();
            damageFilter ??= new();
        }

        void IDearMgr.DoNothing()
        {
            damageFilter.Clear();
            hurtDict.Clear();
        }

        public FilterSlot<DamageData, bool> damageFilter { private set; get; }

        public void ApplyDamage<T>(T newData) where T : DamageData, new()
        {
            if (newData is null ||
                !hurtDict.TryGetValue(newData.victimID, out var target) ||
                (!target.isAlive && !target.isHurtAfterDeath))
                return;

            var data = Mgr.RPool.Load<T>();
            try
            {
                data.Init(newData);

                if (damageFilter.Invoke(data))
                    target.OnHurt(data);
            }
            finally
            {
                Mgr.RPool.Release(data);
            }
        }

        public void AddHurt<T>(EntityId id, T hurt) where T : class, IHealth
        {
            if (hurtDict is null || hurt is null)
                return;

            hurtDict[id] = hurt;
        }

        public void RemoveHurt(EntityId id)
        {
            hurtDict?.Remove(id);
        }
    }
}