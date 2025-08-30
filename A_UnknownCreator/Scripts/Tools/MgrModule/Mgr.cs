namespace UnknownCreator.Modules
{
    public static class Mgr
    {
        #region 管理器引用

        public static IControllerMgr Cntlr => cfg.inputMgr;

        public static IEventsMgr Event => cfg.eventsMgr;

        public static IUpdateMgr Upd => cfg.updateMgr;

        public static ITimerMgr Timer => cfg.timerMgr;

        public static IGameObjPoolMgr GPool => cfg.gameObjPoolMgr;

        public static IReferencePoolMgr RPool => cfg.refPoolMgr;

        public static IVariableMgr KV => cfg.variableBox;

        public static IDamageMgr Dmg => cfg.dmgMgr;

        public static IEntityMgr Ent => cfg.entityMgr;

        public static ICameraMgr Camera => cfg.cameraMgr;

        public static IUnitMgr Unit => cfg.unitMgr;

        //public static IItemMgr Item => cfg.itemMgr;

        public static IProjectileMgr Proj => cfg.projMgr;

        public static ISoundMgr Sound => cfg.soundMgr;

        public static IVfxMgr Vfx => cfg.vfxMgr;

        public static IHBSMController HFSM => cfg.hfsmMgr;

        public static ILoadSceneMgr Scene => cfg.sceneMgr;

        public static IJsonDataMgr JD => cfg.jsonMgr;

        #endregion

        public static bool isCreated { private set; get; } = false;


        private static GameCfg cfg;


        public static void Create(GameCfg _cfg)
        {
            if (isCreated) return;
            isCreated = true;
            cfg = _cfg;
            UCMDebug.isEnableDebug = cfg.enableDebug;
            God.AddMgr(cfg.refPoolMgr, true);
            God.AddMgr(cfg.gameObjPoolMgr, true);
            God.AddMgr(cfg.eventsMgr, true);
            God.AddMgr(cfg.variableBox, true);
            God.AddMgr(cfg.jsonMgr, true);
            God.AddMgr(cfg.updateMgr, true);
            God.AddMgr(cfg.timerMgr, true);
            God.AddMgr(cfg.sceneMgr, true);
            God.AddMgr(cfg.soundMgr, true);
            God.AddMgr(cfg.vfxMgr, true);
            God.AddMgr(cfg.hfsmMgr, true);
            God.AddMgr(cfg.cameraMgr, true);
            God.AddMgr(cfg.inputMgr, true);
            God.AddMgr(cfg.entityMgr, true);
            God.AddMgr(cfg.unitMgr, true);
            //God.AddMgr(cfg.itemMgr, true);
            God.AddMgr(cfg.dmgMgr, true);
            God.AddMgr(cfg.projMgr, true);
            foreach (CustomMgrCreateInfo kvp in cfg.customMgr)
                God.AddMgr(kvp.mgr, kvp.notRemove);
        }
    }
}