using System;
namespace UnknownCreator.Modules
{
    [Flags]
    public enum AbBehavior
    {
        None = 1 << 0,
        Point = 1 << 1,
        Target = 1 << 2,

        /// <summary>
        /// 不会触发Target，Point ,正常走前后摇
        /// </summary>
        NotTarget = 1 << 3,

        /// <summary>
        /// 不会触发Target，Point,前后摇
        /// </summary>
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
        IgnoreSilence = 1 << 1,
        IgnoreStunned = 1 << 2,
        CanDeathTarget = 1 << 3,
        NotLearnable = 1 << 4,
        IgnoreBackswing = 1 << 5,
    }

    public enum AbTriggerMode
    {
        Pressed,
        Released,
    }
}