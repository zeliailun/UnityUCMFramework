using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UnknownCreator.Modules
{
    public abstract partial class ItemBase
    {
        /*
     public void AddItemAbility(bool isPressed, ItemAbilityInfo info)
     {
         var hbsm = hbsmItem.GetHBSM(info.hbsmName);
         if (hbsm is null || !hbsm.HasState(info.abilityName))
         {
             if (isPressed)
                 pressedList.Add(info);
             else
                 releasedList.Add(info);
             hbsm.AddState(info.abilityName);
         }
     }

     public void RemoveItemAbility(bool isPressed, ItemAbilityInfo info)
     {
         var hbsm = hbsmItem.GetHBSM(info.hbsmName);
         if (hbsm != null && hbsm.HasState(info.abilityName))
         {
             if (isPressed)
                 pressedList.Remove(info);
             else
                 releasedList.Remove(info);
             hbsm.RemoveState(info.abilityName);
         }
     }

     public void RemoveAllItemAbility()
     {
         hbsmItem?.RemoveAllHBSM();
     }

     public bool HasItemAbility(string hbsmName, string abName)
     {
         return hbsmItem?.GetHBSM(hbsmName)?.HasState(abName) ?? false;
     }

     public bool HasItemAbility(int index, bool isPressed)
     {
         return index >= 0 && index < (isPressed ? itemData.pressedList.Count : itemData.releasedList.Count);
     }

     public double GetValue(string statsName)
     => statsKV.TryGetValue(statsName, out var result) ? result.baseValue : 0F;

     public bool CanStored(int num)
     => storable.isEnough && storageCapacity > num;    */
    }
}