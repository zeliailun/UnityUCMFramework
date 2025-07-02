using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UnknownCreator.Modules
{
    public sealed class ItemMgr : IItemMgr
    {       /*
        private Dictionary<int, ItemBase> itemDict = new();

        private List<ItemBase> itemList = new();

        //拾取到身上
        public void PickUpItem(Unit unit, ItemBase item)
        {
            if (unit is null || item is null) return;
            item.ownerShip = ItemOwnerShip.Picking;
            if (unit.itemC.AddItem(item))
            {
                itemDict.Remove(item.model.GetInstanceID());
                itemList.Remove(item);
            }
            else
            {
                item.ownerShip = ItemOwnerShip.None;
            }
        }

        //拾取到手上
        public void PickUpItem(Unit unit, ItemBase item, int handIndex = 1)
        {
            if (unit is null || item is null) return;
            item.ownerShip = ItemOwnerShip.Picking;
            if (unit.itemC.ReplaceItemInHand(item, handIndex))
            {
                itemDict.Remove(item.model.GetInstanceID());
                itemList.Remove(item);
            }
            else
            {
                item.ownerShip = ItemOwnerShip.None;
            }
        }

        public ItemBase CreateItem(string itemName)
        {
            ItemBase item = (ItemBase)Mgr.RPool.Load(Type.GetType(itemName));
            item.InitItem(itemName);
            return item;
        }

        public ItemBase CreateItemToGround(string itemName, Vector3 pos, Quaternion rot)
        {
            ItemBase item = CreateItem(itemName);
            PlaceItemToGround(item, pos, rot);
            return item;
        }

        public void PlaceItemToGround(ItemBase item, Vector3 pos, Quaternion rot)
        {
            if (item != null &&
                item.ownerShip == ItemOwnerShip.None &&
                !itemDict.TryGetValue(item.model.GetInstanceID(), out _))
            {
                Mgr.GPool.SetRoot(item.modelT);
                item.modelT.SetPositionAndRotation(pos, rot);
                itemDict.Add(item.model.GetInstanceID(), item);
                itemList.Add(item);
            }
        }

        public void ClearItem()
        {
            itemDict.Clear();
            ItemBase item;
            for (int i = itemList.Count - 1; i >= 0; i--)
            {
                item = itemList[i];
                itemList.RemoveAt(i);
                Mgr.RPool.Release(item);
            }
            itemList.Clear();
        }    */
    }
}