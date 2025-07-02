using System.Collections.Generic;
using System;
using Unity.Mathematics;
using UnityEngine;
using System.Security.Cryptography;

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
            get => dur;
            set
            {
                var old = dur;
                dur = math.clamp(value, 0, math.INFINITY);
                OnDurationChanged(dur, old);
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
        => !isRelease &&
            !isPassive &&
            (duration <= 0 ||
            Mgr.RPool.HasObject(inflicterType, inflicter) ||
            (owner != null && owner.HasAlive() && !owner.isAlive && IsDeathRemove()));

        private bool isUpdateTimer
        => isEnableTimer && timer >= delay;

        private HashSet<(string, CalcType)> statsSet = new();
        private List<(string name, CalcType type, Func<double> callback)> statsList = new();
        private Dictionary<(string, int), double> statsDict = new();


        private HashSet<int> stateSet = new();
        private List<(int id, Func<bool> callback)> stateList = new();
        private Dictionary<int, bool> stateDict = new();


        private Dictionary<(string name, int id), Delegate> evtDict = new();
        private Action clearEvt;

        private double dur;
        private int stack;
        private bool passiveBuff;
        private Type inflicterType;
        private double timer, delay;
        private bool isEnableTimer;

        //==============================================================================================================


        internal void InitBuff(string buffName, AbilityBase ability, Unit owner, Unit inflicter, double dur, IVariableMgr kv, bool isKVRecyclePool)
        {
            this.buffName = buffName;
            this.ability = ability;
            this.owner = owner;
            this.inflicter = inflicter;
            this.kv = kv;
            this.isKVRecyclePool = isKVRecyclePool;
            inflicterType = typeof(Unit);
            stackCount = 0;
            origDuration = duration = dur;
            isInterruptMotion = true;
            isRelease = false;
            OnInitialized();
        }

        internal void UpdateBuff()
        {
            if (isRelease) return;

            float dt = CustomTime.DeltaTime();
            duration -= dt;
            timer += dt;

            UpdateStats();

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

            if (shouldRemoveBuff)
            {
                StopTimer();
                duration = 0;
                owner.buffC.RemoveBuff(this);
            }
        }

        internal void UpdateDuration(double dur)
        {
            origDuration = duration = dur;
        }

        private void UpdateStats()
        {
            if (isRelease || statsList is null || statsList.Count == 0) return;

            string name;
            CalcType type;
            double value;
            (string name, int type) key;
            for (int i = 0; i < statsList.Count; i++)
            {
                name = statsList[i].name;
                type = statsList[i].type;
                value = statsList[i].callback();
                key = (name, (int)type);
                if (!statsDict.TryGetValue(key, out var result) ||
                    result != value)
                    statsDict[key] = value;

                owner.statsC.UpdateStats(this, name, type, value, (IsStacked() && IsStatsStacked()));
            }
        }

        private void UpdateState()
        {
            if (isRelease || stateList is null || stateList.Count == 0) return;

            int id;
            bool value;
            for (int i = 0; i < stateList.Count; i++)
            {
                id = stateList[i].id;
                value = stateList[i].callback();
                if (!stateDict.TryGetValue(id, out var result) ||
                    result != value)
                {
                    stateDict[id] = value;
                    owner.stateC.UpdateState(id, value ? 1 : -1);
                }
            }
        }

        private void RemoveEvnets()
        {
            evtDict.Clear();
            clearEvt?.Invoke();
        }

        private bool MotionCheck(BuffBase newBuff)
        {
            if (newBuff.GetMotionPriority() >= this.GetMotionPriority())
            {
                this.RemoveMotionController();
                return false;
            }
            return true;
        }

        void IReference.ObjRelease()
        {
            if (isRelease) return;

            isRelease = true;
            isInterruptMotion = true;
            OnRelease();
            StopTimer();
            RemoveEvnets();
            RemoveMotionController();
            ClearStats();
            ClearState();
            stackCount = 0;
            if (isKVRecyclePool)
                Mgr.RPool.Release(kv);
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