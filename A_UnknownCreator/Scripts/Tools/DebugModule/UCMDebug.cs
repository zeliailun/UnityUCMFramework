using System.Diagnostics;
using UnityEngine;


namespace UnknownCreator.Modules
{
    public static class UCMDebug
    {
        private static ILogHelper _log = new DefaultLogHelper();

        public static ILogHelper log => _log;

        public static bool isEnableDebug = true;

        public static void SetLog<T>() where T : class, ILogHelper, new()
        => _log = new T();

        public static void SetLog<T>(T value) where T : class, ILogHelper
        => _log = value;

        [Conditional("UCMDebug")]
        public static void Log(object obj)
        {
            if (isEnableDebug) _log.Log(obj);
        }

        [Conditional("UCMDebug")]
        public static void LogWarning(object obj)
        {
            if (isEnableDebug) log.LogWarning(obj);
        }

        [Conditional("UCMDebug")]
        public static void LogError(object obj)
        {
            if (isEnableDebug) log.LogError(obj);
        }

        [Conditional("UCMDebug")]
        public static void DrawLine(Vector3 start, Vector3 end, Color color, float duration)
        {
            if (isEnableDebug) _log.DrawLine(start, end, color, duration);
        }

        [Conditional("UCMDebug")]
        public static void DrawRay(Vector3 start, Vector3 dir, Color color, float duration)
        {
            if (isEnableDebug) _log.DrawLine(start, dir, color, duration);
        }


        [Conditional("UCMDebug")]
        public static void DrawCircle(Vector3 center, float radius, int segments, Color color, float duration)
        {
            if (isEnableDebug)
                _log.DrawCircle(center, radius, segments, color, duration);
        }

        [Conditional("UCMDebug")]
        public static void DrawCube(Vector3 center, Vector3 size, Vector3 direction, Color color, float duration)
        {
            if (isEnableDebug)
                _log.DrawCube(center, size, direction, color, duration);
        }
    }
}
