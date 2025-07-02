using UnityEngine;
namespace UnknownCreator.Modules
{
    public interface ILogHelper
    {
        void Log<T>(T obj);

        void LogWarning<T>(T obj);

        void LogError<T>(T obj);

        void DrawLine(Vector3 start, Vector3 end, Color color, float duration);

        void DrawRay(Vector3 start, Vector3 dir, Color color, float duration);

        void DrawCircle(Vector3 center, float radius, int segments, Color color, float duration);

        void DrawCube(Vector3 center, Vector3 size, Vector3 direction, Color color, float duration);
    }
}