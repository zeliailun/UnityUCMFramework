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

        public IHBSMController hbsm { private set; get; }

        public Unit master { get; set; }

        public GameObject ent { private set; get; }

        public Transform entT { private set; get; }

        public string entName { private set; get; }

        public string entClassName { private set; get; }

        public int entID { private set; get; }

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


        public string modelName { private set; get; }
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
                    Mgr.Event.Send<EvtUnitTeamChanged>(new(this, oldTeam, team), CombatEvtGlobals.OnUnitTeamChanged);
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

        private List<int> hitBoxID = new();
        private Dictionary<int, Transform> bodyPartsDict = new();
        private Func<bool> alive;
        private Type selfType;
        private string modelNewName, modelOldName;
        private bool isChangeModel, isShow;

        #endregion


        //=================================================================================


        public void InitEnt(string entName, GameObject ent, string cfgName)
        {
            selfType = typeof(Unit);

            this.ent = ent;
            this.entName = entName;
            entClassName = GetType().Name;
            entID = ent.GetInstanceID();
            entT = ent.GetComp<Transform>();
            modelLayerT = entT.Find(UnitGlobals.Model);
            //modelLayerObj.SetActive(true);

            unitCfg = Mgr.JD.GetData<Dictionary<string, UnitCfg>>(JsonCfgNameGlobals.UnitJson)[string.IsNullOrWhiteSpace(cfgName) ? entName : cfgName];

            hbsm = Mgr.RPool.Load<HBSMController>();
            hbsm.kv.AddValue<Unit>(this);
            animC = hbsm.AddComp<AnimComp>(true);
            SetModel(unitCfg.defaultModel, Mgr.GPool.Load(unitCfg.defaultModel, false, false), true);

            statsC = hbsm.AddComp<UStatsComp>(true);
            if (!string.IsNullOrWhiteSpace(unitCfg.statsGroup))
            {
                var list = Mgr.JD.GetData<Dictionary<string, List<OverrideStats>>>(JsonCfgNameGlobals.StatsGroupJson)[unitCfg.statsGroup];
                StatsCfg st;
                foreach (var item in list)
                {
                    st = Mgr.JD.GetData<Dictionary<string, StatsCfg>>(JsonCfgNameGlobals.StatsJson)[item.baseCfgName];
                    statsC.AddStats(st, item.baseValue, null);
                }
            }

            stateC = hbsm.AddComp<UStateComp>(true);
            lvExpC = hbsm.AddComp<ULevelExpComp>(true);
            talentC = hbsm.AddComp<UTalentComp>(true);
            abilityC = hbsm.AddComp<UAbilityComp>(true);
            buffC = hbsm.AddComp<UBuffComp>(true);
            //itemC = hbsm.AddComp<UItemComp>(true);
            brainC = hbsm.AddComp<BrainComp>(true);

            foreach (var item in unitCfg.builderDict.Values)
                item?.CreateUnitBuilder(this);

            hbsm.EnableAllHBSM();
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
            Mgr.GPool.Release(modelName, model);
            Mgr.GPool.Release(entName, ent);
            //UnityUtils.Release(unitCfg);
            unitCfg = null;
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
            modelNewName = null;
            modelOldName = null;
            isChangeModel = false;
        }

        internal void SetModel(string name, GameObject obj, bool isShow)
        {
            ClearHitBox();
            ClearBodyPart();
            modelName = name;
            model = obj;
            modelT = model.GetComp<Transform>();
            modelT.SetParent(modelLayerT);
            modelT.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            animC.SetAnimComp(model.GetComp<AnimancerComponent>());
            var modelCfg = Mgr.JD.GetData<Dictionary<string, UnitModelCfg>>(JsonCfgNameGlobals.UnitModelJson)[name];
            if (modelCfg != null)
            {
                foreach (var result in modelCfg.hitBoxList)
                {
                    int id = entT.Find(result).gameObject.GetInstanceID();
                    hitBoxID.Add(id);
                    Mgr.Unit.AddUnitRoot(id, this);
                }

                foreach (var result in modelCfg.bodyPartsList)
                    AddBodyPart(result.id, result.path);
            }
            if (isShow) ShowModel();
            Mgr.Event.Send<EvtUnitModelChanged>(new(modelOldName, modelName, this), CombatEvtGlobals.OnGetModelName);

        }

        private void SetModel()
        {
            ReleaseModel(modelOldName);
            SetModel(modelName, Mgr.GPool.Load(modelName, false, false), isShow = model == null || model.activeSelf);
        }

        private void UpdateModel()
        {
            modelNewName = Mgr.Event.SendR<string>(CombatEvtGlobals.OnGetModelName, entID);

            if (model == null &&
                !string.IsNullOrWhiteSpace(modelNewName))
            {
                isChangeModel = true;
                SetModel(modelNewName, Mgr.GPool.Load(modelNewName, false, false), true);
                return;
            }

            if (modelName == modelNewName) return;

            if (string.IsNullOrWhiteSpace(modelNewName))
            {
                if (!isChangeModel) return;
                isChangeModel = false;
                if (string.IsNullOrWhiteSpace(modelOldName))
                {
                    ReleaseModel(modelName);
                }
                else
                {
                    (modelOldName, modelName) = (modelName, modelOldName);
                    SetModel();
                }
            }
            else
            {
                isChangeModel = true;
                modelOldName = modelName;
                modelName = modelNewName;
                SetModel();
            }
        }

        private void ReleaseModel(string name)
        {
            if (model != null)
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

        public void ChangeModel(string name)
        {
            if (model == null)
            {
                SetModel(name, Mgr.GPool.Load(name, false, false), true);
                return;
            }
            modelOldName = modelName;
            modelName = name;
            SetModel();
        }

        public void SetAlive(Func<bool> func) => alive = func;

        public void ClearAlive() => alive = null;

        public bool HasAlive() => alive != null;


        public bool HasMaster()
        {
            return master != null;
        }

        #endregion


    }
}