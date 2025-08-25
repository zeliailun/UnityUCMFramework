using UnityEngine;

namespace UnknownCreator.Modules
{

    [HideScriptField]
    [CreateAssetMenu(fileName = "UCMGameCfg")]
    public class GameCfgSO : CustomScriptableObject
    {
        [field: SerializeReference]
        public GameCfg cfg { internal set; get; } = new();
    }


}
