using System;

namespace UnknownCreator.Modules
{
    public abstract class StateComp : IReference
    {
        protected IHBSMController cntlr { private set; get; }

        protected IVariableMgr kv => cntlr.kv;

        private bool _enable;
        public bool enable
        {
            set
            {
                if (_enable == value) return;
                _enable = value;
                if (_enable)
                    EnableComp();
                else
                    DisableComp();
            }
            get => _enable;
        }

        public Type compType { private set; get; }

        internal void Init(IHBSMController cntlr,Type type)
        {
            this.cntlr = cntlr;
            this.compType = type;
            _enable = false;
            InitComp();
        }

        public virtual void InitComp()
        {

        }
        public virtual void UpdateComp() { }
        public virtual void FixedUpdateComp() { }
        public virtual void LateUpdateComp() { }
        public virtual void EnableComp() { }
        public virtual void DisableComp() { }
        public virtual void ReleaseComp() { }
        public virtual void DestroyComp() { }

        public virtual bool IsSkipGlobalEnable() => false;

        public virtual bool IsSkipGlobalDisable() => false;

        void IReference.ObjRelease()
        {
            ReleaseComp();
        }

        void IReference.ObjDestroy()
        {
            DestroyComp();
        }
    }
}