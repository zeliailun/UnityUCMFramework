using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UnknownCreator.Modules
{
    public static class StateGlobals
    {
        //默认状态
        public const int Invulnerable = 1;
        public const int Stunned = 2;
        public const int Silenced = 3;


     /*   public const int Rooted = 2;
        public const int Frozen = 5;
        public const int Disarmed = 6;
        public const int NoCollision = 7;
        public const int Invisible = 8;
    */

        /// <summary>
        /// 初始化时用来判定状态数量
        /// </summary>
        public const int StateCount = 4;
    }
}