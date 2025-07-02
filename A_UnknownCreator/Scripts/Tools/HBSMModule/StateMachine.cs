using System;
using System.Collections.Generic;

namespace UnknownCreator.Modules
{
    /// <summary>
    /// 【主状态机，子状态机】
    /// <para>
    /// 可创建多个状态机,主状态机无法被嵌套到其它状态机中，
    /// </para>
    /// </summary>
    public class StateMachine : IStateMachine, IReference
    {

        private Dictionary<string, IState> stDict = new();

        private List<IState> allStateList = new();

        private List<IState> seqStateList = new();

        private IState toState;

        public IState defaultState { get; private set; }

        public IState currentState { get; private set; }

        public IState previousState { get; private set; }

        public bool isTransition { get; private set; }

        public bool isActivated { get; private set; }

        public bool isMaster => cntlr.GetHBSM(stateName) is not null;

        public string stateName { get; private set; }

        public IHBSMController cntlr { get; private set; }

        public IStateMachine parent { get; private set; }

        public void Init(string stateName, IHBSMController cntlr, IStateMachine parent)
        {
            this.stateName = stateName;
            this.cntlr = cntlr;
            this.parent = parent;
            isTransition = false;
        }

        public T AddState<T>(string name, T st, bool isDefault = false) where T : class, IState, new()
        {
            if (IsStopAdd(name)) UCMDebug.LogWarning("【名称相同，父状态，主状态,中立状态均无法添加】");
            return (T)AddState(st, name, isDefault);
        }

        public T AddState<T>(string name, bool isDefault = false) where T : class, IState, new()
        {
            if (IsStopAdd(name)) UCMDebug.LogWarning("【名称相同，父状态，主状态,中立状态均无法添加】");

            if (stDict.TryGetValue(name, out var result))
                return (T)result;
            else
                return (T)AddState(Mgr.RPool.Load<T>(), name, isDefault);
        }

        public T AddState<T>(bool isDefault = false) where T : class, IState, new()
        {
            return AddState<T>(typeof(T).Name, isDefault);
        }

        public IState AddState(string typeName, bool isDefault = false)
        {
            if (IsStopAdd(typeName)) UCMDebug.LogWarning("【名称相同，父状态，主状态,中立状态均无法添加】");

            if (stDict.TryGetValue(typeName, out var result))
                return result;
            else
                return AddState((IState)Mgr.RPool.Load(Type.GetType(typeName)), typeName, isDefault);
        }

        public IState GetState(string name)
        => stDict.TryGetValue(name, out var value) ? value : null;

        public void RemoveState(string name)
        {
            var sb = GetState(name);
            if (sb is null) return;
            if (previousState.Equals(sb)) previousState = null;
            if (defaultState.Equals(sb)) defaultState = null;
            if (currentState.Equals(sb)) currentState = null;
            stDict.Remove(sb.stateName);
            allStateList.Remove(sb);
            seqStateList.Remove(sb);
            Mgr.RPool.Release(sb);
        }

        public void RemoveState<T>() where T : class, IState
        => RemoveState(typeof(T).Name);

        public void ChangeState(string name, bool isAddSeq, bool force = false)
        {
            if (IsStopChange(name)) return;

            toState = GetState(name);

            if (toState == null || (!force && !toState.CanEnter())) return;

            if (currentState == null)
            {
                isTransition = true;
                currentState = toState;
                currentState.Enter();
                isTransition = false;
                return;
            }

            if (name == currentState.stateName || (!force && !currentState.CanExit())) return;

            isTransition = true;

            currentState.Exit();
            previousState = currentState;

            if (isAddSeq && HasState(previousState.stateName))
                seqStateList.Add(previousState);

            currentState = toState;
            currentState.Enter();

            isTransition = false;
        }

        /*
        public void ChangeState(string name, bool isAddSeq)
        {
            if (IsStopChange(name)) return;

            toState = GetState(name);

            if (toState is null || !toState.CanEnter()) return;

            if (currentState is null)
            {
                isTransition = true;
                currentState = toState;
                currentState.Enter();
                isTransition = false;
                return;
            }

            if (name == currentState.stateName || !currentState.CanExit()) return;

            isTransition = true;
            currentState.Exit();
            previousState = currentState;

            if (isAddSeq &&
                previousState != null &&
                HasState(previousState.stateName))
                seqStateList.Add(previousState);

            currentState = toState;
            currentState.Enter();
            isTransition = false;
        }        */

        public void ChangeDefaultState(bool isAddSeq)
        {
            ChangeState(defaultState.stateName, isAddSeq);
        }

        public void ChangeNullState()
        {
            if (currentState is null || !currentState.CanExit()) return;
            isTransition = true;
            currentState.Exit();
            previousState = currentState;
            currentState = null;
            isTransition = false;
        }

        public void BackBeforeSeqState()
        {
            if (!seqStateList.IsValid()) return;

            var st = seqStateList[^1];
            if (st != null)
            {
                isTransition = true;
                currentState.Exit();
                previousState = currentState;
                currentState = st;
                currentState.Enter();
                seqStateList.Remove(currentState);
                isTransition = false;
            }
        }

        public void ClearSeqState()
        {
            seqStateList?.Clear();
        }

        public void SetDefaultState(string name)
        {
            SetDefault(GetState(name));
        }

        public void RepeatEnterCurrentState()
        {
            if (currentState is null || !currentState.CanEnter()) return;
            currentState.Enter();
        }

        public bool IsCurrentState(string name)
        => IsState(name, currentState);

        public bool IsDefaultState(string name)
        => IsState(name, defaultState);

        public bool IsPreviousState(string name)
        => IsState(name, previousState);

        public T GetState<T>(string name = null) where T : class, IState
        => (T)GetState(string.IsNullOrWhiteSpace(name) ? typeof(T).Name : name);

        public void ChangeState<T>(bool isAddSeq) where T : class, IState
        => ChangeState(typeof(T).Name, isAddSeq);

        public void SetDefaultState<T>() where T : class, IState
        => SetDefaultState(typeof(T).Name);

        public bool IsCurrentState<T>() where T : class, IState
        => IsCurrentState(typeof(T).Name);

        public bool IsDefaultState<T>() where T : class, IState
        => IsDefaultState(typeof(T).Name);

        public bool IsPreviousState<T>() where T : class, IState
        => IsPreviousState(typeof(T).Name);

        public bool HasState<T>() where T : class, IState
        {
            return HasState(typeof(T).Name);
        }

        public bool HasState(string name)
        {
            return stDict.TryGetValue(name, out _);
        }

        public void Enter()
        {
            if (!isActivated)
            {
                isActivated = true;
                currentState = defaultState;
                currentState?.Enter();
            }
        }

        public void Exit()
        {
            if (isActivated)
            {
                isActivated = false;
                if (currentState != null)
                {
                    previousState = currentState;
                    currentState.Exit();
                    currentState = null;
                }
            }
        }

        public void Update()
        {
            if (CanAnyUpdate())
                currentState.Update();
        }

        public void FixedUpdate()
        {
            if (CanAnyUpdate())
                currentState.FixedUpdate();
        }

        public void LateUpdate()
        {
            if (CanAnyUpdate())
                currentState.LateUpdate();
        }

        public bool CanEnter()
        => true;

        public bool CanExit()
        => true;

        public bool CanSetDefault()
        => true;

        void IReference.ObjRelease()
        {
            isActivated = false;
            seqStateList.Clear();
            stDict.Clear();
            IState st;
            for (int i = allStateList.Count - 1; i >= 0; i--)
            {
                st = allStateList[i];
                if (allStateList.Remove(st))
                    Mgr.RPool.Release(st);
            }
            toState = null;
            defaultState = null;
            currentState = null;
            previousState = null;
            cntlr = null;
            parent = null;
            stateName = null;
        }

        private bool IsStopChange(string name)
        => !isActivated ||
            isTransition ||
            name == stateName;

        private bool IsStopAdd(string name)
        => name == stateName ||
            (parent is not null && parent.stateName == name) ||
            cntlr.GetHBSM(name) is not null;

        private bool IsState<T>(string name, T st) where T : IState
         => st is not null && name == st.stateName;

        private void SetDefault(IState defState)
        {
            if (defState is null || !defState.CanSetDefault()) return;
            defaultState = defState;
        }

        private IState AddState(IState st, string name, bool isDefault)
        {

            st.Init(name, cntlr, this);
            stDict.Add(name, st);
            allStateList.Add(st);
            if (isDefault) SetDefault(st);
            return st;
        }


        private bool CanAnyUpdate()
        {
            return isActivated && currentState is not null && !isTransition;
        }
    }
}