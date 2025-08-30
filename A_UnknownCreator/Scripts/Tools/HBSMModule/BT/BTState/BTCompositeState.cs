using System.Collections.Generic;

namespace UnknownCreator.Modules
{
    public abstract class BTCompositeState : BTStateBase
    {
        protected List<BTStateBase> children { private set; get; } = new();

        public int childrenCount => children.Count;

        public int currentIndex { get; protected set; }

        public override void Enter()
        {
            for (int i = children.Count - 1; i >= 0; i--)
                children[i]?.Enter();
        }

        public override void Exit()
        {
            if (isStarted == true)
            {
                isStarted = false;
                OnEnd();
            }
            for (int i = children.Count - 1; i >= 0; i--)
                children[i]?.Exit();
        }

        protected override void OnStart()
        {
            currentIndex = 0;
        }

        public override bool HasChild()
        => childrenCount > 0;

        public bool HasCurrentChild()
        => HasChild() && currentIndex < childrenCount;

        public BTStateBase AddChild(BTStateBase child)
        {
            if (child is null) return null;
            child.Init(string.Empty, cntlr, parent);
            children.Add(child);
            return child;
        }

        public T AddChild<T>() where T : BTStateBase, new()
        => (T)AddChild(Mgr.RPool.Load<T>());

        public void RemoveChild(BTStateBase child)
        {
            if (child is null) return;
            if (children.Remove(child))
                Mgr.RPool.Release(child);
        }

        protected override void OnRelease()
        {
            foreach (var item in children) Mgr.RPool.Release(item);
            children.Clear();
        }
    }

}