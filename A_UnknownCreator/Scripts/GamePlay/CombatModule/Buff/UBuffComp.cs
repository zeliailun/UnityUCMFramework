using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

namespace UnknownCreator.Modules
{
    public sealed class UBuffComp : StateComp
    {
        private Dictionary<string, List<BuffBase>> buffDict = new();

        private List<BuffBase> buffList = new();

        private Unit self;

        private int maxAttempts = 3;

        public override void InitComp()
        {
            self = kv.GetValue<Unit>();
        }

        public override void UpdateComp()
        {
            BuffBase buff;
            for (int i = 0; i < buffList.Count;)
            {
                buff = buffList[i];
                buff?.UpdateBuff();

                // 如果当前格子仍然是刚才这个 buff，说明它没被移除，正常前进
                if (i < buffList.Count && ReferenceEquals(buffList[i], buff))
                    i++;
                // 否则说明列表发生了删除/移动，不 i++，继续处理当前下标的新元素
            }
        }

        public override void ReleaseComp()
        {
            ClearBuff();
            self = null;
        }

        public BuffBase AddBuff(string buffName, AbilityBase ability, Unit inflicter, double duration, IVariableMgr kv = null, bool isKVRecyclePool = false)
        {
            if (duration < 0) return null;

            BuffBase newBuff = null;
            if (!buffDict.TryGetValue(buffName, out var list))
            {
                var pool = ListPool<BuffBase>.Get();
                newBuff = CreateBuff(buffName, ability, inflicter, duration, kv, isKVRecyclePool);
                pool.Add(newBuff);
                buffDict.Add(buffName, pool);
                buffList.Add(newBuff);
                newBuff.OnCreated();
                if (!newBuff.isRelease)
                {
                    newBuff.UpdateBuff();

                    if (!newBuff.isRelease)
                        GameEvtBus.Send<EvtBuffAdded>(new(newBuff, self));
                }
                return newBuff;
            }
            if (list.Count > 0)
            {
                newBuff = list[0];
                if (newBuff.IsStacked())
                {
                    newBuff = CreateBuff(buffName, ability, inflicter, duration, kv, isKVRecyclePool);
                    list.Add(newBuff);
                    buffList.Add(newBuff);
                    newBuff.OnCreated();
                    if (!newBuff.isRelease)
                    {
                        newBuff.UpdateBuff();

                        if (!newBuff.isRelease)
                            GameEvtBus.Send<EvtBuffAdded>(new(newBuff, self));
                    }
                    return newBuff;
                }

                newBuff.RefreshBuff(kv, isKVRecyclePool, duration);

            }
            return newBuff;
        }


        public BuffBase AddPermanentBuff(string buffName, AbilityBase ability, Unit inflicter, IVariableMgr kv = null, bool isKVRecyclePool = false)
        {
            return AddBuff(buffName, ability, inflicter, MathGlobals.PInfinity, kv, isKVRecyclePool);
        }


        public void RemoveBuff<T>(T buff) where T : BuffBase
        {
            if (buff is null ||
                buff.isRelease ||
                !buffDict.TryGetValue(buff.buffName, out var list) ||
                !list.IsValid() ||
                !list.Contains(buff)) return;

            RemoveBuff(buff, ref list);
        }

        public void RemoveBuff(string buffName)
        {
            if (string.IsNullOrWhiteSpace(buffName) ||
                !buffDict.TryGetValue(buffName, out var list) ||
                !list.IsValid()) return;

            var buff = list[^1];
            if (buff is null ||
                buff.isRelease ||
                !list.Contains(buff)) return;

            RemoveBuff(buff, ref list);
        }

        public void RemoveSameBuff(string buffName)
        {
            if (!buffDict.TryGetValue(buffName, out var list) ||
                list is null) return;

            BuffBase buff;
            for (int i = list.Count - 1; i >= 0; i--)
            {
                buff = list[i];

                if (buff == null)
                {
                    list.RemoveAt(i);
                    continue;
                }

                GameEvtBus.Send<EvtBuffWillRemove>(new(buff, self));

                if (buff.isRelease) continue;

                buff.OnRemove(false);
                list.RemoveAt(i);
                buffList.Remove(buff);
                Mgr.RPool.Release(buff);
            }
            buffDict.Remove(buffName);
            ListPool<BuffBase>.Release(list);
        }

        public void RemoveBuffByCondition(Func<BuffBase, bool> removeCondition)
        {
            BuffBase newBuff;
            for (int i = buffList.Count - 1; i >= 0; i--)
            {
                newBuff = buffList[i];
                if (newBuff is null || (removeCondition != null && !removeCondition(newBuff))) continue;
                RemoveBuff(newBuff);
            }
        }

        public void PurgableDebuff()
        => RemoveBuffByCondition((buff) => buff.IsDebuff() && buff.IsPurgable());

        public void ClearBuff()
        {
            int attemptCount = 0;
            BuffBase newBuff;
            while (buffList.Count > 0)
            {
                ReleaseBuffList();

                attemptCount++;

                for (int i = buffList.Count - 1; i >= 0; i--)
                {
                    newBuff = buffList[i];
                    if (newBuff is null) continue;
                    newBuff.OnRemove(true);
                    buffList.RemoveAt(i);
                    Mgr.RPool.Release(newBuff);
                }

                if (attemptCount > maxAttempts)
                {
                    UCMDebug.LogWarning("Buff生成可能触发了死循环");
                    break;
                }

            }

        }

        public bool HasBuff(string name)
        => buffDict.TryGetValue(name, out var list) && list?.Count > 0;

        public BuffBase GetBuff(string name)
        => buffDict.TryGetValue(name, out var list) ? list[0] : null;

        public List<BuffBase> GetSameBuff(string name)
        => buffDict.TryGetValue(name, out var list) ? list.CopyToNewList() : null;

        public List<BuffBase> GetAllBuff()
        => buffList;

        public BuffBase GetBuffByIndex(int id)
        => (id >= 0 && id < buffList.Count) ? buffList[id] : null;


        private BuffBase CreateBuff(string buffName, AbilityBase ability, Unit inflicter, double duration, IVariableMgr kv, bool isKVRecyclePool)
        {
            var newBuff = (BuffBase)Mgr.RPool.Load(Type.GetType(buffName));
            newBuff.InitBuff(buffName, ability, self, inflicter, duration, kv, isKVRecyclePool);
            return newBuff;
        }

        private void RemoveBuff(BuffBase buff, ref List<BuffBase> list)
        {
            string buffName = buff.buffName;

            GameEvtBus.Send<EvtBuffWillRemove>(new(buff, self));

            if (buff.isRelease) return;

            buff.OnRemove(false);
            list.Remove(buff);
            buffList.Remove(buff);
            if (list.Count < 1)
            {
                buffDict.Remove(buff.buffName);
                ListPool<BuffBase>.Release(list);
            }
            Mgr.RPool.Release(buff);
        }

        private void ReleaseBuffList()
        {
            foreach (var list in buffDict.Values)
            {
                if (list != null)
                {
                    list.Clear();
                    ListPool<BuffBase>.Release(list);
                }
            }

            buffDict.Clear();
        }
    }
}