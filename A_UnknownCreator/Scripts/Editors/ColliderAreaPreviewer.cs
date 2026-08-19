using UnityEngine;

namespace UnknownCreator.Modules
{
    /// <summary>
    /// 编辑器碰撞区域预览器。
    ///
    /// 作用：
    /// 1. 只在非播放状态的编辑器 Scene 视图中绘制 Collider 区域。
    /// 2. 进入 Play Mode 时不会删除自身，也不会删除任何 Collider。
    /// 3. 打包时可通过配套 Editor 脚本自动从场景中剥离。
    ///
    /// 注意：
    /// 这个组件只做关卡编辑辅助，不要在游戏逻辑中依赖它。
    /// 真正运行时逻辑应该读取子物体上的 Collider。
    /// </summary>
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public class ColliderAreaPreviewer : MonoBase
    {
        [Header("编辑器显示")]
        [SerializeField] private bool drawInEditor = true;

        [Tooltip("勾选后，只有选中这个对象时才显示碰撞区域。")]
        [SerializeField] private bool drawOnlyWhenSelected = false;

        [Tooltip("是否显示未启用对象上的 Collider。")]
        [SerializeField] private bool includeInactive = true;

        [Tooltip("是否显示 disabled 的 Collider。")]
        [SerializeField] private bool drawDisabledColliders = true;

        [Header("颜色")]
        [SerializeField] private Color areaColor = Color.red;

        [SerializeField] private Color selectedColor = Color.green;

        [Range(0f, 1f)]
        [SerializeField] private float solidAlpha = 0.12f;

        [Range(0f, 1f)]
        [SerializeField] private float wireAlpha = 0.9f;

        [SerializeField] private bool drawSolid = true;

        [Header("Collider 默认设置")]
        [SerializeField] private bool defaultIsTrigger = false;

        [Tooltip("只在非播放状态下校验，自动把子 Collider 设置为 Trigger。")]
        [SerializeField] private bool autoSetChildCollidersTrigger = false;

        public override void OnEnable()
        {
    
        }

        public override void OnDisable()
        {
            

        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            solidAlpha = Mathf.Clamp01(solidAlpha);
            wireAlpha = Mathf.Clamp01(wireAlpha);

            // 只在编辑状态下自动修正，避免 Play Mode 中改动运行时状态。
            if (!Application.isPlaying && autoSetChildCollidersTrigger)
            {
                SetAllChildCollidersTrigger(true);
            }
        }

        private void OnDrawGizmos()
        {
            // 进入 Play Mode 后不绘制预览颜色，但组件和 Collider 都保留。
            if (Application.isPlaying)
                return;

            if (!drawInEditor)
                return;

            if (drawOnlyWhenSelected)
                return;

            DrawAllColliders(areaColor);
        }

        private void OnDrawGizmosSelected()
        {
            // 进入 Play Mode 后不绘制预览颜色，但组件和 Collider 都保留。
            if (Application.isPlaying)
                return;

            if (!drawInEditor)
                return;

            DrawAllColliders(selectedColor);
        }

        private void DrawAllColliders(Color color)
        {
            Collider[] colliders = GetComponentsInChildren<Collider>(includeInactive);

            for (int i = 0; i < colliders.Length; i++)
            {
                Collider col = colliders[i];

                if (col == null)
                    continue;

                if (!drawDisabledColliders && !col.enabled)
                    continue;

                DrawCollider(col, color);
            }
        }

        private void DrawCollider(Collider col, Color color)
        {
            if (col is BoxCollider box)
            {
                DrawBox(box, color);
                return;
            }

            if (col is SphereCollider sphere)
            {
                DrawSphere(sphere, color);
                return;
            }

            if (col is CapsuleCollider capsule)
            {
                DrawCapsule(capsule, color);
                return;
            }

            if (col is MeshCollider mesh)
            {
                DrawMesh(mesh, color);
                return;
            }

            DrawBounds(col.bounds, color);
        }

        private void DrawBox(BoxCollider box, Color color)
        {
            Matrix4x4 oldMatrix = Gizmos.matrix;
            Gizmos.matrix = box.transform.localToWorldMatrix;

            if (drawSolid)
            {
                Gizmos.color = WithAlpha(color, solidAlpha);
                Gizmos.DrawCube(box.center, box.size);
            }

            Gizmos.color = WithAlpha(color, wireAlpha);
            Gizmos.DrawWireCube(box.center, box.size);

            Gizmos.matrix = oldMatrix;
        }

        private void DrawSphere(SphereCollider sphere, Color color)
        {
            Vector3 center = sphere.transform.TransformPoint(sphere.center);
            float radius = sphere.radius * GetMaxAbsScale(sphere.transform.lossyScale);

            if (drawSolid)
            {
                Gizmos.color = WithAlpha(color, solidAlpha);
                Gizmos.DrawSphere(center, radius);
            }

            Gizmos.color = WithAlpha(color, wireAlpha);
            Gizmos.DrawWireSphere(center, radius);
        }

        private void DrawCapsule(CapsuleCollider capsule, Color color)
        {
            Transform t = capsule.transform;
            Vector3 scale = t.lossyScale;

            Vector3 localAxis = GetCapsuleLocalAxis(capsule.direction);
            Vector3 worldAxis = t.TransformDirection(localAxis).normalized;

            float axisScale = Mathf.Abs(GetAxisValue(scale, capsule.direction));
            float radiusScale = GetCapsuleRadiusScale(scale, capsule.direction);

            float radius = Mathf.Abs(capsule.radius * radiusScale);
            float height = Mathf.Abs(capsule.height * axisScale);

            height = Mathf.Max(height, radius * 2f);

            Vector3 center = t.TransformPoint(capsule.center);
            float halfLine = Mathf.Max(0f, height * 0.5f - radius);

            Vector3 top = center + worldAxis * halfLine;
            Vector3 bottom = center - worldAxis * halfLine;

            DrawWireCapsule(top, bottom, worldAxis, radius, color);
        }

        private void DrawMesh(MeshCollider mesh, Color color)
        {
            if (mesh.sharedMesh == null)
            {
                DrawBounds(mesh.bounds, color);
                return;
            }

            Matrix4x4 oldMatrix = Gizmos.matrix;
            Gizmos.matrix = mesh.transform.localToWorldMatrix;

            Gizmos.color = WithAlpha(color, wireAlpha);
            Gizmos.DrawWireMesh(mesh.sharedMesh);

            Gizmos.matrix = oldMatrix;
        }

        private void DrawBounds(Bounds bounds, Color color)
        {
            Gizmos.color = WithAlpha(color, wireAlpha);
            Gizmos.DrawWireCube(bounds.center, bounds.size);
        }

        private void DrawWireCapsule(Vector3 top, Vector3 bottom, Vector3 axis, float radius, Color color)
        {
            Gizmos.color = WithAlpha(color, wireAlpha);

            Vector3 right = Vector3.Cross(axis, Vector3.up);

            if (right.sqrMagnitude < 0.0001f)
            {
                right = Vector3.Cross(axis, Vector3.right);
            }

            right.Normalize();
            Vector3 forward = Vector3.Cross(axis, right).normalized;

            Gizmos.DrawWireSphere(top, radius);
            Gizmos.DrawWireSphere(bottom, radius);

            Gizmos.DrawLine(top + right * radius, bottom + right * radius);
            Gizmos.DrawLine(top - right * radius, bottom - right * radius);
            Gizmos.DrawLine(top + forward * radius, bottom + forward * radius);
            Gizmos.DrawLine(top - forward * radius, bottom - forward * radius);
        }

        private Color WithAlpha(Color color, float alpha)
        {
            color.a = alpha;
            return color;
        }

        private float GetMaxAbsScale(Vector3 scale)
        {
            return Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.y), Mathf.Abs(scale.z));
        }

        private Vector3 GetCapsuleLocalAxis(int direction)
        {
            switch (direction)
            {
                case 0:
                    return Vector3.right;
                case 1:
                    return Vector3.up;
                case 2:
                    return Vector3.forward;
                default:
                    return Vector3.up;
            }
        }

        private float GetAxisValue(Vector3 value, int axis)
        {
            switch (axis)
            {
                case 0:
                    return value.x;
                case 1:
                    return value.y;
                case 2:
                    return value.z;
                default:
                    return value.y;
            }
        }

        private float GetCapsuleRadiusScale(Vector3 scale, int direction)
        {
            float x = Mathf.Abs(scale.x);
            float y = Mathf.Abs(scale.y);
            float z = Mathf.Abs(scale.z);

            switch (direction)
            {
                // Capsule X 轴方向，高度走 X，半径看 Y/Z。
                case 0:
                    return Mathf.Max(y, z);

                // Capsule Y 轴方向，高度走 Y，半径看 X/Z。
                case 1:
                    return Mathf.Max(x, z);

                // Capsule Z 轴方向，高度走 Z，半径看 X/Y。
                case 2:
                    return Mathf.Max(x, y);

                default:
                    return Mathf.Max(x, z);
            }
        }

        public BoxCollider CreateBoxArea()
        {
            BoxCollider box = CreateColliderArea<BoxCollider>("BoxArea");
            box.size = Vector3.one;
            return box;
        }

        public SphereCollider CreateSphereArea()
        {
            SphereCollider sphere = CreateColliderArea<SphereCollider>("SphereArea");
            sphere.radius = 0.5f;
            return sphere;
        }

        public CapsuleCollider CreateCapsuleArea()
        {
            CapsuleCollider capsule = CreateColliderArea<CapsuleCollider>("CapsuleArea");
            capsule.radius = 0.5f;
            capsule.height = 2f;
            capsule.direction = 1;
            return capsule;
        }

        private T CreateColliderArea<T>(string prefix) where T : Collider
        {
            GameObject go = new GameObject(GetNextChildName(prefix));

            Transform child = go.transform;
            child.SetParent(transform);
            child.localPosition = Vector3.zero;
            child.localRotation = Quaternion.identity;
            child.localScale = Vector3.one;

            T collider = go.AddComponent<T>();
            collider.isTrigger = defaultIsTrigger;

            return collider;
        }

        private string GetNextChildName(string prefix)
        {
            int index = 1;

            for (int i = 0; i < transform.childCount; i++)
            {
                Transform child = transform.GetChild(i);

                if (child.name.StartsWith(prefix))
                {
                    index++;
                }
            }

            return $"{prefix}_{index:00}";
        }

        public void SetAllChildCollidersTrigger(bool isTrigger)
        {
            Collider[] colliders = GetComponentsInChildren<Collider>(true);

            for (int i = 0; i < colliders.Length; i++)
            {
                Collider col = colliders[i];

                if (col == null)
                    continue;

                col.isTrigger = isTrigger;
            }
        }
#endif
    }
}
