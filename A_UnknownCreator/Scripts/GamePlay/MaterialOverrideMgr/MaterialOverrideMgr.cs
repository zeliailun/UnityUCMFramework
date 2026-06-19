using System.Collections.Generic;
using UnityEngine;

namespace UnknownCreator.Modules
{
    /// <summary>
    /// 框架级材质覆盖管理器。
    ///
    /// 功能：
    /// 1. 注册对象并缓存 Renderer 原始材质。
    /// 2. 支持按优先级压入/弹出临时材质。
    /// 3. 支持指定 Entity 或指定 RendererName。
    /// 4. 支持 Renderer 锁定。
    /// 5. 支持材质实例池，避免频繁 new Material。
    /// 6. 支持模型替换后重新扫描 Renderer。
    /// </summary>
    public class MaterialOverrideMgr : IMaterialOverrideMgr
    {
        public int Priority() => 0;

        #region 材质对象池

        private struct PooledMaterial
        {
            public Material mat;
            public float lastUsedTime;

            public PooledMaterial(Material mat)
            {
                this.mat = mat;
                lastUsedTime = Time.time;
            }
        }

        private readonly Dictionary<string, Queue<PooledMaterial>> matPool = new();

        private float maxIdleTime = 300f;
        private int maxPoolPerMaterial = 50;
        private float poolCheckInterval = 100f;
        private float lastPoolCheckTime;

        #endregion

        #region 渲染器数据

        private class RendererMaterialData
        {
            public Renderer renderer;
            public Material originalMaterial;

            /// <summary>
            /// matInstance：实际显示用的材质实例。
            /// resName：资源路径/名称，用于回收到对应池。
            /// priority：优先级，越大越优先。
            /// </summary>
            public readonly List<(Material matInstance, string resName, int priority)> stack = new();

            public Material currentTop;
            public bool dirty;
            public ObjInfo owner;
            public bool isLocked;
        }

        #endregion

        #region 对象数据

        private class ObjInfo
        {
            public EntityId id;
            public Transform parentT;

            public readonly List<RendererMaterialData> rendererDatas = new();
            public readonly Dictionary<string, RendererMaterialData> nameToData = new();

            public bool anyRendererDirty;
        }

        private readonly Dictionary<EntityId, ObjInfo> registeredObjs = new();

        #endregion

        #region 注册 / 注销

        public void RegisterObject(EntityId id, Transform parentT)
        {
            if (parentT == null)
                return;

            if (registeredObjs.ContainsKey(id))
                return;

            var info = new ObjInfo
            {
                id = id,
                parentT = parentT
            };

            CollectRenderers(info);
            registeredObjs[id] = info;
        }

        public void UnregisterObject(EntityId id)
        {
            if (!registeredObjs.TryGetValue(id, out var info))
                return;

            ReleaseObject(info, restoreOriginalMaterial: true);
            registeredObjs.Remove(id);
        }

        /// <summary>
        /// 重新扫描对象当前子级 Renderer。
        /// 常用于换模型、对象池复用后子物体变化等场景。
        /// </summary>
        public void RefreshObject(EntityId id, bool useCurrentChildren = true)
        {
            if (!registeredObjs.TryGetValue(id, out var obj))
                return;

            if (!useCurrentChildren)
                return;

            ReleaseObject(obj, restoreOriginalMaterial: false);

            obj.rendererDatas.Clear();
            obj.nameToData.Clear();
            obj.anyRendererDirty = false;

            CollectRenderers(obj);

            foreach (var rd in obj.rendererDatas)
            {
                SetRendererDirty(rd);
            }

            ApplyMaterials();
        }

        /// <summary>
        /// 兼容旧命名。
        /// 如果你的旧代码里已经调用 SwapModel，可以先不急着全部改。
        /// </summary>
        public void SwapModel(EntityId id, bool useCurrentChildren = true)
        {
            RefreshObject(id, useCurrentChildren);
        }

        private void CollectRenderers(ObjInfo info)
        {
            var smrs = info.parentT.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            var mrs = info.parentT.GetComponentsInChildren<MeshRenderer>(true);

            foreach (var r in smrs)
                AddRendererData(info, r);

            foreach (var r in mrs)
                AddRendererData(info, r);

            // 保留占位逻辑：让栈永远不是空栈，方便后续扩展。
            foreach (var rd in info.rendererDatas)
            {
                rd.stack.Add((null, null, int.MinValue));
            }
        }

        private void AddRendererData(ObjInfo info, Renderer renderer)
        {
            if (renderer == null)
                return;

            var rd = new RendererMaterialData
            {
                renderer = renderer,

                // 复制一份，避免直接污染资源本体。
                originalMaterial = renderer.sharedMaterial != null
                    ? new Material(renderer.sharedMaterial)
                    : null,

                owner = info,
                isLocked = false
            };

            info.rendererDatas.Add(rd);

            // 同名 Renderer 时，后加入的会覆盖前面的映射。
            // 如果后续需要严格区分同名对象，可以改成完整路径 Key。
            info.nameToData[renderer.gameObject.name] = rd;
        }

        private void ReleaseObject(ObjInfo info, bool restoreOriginalMaterial)
        {
            foreach (var rd in info.rendererDatas)
            {
                if (restoreOriginalMaterial && rd.renderer != null)
                {
                    rd.renderer.sharedMaterial = rd.originalMaterial;
                }

                for (int i = 0; i < rd.stack.Count; i++)
                {
                    SafeReleaseMaterial(rd, rd.stack[i]);
                }

                rd.stack.Clear();
                rd.currentTop = null;
                rd.dirty = false;
            }

            info.anyRendererDirty = false;
        }

        #endregion

        #region Renderer 锁定

        public void LockRenderer(EntityId entityId, string rendererName)
        {
            if (!TryGetRendererData(entityId, rendererName, out var rd))
                return;

            rd.isLocked = true;
        }

        public void UnlockRenderer(EntityId entityId, string rendererName)
        {
            if (!TryGetRendererData(entityId, rendererName, out var rd))
                return;

            rd.isLocked = false;
            SetRendererDirty(rd);
        }

        public bool IsRendererLocked(EntityId entityId, string rendererName)
        {
            return TryGetRendererData(entityId, rendererName, out var rd) && rd.isLocked;
        }

        private bool TryGetRendererData(EntityId entityId, string rendererName, out RendererMaterialData rd)
        {
            rd = null;

            if (string.IsNullOrEmpty(rendererName))
                return false;

            if (!registeredObjs.TryGetValue(entityId, out var info))
                return false;

            return info.nameToData.TryGetValue(rendererName, out rd);
        }

        #endregion

        #region 材质操作

        public void PushMaterial(string materialName, int priority, MaterialTarget? target = null)
        {
            if (string.IsNullOrEmpty(materialName))
                return;

            foreach (var rd in GetTargetRendererDatas(target))
            {
                if (rd.isLocked)
                    continue;

                // 每个 Renderer 独立实例，避免不同 Renderer 之间材质参数串改。
                var matInstance = GetMaterialFromPool(materialName);
                if (matInstance == null)
                    continue;

                rd.stack.Add((matInstance, materialName, priority));
                SetRendererDirty(rd);
            }

            ApplyMaterials();
        }

        public void PopMaterial(string materialName, MaterialTarget? target = null)
        {
            if (string.IsNullOrEmpty(materialName))
                return;

            foreach (var rd in GetTargetRendererDatas(target))
            {
                if (rd.isLocked)
                    continue;

                for (int i = rd.stack.Count - 1; i >= 0; i--)
                {
                    if (rd.stack[i].resName != materialName)
                        continue;

                    SafeReleaseMaterial(rd, rd.stack[i]);
                    rd.stack.RemoveAt(i);
                    SetRendererDirty(rd);
                    break;
                }
            }

            ApplyMaterials();
        }

        public void ApplyMaterials()
        {
            foreach (var obj in registeredObjs.Values)
            {
                if (!obj.anyRendererDirty)
                    continue;

                bool stillDirty = false;

                foreach (var rd in obj.rendererDatas)
                {
                    if (!rd.dirty)
                        continue;

                    if (rd.isLocked)
                    {
                        rd.dirty = false;
                        continue;
                    }

                    if (rd.renderer == null)
                    {
                        rd.dirty = false;
                        continue;
                    }

                    var top = GetTopPriorityMaterial(rd);

                    if (top != rd.currentTop)
                    {
                        rd.currentTop = top;

                        // 这里必须用 sharedMaterial。
                        // 使用 material 会导致 Unity 自动生成额外实例，反而绕过我们自己的池化逻辑。
                        rd.renderer.sharedMaterial = top != null ? top : rd.originalMaterial;
                    }

                    rd.dirty = false;
                }

                foreach (var rd in obj.rendererDatas)
                {
                    if (rd.dirty)
                    {
                        stillDirty = true;
                        break;
                    }
                }

                obj.anyRendererDirty = stillDirty;
            }
        }

        #endregion

        #region 查询目标

        private IEnumerable<RendererMaterialData> GetTargetRendererDatas(MaterialTarget? target)
        {
            if (!target.HasValue || target.Value.IsEmpty)
            {
                foreach (var obj in registeredObjs.Values)
                {
                    foreach (var rd in obj.rendererDatas)
                    {
                        yield return rd;
                    }
                }

                yield break;
            }

            var t = target.Value;

            if (!registeredObjs.TryGetValue(t.entityId, out var info))
                yield break;

            if (string.IsNullOrEmpty(t.rendererName))
            {
                foreach (var rd in info.rendererDatas)
                {
                    yield return rd;
                }
            }
            else
            {
                if (info.nameToData.TryGetValue(t.rendererName, out var rd))
                {
                    yield return rd;
                }
            }
        }

        private void SetRendererDirty(RendererMaterialData rd)
        {
            if (rd == null)
                return;

            rd.dirty = true;

            if (rd.owner != null)
                rd.owner.anyRendererDirty = true;
        }

        private Material GetTopPriorityMaterial(RendererMaterialData rd)
        {
            int bestPriority = int.MinValue;
            Material best = null;

            foreach (var item in rd.stack)
            {
                if (item.matInstance == null)
                    continue;

                if (item.priority > bestPriority)
                {
                    bestPriority = item.priority;
                    best = item.matInstance;
                }
            }

            return best;
        }

        #endregion

        #region 材质池

        private Material GetMaterialFromPool(string materialName)
        {
            var resMat = UnityGlobals.LoadSync<Material>(materialName);
            if (resMat == null)
            {
                Debug.LogError($"Material load failed: {materialName}");
                return null;
            }

            if (matPool.TryGetValue(materialName, out var queue) && queue.Count > 0)
            {
                var pm = queue.Dequeue();

                if (pm.mat == null)
                    return new Material(resMat);

                // 重置材质状态，避免上一次使用时改过颜色/贴图/参数后污染下一次。
                pm.mat.CopyPropertiesFromMaterial(resMat);

                return pm.mat;
            }

            return new Material(resMat);
        }

        private void SafeReleaseMaterial(RendererMaterialData rd, (Material matInstance, string resName, int priority) item)
        {
            if (item.matInstance == null)
                return;

            // 防止回收正在使用的材质。
            if (rd.currentTop == item.matInstance)
            {
                if (rd.renderer != null)
                    rd.renderer.sharedMaterial = rd.originalMaterial;

                rd.currentTop = null;
            }

            ReleaseMaterialToPool(item.resName, item.matInstance);
        }

        private void ReleaseMaterialToPool(string materialName, Material mat)
        {
            if (string.IsNullOrEmpty(materialName) || mat == null)
                return;

            if (!matPool.TryGetValue(materialName, out var queue))
            {
                queue = new Queue<PooledMaterial>();
                matPool.Add(materialName, queue);
            }

            if (queue.Count >= maxPoolPerMaterial)
            {
                Object.Destroy(mat);
            }
            else
            {
                queue.Enqueue(new PooledMaterial(mat));
            }
        }

        private void CleanUpPool()
        {
            float now = Time.time;

            foreach (var key in new List<string>(matPool.Keys))
            {
                var queue = matPool[key];
                var temp = new Queue<PooledMaterial>();

                while (queue.Count > 0)
                {
                    var item = queue.Dequeue();

                    if (item.mat == null)
                        continue;

                    if (now - item.lastUsedTime > maxIdleTime)
                        Object.Destroy(item.mat);
                    else
                        temp.Enqueue(item);
                }

                if (temp.Count > 0)
                    matPool[key] = temp;
                else
                    matPool.Remove(key);
            }
        }

        private void ClearPool()
        {
            foreach (var queue in matPool.Values)
            {
                while (queue.Count > 0)
                {
                    var item = queue.Dequeue();

                    if (item.mat != null)
                        Object.Destroy(item.mat);
                }
            }

            matPool.Clear();
        }

        #endregion

        #region 生命周期

        public void WorkWork()
        {
            lastPoolCheckTime = Time.time;
        }

        public void DoNothing()
        {
            foreach (var id in new List<EntityId>(registeredObjs.Keys))
            {
                UnregisterObject(id);
            }

            registeredObjs.Clear();
            ClearPool();
        }

        public void UpdateMGR()
        {
            ApplyMaterials();

            if (Time.time - lastPoolCheckTime >= poolCheckInterval)
            {
                CleanUpPool();
                lastPoolCheckTime = Time.time;
            }
        }

        public void LateUpdateMGR() { }

        public void FixedUpdateMGR() { }

        #endregion
    }
}
