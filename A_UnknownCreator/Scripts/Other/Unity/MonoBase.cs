
using UnityEngine;
namespace UnknownCreator.Modules
{
    public abstract class MonoBase : MonoBehaviour, IOnUpdate
    {
        public virtual void Awake()
        {
        }

        public virtual void Start()
        {

        }


        public virtual void OnEnable()
        {
            Mgr.Upd.AddUpdata(this);
        }

        public virtual void OnDisable()
        {
            Mgr.Upd.RemoveUpdata(this);
        }

        public virtual void OnDestroy()
        {
        }

        protected virtual void OnUpdate()
        {

        }

        void IOnUpdate.OnUpdate()
        {
            OnUpdate();
        }


    }
}