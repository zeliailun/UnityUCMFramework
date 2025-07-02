using System;
using System.Collections.Generic;
namespace UnknownCreator.Modules
{
    public class HBSMController : IReference, IHBSMController
    {
        private Dictionary<string, IStateMachine> hfsmDict = new();

        private List<IStateMachine> hfsmList = new();

        private Dictionary<Type, StateComp> compDict = new();

        private List<StateComp> compListBefore = new();

        private List<StateComp> compListAfter = new();

        private IVariableMgr box;
        public IVariableMgr kv => box ??= Mgr.RPool.Load<VariableMgr>();

        private int maxAttempts = 3;

        void IDearMgr.WorkWork()
        {
            hfsmDict ??= new();
            hfsmList ??= new();
            compDict ??= new();
            compListBefore ??= new();
            compListAfter ??= new();
            box ??= Mgr.RPool.Load<VariableMgr>();
        }

        void IDearMgr.DoNothing()
        {
            ReleaseAllHBSM();
            hfsmDict.Clear();
            hfsmList.Clear();
            compDict.Clear();
            compListBefore.Clear();
            compListAfter.Clear();
        }

        void IDearMgr.UpdateMGR()
        {
            UpdateAllHBSM();
        }

        void IDearMgr.FixedUpdateMGR()
        {
            FixedUpdateAllHBSM();
        }

        void IDearMgr.LateUpdateMGR()
        {
            LateUpdateAllHBSM();
        }

        public void UpdateAllHBSM()
        {
            for (int i = 0; i < compListBefore.Count; i++)
            {
                if (compListBefore[i].enable)
                    compListBefore[i].UpdateComp();
            }
            for (int i = 0; i < hfsmList.Count; i++) hfsmList[i].Update();
            for (int i = 0; i < compListAfter.Count; i++)
            {
                if (compListAfter[i].enable)
                    compListAfter[i].UpdateComp();
            }
        }

        public void FixedUpdateAllHBSM()
        {
            for (int i = 0; i < compListBefore.Count; i++)
            {
                if (compListBefore[i].enable)
                    compListBefore[i].FixedUpdateComp();
            }
            for (int i = 0; i < hfsmList.Count; i++) hfsmList[i].FixedUpdate();
            for (int i = 0; i < compListAfter.Count; i++)
            {
                if (compListAfter[i].enable)
                    compListAfter[i].FixedUpdateComp();
            }
        }

        public void LateUpdateAllHBSM()
        {
            for (int i = 0; i < compListBefore.Count; i++)
            {
                if (compListBefore[i].enable)
                    compListBefore[i].LateUpdateComp();
            }
            for (int i = 0; i < hfsmList.Count; i++) hfsmList[i].LateUpdate();
            for (int i = 0; i < compListAfter.Count; i++)
            {
                if (compListAfter[i].enable)
                    compListAfter[i].LateUpdateComp();
            }
        }

        public void EnableAllHBSM()
        {
            for (int i = 0; i < compListBefore.Count; i++)
                if (!compListBefore[i].IsSkipGlobalEnable())
                    compListBefore[i].enable = true;

            for (int i = 0; i < hfsmList.Count; i++) hfsmList[i].Enter();


            for (int i = 0; i < compListAfter.Count; i++)
                if (!compListAfter[i].IsSkipGlobalEnable())
                    compListAfter[i].enable = true;
        }

        public void DisableAllHBSM()
        {
            for (int i = 0; i < compListBefore.Count; i++)
                if (!compListBefore[i].IsSkipGlobalDisable())
                    compListBefore[i].enable = false;

            for (int i = 0; i < hfsmList.Count; i++) hfsmList[i].Exit();

            for (int i = 0; i < compListAfter.Count; i++)
                if (!compListAfter[i].IsSkipGlobalDisable())
                    compListAfter[i].enable = false;
        }

        public void ReleaseAllHBSM()
        {
            RemoveBeforeComp();
            RemoveAllHBSM();
            RemoveAfterComp();
            Mgr.RPool.Release(box);
            box = null;
        }

        public void Create(Action<IHBSMController> builder)
        => builder?.Invoke(this);

        public void Create(IHBSMBuilder builder)
        => builder?.CreateHBSM(this);

        public void Create(List<IHBSMBuilder> list)
        {
            if (list is null) return;
            foreach (var builder in list)
                Create(builder);
        }

        public StateMachine AddHBSM(string name)
        => Add<StateMachine>(name);

        public T AddHBSM<T>(string name) where T : class, IStateMachine, new()
        => Add<T>(name);

        public IStateMachine GetHBSM(string name)
        => hfsmDict.TryGetValue(name, out var hfsm) ? hfsm : null;

        public void RemoveHBSM(string name)
        {
            if (!hfsmDict.Remove(name, out var result)) return;
            hfsmList.Remove(result);
            Mgr.RPool.Release(result);
        }

        public void RemoveAllHBSM()
        {
            hfsmDict.Clear();
            IStateMachine hfsm;
            for (int i = hfsmList.Count - 1; i >= 0; i--)
            {
                hfsm = hfsmList[i];
                hfsmList.RemoveAt(i);
                Mgr.RPool.Release(hfsm);
            }
        }

        public void EnableHBSM(string name)
        {
            GetHBSM(name)?.Enter();
        }

        public void DisableHBSM(string name)
        {
            GetHBSM(name)?.Exit();
        }

        public T AddComp<T>(bool isBefore) where T : StateComp, new()
        {
            var type = typeof(T);
            if (!compDict.TryGetValue(type, out var result))
            {
                var comp = Mgr.RPool.Load<T>();
                comp.Init(this, type);
                compDict.Add(type, comp);
                if (isBefore)
                    compListBefore.Add(comp);
                else
                    compListAfter.Add(comp);
                return comp;
            }
            return (T)result;
        }

        public StateComp AddComp(string comp, bool isBefore)
        {
            var type = Type.GetType(comp);
            if (!compDict.TryGetValue(type, out var result))
            {
                var sc = (StateComp)Mgr.RPool.Load(type);
                sc.Init(this, type);
                compDict.Add(type, sc);
                if (isBefore)
                    compListBefore.Add(sc);
                else
                    compListAfter.Add(sc);
                return sc;
            }
            return result;
        }

        public T GetComp<T>() where T : StateComp, new()
        => compDict.TryGetValue(typeof(T), out var result) ? (T)result : null;

        public bool RemoveComp<T>() where T : StateComp, new()
        => RemoveComp(typeof(T));

        public bool RemoveComp(Type type)
        {
            if (compDict.Remove(type, out var result))
            {
                compListBefore.Remove(result);
                compListAfter.Remove(result);
                Mgr.RPool.Release(result);
                return true;
            }
            return false;
        }

        public void RemoveBeforeComp()
        {
            RemoveCompFromList(compListBefore);
            if (compListBefore.Count > 0)
            {
                UCMDebug.LogError("状态机Before组件生成可能触发了死循环");
            }
        }

        public void RemoveAfterComp()
        {
            RemoveCompFromList(compListAfter);
            if (compListAfter.Count > 0)
            {
                UCMDebug.LogError("状态机After组件生成可能触发了死循环");
            }
        }

        private void RemoveCompFromList(List<StateComp> compList)
        {
            StateComp comp;
            for (int attempt = 0; attempt < maxAttempts; attempt++)
            {
                for (int i = compList.Count - 1; i >= 0; i--)
                {
                    comp = compList[i];
                    compList.RemoveAt(i);
                    compDict.Remove(comp.compType);
                    Mgr.RPool.Release(comp);
                }

                if (compList.Count == 0) break;
            }
        }

        private T Add<T>(string name) where T : class, IStateMachine, new()
        {
            var hbsm = GetHBSM(name);
            if (hbsm != null) return (T)hbsm;
            var hfsm = Mgr.RPool.Load<T>();
            hfsm.Init(name, this, null);
            hfsmDict.Add(name, hfsm);
            hfsmList.Add(hfsm);
            return hfsm;
        }

        void IReference.ObjRelease()
        {
            ReleaseAllHBSM();
        }
    }
}