using System;
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
        }

        void IDearMgr.DoNothing()
        {
            hurtDict.Clear();
        }

        public Func<DamageData, bool> FilterDamageCalc { set; get; } = _ => true;

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

                if (FilterDamageCalc(data))
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