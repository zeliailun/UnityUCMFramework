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
        public IReadOnlyList<double> unitExpList => unitExpListCache;

        private readonly List<double> unitExpListCache = new();

        private Dictionary<EntityId, Unit> rootDict = new();

        void IDearMgr.WorkWork()
        {
            rootDict ??= new();
            UpdateMaxLevelAndFormula(expBuilder, unitMaxLevel);
        }

        void IDearMgr.DoNothing()
        {
            rootDict.Clear();
        }

        public void AddUnitRoot(EntityId selfID, Unit unit)
         => rootDict.TryAdd(selfID, unit);

        public Unit GetUnitRoot(EntityId selfID)
        => rootDict.TryGetValue(selfID, out var value) ? value : null;

        public void RemoveUnitRoot(EntityId selfID)
        => rootDict.Remove(selfID);

        public void UpdateMaxLevelAndFormula(IUnitExpBuilder expBuilder, int value)
        {
            this.expBuilder = expBuilder;
            unitMaxLevel = Mathf.Max(0, value);

            unitExpListCache.Clear();

            if (this.expBuilder == null)
            {
                UCMDebug.LogWarning("UnitExpBuilder 未配置，无法生成全局单位经验表");
                return;
            }

            var list = this.expBuilder.ExpBuilder(unitMaxLevel);
            if (list == null)
            {
                UCMDebug.LogWarning("UnitExpBuilder 生成的经验表为空");
                return;
            }

            unitExpListCache.AddRange(list);
        }


    }



}