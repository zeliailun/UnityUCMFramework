using System;
using UnityEngine;

namespace UnknownCreator.Modules
{
    public static class UnitGlobals
    {
        private static Type type = typeof(Unit);


        //实体单位的显示对象,初始化时会根据该名称查找其子类对象
        public const string Model = nameof(Model);


        public static Unit GetUnit(this EntityId id)
        => (Mgr.Ent.GetEntity(id) is not null and Unit ent) ? ent : null;

        public static Unit GetUnit(this IEntity obj)
        => obj?.entID.GetUnit();

        public static Unit GetUnit(this GameObject obj)
        => obj == null ? null : obj.GetEntityId().GetUnit();

        public static Unit GetUnitByHitBox(this GameObject obj)
        => obj == null ? null : Mgr.Unit.GetUnitRoot(obj.GetEntityId());

        public static bool IsValid(this Unit obj)
        => obj != null && !Mgr.RPool.HasObject(type, obj);

        public static bool IsValidAlive(this Unit obj)
        => obj.IsValid() && obj.isAlive;

        public static bool IsSelf(this Unit obj, Unit target)
        {
            if (obj == null || target == null)
                return false;

            return obj.entID == target.entID;
        }

        public static bool IsSelf(this Unit obj, EntityId targetID)
        {
            if (obj == null)
                return false;

            return obj.entID == targetID;
        }

        public static bool IsEnemy(this Unit obj, Unit target)
            => obj != null && target != null && obj.unitTeam != target.unitTeam;

    }
}