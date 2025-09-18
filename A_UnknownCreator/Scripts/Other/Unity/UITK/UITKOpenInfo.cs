using UnityEngine.UIElements;
using System;
using UnityEngine.SceneManagement;
namespace UnknownCreator.Modules
{
    [Serializable]
    public struct UITKOpenInfo
    {
        public string uidName;
        public string uiName;
        public float delay;
        public float startDuration;
        public float endDuration;
        public bool isAutoHide;
        public bool isChangeSceneHide;
        public bool isTimeScale;
        public Action<VisualElement> onShow;
        public Action<VisualElement> onAutoHide;
        public Action<Scene, Scene,VisualElement> onChangeScene;
    }
}
