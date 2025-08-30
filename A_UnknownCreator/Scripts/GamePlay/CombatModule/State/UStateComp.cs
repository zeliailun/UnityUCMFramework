using System.Collections.Generic;
using System;

namespace UnknownCreator.Modules
{
    public sealed class UStateComp : StateComp
    {
        private Dictionary<int, int> stateDict = new();

        private Unit self;

        public override void InitComp()
        {
            for (int i = 0; i < StateGlobals.StateCount; i++) AddState(i);
            for (int i = 0; i <= Mgr.Unit.unitStateCount; i++) AddState(100 + i);
            self = kv.GetValue<Unit>();
        }

        public override void ReleaseComp()
        {
            RemoveAllState();
            self = null;
        }

        public void AddState(int typeID)
        => stateDict.TryAdd(typeID, 0);

        public bool HasState(int typeID)
        => HasOrGetStateValue(typeID, out _);

        public bool BeState(int typeID)
        => HasOrGetStateValue(typeID, out var value) && value > 0;

        public bool BeStates(int[] typeID)
        {
            foreach (var item in typeID)
            {
                if (HasOrGetStateValue(item, out var value) && value > 0)
                    return true;
            }
            return false;
        }

        public void UpdateState(int typeID, int value)
        {
            if (!HasOrGetStateValue(typeID, out var stateValue) ||
                Math.Abs(value) != 1 ||
                (stateValue <= 0 && value == -1)) return;

            int newValue = stateValue + value;
            if (newValue != stateValue)
            {
                stateDict[typeID] = newValue;
                Mgr.Event.Send<EvtStateUpdate>(new(self, typeID, newValue > 0), CombatEvtGlobals.OnStateUpdated);
            }
        }

        public bool HasOrGetStateValue(int typeID, out int data)
        {

            if (stateDict.TryGetValue(typeID, out var result))
            {
                data = result;
                return true;
            }
            data = -1;
            return false;
        }

        public void RemoveAllState()
        {
            stateDict.Clear();
        }
    }
}