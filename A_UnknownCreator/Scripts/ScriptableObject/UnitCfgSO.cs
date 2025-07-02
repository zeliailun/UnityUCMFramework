using UnityEngine;

namespace UnknownCreator.Modules
{
    public class UnitCfgSO : CustomScriptableObject
    {
        /// <summary>
        /// 确保Unit预制体和UnitCfg数据文件名称一致 \n 确保UnitModel预制体和ModelCfg文件名称一致
        /// </summary>

        [field: SerializeField]
        public UnitCfg cfg { internal set; get; } = new();

        public override void OnValidate()
        {
            base.OnValidate();
            cfg.builderDict.Clear();
            foreach (var item in cfg.builders)
            {
                if (item != null)
                    cfg.builderDict[item.GetType().Name] = item;
            }
        }
    }
}