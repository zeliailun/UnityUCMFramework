using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnknownCreator.Modules
{
    [Serializable]
    public sealed class ProjectileMgr : IProjectileMgr
    {
        private Dictionary<long, Projectile> projDict = new();

        private List<Projectile> projList = new();

        private int maxAttempts = 3;
        private bool needsCompact;
        private bool isUpdating;

        [JsonIgnore]
        public FilterSlot<(Projectile, GameObject), (bool, Unit)> projFilter { private set; get; } = new();

        //private ProjectileMgr() { }

        void IDearMgr.WorkWork()
        {
            projDict ??= new();
            projList ??= new();
            projFilter ??= new();
            needsCompact = false;
            isUpdating = false;
        }

        void IDearMgr.DoNothing()
        {
            projFilter.Clear();
            ReleaseAllProjectile();
            projDict = null;
            projList = null;
            needsCompact = false;
            isUpdating = false;
        }

        void IDearMgr.UpdateMGR()
        {
            float deltaTime = CustomTime.DeltaTime();
            isUpdating = true;
            try
            {
                for (int i = projList.Count - 1; i >= 0; i--)
                {
                    Projectile proj = projList[i];
                    if (proj is null || proj.isRelease)
                    {
                        projList[i] = null;
                        if (proj != null)
                            proj.activeIndex = -1;
                        needsCompact = true;
                        continue;
                    }

                    proj.UpdateProjectile(deltaTime);
                }
            }
            finally
            {
                isUpdating = false;
                // 释放可能发生在移动、命中或事件回调中，统一到帧末整理可避免逐个 Remove 的 O(n) 搬移。
                CompactProjectileList();
            }
        }



        public void ReleaseProjectile(long id)
        {
            if (projDict.Remove(id, out var value))
                ReleaseProjectileInternal(value);
        }

        public void ReleaseProjectile(Projectile proj)
        {
            if (proj == null ||
                !projDict.TryGetValue(proj.id, out Projectile value) ||
                !ReferenceEquals(value, proj))
                return;

            projDict.Remove(proj.id);
            ReleaseProjectileInternal(value);
        }

        public void ReleaseAllProjectile()
        {
            int attemptCount = 0;

            while (projDict.Count > 0 && projList.Count > 0)
            {
                attemptCount++;

                int count = projList.Count;

                Projectile value;
                for (int i = count - 1; i >= 0; i--)
                {
                    if (i >= projList.Count)
                        continue;

                    value = projList[i];
                    if (value == null || value.isRelease)
                    {
                        projList[i] = null;
                        if (value != null)
                            value.activeIndex = -1;
                        needsCompact = true;
                        continue;
                    }

                    if (projDict.TryGetValue(value.id, out Projectile current) &&
                        ReferenceEquals(current, value))
                    {
                        projDict.Remove(value.id);
                        ReleaseProjectileInternal(value);
                    }
                    else
                    {
                        projList[i] = null;
                        value.activeIndex = -1;
                        needsCompact = true;
                    }
                }

                if (!isUpdating)
                    CompactProjectileList();

                if (attemptCount > maxAttempts)
                {
                    UCMDebug.LogWarning("投射物释放可能触发了死循环");
                    break;
                }
            }

            projDict.Clear();
            if (!isUpdating)
                CompactProjectileList();
        }

        public Projectile GetProjectile(long id)
        => projDict.TryGetValue(id, out var value) ? value : null;

        public bool IsValidProjectile(Projectile pb)
        => pb != null &&
           projDict.TryGetValue(pb.id, out Projectile current) &&
           ReferenceEquals(current, pb) &&
           !pb.isRelease;

        public bool IsValidProjectile(long id)
        => projDict.TryGetValue(id, out var pb) &&
        !pb.isRelease;

        public Projectile LaunchProjectile<IMvt, ICheck, Data>(ProjectileInfo<IMvt, ICheck, Data> info)
        where IMvt : class, IProjMvt
        where ICheck : class, IProjCheck
        where Data : ProjectileData, new()
        {
            return LaunchProjectile(info.mvt, info.check, info.data, info.kv);
        }


        public Projectile LaunchProjectile(IProjMvt mvt, IProjCheck check, ProjectileData data, IVariableMgr kv)
        {
            var proj = Mgr.RPool.Load<Projectile>();
            proj.InitProjectile(data, mvt, check, kv);
            projDict.Add(proj.id, proj);
            proj.activeIndex = projList.Count;
            projList.Add(proj);
            proj.UpdateProjectile(CustomTime.DeltaTime());
            return proj;
        }

        public Projectile LaunchProjectile(ProjectileSnapshot snapshot)
        {

            return LaunchProjectile(snapshot.mvt, snapshot.check, snapshot.data, snapshot.kv);
        }

        private void ReleaseProjectileInternal(Projectile proj)
        {
            int index = proj.activeIndex;
            if ((uint)index < (uint)projList.Count && ReferenceEquals(projList[index], proj))
            {
                projList[index] = null;
            }
            else
            {
                // 正常流程不会进入这里；保留兜底以防外部旧代码破坏索引一致性。
                index = projList.IndexOf(proj);
                if (index >= 0)
                    projList[index] = null;
            }

            proj.activeIndex = -1;
            needsCompact = true;

            // 销毁回调、事件和对象池释放仍在当前调用点立即执行，时序保持不变。
            Mgr.RPool.Release(proj);
        }

        private void CompactProjectileList()
        {
            if (!needsCompact || projList == null || isUpdating)
                return;

            int writeIndex = 0;
            for (int readIndex = 0; readIndex < projList.Count; readIndex++)
            {
                Projectile proj = projList[readIndex];
                if (proj == null || proj.isRelease)
                    continue;

                if (writeIndex != readIndex)
                    projList[writeIndex] = proj;

                proj.activeIndex = writeIndex;
                writeIndex++;
            }

            if (writeIndex < projList.Count)
                projList.RemoveRange(writeIndex, projList.Count - writeIndex);

            needsCompact = false;
        }
    }
}
