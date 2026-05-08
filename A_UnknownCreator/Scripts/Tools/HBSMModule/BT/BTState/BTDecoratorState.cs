namespace UnknownCreator.Modules
{
    public abstract class BTDecoratorState : BTStateBase
    {
        protected BTStateBase child { private set; get; }

        protected override void OnRelease()
        {
            RemoveChild();
        }

        public BTStateBase AddChild(BTStateBase child)
        {
            if (child is null) return null;

            RemoveChild();

            child.Init(string.Empty, cntlr, parent);
            this.child = child;
            return child;
        }

        public T AddChild<T>() where T : BTStateBase, new()
        => (T)AddChild(Mgr.RPool.Load<T>());

        public void RemoveChild()
        {
            Mgr.RPool.Release(child);
            child = null;
        }

        public override bool HasChild()
        => child != null;

        public override void Enter()
        {
            child?.Enter();
        }

        public override void Exit()
        {
            if (isStarted == true)
            {
                isStarted = false;
                OnEnd();
            }
            child?.Exit();
        }
    }
}