using UnityEngine;
namespace UnknownCreator.Modules
{
    public class StatsCfgSO : CustomScriptableObject
    {
        [Info("ID必须唯一 ")]
        [SerializeField]
        internal StatsCfg cfg = new();

        public bool isPInf;
        public bool isNInf;


        public override void OnEnable()
        {
            base.OnEnable();
            Default();
        }


        public override void OnValidate()
        {
            base.OnValidate();
            Default();

            if (isPInf)
                cfg.maxValue = double.PositiveInfinity;

            if (isNInf)
                cfg.minValue = double.NegativeInfinity;
        }

        private void Default()
        {
            if (cfg == null) return;

            if (!ReferenceEquals(name, cfg.idName))
                cfg.idName = name;

            if (cfg.minStatsName == cfg.idName)
                cfg.minStatsName = string.Empty;

            if (cfg.maxStatsName == cfg.idName)
                cfg.maxStatsName = string.Empty;
        }
    }
}

