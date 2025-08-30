using UnityEngine;
using UnityEngine.UIElements;
namespace UnknownCreator.Modules
{
    public abstract class UITKCompStateBase : StateComp
    {
        protected UIDocument uid => _uid = _uid != null ? _uid : kv.GetValue<UIDocument>();
        protected VisualElement root => uid.rootVisualElement;

        private UIDocument _uid;


        public override void ReleaseComp()
        {
            _uid = null;
        }
    }
}