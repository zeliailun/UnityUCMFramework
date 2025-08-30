using System;
using System.Collections.Generic;
namespace UnknownCreator.Modules
{
    public interface IHBSMController : IDearMgr
    {
        IVariableMgr kv { get; }
        void UpdateAllHBSM();
        void FixedUpdateAllHBSM();
        void LateUpdateAllHBSM();
        void EnableAllHBSM();
        void DisableAllHBSM();
        void ReleaseAllHBSM();
        void Create(Action<IHBSMController> builder);
        void Create(IHBSMBuilder builder);
        void Create(List<IHBSMBuilder> list);
        StateMachine AddHBSM(string name);
        T AddHBSM<T>(string name) where T : class, IStateMachine, new();
        void RemoveHBSM(string name);
        void RemoveAllHBSM();
        IStateMachine GetHBSM(string name);
        void EnableHBSM(string name);
        void DisableHBSM(string name);
        T AddComp<T>(bool isBefore) where T : StateComp, new();
        StateComp AddComp(string comp, bool isBefore);
        bool RemoveComp<T>() where T : StateComp, new();
        bool RemoveComp(Type type);
        void RemoveBeforeComp();
        void RemoveAfterComp();
        T GetComp<T>() where T : StateComp, new();
    }
}