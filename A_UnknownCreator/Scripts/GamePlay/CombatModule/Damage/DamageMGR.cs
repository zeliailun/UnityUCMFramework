using System;
using System.Collections.Generic;

namespace UnknownCreator.Modules
{
    public sealed class DamageMGR : IDamageMgr
    {
        private Dictionary<int, IHealth> hurtDict = new();

        //private DamageMGR() { }

        void IDearMgr.WorkWork()
        {
            hurtDict ??= new();
        }

        void IDearMgr.DoNothing()
        {
            hurtDict.Clear();
            hurtDict = null;
        }

        public Func<DamageData, bool> FilterDamageCalc { set; get; }

        public void ApplyDamage<T>(T newData) where T : DamageData, new()
        {
            if (newData is null ||
                !hurtDict.TryGetValue(newData.victimID, out var target) ||
                (!target.isAlive && !target.isHurtAfterDeath))
                return;

            var data = Mgr.RPool.Load<T>();
            data.Init(newData);
            if (FilterDamageCalc(data))
                target.OnHurt(data);
            Mgr.RPool.Release(data);
        }

        public void AddHurt<T>(int id, T hurt) where T : class, IHealth
        => hurtDict.TryAdd(id, hurt);

        public void RemoveHurt(int id)
        {
            hurtDict.Remove(id);
        }
    }
}