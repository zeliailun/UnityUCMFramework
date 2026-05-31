using UnityEngine;
using UnityEngine.UIElements;

namespace UnknownCreator.Modules
{
    [RequireComponent(typeof(PanelRenderer))]
    public abstract class UITKMonoBase : MonoBehaviour, IOnUpdate
    {
        public PanelRenderer pr
        {
            get
            {
                GetUid();
                return _pr;
            }
        }
        private PanelRenderer _pr;

        protected virtual void Awake()
        {

        }

        protected virtual void OnDestroy()
        {

        }

        public virtual void OnUpdate()
        {

        }

        protected virtual void OnEnable()
        {
            pr.RegisterUIReloadCallback(OnUIReload);
            Mgr.Upd.AddUpdata(this);
        }

        protected virtual void OnDisable()
        {
            Mgr.Upd.RemoveUpdata(this);
            pr.UnregisterUIReloadCallback(OnUIReload);
           
        }

        protected virtual void OnUIReload(PanelRenderer panelRenderer, VisualElement rootElement, int version)
        {
  
        }

        private void GetUid()
        {
            if (_pr == null) _pr = GetComponent<PanelRenderer>();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            GetUid();
        }


#endif
    }
}