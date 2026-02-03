using UnityEngine;

namespace UnknownCreator.Modules
{
    public abstract class DamageData
    {
        public EntityId victimID { get; set; }

        public virtual void Init(DamageData newData)
        {
            victimID = newData.victimID;
        }
    }
}