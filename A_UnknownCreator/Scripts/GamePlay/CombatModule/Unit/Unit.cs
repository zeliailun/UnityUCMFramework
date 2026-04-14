using System;
using System.Collections.Generic;
using Animancer;
using UnityEngine;

namespace UnknownCreator.Modules
{
    public sealed partial class Unit : IEntity
    {

        #region 基础

        // public UnitCfgSO unitCfg { private set; get; }

        public UnitCfg unitCfg { private set; get; }

        public UnitModelCfg unitModelCfg { private set; get; }

        public IHBSMController hbsm { private set; get; }

        public Unit master { get; set; }

        public GameObject ent { private set; get; }

        public Transform entT { private set; get; }

        public string entName { private set; get; }

        public EntityId entID { private set; get; }

        public Vector3 entP => entT.position;

        public Quaternion entR => entT.rotation;

        public bool hasMaster => master != null && !Mgr.RPool.HasObject(selfType, master);


        private bool _enable;
        public bool enable
        {
            set
            {
                if (_enable == value) return;
                _enable = value;
                if (_enable)
                    ShowEnt();
                else
                    HideEnt();
            }
            get => _enable;
        }


        //================================================================================


        public string modelCfgName { private set; get; }
        public GameObject model { private set; get; }
        public Transform modelLayerT { private set; get; }
        public Transform modelT { private set; get; }
        public GameObject modelLayerObj => modelLayerT.gameObject;
        public Vector3 modelP => modelT.position;
        public Quaternion modelR => modelT.rotation;




        //================================================================================


        public bool isAlive
        => alive?.Invoke() ?? false;

        public bool isAI
        => brainC.brainType == BrainType.AI;

        public bool isPlayer
        => brainC.brainType == BrainType.Player;

        public bool isCastAbilityPhase
        => abilityC.isCastPoint || abilityC.isCastBackswing;

        private int ut;
        public int unitType
        {
            get => ut;
            set
            {
                if (ut != value && value > -1 && value < Mgr.Unit.unitTypeCount)
                    ut = value;
            }
        }

        private int team;
        public int unitTeam
        {
            get => team;
            set
            {
                if (team != value && value > -1 && value < Mgr.Unit.unitTeamCount)
                {
                    var oldTeam = team;
                    team = value;
                    Mgr.Event.Send<EvtUnitTeamChanged>(new(this, oldTeam, team), UCMGameEvents.OnUnitTeamChanged);
                }
            }
        }

        #endregion


        #region 组件

        public BrainComp brainC { private set; get; }

        public AnimComp animC { private set; get; }

        public UStateComp stateC { private set; get; }

        public UStatsComp statsC { private set; get; }

        public UAbilityComp abilityC { private set; get; }

        // public UItemComp itemC { private set; get; }

        public UBuffComp buffC { private set; get; }

        public UTalentComp talentC { private set; get; }

        public ULevelExpComp lvExpC { private set; get; }

        #endregion


        #region 私有

        private List<EntityId> hitBoxID = new();
        private Dictionary<int, Transform> bodyPartsDict = new();
        private Func<bool> alive;
        private Type selfType;
        private string modelNewCfgName, modelOldCfgName;
        private bool isChangeModel, isShow;

        #endregion


        //=================================================================================


        public void Init(UnitCfg cfg)
        {
            if (cfg is null) return;

            selfType = typeof(Unit);

            unitCfg = cfg;
            ent = Mgr.GPool.Load(unitCfg.root, true, false);
            entName = ent.name;
            entID = ent.GetEntityId();
            entT = ent.GetComp<Transform>();
            modelLayerT = entT.Find(UnitGlobals.Model);
            hbsm = Mgr.RPool.Load<HBSMController>();
            hbsm.kv.AddValue<Unit>(this);


        }

        public void Setup()
        {
            animC = hbsm.AddComp<AnimComp>(true);
            statsC = hbsm.AddComp<UStatsComp>(true);
            if (!string.IsNullOrWhiteSpace(unitCfg.statsGroup))
            {
                var list = Mgr.JD.GetData<Dictionary<string, List<OverrideStats>>>(JsonCfgKeyGlobals.StatsGroupJson)[unitCfg.statsGroup];
                StatsCfg st;
                foreach (var item in list)
                {
                    st = Mgr.JD.GetData<Dictionary<string, StatsCfg>>(JsonCfgKeyGlobals.StatsJson)[item.baseCfgName];
                    statsC.AddStats(st, item.baseValue, null);
                }
            }

            SetModel(unitCfg.model, true);

            stateC = hbsm.AddComp<UStateComp>(true);
            lvExpC = hbsm.AddComp<ULevelExpComp>(true);
            talentC = hbsm.AddComp<UTalentComp>(true);
            abilityC = hbsm.AddComp<UAbilityComp>(true);
            buffC = hbsm.AddComp<UBuffComp>(true);
            //itemC = hbsm.AddComp<UItemComp>(true);
            brainC = hbsm.AddComp<BrainComp>(true);

            foreach (var item in unitCfg.builderDict.Values)
                item?.CreateUnitBuilder(this);
        }

        public void UpdataEnt()
        {
            if (master != null &&
                Mgr.RPool.HasObject(selfType, master))
                master = null;

            UpdateModel();
            hbsm.UpdateAllHBSM();
        }

        public void FixedUpdataEnt()
        {
            hbsm.FixedUpdateAllHBSM();
        }

        public void LateUpdataEnt()
        {

            hbsm.LateUpdateAllHBSM();
        }

        void IReference.ObjRelease()
        {
            _enable = false;
            ClearHitBox();
            ClearBodyPart();
            Mgr.RPool.Release(hbsm);
            Mgr.GPool.Release(unitModelCfg?.model, model);
            Mgr.GPool.Release(unitCfg.root, ent);
            unitCfg = null;
            unitModelCfg = null;
            master = null;
            brainC = null;
            lvExpC = null;
            stateC = null;
            statsC = null;
            buffC = null;
            abilityC = null;
            talentC = null;
            animC = null;
            hbsm = null;
            ent = null;
            entT = null;
            model = null;
            modelT = null;
            modelLayerT = null;
            modelCfgName = null;
            modelNewCfgName = null;
            modelOldCfgName = null;
            isChangeModel = false;
        }

        internal void SetModel(string cfgName, bool show)
        {
            if (string.IsNullOrWhiteSpace(cfgName))
            {
                UCMDebug.LogWarning("没有模型配置文件");
                return;
            }

            ClearHitBox();
            ClearBodyPart();

            unitModelCfg = Mgr.JD.GetData<Dictionary<string, UnitModelCfg>>(JsonCfgKeyGlobals.UnitModelJson)[cfgName];

            if (unitModelCfg == null) UCMDebug.LogError($"未找到模型配置文件: {cfgName}");

            model = Mgr.GPool.Load(unitModelCfg.model, false, false);
            modelCfgName = cfgName;
            modelT = model.GetComp<Transform>();
            modelT.SetParent(modelLayerT);
            modelT.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            animC.SetAnimComp(model.GetComp<AnimancerComponent>());

            foreach (var result in unitModelCfg.hitBoxList)
            {
                var id = entT.Find(result).gameObject.GetEntityId();
                hitBoxID.Add(id);
                Mgr.Unit.AddUnitRoot(id, this);
            }

            foreach (var result in unitModelCfg.bodyPartsList)
                AddBodyPart(result.id, result.path);

            if (show) ShowModel();

            Mgr.Event.Send<EvtUnitModelChanged>(new(modelOldCfgName, modelCfgName, this), UCMGameEvents.OnGetModelName);

        }

        private void SetModel(string cfgName)
        {
            if (unitModelCfg is null) return;

            ReleaseModel(unitModelCfg.model);
            SetModel(cfgName, isShow = (model == null || model.activeSelf));
        }

        private void UpdateModel()
        {
            if (unitModelCfg is null) return;

            modelNewCfgName = Mgr.Event.SendR<string>(UCMGameEvents.OnGetModelName, entID);

            if (model == null &&
                !string.IsNullOrWhiteSpace(modelNewCfgName))
            {
                isChangeModel = true;
                SetModel(modelNewCfgName, true);
                return;
            }

            if (modelCfgName == modelNewCfgName) return;

            if (string.IsNullOrWhiteSpace(modelNewCfgName))
            {
                if (!isChangeModel) return;
                isChangeModel = false;
                if (string.IsNullOrWhiteSpace(modelOldCfgName))
                {
                    ReleaseModel(unitModelCfg.model);
                }
                else
                {
                    (modelOldCfgName, modelCfgName) = (modelCfgName, modelOldCfgName);
                    SetModel(modelCfgName);
                }
            }
            else
            {
                isChangeModel = true;
                modelOldCfgName = modelCfgName;
                SetModel(modelCfgName = modelNewCfgName);
            }
        }

        private void ReleaseModel(string name)
        {
            if (unitModelCfg is null) return;

            if (model != null && !string.IsNullOrWhiteSpace(name))
            {
                Mgr.GPool.Release(name, model);
                Mgr.GPool.SetRoot(model, true);
                model = null;
                modelT = null;
            }
        }

        private void ClearHitBox()
        {
            foreach (var item in hitBoxID)
                Mgr.Unit.RemoveUnitRoot(item);
            hitBoxID.Clear();
        }



        #region 功能方法


        public void AddBodyPart(int id, string path)
        {
            if (!bodyPartsDict.TryGetValue(id, out _))
                bodyPartsDict.Add(id, modelT.Find(path));
        }

        public void RemoveBodyPart(int id)
        {
            bodyPartsDict.Remove(id);
        }

        public Transform GetBodyPart(int id)
        {
            return bodyPartsDict.TryGetValue(id, out Transform result) ? result : null;
        }

        public void ClearBodyPart()
        {
            bodyPartsDict.Clear();
        }


        public void ShowEnt()
        {
            ent.SetActive(true);
            hbsm.EnableAllHBSM();
        }

        public void HideEnt()
        {
            hbsm.DisableAllHBSM();
            ent.SetActive(false);
        }

        public void ShowModelLayer()
        {
            if (!modelLayerObj.activeSelf) modelLayerObj.SetActive(true);
        }

        public void HideModelLayer()
        {
            if (modelLayerObj.activeSelf) modelLayerObj.SetActive(false);
        }

        public void ShowModel()
        {
            if (model != null && !model.activeSelf) model.SetActive(true);
        }

        public void HideModel()
        {
            if (model != null && model.activeSelf) model.SetActive(false);
        }




        public T GetUnitBuilder<T>() where T : class, IUnitBuilder
        {
            if (unitCfg.builderDict.TryGetValue(typeof(T).Name, out var result))
            {
                return result as T;
            }
            return null;
        }

        public void ChangeModel(string cfgName)
        {
            if (model == null)
            {
                SetModel(cfgName, true);
                return;
            }
            modelOldCfgName = modelCfgName;
            SetModel(modelCfgName = cfgName);
        }

        public void SetAlive(Func<bool> func) => alive = func;

        public void ClearAlive() => alive = null;

        public bool HasAlive() => alive != null;

        public bool HasMaster()
        {
            return master != null;
        }

        public EntityId GetOwnerID()
        => HasMaster() ? master.entID : entID;

        #endregion


    }
}