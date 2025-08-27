using UnityEngine;

namespace UnknownCreator.Modules
{

    [HideScriptField]
    [CreateAssetMenu(menuName = "UnknownCreator/GameCfgSO", fileName = "GameCfg")]
    public class GameCfgSO : CustomScriptableObject
    {
        [field: SerializeField]
        public GameCfg cfg { internal set; get; } = new();
    }


}
