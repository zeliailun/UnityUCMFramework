using System.Collections.Generic;
namespace UnknownCreator.Modules
{
    public enum BTStateType
    {
        None,
        Failed,
        Succeed,
        Running
    }

    public abstract class BTStateBase : StateBase
    {
        public BTStateType stateType { get; private set; } = BTStateType.Running;

        public bool isStarted { get; protected set; } = false;

        public bool isActivated { get; set; } = true;

        public sealed override void Update()
        {
            if (stateType is BTStateType.Running) UpdateBT();
        }

        protected sealed override void Release()
        {
            OnRelease();
            isActivated = true;
            isStarted = false;
        }

        public BTStateType UpdateBT()
        {
           
            if (!isActivated)
            {
                stateType = BTStateType.None;
                return stateType;
            }

            if (!isStarted)
            {
                isStarted = true;
                OnStart();
            }

            stateType = OnUpdate();

            if (stateType != BTStateType.Running)
            {
                isStarted = false;
                OnEnd();
            }
            return stateType;
        }

        public virtual bool HasChild() => false;

        protected abstract BTStateType OnUpdate();

        protected virtual void OnStart() { }

        protected virtual void OnEnd() { }

        protected virtual void OnRelease() { }
    }
}