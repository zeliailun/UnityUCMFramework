namespace UnknownCreator.Modules
{
    public abstract class EntityComp : IReference
    {
        public IEntity self { private set; get; }

        public bool isActivated { private set; get; }

        internal void InitComp(IEntity self)
        {
            isActivated = true;
            this.self = self;
            InitComp();
        }

        public T GetEnt<T>() where T : class, IEntity
        => self as T;

        public virtual void InitComp()
        {

        }
        public virtual void CreatedComp() { }
        public virtual void UpdateComp() { }
        public virtual void ShowComp() { }
        public virtual void HideComp() { }
        public virtual void ReleaseComp() { }
        public virtual void DestroyComp() { }

        void IReference.ObjRelease()
        {
            isActivated = false;
            HideComp();
            ReleaseComp();
            self = null;
        }

        void IReference.ObjDestroy()
        {
            DestroyComp();
        }
    }
}