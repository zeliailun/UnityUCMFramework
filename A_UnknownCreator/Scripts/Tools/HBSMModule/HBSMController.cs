using System;
using System.Collections.Generic;
namespace UnknownCreator.Modules
{
    [Serializable]
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

        private int iterateDepth;
        private bool isIterating => iterateDepth > 0;

        private bool needCompactHfsm;
        private bool needCompactBeforeComp;
        private bool needCompactAfterComp;

        private readonly List<IStateMachine> pendingAddHfsmList = new();
        private readonly List<IStateMachine> pendingReleaseHfsmList = new();

        private readonly List<StateComp> pendingAddBeforeCompList = new();
        private readonly List<StateComp> pendingAddAfterCompList = new();
        private readonly List<StateComp> pendingReleaseCompList = new();

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
            BeginIterate();
            try
            {
                StateComp compB;
                for (int i = 0; i < compListBefore.Count; i++)
                {
                    compB = compListBefore[i];
                    if (compB != null && compB.enable)
                        compB.UpdateComp();
                }

                IStateMachine hfsm;
                for (int i = 0; i < hfsmList.Count; i++)
                {
                    hfsm = hfsmList[i];
                    hfsm?.Update();
                }

                StateComp compA;
                for (int i = 0; i < compListAfter.Count; i++)
                {
                    compA = compListAfter[i];
                    if (compA != null && compA.enable)
                        compA.UpdateComp();
                }
            }
            finally
            {
                EndIterate();
            }
        }

        public void FixedUpdateAllHBSM()
        {
            BeginIterate();
            try
            {
                StateComp compB;
                for (int i = 0; i < compListBefore.Count; i++)
                {
                    compB = compListBefore[i];
                    if (compB != null && compB.enable)
                        compB.FixedUpdateComp();
                }

                IStateMachine hfsm;
                for (int i = 0; i < hfsmList.Count; i++)
                {
                    hfsm = hfsmList[i];
                    hfsm?.FixedUpdate();
                }

                StateComp compA;
                for (int i = 0; i < compListAfter.Count; i++)
                {
                    compA = compListAfter[i];
                    if (compA != null && compA.enable)
                        compA.FixedUpdateComp();
                }
            }
            finally
            {
                EndIterate();
            }
        }

        public void LateUpdateAllHBSM()
        {
            BeginIterate();
            try
            {
                StateComp compB;
                for (int i = 0; i < compListBefore.Count; i++)
                {
                    compB = compListBefore[i];
                    if (compB != null && compB.enable)
                        compB.LateUpdateComp();
                }

                IStateMachine hfsm;
                for (int i = 0; i < hfsmList.Count; i++)
                {
                    hfsm = hfsmList[i];
                    hfsm?.LateUpdate();
                }

                StateComp compA;
                for (int i = 0; i < compListAfter.Count; i++)
                {
                    compA = compListAfter[i];
                    if (compA != null && compA.enable)
                        compA.LateUpdateComp();
                }
            }
            finally
            {
                EndIterate();
            }
        }

        public void EnableAllHBSM()
        {
            BeginIterate();
            try
            {
                StateComp compB;
                for (int i = 0; i < compListBefore.Count; i++)
                {
                    compB = compListBefore[i];
                    if (compB != null)
                        compB.enable |= !compB.IsSkipGlobalEnable();
                }

                IStateMachine hfsm;
                for (int i = 0; i < hfsmList.Count; i++)
                {
                    hfsm = hfsmList[i];
                    hfsm?.Enter();
                }

                StateComp compA;
                for (int i = 0; i < compListAfter.Count; i++)
                {
                    compA = compListAfter[i];
                    if (compA != null)
                        compA.enable |= !compA.IsSkipGlobalEnable();
                }
            }
            finally
            {
                EndIterate();
            }
        }

        public void DisableAllHBSM()
        {
            BeginIterate();
            try
            {
                StateComp compB;
                for (int i = 0; i < compListBefore.Count; i++)
                {
                    compB = compListBefore[i];
                    if (compB != null)
                        compB.enable &= compB.IsSkipGlobalDisable();
                }

                IStateMachine hfsm;
                for (int i = 0; i < hfsmList.Count; i++)
                {
                    hfsm = hfsmList[i];
                    hfsm?.Exit();
                }

                StateComp compA;
                for (int i = 0; i < compListAfter.Count; i++)
                {
                    compA = compListAfter[i];
                    if (compA != null)
                        compA.enable &= compA.IsSkipGlobalDisable();
                }
            }
            finally
            {
                EndIterate();
            }
        }

        public void RefreshAllHBSM()
        {
            BeginIterate();
            try
            {
                for (int i = 0; i < compListBefore.Count; i++)
                    compListBefore[i]?.RefreshComp();

                for (int i = 0; i < hfsmList.Count; i++)
                    hfsmList[i]?.Refresh();

                for (int i = 0; i < compListAfter.Count; i++)
                    compListAfter[i]?.RefreshComp();
            }
            finally
            {
                EndIterate();
            }
        }

        public void ReleaseAllHBSM()
        {
            iterateDepth = 0;

            RemoveBeforeComp();
            RemoveAllHBSM();
            RemoveAfterComp();

            ClearPendingData();

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
        {
            if (string.IsNullOrWhiteSpace(name)) return null;
            return hfsmDict.TryGetValue(name, out var hfsm) ? hfsm : null;
        }

        public void RemoveHBSM(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return;

            if (!hfsmDict.Remove(name, out var result)) return;

            if (TryRemovePendingAdd(pendingAddHfsmList, result))
            {
                Mgr.RPool.Release(result);
                return;
            }

            if (isIterating)
            {
                MarkNull(hfsmList, result);
                pendingReleaseHfsmList.Add(result);
                needCompactHfsm = true;
            }
            else
            {
                hfsmList.Remove(result);
                Mgr.RPool.Release(result);
            }
        }

        public void RemoveAllHBSM()
        {
            hfsmDict.Clear();

            for (int i = pendingAddHfsmList.Count - 1; i >= 0; i--)
                Mgr.RPool.Release(pendingAddHfsmList[i]);

            pendingAddHfsmList.Clear();

            if (isIterating)
            {
                for (int i = 0; i < hfsmList.Count; i++)
                {
                    if (hfsmList[i] == null) continue;

                    pendingReleaseHfsmList.Add(hfsmList[i]);
                    hfsmList[i] = null;
                }

                needCompactHfsm = true;
            }
            else
            {
                for (int i = hfsmList.Count - 1; i >= 0; i--)
                    Mgr.RPool.Release(hfsmList[i]);

                hfsmList.Clear();
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

            if (compDict.TryGetValue(type, out var result))
                return (T)result;

            var comp = Mgr.RPool.Load<T>();
            comp.Init(this, type);

            compDict.Add(type, comp);

            if (isIterating)
            {
                if (isBefore)
                    pendingAddBeforeCompList.Add(comp);
                else
                    pendingAddAfterCompList.Add(comp);
            }
            else
            {
                if (isBefore)
                    compListBefore.Add(comp);
                else
                    compListAfter.Add(comp);
            }

            return comp;
        }

        public StateComp AddComp(string comp, bool isBefore)
        {
            if (string.IsNullOrWhiteSpace(comp))
                return null;

            var type = Type.GetType(comp);

            if (type == null || !typeof(StateComp).IsAssignableFrom(type))
            {
                UCMDebug.LogError($"无效 StateComp 类型：{comp}");
                return null;
            }

            if (compDict.TryGetValue(type, out var result))
                return result;

            var sc = (StateComp)Mgr.RPool.Load(type);
            sc.Init(this, type);

            compDict.Add(type, sc);

            if (isIterating)
            {
                if (isBefore)
                    pendingAddBeforeCompList.Add(sc);
                else
                    pendingAddAfterCompList.Add(sc);
            }
            else
            {
                if (isBefore)
                    compListBefore.Add(sc);
                else
                    compListAfter.Add(sc);
            }

            return sc;
        }

        public T GetComp<T>() where T : StateComp, new()
        => compDict.TryGetValue(typeof(T), out var result) ? (T)result : null;

        public bool RemoveComp<T>() where T : StateComp, new()
        => RemoveComp(typeof(T));


        public bool RemoveComp(Type type)
        {
            if (type == null) return false;

            if (!compDict.Remove(type, out var result))
                return false;

            if (TryRemovePendingAdd(pendingAddBeforeCompList, result) ||
                TryRemovePendingAdd(pendingAddAfterCompList, result))
            {
                Mgr.RPool.Release(result);
                return true;
            }

            if (isIterating)
            {
                if (MarkNull(compListBefore, result))
                    needCompactBeforeComp = true;

                if (MarkNull(compListAfter, result))
                    needCompactAfterComp = true;

                pendingReleaseCompList.Add(result);
            }
            else
            {
                compListBefore.Remove(result);
                compListAfter.Remove(result);
                Mgr.RPool.Release(result);
            }

            return true;
        }

        public void RemoveBeforeComp()
        {
            RemoveCompFromList(compListBefore, pendingAddBeforeCompList, ref needCompactBeforeComp);

            if (!isIterating && compListBefore.Count > 0)
                UCMDebug.LogError("状态机Before组件生成可能触发了死循环");
        }

        public void RemoveAfterComp()
        {
            RemoveCompFromList(compListAfter, pendingAddAfterCompList, ref needCompactAfterComp);

            if (!isIterating && compListAfter.Count > 0)
                UCMDebug.LogError("状态机After组件生成可能触发了死循环");
        }

        private void RemoveCompFromList(
            List<StateComp> compList,
            List<StateComp> pendingAddList,
            ref bool needCompact)
        {
            for (int i = pendingAddList.Count - 1; i >= 0; i--)
            {
                var pendingComp = pendingAddList[i];

                if (pendingComp != null)
                {
                    compDict.Remove(pendingComp.compType);
                    Mgr.RPool.Release(pendingComp);
                }
            }

            pendingAddList.Clear();

            if (isIterating)
            {
                for (int i = 0; i < compList.Count; i++)
                {
                    var comp = compList[i];
                    if (comp == null) continue;

                    compDict.Remove(comp.compType);
                    pendingReleaseCompList.Add(comp);
                    compList[i] = null;
                }

                needCompact = true;
                return;
            }

            StateComp sc;

            for (int attempt = 0; attempt < maxAttempts; attempt++)
            {
                for (int i = compList.Count - 1; i >= 0; i--)
                {
                    sc = compList[i];
                    compList.RemoveAt(i);

                    if (sc == null) continue;

                    compDict.Remove(sc.compType);
                    Mgr.RPool.Release(sc);
                }

                if (compList.Count == 0)
                    break;
            }
        }

        private T Add<T>(string name) where T : class, IStateMachine, new()
        {
            if (string.IsNullOrWhiteSpace(name))
                return null;

            var hbsm = GetHBSM(name);
            if (hbsm != null)
            {
                if (hbsm is T typed)
                    return typed;

                UCMDebug.LogError($"已存在同名状态机，但类型不匹配：{name}");
                return null;
            }

            var hfsm = Mgr.RPool.Load<T>();
            hfsm.Init(name, this, null);

            hfsmDict.Add(name, hfsm);

            if (isIterating)
                pendingAddHfsmList.Add(hfsm);
            else
                hfsmList.Add(hfsm);

            return hfsm;
        }

        private void BeginIterate()
        {
            iterateDepth++;
        }

        private void EndIterate()
        {
            iterateDepth--;

            if (iterateDepth <= 0)
            {
                iterateDepth = 0;
                FlushPendingChanges();
            }
        }

        private void FlushPendingChanges()
        {
            if (needCompactHfsm)
            {
                CompactNulls(hfsmList);
                needCompactHfsm = false;
            }

            if (needCompactBeforeComp)
            {
                CompactNulls(compListBefore);
                needCompactBeforeComp = false;
            }

            if (needCompactAfterComp)
            {
                CompactNulls(compListAfter);
                needCompactAfterComp = false;
            }

            if (pendingAddHfsmList.Count > 0)
            {
                hfsmList.AddRange(pendingAddHfsmList);
                pendingAddHfsmList.Clear();
            }

            if (pendingAddBeforeCompList.Count > 0)
            {
                compListBefore.AddRange(pendingAddBeforeCompList);
                pendingAddBeforeCompList.Clear();
            }

            if (pendingAddAfterCompList.Count > 0)
            {
                compListAfter.AddRange(pendingAddAfterCompList);
                pendingAddAfterCompList.Clear();
            }

            for (int i = pendingReleaseHfsmList.Count - 1; i >= 0; i--)
                Mgr.RPool.Release(pendingReleaseHfsmList[i]);

            pendingReleaseHfsmList.Clear();

            for (int i = pendingReleaseCompList.Count - 1; i >= 0; i--)
                Mgr.RPool.Release(pendingReleaseCompList[i]);

            pendingReleaseCompList.Clear();
        }

        private static bool MarkNull<T>(List<T> list, T value) where T : class
        {
            if (value == null) return false;

            bool found = false;

            for (int i = 0; i < list.Count; i++)
            {
                if (!ReferenceEquals(list[i], value)) continue;

                list[i] = null;
                found = true;
            }

            return found;
        }

        private static bool TryRemovePendingAdd<T>(List<T> list, T value) where T : class
        {
            if (value == null) return false;

            for (int i = list.Count - 1; i >= 0; i--)
            {
                if (!ReferenceEquals(list[i], value)) continue;

                list.RemoveAt(i);
                return true;
            }

            return false;
        }

        private static void CompactNulls<T>(List<T> list) where T : class
        {
            int writeIndex = 0;

            for (int readIndex = 0; readIndex < list.Count; readIndex++)
            {
                var item = list[readIndex];

                if (item == null) continue;

                if (writeIndex != readIndex)
                    list[writeIndex] = item;

                writeIndex++;
            }

            if (writeIndex < list.Count)
                list.RemoveRange(writeIndex, list.Count - writeIndex);
        }



        private void ClearPendingData()
        {
            needCompactHfsm = false;
            needCompactBeforeComp = false;
            needCompactAfterComp = false;

            pendingAddHfsmList.Clear();
            pendingReleaseHfsmList.Clear();

            pendingAddBeforeCompList.Clear();
            pendingAddAfterCompList.Clear();
            pendingReleaseCompList.Clear();
        }


        void IReference.ObjRelease()
        {
            ReleaseAllHBSM();
        }
    }
}