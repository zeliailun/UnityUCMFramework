namespace UnknownCreator.Modules
{
    public abstract class DamageData
    {
        public int victimID { get; set; }

        public virtual void Init(DamageData newData)
        {
            victimID = newData.victimID;
        }
    }
}