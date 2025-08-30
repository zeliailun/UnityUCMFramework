using UnityEngine;
using UnityEngine.UIElements;

namespace UnknownCreator.Modules
{
    public abstract class UITKMonoBase : MonoBehaviour, IOnUpdate
    {
        protected VisualElement root => uid.rootVisualElement;
        public UIDocument uid
        {
            get
            {
                GetUid();
                return _uid;
            }
        }
        private UIDocument _uid;

        protected virtual void Awake()
        {

        }

        protected virtual void OnDestroy()
        {

        }

        protected virtual void OnEnable()
        {
            Mgr.Upd.AddUpdata(this);
        }

        protected virtual void OnDisable()
        {
            Mgr.Upd.RemoveUpdata(this);
        }

        public virtual void OnUpdate()
        {

        }


        private void GetUid()
        {
            if (_uid == null) _uid = GetComponent<UIDocument>();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            GetUid();
        }


#endif
    }
}