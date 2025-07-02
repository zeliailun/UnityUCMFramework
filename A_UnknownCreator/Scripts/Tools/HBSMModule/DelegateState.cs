using System;
namespace UnknownCreator.Modules
{
    /// <summary>
    /// 状态委托版本无需继承实现
    /// </summary>
    public sealed class DelegateState : StateBase
    {
        public Action init, release, enter, exit, update;

        public Func<bool> canEnter, canExit, canSetDefault;

        public sealed override void Init() => init?.Invoke();
        public sealed override void Enter() => enter?.Invoke();
        public sealed override void Exit() => exit?.Invoke();
        public sealed override void Update() => update?.Invoke();
        public sealed override bool CanEnter() => canEnter?.Invoke() ?? true;
        public sealed override bool CanExit() => canExit?.Invoke() ?? true;
        public sealed override bool CanSetDefault() => canSetDefault?.Invoke() ?? true;

        protected sealed override void Release()
        {
            release?.Invoke();
            init = null;
            release = null;
            enter = null;
            exit = null;
            update = null;
            canEnter = null;
            canExit = null;
            canSetDefault = null;
        }
    }
}