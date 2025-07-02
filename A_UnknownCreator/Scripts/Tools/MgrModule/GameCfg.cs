using System;
using UnityEngine;
using System.Collections.Generic;

namespace UnknownCreator.Modules
{
    public class GameCfg
    {
        public bool enableDebug;

        public List<CustomMgrCreateInfo> customMgr = new();

        [SerializeReference, ShowSerializeReference]
        public IReferencePoolMgr refPoolMgr = new ReferencePoolMgr();

        [SerializeReference, ShowSerializeReference]
        public IGameObjPoolMgr gameObjPoolMgr = new GameObjPoolMgr();


        [SerializeReference, ShowSerializeReference]
        public IVariableMgr variableBox = new VariableMgr();

        [SerializeReference, ShowSerializeReference]
        public IEventsMgr eventsMgr = new EventsMgr();

        [SerializeReference, ShowSerializeReference]
        public IUpdateMgr updateMgr = new GameUpdateMgr();

        [SerializeReference, ShowSerializeReference]
        public ITimerMgr timerMgr = new TimerMgr();

        [SerializeReference, ShowSerializeReference]
        public ILoadSceneMgr sceneMgr = new LoadSceneMgr();

        [SerializeReference, ShowSerializeReference]
        public ISoundMgr soundMgr = new SoundMgr();

        [SerializeReference, ShowSerializeReference]
        public IVfxMgr vfxMgr = new VfxMgr();

        [SerializeReference, ShowSerializeReference]
        public ICameraMgr cameraMgr = new CameraMgr();

        [SerializeReference, ShowSerializeReference]
        public IControllerMgr inputMgr = new ControllerMgr();

        [SerializeReference, ShowSerializeReference]
        public IHBSMController hfsmMgr = new HBSMController();

        [SerializeReference, ShowSerializeReference]
        public IEntityMgr entityMgr = new EntityMgr();

        [Tooltip("-战斗模块信息提示-\n\n " +
          "1.StateCount会根据填写的数量从100开始创建，当单位创建时会默认加入其状态控制器\n\n " +
          "2.设置团队ID时必须小于TeamCount设定的值\n\n " +
          "3.设置单位类型ID时必须小于TypeCount设定的值\n\n " +
          "4.GlobalLevelExp为true时所有实体单位的等级经验会跟随管理器设置")]
        [SerializeReference, ShowSerializeReference]
        public IUnitMgr unitMgr = new UnitMgr();

        //  [HideInInspector]
        // public IItemMgr itemMgr = null;

        [SerializeReference, ShowSerializeReference]
        public IDamageMgr dmgMgr = new DamageMGR();

        [SerializeReference, ShowSerializeReference]
        public IProjectileMgr projMgr = new ProjectileMgr();

        [SerializeReference, ShowSerializeReference]
        public IJsonDataMgr jsonMgr = new JsonDataMgr();


    }


    [Serializable]
    public struct CustomMgrCreateInfo
    {
        [SerializeReference, ShowSerializeReference]
        public IDearMgr mgr;
        public bool notRemove;
    }


}