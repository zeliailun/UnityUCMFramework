using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace UnknownCreator.Modules
{
    public static partial class UnityGlobals
    {

        private static float RandomValue(float min, float max)
        {
            return Random.Range(min, max + 1);
        }

        public static Quaternion GetRandomRotateYAxis()
        {
            return Quaternion.Euler(0, Random.Range(0f, 360f), 0);
        }

        public static Vector3 GetRandomPosition(float minX, float maxX, float minY, float maxY, float minZ, float maxZ)
        {
            float randomX = RandomValue(minX, maxX);
            float randomY = RandomValue(minY, maxY);
            float randomZ = RandomValue(minZ, maxZ);
            return new Vector3(randomX, randomY, randomZ);
        }

        public static Vector3 GetRandomPosition(Vector3 pos, float x, float z)
        => new(pos.x += RandomValue(-x, x), 0, pos.z += RandomValue(-z, z));

        public static Vector3 GetRandomPosition(Vector3 pos, float x, float y, float z, bool underground = false)
        {
            Vector3 newPos = GetRandomPosition(pos, x, z);
            newPos.y += RandomValue(underground ? -y : 0, y);
            return new(newPos.x, newPos.y, newPos.z);
        }

        public static Vector3 GetAroundRandomPosition(ref Vector3 lastPosition, ref Quaternion lastRotation, Vector3 targetPos, float minAngle, float range)
        {
            if (lastPosition == Vector3.zero)
                return lastPosition = targetPos + Random.insideUnitSphere * range;

            Quaternion newRotation = Quaternion.Euler(0, Random.Range(0f, 360f), 0);
            Quaternion deltaRotation = newRotation * Quaternion.Inverse(lastRotation);
            float angle = Quaternion.Angle(deltaRotation, Quaternion.identity);

            if (angle > minAngle)
            {
                return lastPosition = targetPos + newRotation * Vector3.forward * range;
            }

            return Vector3.zero;
        }

        public static Vector3 GetAroundRandomPosition(Vector3 dir, Vector3 targetPos, float min, float max, int layer = NavMesh.AllAreas)
        {
            float num = RandomValue(min, max);
            Vector3 newPosition = targetPos + dir * num;
            for (int i = 0; i < 3; i++)
            {
                if (NavMesh.SamplePosition(newPosition, out NavMeshHit hit, max, layer))
                    return hit.position;
            }
            return Vector3.zero;
        }

        public static Vector3 GetAroundRandomPosition(Vector3 targetPos, float min, float max)
        {
            Vector3 randomDirection = Random.insideUnitSphere;
            randomDirection.y = 0;
            Vector3 newPosition = targetPos + randomDirection.normalized * RandomValue(min, max);
            for (int i = 0; i < 10; i++)
            {
                if (NavMesh.SamplePosition(newPosition, out NavMeshHit hit, max, NavMesh.AllAreas))
                    return hit.position;
            }
            return newPosition;
        }

        public static bool GetRandomNavPoint(Vector3 pos, out Vector3 result, float range, int count = 30)
        {
            Vector3 randomPoint;
            for (int i = 0; i < count; i++)
            {
                randomPoint = pos + (UnityEngine.Random.insideUnitSphere * range);
                if (NavMesh.SamplePosition(randomPoint,
                    out var hit,
                    10,
                    NavMesh.AllAreas))
                {
                    result = hit.position;
                    return true;
                }
            }
            result = Vector3.zero;
            return false;
        }
    }
}