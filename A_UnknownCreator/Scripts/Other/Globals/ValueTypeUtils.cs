using System;
using UnityEngine;
namespace UnknownCreator.Modules
{
    public static class ValueTypeUtils
    {
        public static bool IsApproximatelyEqual(float a, float b, float tolerance)
        {
            return Mathf.Abs(a - b) <= tolerance;
        }

        public static int BoolToInt(bool val)
        {
            return val ? 1 : 0;
        }
        public static bool IntToBool(int val)
        {
            return val == 1;
        }

        public static float Remap01ToDB(float num)
        {
            if (num <= 0.0f) num = 0.0001f;
            return (float)(Math.Log10(num) * 20.0f);
        }

        public static float RemapDBTo01(float db)
        {
            return (float)Math.Pow(10, db / 20f);
        }
    }
}