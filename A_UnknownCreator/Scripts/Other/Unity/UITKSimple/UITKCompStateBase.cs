
using UnityEngine.UIElements;
namespace UnknownCreator.Modules
{
    public abstract class UITKCompStateBase : StateComp
    {
        protected UITKBuilder builder => _builder ??= kv.GetValue<UITKBuilder>(nameof(UITKBuilder));

        protected VisualElement root => _root ??= kv.GetValue<VisualElement>(nameof(VisualElement));
        protected PanelRenderer pr => builder.pr;

        private UITKBuilder _builder;
        private VisualElement _root;


        public override void RefreshComp()
        {
            _root = kv.GetValue<VisualElement>(nameof(VisualElement));
            OnRefreshComp();
            Mgr.Event.Send<UITKCompStateBase>(this, UITKGlobals.OnRefreshUIComp);
        }

        public override void ReleaseComp()
        {
            _root = null;
            _builder = null;
        }

        protected virtual void OnRefreshComp() { }
    }
}