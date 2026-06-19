using UnityEngine.UIElements;
using System;
namespace UnknownCreator.Modules
{
    [Serializable]
    public struct UITKHideInfo
    {
        public string prName;
        public string uiName;
        public float hideDuration;
        public bool isTimeScale;
        public Action<VisualElement> onHide;

        public UITKHideInfo(
        string uidName,
        string uiName,
        float hideDuration = 0,
        bool isTimeScale = false,
        Action<VisualElement> onHide = null)
        {
            this.prName = uidName;
            this.uiName = uiName;
            this.hideDuration = hideDuration;
            this.isTimeScale = isTimeScale;
            this.onHide = onHide;
        }
    }
}