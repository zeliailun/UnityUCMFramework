using UnityEngine;
namespace UnknownCreator.Modules
{
    public interface IDamageMgr : IDearMgr
    {
        FilterSlot<DamageData, bool> damageFilter { get; }

        void ApplyDamage<T>(T newData) where T : DamageData, new();

        void AddHurt<T>(EntityId id, T hurt) where T : class, IHealth;

        void RemoveHurt(EntityId id);
    }
}