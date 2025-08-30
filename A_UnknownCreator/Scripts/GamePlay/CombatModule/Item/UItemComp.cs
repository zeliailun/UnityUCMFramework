using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UnknownCreator.Modules
{
    public class ItemSlot : IReference
    {
        public ItemBase slotItem;

        void IReference.ObjRelease()
        {
            if (slotItem is null) return;
            Mgr.RPool.Release(slotItem);
        }
    }

    public sealed class UItemComp : StateComp
    {
      /*  private Dictionary<string, ItemSlot> slotsDict = new();

        private Dictionary<int, ItemBase> handItemDict = new();

        private List<ItemBase> itemHotBar = new();

        private List<ItemBase> updateItem = new();

        public int hotBarCount => itemHotBar.Count;

        public int handItemCount => handItemDict.Count;

        public bool hasHandItem
        => handItemDict != null && handItemDict.Count > 0;

        private Unit self;

        public override void InitComp()
        {
            self = kv.GetValue<Unit>();
        }

        public override void ReleaseComp()
        {
            self = null;
        }

        public override void UpdateComp()
        {
            for (int i = updateItem.Count - 1; i >= 0; i--)
                updateItem[i]?.UpdateItem();
        }

        public void ItemPressed(int handIndex, int abilityIndex)
        {
            if(handItemDict.TryGetValue(handIndex,out var item))
                item.ItemPressed(abilityIndex);
        }

        public void ItemReleased(int handIndex, int abilityIndex)
        {
            if (handItemDict.TryGetValue(handIndex, out var item))
                item.ItemReleased(abilityIndex);
        }

        public void AddEquipSlots(string slotName)
        {
            if (!slotsDict.TryGetValue(slotName, out _))
            {
                slotsDict.Add(slotName, Mgr.RPool.Load<ItemSlot>());
            }
        }

        public void RemoveEquipSlots(string slotName)
        {
            if (slotsDict.TryGetValue(slotName, out var result))
            {
                if (result.slotItem is null) return;
                if (handItemDict.TryGetValue(result.slotItem.handIndex, out var result2))
                {
                    handItemDict.Remove(result2.handIndex);
                    itemHotBar.Remove(result2);
                    updateItem.Remove(result2);
                }
                slotsDict.Remove(slotName);
                Mgr.RPool.Release(result);
            }
        }

        public bool AddItem(ItemBase newItem)
        {
            if (newItem is null) return false;
            if (slotsDict.TryGetValue(newItem.itemData.slotName, out var result) &&
                result.slotItem is null)
            {
                handItemDict.Remove(newItem.handIndex);
                result.slotItem = newItem;
                if (!updateItem.Contains(newItem)) updateItem.Add(result.slotItem);
                result.slotItem.EquipItemToBody(self);
                result.slotItem?.UpdateItem();
                return true;
            }
            return StorageItem(newItem);
        }

        public bool ReplaceItemInHand(ItemBase newItem, int handIndex)
        {
            if (newItem is null || handIndex > self.handCount) return false;
            if (handItemDict.TryGetValue(handIndex, out var handItem))
            {
                if(handItem.Equals(newItem)) return false;

                //先尝试放到身上，没有就放到地上
                if (!AddItem(handItem)) DropHandItem(handIndex, handItem);

                handItemDict[handIndex] = newItem;
                if (!updateItem.Contains(newItem)) updateItem.Add(newItem);
                newItem.EquipItemToHand(self, handIndex);
                newItem?.UpdateItem();
                return true;
            }
            else
            {
                handItemDict.Add(handIndex, newItem);
                if (!updateItem.Contains(newItem)) updateItem.Add(newItem);
                newItem.EquipItemToHand(self, handIndex);
                newItem?.UpdateItem();
                return true;
            }
        }

        public bool StorageItem(ItemBase item)
        {
            if (item is null || item.hasItem) return false;
            foreach (var slot in slotsDict.Values)
            {
                if (slot.slotItem is null) continue;
                if (slot.slotItem.StorageItem(item))
                {
                    handItemDict.Remove(item.handIndex);
                    updateItem.Remove(item);
                    return true;
                }
            }
            return false;
        }

        public void DropAllHandItem()
        {
            if (hasHandItem)
            {
                foreach (var item in handItemDict.Values)
                {
                    itemHotBar.Remove(item);
                    updateItem.Remove(item);
                    item.DropItem();
                  //  Mgr.Item.PlaceItemToGround(item, self.entP, Quaternion.identity);
                }
                handItemDict.Clear();
            }
        }

        public void SetItemInHotbar(int index, ItemBase item)
        {
            if (IsValidHotbarIndex(index) &&
                item != null &&
                item.ownerShip != ItemOwnerShip.None &&
                item.ownerShip != ItemOwnerShip.Picking)
                itemHotBar[index] = item;
        }

        public void ChangeHotbarItem(int index, int handIndex = 1)
        {
            if (!IsValidHotbarIndex(index)) return;
            var item = itemHotBar[index];
            if (item == null) return;
            ReplaceItemInHand(item, handIndex);
        }

        public ItemBase GetHotbarItem(int index)
        => IsValidHotbarIndex(index) ? itemHotBar[index] : null;

        public bool HasHotbarItem(int index)
        => GetHotbarItem(index) != null;

        public bool IsValidHotbarIndex(int index)
        => index >= 0 && index < hotBarCount;

        private void DropHandItem(int handIndex, ItemBase handItem)
        {
            if (handItem is null) return;
            var num = itemHotBar.IndexOf(handItem);
            if (ReferenceEquals(GetHotbarItem(num), handItem)) itemHotBar[num] = null;
            handItemDict.Remove(handIndex);
            updateItem.Remove(handItem);
            handItem.DropItem();
        //    Mgr.Item.PlaceItemToGround(handItem, self.entP, Quaternion.identity);
        }
           */
    }
}