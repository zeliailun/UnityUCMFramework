using UnityEngine;
namespace UnknownCreator.Modules
{
    public static partial class UnityGlobals
    {

        /// <summary>
        /// 判断目标是否在地面
        /// </summary>
        /// <param name="target">游戏对象</param>
        /// <param name="targetRadius">碰撞框半径</param>
        /// <param name="targetHeight">碰撞框高度</param>
        /// <param name="hitsCollider">储存的碰撞信息数组</param>
        /// <param name="groundLayer">地面层</param>
        /// <returns></returns>
        public static bool IsGrounded(this GameObject target, float targetRadius, float targetHeight, Collider[] hitsCollider, LayerMask groundLayer)
        => Physics.OverlapCapsuleNonAlloc
           (
            target.transform.position + target.transform.up * targetRadius - target.transform.up * 0.1F,
            target.transform.position + target.transform.up * targetHeight - target.transform.up * targetRadius,
            targetRadius,
            hitsCollider,
            groundLayer
           ) > 0;

        /// <summary>
        /// 判断目标与地面的高度差距是否满足
        /// </summary>
        /// <param name="target">游戏对象</param>
        /// <param name="hitsRaycast">储存的碰撞信息数组</param>
        /// <param name="height">高度差距</param>
        /// <param name="groundLayer">地面层</param>
        /// <returns></returns>
        public static bool IsEnoughHeight(this GameObject target, RaycastHit[] hitsRaycast, float height, LayerMask groundLayer)
        => Physics.RaycastNonAlloc(target.transform.position, -Vector3.up, hitsRaycast, height, groundLayer) > 0;

        /// <summary>
        /// 从目标自身，父物体，子物体中查找到指定组件
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="target"></param>
        /// <returns></returns>
        public static T GetComp<T>(this GameObject target) where T : Component
        {
            if (target.TryGetComponent<T>(out var comp1)) return comp1;

            var comp2 = target.GetComponentInChildren<T>();
            if (comp2 != null) return comp2;

            var comp3 = target.GetComponentInParent<T>();
            if (comp3 != null) return comp3;

            return null;
        }


        public static Vector3 GetObjSizeByMeshFilter(this GameObject obj)
        {
            var mf = obj.GetComp<MeshFilter>();
            if (mf == null) return Vector3.zero;
            Vector3 meshSize = mf.mesh.bounds.size;
            Vector3 scale = obj.transform.lossyScale;
            return new Vector3(meshSize.x * scale.x, meshSize.y * scale.y, meshSize.z * scale.z);
        }

        public static Vector3 GetObjSizeBySkinnedMesh(this GameObject obj)
        {
            var sm = obj.GetComp<SkinnedMeshRenderer>();
            if (sm == null) return Vector3.zero;
            Vector3 meshSize = sm.sharedMesh.bounds.size;
            Vector3 scale = obj.transform.lossyScale;
            return new Vector3(meshSize.x * scale.x, meshSize.y * scale.y, meshSize.z * scale.z);
        }

        public static GameObject SetLayer(this GameObject self, int layer)
        {
            self.layer = layer;
            Transform rootTransform = self.transform;
            for (var i = 0; i < rootTransform.childCount; i++)
            {
                SetLayer(rootTransform.GetChild(i).gameObject, layer);
            }
            return self;
        }

        public static void Destroy(this GameObject self)
        {
            if (null != self)
            {
                Object.Destroy(self);
            }
        }

        public static void Destroy(this GameObject self, float delay)
        {
            if (null != self)
            {
                Object.Destroy(self, delay);
            }
        }


        public static bool IsFacingTarget(Transform self, Vector3 targetPosition, float maxAngleInDegrees)
        {
            return IsFacingTargetByDirection(self, Direction(targetPosition, self.position), maxAngleInDegrees);
        }

        public static bool IsFacingTargetLimitY(Transform self, Vector3 targetPosition, float maxAngleInDegrees)
        {
            Vector3 directionToTarget = Direction(targetPosition, self.position);
            directionToTarget.y = 0;
            return IsFacingTargetByDirection(self, directionToTarget, maxAngleInDegrees);
        }

        public static bool IsFacingTargetByDirection(Transform self, Vector3 directionToTarget, float maxAngleInDegrees)
        {
            float angleToTarget = Vector3.Angle(self.forward, directionToTarget);
            return angleToTarget <= maxAngleInDegrees;
        }

        public static void FacingTargetLimitY(Transform self, Vector3 targetPosition, float sp)
        {

            Vector3 dir = Direction(targetPosition, self.position);
            dir.y = 0;
            FacingTargetByDirection(self, dir, sp);
        }

        public static void FacingTargetByDirection(Transform self, Vector3 direction, float sp)
        {
            if (direction != Vector3.zero)
            {
                self.rotation = Quaternion.Slerp
                (
                      self.rotation,
                      Quaternion.LookRotation(direction, Vector3.up),
                      CustomTime.DeltaTime() * sp
                );
            }
        }

        public static void FacingTarget(Transform self, Vector3 targetPosition, float sp)
        {
            Vector3 direction = Direction(targetPosition, self.position);
            if (!IsZero(direction))
            {
                self.rotation = Quaternion.Slerp
                (
                      self.rotation,
                      Quaternion.LookRotation(direction, Vector3.up),
                      CustomTime.DeltaTime() * sp
                );
            }
        }

        /// <summary>
        /// 判断目标位置能否被自身看到
        /// </summary>
        public static bool IsAlwaysVisible(Transform viewer, Vector3 targetPosition, LayerMask obstacleMask)
        {
            Vector3 dirToTarget = targetPosition - viewer.position;
            return Physics.Raycast(viewer.position, dirToTarget.normalized, dirToTarget.magnitude, obstacleMask);
        }


        public static float Angle(Vector3 form, Vector3 to)
        {
            return form.x * form.z + to.x * to.z;
        }

        public static float DistanceH(Vector3 pos1, Vector3 pos2)
        => Vector3.Distance(pos1, pos2);

        public static float DistanceL(Vector3 pos1, Vector3 pos2)
        => (pos1 - pos2).sqrMagnitude;

        public static float HorizontalDistance(Vector3 a, Vector3 b)
        {
            a.y = 0;
            b.y = 0;
            return DistanceH(a, b);
        }

        public static Vector3 Direction(Vector3 a, Vector3 b)
        {
            a = AdjustForEquality(a, b);
            return (a - b).normalized;
        }

        public static Vector3 DirectionLimitY(Vector3 a, Vector3 b)
        {
            a = AdjustForEquality(a, b);
            a.y = 0;
            b.y = 0;
            return (a - b).normalized;
        }

        public static Vector3 DirectionLimitX(Vector3 a, Vector3 b)
        {
            a = AdjustForEquality(a, b);
            a.x = 0;
            b.x = 0;
            return (a - b).normalized;
        }

        public static Vector3 DirectionLimitZ(Vector3 a, Vector3 b)
        {
            a = AdjustForEquality(a, b);
            a.z = 0;
            b.z = 0;
            return (a - b).normalized;
        }

        public static Vector3 GetBezierPoint(Vector3 start, Vector3 center, Vector3 end, float t)
        {
            return (1 - t) * (1 - t) * start + 2 * t * (1 - t) * center + t * t * end;
        }

        public static Vector3 NewV3(float value)
        => new(value, value, value);

        private static Vector3 AdjustForEquality(Vector3 a, Vector3 b)
        {
            return a == b ? a + Vector3.one : a;
        }

        /// <summary>
        /// 获取路径点的总距离
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static float GetPathDistance(this Vector3[] path)
        {
            float distance = 0;
            for (int i = 0; i < path.Length - 1; i++)
            {
                distance += Vector3.Distance(path[i], path[i + 1]);
            }
            return distance;
        }

        public static Vector3 ClampV3Min(this Vector3 vector, float num)
        {
            return new Vector3(
                Mathf.Max(vector.x, num),
                Mathf.Max(vector.y, num),
                Mathf.Max(vector.z, num)
            );
        }

        public static bool IsZero(this Vector3 vector)
        => vector.sqrMagnitude < MathUtils.Epsilon;
    }
}