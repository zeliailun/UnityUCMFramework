using System;
using UnityEngine.UIElements;
namespace UnknownCreator.Modules
{
    public abstract class UITKStateBase : StateBase
    {
        protected UIDocument uid => _uid = _uid != null ? _uid : kv.GetValue<UIDocument>();
        protected VisualElement root => uid.rootVisualElement;
        protected VisualElement currentP => _current ??= FindElementByPath(GetCurrentPanelPath());

        private VisualElement _current;
        private UIDocument _uid;

        public override void Enter()
        {
            OnShowUI();
            currentP.style.display = DisplayStyle.Flex;
            OnShowUIAfter();
        }

        public override void Exit()
        {
            OnHideUI();
            currentP.style.display = DisplayStyle.None;
        }

        protected override void Release()
        {
            _uid = null;
            _current = null;
        }

        protected virtual void OnShowUI()
        {

        }

        protected virtual void OnShowUIAfter()
        {

        }

        protected virtual void OnHideUI()
        {

        }

        protected virtual string GetCurrentPanelPath()
        => string.Empty;

        public VisualElement FindElementByPath(string path)
        {
            if (string.IsNullOrEmpty(path))
                return null;

            string[] parts = path.Split('/');

            VisualElement panel = root;

            foreach (var part in parts)
            {
                panel = panel.Q<VisualElement>(part);
                if (panel == null)
                    return null;
            }

            return panel;
        }

    }
}