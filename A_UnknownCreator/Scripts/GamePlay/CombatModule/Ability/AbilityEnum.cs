using System;
namespace UnknownCreator.Modules
{
    [Flags]
    public enum AbBehavior
    {
        None = 1 << 0,
        Point = 1 << 1,
        Target = 1 << 2,
        NotTarget = 1 << 3,

        //立即施法，会忽略掉自身前摇动画等内容，单独使用时还会忽略掉施法距离
        Immediate = 1 << 4,
    }

    [Flags]
    public enum AbTargetTeam
    {
        None = 1 << 0,
        Self = 1 << 1,
        Friendly = 1 << 2,
        Enemy = 1 << 3,
    }


    [Flags]
    public enum AbFlags
    {
        None = 1 << 0,

        //可沉默施法
        IgnoreSilence = 1 << 1,

        //可眩晕施法
        IgnoreStunned = 1 << 2,

        //可以对死亡单位为目标
        CanDeathTarget = 1 << 3,

        //无法学习升级
        NotLearnable = 1 << 4,

        //施法后忽略后摇过程（提前结束动画，限制，触发完全施法）
        IgnoreBackswing = 1 << 5,

        //施法时，会打断正处于后摇的能力
        InterruptOtherCastBackswing = 1 << 6,
    }

    public enum AbTriggerMode
    {
        Pressed,
        Released,
    }
}