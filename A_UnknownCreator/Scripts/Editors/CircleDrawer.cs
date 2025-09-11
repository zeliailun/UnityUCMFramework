#if UNITY_EDITOR
using UnityEngine;

namespace UnknownCreator.Modules
{

[ExecuteInEditMode] // 让它在编辑模式下也能执行
public class CircleDrawer : MonoBehaviour
{
    public float radius = 2f;
    public int segments = 40;
    public Color color = Color.green;

    private void OnDrawGizmos()
    {
        if (segments < 3) return;

        Gizmos.color = color;
        float angleStep = 360f / segments;

        Vector3 center = transform.position;

        Vector3 prevPoint = Vector3.zero;

        for (int i = 0; i <= segments; i++)
        {
            float angle = Mathf.Deg2Rad * angleStep * i;
            Vector3 point = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * radius + center;

            if (i > 0)
                Gizmos.DrawLine(prevPoint, point);

            prevPoint = point;
        }
    }
}
}
#endif