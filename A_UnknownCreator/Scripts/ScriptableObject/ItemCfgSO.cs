using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UnknownCreator.Modules
{
    public class ItemCfgSO : ScriptableObject
    {
        public Texture2D icon { private set; get; }

#if UNITY_EDITOR
        public GameObject model { get; private set; }

        private void ChangeName()
        {
            if (model != null)
                defaultModel = model.name;
        }
#endif

        public string defaultModel { get; private set; }

        public string slotName { get; private set; }

        public int itemType { get; private set; }

        public int requiredCapacity { get; private set; }

        public bool isStackable { get; private set; }

        public int stack { get; private set; } = 1;

        public int maxStack { get; private set; } = 1;

        public bool isStorable { get; private set; }

        public int storageCapacity { get; private set; }

        public ItemEffectTrigger triggerType { get; private set; }

        // public string[] slotArr { get; private set; } = Array.Empty<string>();

        internal List<ItemAbilityInfo> pressedList = new();

        internal List<ItemAbilityInfo> releasedList = new();

        internal StatsGroupCfgSO[] itemStats = Array.Empty<StatsGroupCfgSO>();

        //internal Dictionary<string, ItemData> dataDict = new();

#if UNITY_EDITOR



#endif
    }

    public struct ItemAbilityInfo
    {
        public string hbsmName;
        public string abilityName;
    }
}
