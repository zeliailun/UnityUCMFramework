namespace UnknownCreator.Modules
{
    /// <summary>
    /// 投射物销毁时 (不要在事件里重复移除会引起bug)
    /// </summary>
    public readonly struct EvtProjectileDestroy : IBusEvent
    {
        public readonly Projectile proj;
        public readonly ProjectileData data;
        public readonly IVariableMgr kv;
        public readonly Unit owner;

        public EvtProjectileDestroy(Projectile proj, ProjectileData data, IVariableMgr kv, Unit owner)
        {
            this.proj = proj;
            this.data = data;
            this.kv = kv;
            this.owner = owner;
        }
    }
}
