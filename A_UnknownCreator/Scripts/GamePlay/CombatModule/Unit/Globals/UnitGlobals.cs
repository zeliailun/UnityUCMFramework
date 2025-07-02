using System;
using UnityEngine;

namespace UnknownCreator.Modules
{
    public static class UnitGlobals
    {
        private static Type type = typeof(Unit);

        //实体单位的显示对象,初始化时会根据该名称查找其子类对象
        public const string Model = nameof(Model);

        public const string HealthSM = nameof(HealthSM);


        public static Unit GetUnit(this int id)
        => (Mgr.Ent.GetEntity(id) is not null and Unit ent) ? ent : null;

        public static Unit GetUnit(this IEntity obj)
        => obj?.entID.GetUnit();

        public static Unit GetUnit(this GameObject obj)
        => obj == null ? null : obj.GetInstanceID().GetUnit();

        public static Unit GetUnitByHitBox(this GameObject obj)
        => obj == null ? null : Mgr.Unit.GetUnitRoot(obj.GetInstanceID());

        public static bool IsVaild(this Unit obj)
        => obj != null && !Mgr.RPool.HasObject(type, obj);

        public static bool IsSame(this Unit obj, Unit target)
        => obj?.entID == target?.entID;

        public static bool IsSame(this Unit obj, int targetID)
        => obj?.entID == targetID;

        public static bool IsEnemy(this Unit obj, Unit target)
        => obj.unitTeam != target.unitTeam;

    }
}