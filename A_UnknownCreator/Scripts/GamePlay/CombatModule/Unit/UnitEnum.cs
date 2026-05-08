using System;
using System.Collections;
using UnityEngine;

namespace UnknownCreator.Modules
{
    public enum UnitTargetTeam
    {
        Friendly,
        Enemy,

        /// <summary>
        /// 任意GameObject
        /// </summary>
        All,
    }

    [Flags]
    public enum UnitTargetFlags
    {
        None = 0,
        CanFindDeathUnit = 1 << 0,
        CanFindInvincibleUnit = 1 << 1,
        IgnoreSelf = 1 << 2,
    }

    public enum UnitFindType
    {
        None,
        /// <summary>
        /// 随机排序
        /// </summary>
        Unordered,

        /// <summary>
        /// 从近到远
        /// </summary>
        Closest,

        /// <summary>
        /// 从远到近
        /// </summary>
        Farthest,
    }
}