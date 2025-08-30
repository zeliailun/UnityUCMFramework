using System;
using UnityEngine;

namespace UnknownCreator.Modules
{
    public static partial class UnityGlobals
    {
        public static bool GetValue(string key)
        {
          return  ValueTypeGlobals.IntToBool(PlayerPrefs.GetInt(key));
        }

        public static void SetKV(string key, bool value)
        {
            SetKV(key, ValueTypeGlobals.BoolToInt(value));
        }

        public static void SetKV(string key, int value)
        {
            if (PlayerPrefs.HasKey(key)) return;
            PlayerPrefs.SetInt(key, value);
            PlayerPrefs.Save();
        }

        public static void SetFloat(string key, int value)
        {
            if (PlayerPrefs.HasKey(key)) return;
            PlayerPrefs.SetFloat(key, value);
            PlayerPrefs.Save();
        }
    }

}