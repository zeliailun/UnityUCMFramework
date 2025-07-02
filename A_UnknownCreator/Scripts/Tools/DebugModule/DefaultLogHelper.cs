using UnityEngine;

namespace UnknownCreator.Modules
{
    public class DefaultLogHelper : ILogHelper
    {
        public void LogError<T>(T obj)
        {
            Debug.LogError("<color=red>" + obj + "</color>");
            Debug.Break();
        }

        public void LogWarning<T>(T obj)
        {
            Debug.LogWarning("<color=orange>" + obj + "</color>");
        }

        public void Log<T>(T obj)
        {
            Debug.Log("<color=white>" + obj + "</color>");
        }

        public void DrawLine(Vector3 start, Vector3 end, Color color, float duration)
        {
            Debug.DrawLine(start, end, color, duration);
        }

        public void DrawRay(Vector3 start, Vector3 dir, Color color, float duration)
        {
            Debug.DrawLine(start, dir, color, duration);
        }

        public void DrawCircle(Vector3 center, float radius, int segments, Color color, float duration)
        {
            float angleStep = 360f / segments;

            for (int i = 0; i < segments; i++)
            {
                float angle1 = Mathf.Deg2Rad * angleStep * i;
                float angle2 = Mathf.Deg2Rad * angleStep * (i + 1);

                Vector3 point1 = new Vector3(Mathf.Cos(angle1), 0, Mathf.Sin(angle1)) * radius + center;
                Vector3 point2 = new Vector3(Mathf.Cos(angle2), 0, Mathf.Sin(angle2)) * radius + center;

                DrawLine(point1, point2, color, duration);
            }
        }

        public void DrawCube(Vector3 center, Vector3 size, Vector3 direction, Color color, float duration)
        {
            if (direction == Vector3.zero) direction = Vector3.forward;

            Quaternion rotation = Quaternion.LookRotation(direction);
            Vector3 halfSize = size * 0.5f;

            Vector3[] points = new Vector3[8]
            {
            new (-halfSize.x, -halfSize.y, -halfSize.z),
            new (halfSize.x, -halfSize.y, -halfSize.z),
            new (halfSize.x, -halfSize.y, halfSize.z),
            new (-halfSize.x, -halfSize.y, halfSize.z),
            new (-halfSize.x, halfSize.y, -halfSize.z),
            new (halfSize.x, halfSize.y, -halfSize.z),
            new (halfSize.x, halfSize.y, halfSize.z),
            new (-halfSize.x, halfSize.y, halfSize.z)
            };

            for (int i = 0; i < points.Length; i++)
            {
                points[i] = center + rotation * points[i];
            }

            // »æÖÆ12Ìõ±ß
            DrawLine(points[0], points[1], color, duration);
            DrawLine(points[1], points[2], color, duration);
            DrawLine(points[2], points[3], color, duration);
            DrawLine(points[3], points[0], color, duration);
            DrawLine(points[4], points[5], color, duration);
            DrawLine(points[5], points[6], color, duration);
            DrawLine(points[6], points[7], color, duration);
            DrawLine(points[7], points[4], color, duration);
            DrawLine(points[0], points[4], color, duration);
            DrawLine(points[1], points[5], color, duration);
            DrawLine(points[2], points[6], color, duration);
            DrawLine(points[3], points[7], color, duration);
        }

    }
}