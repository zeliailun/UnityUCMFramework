using System;
using UnityEngine;
namespace UnknownCreator.Modules
{
    public interface IDamageMgr : IDearMgr
    {
        Func<DamageData, bool> FilterDamageCalc { set; get; }

        void ApplyDamage<T>(T newData) where T : DamageData, new();

        void AddHurt<T>(EntityId id, T hurt) where T : class, IHealth;

        void RemoveHurt(EntityId id);
    }
}