using UnityEngine.UIElements;
using System;
namespace UnknownCreator.Modules
{
    [Serializable]
    public struct UITKHideInfo
    {
        public string uidName;
        public string uiName;
        public float hideDuration;
        public bool isTimeScale;
        public Action<VisualElement> onHide;
    }
}