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

        [Tooltip("-ս��ģ����Ϣ��ʾ-\n\n " +
          "1.StateCount�������д��������100��ʼ����������λ����ʱ��Ĭ�ϼ�����״̬������\n\n " +
          "2.�����Ŷ�IDʱ����С��TeamCount�趨��ֵ\n\n " +
          "3.���õ�λ����IDʱ����С��TypeCount�趨��ֵ\n\n " +
          "4.GlobalLevelExpΪtrueʱ����ʵ�嵥λ�ĵȼ������������������")]
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