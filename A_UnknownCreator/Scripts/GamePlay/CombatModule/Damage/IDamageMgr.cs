using System;
namespace UnknownCreator.Modules
{
    public interface IDamageMgr : IDearMgr
    {
        Func<DamageData, bool> FilterDamageCalc { set; get; }

        void ApplyDamage<T>(T newData) where T : DamageData, new();

        void AddHurt<T>(int id, T hurt) where T : class, IHealth;

        void RemoveHurt(int id);
    }
}