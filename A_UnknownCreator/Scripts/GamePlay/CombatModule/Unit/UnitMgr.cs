using System;
using System.Collections.Generic;
using UnityEngine;


namespace UnknownCreator.Modules
{
    public sealed class UnitMgr : IUnitMgr
    {
        // private UnitMgr() { }

        public Func<(Unit, double), bool> FilterExpAdd { set; get; }

        [field: SerializeField]
        public int hitBoxLayer { get; private set; }

        [field: SerializeField]
        public int unitLayer { get; private set; }

        [field: SerializeField]
        public int unitStateCount { get; private set; }

        [field: SerializeField]
        public int unitTeamCount { get; private set; }

        [field: SerializeField]
        public int unitTypeCount { get; private set; }

        [field: SerializeField]
        public int unitMaxLevel { get; private set; }

        [field: SerializeField]
        public bool isUseGlobalLevelExp { get; set; } = true;

        [field: ShowSerializeReference]
        [field: SerializeReference]
        public IUnitExpBuilder expBuilder { get; private set; }

        [JsonIgnore]
        public List<double> unitExpList { get; private set; } = new();


        //===========================================================================

        private Dictionary<int, Unit> rootDict = new();

        void IDearMgr.WorkWork()
        {
            rootDict ??= new();
            UpdateMaxLevelAndFormula(expBuilder, unitMaxLevel);
        }

        void IDearMgr.DoNothing()
        {
            rootDict.Clear();
        }

        public void AddUnitRoot(int selfID, Unit unit)
         => rootDict.TryAdd(selfID, unit);

        public Unit GetUnitRoot(int selfID)
        => rootDict.TryGetValue(selfID, out var value) ? value : null;

        public void RemoveUnitRoot(int selfID)
        => rootDict.Remove(selfID);

        public void UpdateMaxLevelAndFormula(IUnitExpBuilder expBuilder, int value)
        {
            unitMaxLevel = value;
            unitExpList = expBuilder.ExpBuilder(unitMaxLevel);
        }
    }



}