namespace UnknownCreator.Modules
{
    public interface IStateMachine : IState
    {
        IState currentState { get; }
        IState defaultState { get; }
        IState previousState { get; }
        bool isMaster { get; }
        bool isTransition { get; }
        bool isActivated { get; }

        IState AddState(string typeName, bool isDefault = false);
        T AddState<T>(string name, T sb, bool isDefault = false) where T : class, IState, new();
        T AddState<T>(string name = null, bool isDefault = false) where T : class, IState, new();
        T AddState<T>(bool isDefault = false) where T : class, IState, new();
        void RepeatEnterCurrentState();
        void ChangeNullState();
        void ChangeDefaultState(bool isAddSeq);
        void ChangeState<T>(bool isAddSeq) where T : class, IState;
        void ChangeState(string name, bool isAddSeq, bool force = false);
        void BackBeforeSeqState();
        void ClearSeqState();
        IState GetState(string name);
        bool IsCurrentState(string name);
        bool IsDefaultState(string name);
        bool IsPreviousState(string name);
        void RemoveState(string name);
        void SetDefaultState(string name);
        T GetState<T>(string name = null) where T : class, IState;
        void SetDefaultState<T>() where T : class, IState;
        bool IsCurrentState<T>() where T : class, IState;
        bool IsDefaultState<T>() where T : class, IState;
        bool IsPreviousState<T>() where T : class, IState;
        bool HasState<T>() where T : class, IState;
        bool HasState(string name);
    }
}