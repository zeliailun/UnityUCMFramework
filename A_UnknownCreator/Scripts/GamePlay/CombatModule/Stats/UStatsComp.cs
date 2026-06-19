using System;
using System.Collections.Generic;

namespace UnknownCreator.Modules
{
    public class UStatsComp : StateComp
    {
        private Dictionary<string, List<StatData>> statsDict = new();

        private List<StatData> statsList = new();

        public IReadOnlyList<StatData> allStatsList => statsList;

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

        public StatData GetStat(string name)
        => statsDict.TryGetValue(name, out var data) ? data[0] : null;

        public double GetStatsValue(string name)
        => GetStat(name)?.finalValue ?? 0;

        public List<StatData> GetStatsListByName(string name)
        => statsDict.TryGetValue(name, out var data) ? data : null;


        public StatData AddStats(StatsCfg cfg, double newV, object holder)
        {

            var unit = self.As<Unit>();
            StatData sd = Mgr.RPool.Load<StatData>();
            sd.Init(cfg, newV, this, unit, holder);

            if (statsDict.TryGetValue(cfg.idName, out var data))
                data.Add(sd);
            else
                statsDict.Add(sd.idName, new List<StatData>() { sd });

            statsList.Add(sd);

            // 新属性加入人物后，让已有 Buff 重新应用一次。
            unit?.buffC?.RefreshAllBuffStats();

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
            for (int i = statsList.Count - 1; i >= 0; i--)
            {
                var sd = statsList[i];
                statsList.RemoveAt(i);
                Mgr.RPool.Release(sd);
            }

            statsDict.Clear();
        }

        public void UpdateStats(BuffBase buff, string statsName, CalcType calcType, double value, bool isStatsStacked)
        {
            if (!statsDict.TryGetValue(statsName, out var data) ||
                !data.IsValid())
                return;

            var evt = new EvtStatWillUpdate(self.As<Unit>(), buff, statsName, calcType, value, isStatsStacked);

            GameEvtBus.Send<EvtStatWillUpdate>(evt);

            if (!statsDict.TryGetValue(statsName, out var data2) ||
                !data2.IsValid() ||
                !Mgr.Unit.unitStatsFilter.Invoke(evt))
                return;

            StatData sd;
            for (int i = data.Count - 1; i >= 0; i--)
            {
                sd = data[i];
                if (sd is null || !sd.canCalcValue) continue;
                sd.AddOrUpdateBuff(buff, calcType, value, isStatsStacked);
            }
        }

        public void ClearStatsCalc(BuffBase buff, CalcType calcType, string statsName, bool isStatsStacked)
        {
            if (buff == null ||
                statsDict == null ||
                !statsDict.TryGetValue(statsName, out var data) ||
                !data.IsValid())
                return;

            for (int i = data.Count - 1; i >= 0; i--)
            {
                var sd = data[i];
                if (sd == null)
                    continue;

                sd.Remove(buff, calcType, isStatsStacked);
            }
        }
        public bool HasStats(string statsName)
        => statsDict.TryGetValue(statsName, out var data) && data.Count > 0;
    }

}