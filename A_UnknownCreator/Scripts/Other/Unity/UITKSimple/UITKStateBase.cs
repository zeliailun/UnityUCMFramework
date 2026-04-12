using UnityEngine.UIElements;

namespace UnknownCreator.Modules
{
    public abstract class UITKStateBase : StateBase
    {
        protected UITKBuilder builder => _builder ??= kv.GetValue<UITKBuilder>(nameof(UITKBuilder));
        protected VisualElement root => _root ??= kv.GetValue<VisualElement>(nameof(VisualElement));
        protected VisualElement currentP => _current ??= FindElementByPath(GetCurrentPanelPath());
        protected PanelRenderer pr => builder.pr;
        protected Button backBtn => _back ??= currentP?.Q<Button>(GetBackName());

        private Button _back;
        private VisualElement _root;
        private VisualElement _current;
        private UITKBuilder _builder;

        public override void Refresh()
        {
            _root = kv.GetValue<VisualElement>(nameof(VisualElement));
            _current = FindElementByPath(GetCurrentPanelPath());
            BindBack();
            OnRefreshUI();
           
        }

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
            OnRelease();
            _root = null;
            _current = null;
            _builder = null;
            _back = null;
        }

        protected virtual void BindBack()
        {
            backBtn?.RegisterCallback<ClickEvent>(OnBackClick);
        }

        protected virtual void OnBackClick(ClickEvent evt)
        {
            if (!backBtn.TryClickCooldown()) return;
            parent?.BackBeforeSeqState();
            OnBack();
        }

        protected virtual void OnBack()
        {

        }

        protected virtual void OnRefreshUI()
        {

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

        protected virtual void OnRelease()
        {

        }

        protected virtual string GetCurrentPanelPath()
        => string.Empty;

        protected virtual string GetBackName()
        => "Back";

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