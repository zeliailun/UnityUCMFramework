using UnityEngine;

namespace UnknownCreator.Modules
{
    public static class SettingHandle
    {
        public static void InitBoolValue(string key, ref bool currentValue, bool defaultValue)
        {
            currentValue = PlayerPrefs.HasKey(key) ? UnityGlobals.GetValue(key) : defaultValue;
        }

        public static void InitSoundValue(string key, float defaultValue = 0f)
        {
            float value = PlayerPrefs.GetFloat(key, defaultValue);
            Mgr.Sound.mixer.SetFloat(key, value);
        }

        public static void SaveBoolValue(string key, bool val)
        {
            PlayerPrefs.SetInt(key, ValueTypeUtils.BoolToInt(val));
        }

        public static void SaveSoundValue(string key)
        {
            if (Mgr.Sound.mixer.GetFloat(key, out var val))
            {
                PlayerPrefs.SetFloat(key, val);
            }

        }
    }
}
