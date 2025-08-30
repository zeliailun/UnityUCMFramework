using System;
using System.Collections.Generic;

namespace UnknownCreator.Modules
{
    public class UStatsComp : StateComp
    {
        private Dictionary<string, List<StatData>> statsDict = new();

        private List<StatData> statsList = new();

        private IEntity self;

        public override void InitComp()
        {
            self = kv.GetValue<Unit>();
        }

        public override void ReleaseComp()
        {
            RemoveAllStats();
            self = null;
        }

        public StatData GetHolderStats(string name, object holder)
        {
            if (statsDict.TryGetValue(name, out var data))
            {
                for (int i = 0; i < data.Count; i++)
                {
                    if (holder.Equals(data[i].holder))
                        return data[i];
                }
            }
            return null;
        }

        public StatData GetStats(string name)
        => statsDict.TryGetValue(name, out var data) ? data[0] : null;

        public double GetStatsValue(string name)
        => GetStats(name)?.finalValue ?? 0;

        public List<StatData> GetStatsListByName(string name)
        => statsDict.TryGetValue(name, out var data) ? data : null;

        public List<StatData> GetAllStatsList()
        => statsList;

        public StatData AddStats(StatsCfg cfg, double newV, object holder)
        {

            StatData sd = Mgr.RPool.Load<StatData>();
            sd.Init(cfg, newV, this, self.As<Unit>(), holder);
            if (statsDict.TryGetValue(cfg.idName, out var data))
                data.Add(sd);
            else
                statsDict.Add(sd.idName, new List<StatData>() { sd });
            statsList.Add(sd);
            return sd;
        }

        public void RemoveStats(StatData sd)
        {
            if (sd == null) return;
            if (statsDict.TryGetValue(sd.idName, out var data) &&
                data.Remove(sd))
            {
                statsList.Remove(sd);
                Mgr.RPool.Release(sd);

                if (data.Count < 1)
                    statsDict.Remove(sd.idName);
            }
        }

        public void RemoveAllStats()
        {
            statsDict.Clear();
            StatData sd;
            for (int i = statsList.Count - 1; i >= 0; i--)
            {
                sd = statsList[i];
                if (statsList.Remove(sd))
                    Mgr.RPool.Release(sd);
            }
        }

        public void UpdateStats(BuffBase buff, string statsName, CalcType calcType, double value, bool isStatsStacked)
        {
            if (!statsDict.TryGetValue(statsName, out var data) ||
                !data.IsValid())
                return;

            StatData sd;
            for (int i = 0; i < data.Count; i++)
            {
                sd = data[i];
                if (sd is null || !sd.canCalcValue) continue;
                sd.AddOrUpdateBuff(buff, calcType, value, isStatsStacked);

            }

        }

        public void ClearStatsCalc(BuffBase buff, CalcType calcType, string statsName, bool isStatsStacked)
        {
            if (!statsDict.TryGetValue(statsName, out var data) ||
                !data.IsValid())
                return;

            for (int i = data.Count - 1; i >= 0; i--)
                data[i]?.Remove(buff, calcType, isStatsStacked);
        }

        public bool HasStats(string statsName)
        => statsDict.TryGetValue(statsName, out var data) && data.Count > 0;
    }

}