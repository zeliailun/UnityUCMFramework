using UnityEngine.UIElements;
using System;
using UnityEngine.SceneManagement;
namespace UnknownCreator.Modules
{
    [Serializable]
    public struct UITKOpenInfo
    {
        public string prName;
        public string uiName;
        public float delay;
        public float startDuration;
        public float endDuration;
        public bool isAutoHide;
        public bool isChangeSceneHide;
        public bool isTimeScale;
        public Action<VisualElement> onShow;
        public Action<VisualElement> onAutoHide;
        public Action<Scene, Scene, VisualElement> onChangeScene;

        public UITKOpenInfo(
            string uidName,
            string uiName,
            float delay = 0f,
            float startDuration = 0f,
            float endDuration = 0f,
            bool isAutoHide = false,
            bool isChangeSceneHide = false,
            bool isTimeScale = false,
            Action<VisualElement> onShow = null,
            Action<VisualElement> onAutoHide = null,
            Action<Scene, Scene, VisualElement> onChangeScene = null)
        {
            this.prName = uidName;
            this.uiName = uiName;
            this.delay = delay;
            this.startDuration = startDuration;
            this.endDuration = endDuration;
            this.isAutoHide = isAutoHide;
            this.isChangeSceneHide = isChangeSceneHide;
            this.isTimeScale = isTimeScale;
            this.onShow = onShow;
            this.onAutoHide = onAutoHide;
            this.onChangeScene = onChangeScene;
        }
    }
}
