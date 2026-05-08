namespace UnknownCreator.Modules
{
    public readonly struct EvtProjectileMotion : IBusEvent
    {
        public readonly Projectile proj;
        public readonly ProjectileData data;
        public readonly IVariableMgr kv;
        public readonly Unit owner;

        public EvtProjectileMotion(Projectile proj, ProjectileData data, IVariableMgr kv, Unit owner)
        {
            this.proj = proj;
            this.data = data;
            this.kv = kv;
            this.owner = owner;
        }
    }
}
