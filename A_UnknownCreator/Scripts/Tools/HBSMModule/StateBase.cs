namespace UnknownCreator.Modules
{
    public abstract class StateBase : IState, IReference
    {
        public string stateName { get; private set; }

        public IHBSMController cntlr { get; private set; }

        public IStateMachine parent { get; private set; }

        public IVariableMgr kv => cntlr.kv;

        public void Init(string stateName, IHBSMController cntlr, IStateMachine parent)
        {
            this.stateName = stateName;
            this.cntlr = cntlr;
            this.parent = parent;
            Init();
        }

        public virtual void Init() { }
        public virtual void Enter() { }
        public virtual void Exit() { }
        public virtual void Update() { }

        public virtual void FixedUpdate() { }

        public virtual void LateUpdate() { }

        public virtual bool CanEnter() => true;
        public virtual bool CanExit() => true;
        public virtual bool CanSetDefault() => true;
        protected virtual void Release() { }
        protected virtual void Destroy() { }

        void IReference.ObjRelease()
        {
            Release();
            cntlr = null;
            parent = null;
            stateName = null;
        }

        void IReference.ObjDestroy()
        {
            Destroy();
        }
    }

}