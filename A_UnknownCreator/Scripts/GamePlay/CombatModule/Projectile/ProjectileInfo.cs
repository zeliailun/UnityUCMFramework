namespace UnknownCreator.Modules
{
    public struct ProjectileInfo<IMvt, ICheck, Data>
        where IMvt : class, IProjMvt
        where Data : ProjectileData, new()
        where ICheck : class, IProjCheck
    {
        public IMvt mvt;
        public ICheck check;
        public IVariableMgr kv;
        public Data data;

        public readonly bool isValid => mvt != null && kv != null && data != null;

        public ProjectileInfo(IMvt mvt, ICheck check, IVariableMgr kv, Data data)
        {
            this.mvt = mvt;
            this.check = check;
            this.kv = kv;
            this.data = data;
        }

        public readonly ProjectileInfo<IProjMvt, IProjCheck, ProjectileData> As()
        {
            return new ProjectileInfo<IProjMvt, IProjCheck, ProjectileData>(
                mvt,
                check,
                kv,
                data
            );
        }

        public static ProjectileInfo<IMvt, ICheck, Data> Create(bool withCheck = true)
        {
            var mvt = Mgr.RPool.Load<IMvt>();
            var check = withCheck ? Mgr.RPool.Load<ICheck>() : null;
            var vb = Mgr.RPool.Load<VariableMgr>();
            var data = Mgr.RPool.Load<Data>();
            return new ProjectileInfo<IMvt, ICheck, Data>(mvt, check, vb, data);
        }


    }
}