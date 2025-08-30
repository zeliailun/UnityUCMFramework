using System;
using System.Collections;
using System.Collections.Generic;
using System.Xml.Linq;
using UnityEngine;

namespace UnknownCreator.Modules
{
    public abstract partial class ItemBase : IReference
    {
        /*
        private IHBSMController hbsmItem = new HBSMController();

        private Dictionary<string, ItemBase> itemDict = new();

        public ItemCfgSO itemData { get; private set; }

        public GameObject model { get; private set; }

        public Transform modelT { get; private set; }

        public Unit owner { get; private set; }

        public string itemName { get; private set; }

        public ItemOwnerShip ownerShip { get; internal set; }

        public bool isAddAbiliyAndStats { get; private set; }

        public int handIndex { get; private set; }

        public int itemType { get; private set; }

        public int stack
        {
            get => stk;
            set
            {
               if (!stackable.isEnough)
                {
                    stk = 1;
                    return;
                }  
                stk = Math.Clamp(value, 0, maxStack + 1);
            }
        }

        public int maxStack
        {
            get => maxStk;
            set
            {
                if (!stackable.isEnough)
                {
                    maxStk = 1;
                    return;
                } 
                maxStk = value < 1 ? 1 : value;
                if (maxStk < stk) stk = maxStk;
            }
        }

        public int storageCapacity { get; set; }


       // public StateVariable stackable { get; set; }

       // public StateVariable storable { get; set; }

        public Texture2D icon { get; set; }

        public bool hasItem => itemDict.Count > 0;

        private List<ItemAbilityInfo> pressedList;
        private List<ItemAbilityInfo> releasedList;
        private Dictionary<string, StatsData> statsKV = new();
        private int stk;
        private int maxStk;

        internal void InitItem(string itemName)
        {
            this.itemName = itemName;
            itemData = UnityUtils.LoadSync<ItemCfgSO>(itemName);
            model = Mgr.GPool.Load(itemData.defaultModel, false, false);
            modelT = model.GetComp<Transform>();
            icon = itemData.icon;
            itemType = itemData.itemType;
            stack = itemData.stack;
            maxStack = itemData.maxStack;
            storageCapacity = itemData.storageCapacity;
          //  if (itemData.isStorable) storable.ModifyValue(1);
          //  if (itemData.isStackable) stackable.ModifyValue(1);
            isAddAbiliyAndStats = false;
            handIndex = -1;
            ownerShip = ItemOwnerShip.None;
            pressedList= itemData.pressedList.CopyToNewList();
            releasedList= itemData.releasedList.CopyToNewList();
            foreach (var result in itemData.pressedList)
                hbsmItem.AddHBSM(result.hbsmName).AddState(result.abilityName);
            foreach (var result in itemData.releasedList)
                hbsmItem.AddHBSM(result.hbsmName).AddState(result.abilityName);
            hbsmItem.EnableAllHBSM();
            foreach (var slotName in itemData.slotArr)
            {
                AddSlot(slotName);
            } 
            OnInitialized();
        }

        internal void UpdateItem()
        {
            hbsmItem?.UpdateAllHBSM();
        }

        void IReference.Release()
        {
            OnRelease();
            RemoveStats();
            hbsmItem.RemoveAllHBSM();
            pressedList?.Clear();
            releasedList?.Clear();
         //   storable.Clear();
     //       stackable.Clear();
            ownerShip = ItemOwnerShip.None;
            Mgr.GPool.Release(itemData.defaultModel,model);
            model = null;
            modelT = null;
            owner =null;
            UnityUtils.Release(itemData);
        }

        /// <summary>
        /// 拿到身上
        /// </summary>
        internal void EquipItemToBody(Unit owner)
        {
            this.owner = owner;
            if (itemData.triggerType != ItemEffectTrigger.OnlyHand)
                AddStats();
            else
                RemoveStats();
            modelT.SetParent(owner.GetSlotTransform(itemData.slotName));
            model.SetActive(true);
            ownerShip = ItemOwnerShip.Body;
            OnEquipToBody();
        }

        /// <summary>
        /// 拿到手上
        /// </summary>
        internal void EquipItemToHand(Unit owner, int handIndex)
        {
            this.owner = owner;
            if (itemData.triggerType != ItemEffectTrigger.OnlyBody)
                AddStats();
            else
                RemoveStats();
            this.handIndex = handIndex;
            modelT.SetParent(owner.GetHandTransform(handIndex));
            model.SetActive(true);
            ownerShip = ItemOwnerShip.Hand; 
            OnEquipToHand();
        }

        /// <summary>
        /// 收回到储物空间
        /// </summary>
        internal bool StorageItem(ItemBase item)
        {
            if (item is null || !CanStored(item.itemData.requiredCapacity)) return false;
            RemoveStats();
            model.SetActive(false);
            modelT.SetParent(owner.entT);
            ownerShip = ItemOwnerShip.Storeroom;
            OnStoraged();
            return true;
        }

        /// <summary>
        /// 扔到地上
        /// </summary>
        internal void DropItem()
        {
            OnDrop();
            RemoveStats();
            owner = null;
            ownerShip = ItemOwnerShip.None;
        }

        internal void ItemPressed(int index)
        {
            if (!HasItemAbility(index, true)) return;
            var info = itemData.pressedList[index];
            hbsmItem.GetHBSM(info.hbsmName)?.ChangeState(info.abilityName);
        }

        internal void ItemReleased(int index)
        {
            if (!HasItemAbility(index, false)) return;
            var info = itemData.releasedList[index];
            hbsmItem.GetHBSM(info.hbsmName)?.ChangeState(info.abilityName);
        }

        private void AddStats()
        {
            if (!isAddAbiliyAndStats)
            {
                isAddAbiliyAndStats = true;
                if (statsKV.Count < 1)
                {
                    foreach (var group in itemData.itemStats)
                    {
                        foreach (var value in group.cfg)
                        {
                            if (!statsKV.TryGetValue(value.baseCfg.cfg.idName, out _))
                                statsKV.Add(value.baseCfg.cfg.idName, owner.statsC.AddStats(value.baseCfg.cfg,0,this));
                        }
                    }
                }
            }
        }

        private void RemoveStats()
        {
            if (isAddAbiliyAndStats)
            {
                isAddAbiliyAndStats = false;  
                if (statsKV.Count > 0)
                {
                    foreach (var result in statsKV.Values)
                        owner.statsC.RemoveStats(result);
                    statsKV.Clear();
                }
            }
        }
          */
    }
}