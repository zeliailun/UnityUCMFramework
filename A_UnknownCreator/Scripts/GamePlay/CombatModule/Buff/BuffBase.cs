using System;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

namespace UnknownCreator.Modules
{

    public abstract partial class BuffBase : IReference
    {
        public IVariableMgr kv { get; private set; }
        public AbilityBase ability { get; private set; }
        public Unit owner { get; private set; }
        public Unit inflicter { get; private set; }
        public string buffName { get; private set; }
        public double origDuration { get; private set; }
        public double duration
        {
            get => _dur;
            set
            {
                var old = _dur;
                _dur = _dur = math.max(0d, value);
                OnDurationChanged(_dur, old);
            }
        }

        public int stackCount
        {
            get => stack;
            set
            {
                var old = stack;
                stack = (int)math.clamp(value, 0, math.INFINITY);
                OnStackCountChanged(stack, old);
            }
        }

        public bool isPassive
        {
            get => passiveBuff && ability != null;
            internal set => passiveBuff = value;
        }

        public bool isKVRecyclePool { get; set; }

        public bool isInterruptMotion { get; private set; }

        public bool isRelease { get; private set; }

        private bool shouldRemoveBuff
        => !isRelease && !isPassive &&
            (duration <= 0 || Mgr.RPool.HasObject(inflicterType, inflicter) || (owner.HasAlive() && !owner.isAlive && IsDeathRemove()));

        private bool isUpdateTimer
        => isEnableTimer && timer >= delay;

        private HashSet<(string, CalcType)> statsSet = new();
        private List<(string name, CalcType type, Func<double> callback)> statsList = new();
        private Dictionary<(string, int), double> statsDict = new();


        private HashSet<int> stateSet = new();
        private List<(int id, Func<bool> callback)> stateList = new();
        private Dictionary<int, bool> stateDict = new();


        private Dictionary<(string name, EntityId id), Delegate> evtDict = new();
        private Action clearEvt;

        private readonly Dictionary<BusEventKey, EventHandle> busEvtDict = new();

        private double _dur;
        private int stack;
        private bool passiveBuff;
        private Type inflicterType;
        private double timer, delay;
        private bool isEnableTimer;

        //==============================================================================================================


        internal void InitBuff(string buffName, AbilityBase ability, Unit owner, Unit inflicter, double newDuration, IVariableMgr kv, bool isKVRecyclePool)
        {
            this.buffName = buffName;
            this.ability = ability;
            this.owner = owner;
            this.inflicter = inflicter;
            this.kv = kv;
            this.isKVRecyclePool = isKVRecyclePool;
            inflicterType = typeof(Unit);
            origDuration = duration = newDuration;
            timer = 0;
            stack = 0;
            isEnableTimer = false;
            isInterruptMotion = true;
            isRelease = false;
            OnInitialized();
        }

        internal void RefreshBuff(IVariableMgr kv, bool isKVRecyclePool, double newDuration)
        {
            if (this.isKVRecyclePool) Mgr.RPool.Release(this.kv);
            this.kv = kv;
            this.isKVRecyclePool = isKVRecyclePool;
            origDuration = newDuration;
            this.duration = newDuration;
            timer = 0;
            OnRefresh();
        }

        internal void UpdateBuff()
        {
            if (isRelease) return;

            float dt = CustomTime.DeltaTime();

            if (!isPassive && duration > 0)
                duration -= dt;

            timer += dt;

            UpdateStats(false);

            UpdateState();

            if (isRelease) return;

            if (!isInterruptMotion)
                OnUpdateMotionController();

            if (isRelease) return;

            if (isUpdateTimer)
            {
                timer = 0;
                OnIntervalThink();
            }

            if (isRelease) return;

            OnUpdate();

            if (isRelease) return;


            if (shouldRemoveBuff)
            {
                StopThink();
                duration = 0;
                OnDurationEnd();

                if (isRelease) return;

                owner.buffC.RemoveBuff(this);
            }
        }


        internal void ForceUpdateStats()
        {
            UpdateStats(true);
        }

        internal void ForceUpdateStats(string statName)
        {
            if (string.IsNullOrWhiteSpace(statName))
                return;

            UpdateStats(true, statName);
        }

        private void UpdateStats(bool force, string targetStatName = null)
        {
            if (isRelease || statsList is null || statsList.Count == 0)
                return;

            string name;
            CalcType type;
            double value;
            (string name, int type) key;

            bool isStacked = IsStacked() && IsStatsStacked();

            for (int i = 0; i < statsList.Count; i++)
            {
                name = statsList[i].name;

                if (targetStatName != null && name != targetStatName)
                    continue;

                type = statsList[i].type;
                value = statsList[i].callback();
                key = (name, (int)type);

                if (!force &&
                   statsDict.TryGetValue(key, out var oldValue) &&
                    Math.Abs(oldValue - value) < 0.0001)
                {
                    continue;
                }

                statsDict[key] = value;
                owner.statsC.UpdateStats(this, name, type, value, isStacked);
            }
        }

        private void UpdateState()
        {
            if (isRelease || stateList is null || stateList.Count == 0) return;

            for (int i = 0; i < stateList.Count; i++)
            {
                int id = stateList[i].id;
                bool newValue = stateList[i].callback();

                if (!stateDict.TryGetValue(id, out bool oldValue))
                {
                    stateDict[id] = newValue;

                    if (newValue)
                        owner.stateC.UpdateState(id, 1);

                    continue;
                }

                if (oldValue == newValue)
                    continue;

                stateDict[id] = newValue;
                owner.stateC.UpdateState(id, newValue ? 1 : -1);
            }
        }

        void IReference.ObjRelease()
        {
            if (isRelease) return;

            isRelease = true;
            OnRelease();
            StopThink();
            RemoveMotionController();
            ClearAllEvent();
            ClearStats();
            ClearState();
            if (isKVRecyclePool) Mgr.RPool.Release(kv);
            kv = null;
            owner = null;
            inflicter = null;
            ability = null;
            clearEvt = null;
            inflicterType = null;
        }

        void IReference.ObjDestroy()
        {
            OnDestroy();
        }
    }
}
