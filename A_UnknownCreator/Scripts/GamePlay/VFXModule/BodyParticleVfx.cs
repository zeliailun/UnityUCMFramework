using System.Collections.Generic;
using UnityEngine;

namespace UnknownCreator.Modules
{
    [DisallowMultipleComponent]
    public sealed class BodyParticleVfx : MonoBase
    {
        private const string RuntimeRootName = "[BodyVfxRuntimeEmitters]";

        private enum RootEmitterUsage
        {
            None,
            Surface,
            Anchor
        }

        private sealed class EmitterPool
        {
            public BodyVfxEmitter template;
            public ParticleSystem templateParticle;

            public readonly List<ParticleSystem> surfaceEmitters = new();
            public readonly List<ParticleSystem> anchorEmitters = new();

            public int activeSurfaceCount;
            public int activeAnchorCount;

            // 根节点单对象特效不允许复制整个 VFX 根对象，
            // 因此直接使用根节点上的 ParticleSystem。
            public bool useRootEmitterDirectly;
            public RootEmitterUsage rootUsage;

            // 根发射器每次绑定前记录当前 Transform。
            // Anchor 跟随结束后恢复到对象池 / VFX 系统设置的位置。
            public bool hasRootTransformSnapshot;
            public Vector3 rootSnapshotLocalPosition;
            public Quaternion rootSnapshotLocalRotation;
            public Vector3 rootSnapshotLocalScale;

            // 缓存模板原始粒子大小，避免根发射器直接缩放时重复累乘。
            public bool templateStartSize3D;
            public float templateStartSizeMultiplier;
            public float templateStartSizeXMultiplier;
            public float templateStartSizeYMultiplier;
            public float templateStartSizeZMultiplier;

            // 根发射器在 Surface / Anchor 之间切换时，
            // 需要恢复绑定 Renderer 前的原始 Shape 配置。
            public bool templateShapeEnabled;
            public ParticleSystemShapeType templateShapeType;
            public Vector3 templateShapePosition;
            public Vector3 templateShapeRotation;
            public Vector3 templateShapeScale;
            public ParticleSystemMeshShapeType templateMeshShapeType;
            public float templateNormalOffset;
            public MeshRenderer templateMeshRenderer;
            public SkinnedMeshRenderer templateSkinnedMeshRenderer;
        }

        private sealed class AnchorBinding
        {
            public ParticleSystem particle;
            public Transform anchor;
        }


        private readonly List<EmitterPool> emitterPools = new();
        private readonly List<AnchorBinding> anchorBindings = new();

        private Transform runtimeRoot;

        private UnitVfxTarget currentTarget;

        private float lastScaleRatio = -1f;

        private bool isInitialized;
        private bool isBound;


        public bool IsBound => isBound;


        public override void Awake()
        {
            Initialize();
        }


        /// <summary>
        /// 绑定当前特效实例到单位身体目标。
        /// 只负责配置和启用身体发射器，
        /// 不负责整个 VFX 的对象池生命周期。
        /// </summary>
        public bool Bind(UnitVfxTarget target)
        {
            Initialize();
            ResetBinding();

            if (target == null)
                return false;

            currentTarget = target;

            CaptureRootEmitterTransforms();

            bool hasBoundEmitter = false;

            for (int i = 0; i < emitterPools.Count; i++)
            {
                EmitterPool pool = emitterPools[i];

                if (pool?.template == null)
                    continue;

                bool bound = pool.template.BindType switch
                {
                    BodyVfxBindType.Auto =>
                        BindAuto(pool, target),

                    BodyVfxBindType.BodySurface =>
                        BindBodySurface(pool, target),

                    BodyVfxBindType.BodyAnchor =>
                        BindBodyAnchors(pool, target),

                    _ => false
                };

                hasBoundEmitter |= bound;
            }

            isBound = hasBoundEmitter;

            if (isBound)
                UpdateDynamicScale(true);

            return hasBoundEmitter;
        }


        /// <summary>
        /// 由 Shuriken.UpdateVfx 驱动。
        /// 更新 Anchor 跟随和身体动态缩放。
        /// </summary>
        public void UpdateBinding()
        {
            if (!isBound ||
                !gameObject.activeInHierarchy)
            {
                return;
            }

            UpdateAnchorBindings();
            UpdateDynamicScale(false);
        }


        /// <summary>
        /// Shuriken.PlayVfx 激活根节点后，
        /// 统一启动当前已绑定的身体发射器。
        /// </summary>
        public void PlayBoundEmitters()
        {
            if (!isBound)
                return;

            for (int i = 0; i < emitterPools.Count; i++)
            {
                EmitterPool pool = emitterPools[i];

                if (pool == null)
                    continue;

                if (pool.useRootEmitterDirectly)
                {
                    if (pool.rootUsage != RootEmitterUsage.None)
                    {
                        PlayEmitter(
                            pool.templateParticle,
                            false);
                    }

                    continue;
                }

                PlayEmitters(
                    pool.surfaceEmitters,
                    pool.activeSurfaceCount);

                PlayEmitters(
                    pool.anchorEmitters,
                    pool.activeAnchorCount);
            }
        }


        /// <summary>
        /// 清理当前 Unit Renderer / Anchor 的运行时引用。
        /// 发射器 GameObject 不销毁，
        /// 继续保留在当前池化 VFX 实例内部复用。
        /// </summary>
        public void ResetBinding()
        {
            Initialize();

            anchorBindings.Clear();

            for (int i = 0; i < emitterPools.Count; i++)
            {
                EmitterPool pool = emitterPools[i];

                if (pool == null)
                    continue;

                if (pool.useRootEmitterDirectly)
                {
                    ResetRootEmitter(pool);
                }
                else
                {
                    ResetSurfaceEmitters(pool);
                    ResetAnchorEmitters(pool);
                }

                pool.activeSurfaceCount = 0;
                pool.activeAnchorCount = 0;
                pool.rootUsage = RootEmitterUsage.None;
            }

            currentTarget = null;
            lastScaleRatio = -1f;

            isBound = false;
        }


        private void Initialize()
        {
            if (isInitialized)
                return;

            isInitialized = true;

            runtimeRoot = transform.Find(RuntimeRootName);

            if (runtimeRoot == null)
            {
                runtimeRoot =
                    new GameObject(RuntimeRootName).transform;

                runtimeRoot.SetParent(transform, false);
            }

            BodyVfxEmitter[] templates =
                GetComponentsInChildren<BodyVfxEmitter>(true);

            for (int i = 0; i < templates.Length; i++)
            {
                BodyVfxEmitter template = templates[i];

                if (template == null)
                    continue;

                // RuntimeEmitters 内部对象永远不能再次成为模板。
                if (template.transform.IsChildOf(runtimeRoot))
                    continue;

                bool isRootEmitter =
                    template.transform == transform;

                // 根节点发射器只允许用于真正的单对象特效。
                // RuntimeRoot 是本组件自动创建的内部节点，不算业务子物体。
                if (isRootEmitter && HasNonRuntimeChildren())
                {
                    UCMDebug.LogError(
                        $"[{nameof(BodyParticleVfx)}] {name}：" +
                        $"根节点上的 {nameof(BodyVfxEmitter)} 只支持单对象特效。" +
                        "当前根节点还包含其他子物体。" +
                        "如果特效由多个对象组成，请把身体发射粒子拆成独立叶子子物体。");

                    continue;
                }

                // 非根节点模板仍然必须是独立叶子粒子，
                // 防止复制模板时把额外子特效一起复制。
                if (!isRootEmitter &&
                    template.transform.childCount > 0)
                {
                    UCMDebug.LogError(
                        $"[{nameof(BodyParticleVfx)}] {template.name}：" +
                        $"{nameof(BodyVfxEmitter)} 所在对象必须是独立叶子节点，" +
                        "不能包含子物体。" +
                        "请把身体发射粒子拆成独立子物体。");

                    continue;
                }

                ParticleSystem templateParticle =
                    template.GetComponent<ParticleSystem>();

                if (templateParticle == null)
                {
                    UCMDebug.LogError(
                        $"[{nameof(BodyParticleVfx)}] {template.name}：" +
                        $"没有找到 {nameof(ParticleSystem)}。" +
                        $"{nameof(BodyVfxEmitter)} 必须和粒子组件挂在同一个对象上。");

                    continue;
                }

                ParticleSystem.MainModule templateMain =
                    templateParticle.main;

                ParticleSystem.ShapeModule templateShape =
                    templateParticle.shape;

                EmitterPool pool = new EmitterPool
                {
                    template = template,
                    templateParticle = templateParticle,
                    useRootEmitterDirectly = isRootEmitter,

                    hasRootTransformSnapshot = false,

                    templateStartSize3D =
                        templateMain.startSize3D,

                    templateStartSizeMultiplier =
                        templateMain.startSizeMultiplier,

                    templateStartSizeXMultiplier =
                        templateMain.startSizeXMultiplier,

                    templateStartSizeYMultiplier =
                        templateMain.startSizeYMultiplier,

                    templateStartSizeZMultiplier =
                        templateMain.startSizeZMultiplier,

                    templateShapeEnabled =
                        templateShape.enabled,

                    templateShapeType =
                        templateShape.shapeType,

                    templateShapePosition =
                        templateShape.position,

                    templateShapeRotation =
                        templateShape.rotation,

                    templateShapeScale =
                        templateShape.scale,

                    templateMeshShapeType =
                        templateShape.meshShapeType,

                    templateNormalOffset =
                        templateShape.normalOffset,

                    templateMeshRenderer =
                        templateShape.meshRenderer,

                    templateSkinnedMeshRenderer =
                        templateShape.skinnedMeshRenderer
                };

                emitterPools.Add(pool);

                if (isRootEmitter)
                {
                    // 根节点就是整个 VFX 对象，不能把 GameObject 关闭，
                    // 这里只停止未绑定时可能由 Play On Awake 启动的粒子。
                    templateParticle.Stop(
                        true,
                        ParticleSystemStopBehavior.StopEmittingAndClear);

                    templateParticle.Clear(true);
                }
                else
                {
                    // 普通子物体继续只作为模板，不直接播放。
                    template.gameObject.SetActive(false);
                }
            }
        }


        private bool HasNonRuntimeChildren()
        {
            for (int i = 0; i < transform.childCount; i++)
            {
                Transform child = transform.GetChild(i);

                if (child == runtimeRoot)
                    continue;

                return true;
            }

            return false;
        }


        private bool BindAuto(
            EmitterPool pool,
            UnitVfxTarget target)
        {
            if (target.HasRenderer &&
                BindBodySurface(pool, target))
            {
                return true;
            }

            return target.HasAnchor &&
                   BindBodyAnchors(pool, target);
        }


        private bool BindBodySurface(
            EmitterPool pool,
            UnitVfxTarget target)
        {
            if (!target.HasRenderer)
                return false;

            if (pool.useRootEmitterDirectly)
                return BindRootBodySurface(pool, target);

            bool hasBoundEmitter = false;

            for (int i = 0; i < target.RendererCount; i++)
            {
                Renderer renderer = target.GetRenderer(i);

                if (renderer is not SkinnedMeshRenderer &&
                    renderer is not MeshRenderer)
                {
                    continue;
                }

                ParticleSystem particle = GetSurfaceEmitter(
                    pool,
                    pool.activeSurfaceCount);

                if (particle == null)
                    continue;

                ResetEmitterTransform(
                    particle,
                    pool.template);

                if (!BindRenderer(
                        particle,
                        renderer,
                        pool.template))
                {
                    DisableEmitter(particle);
                    continue;
                }

                PrepareEmitter(particle, true);

                pool.activeSurfaceCount++;
                hasBoundEmitter = true;
            }

            return hasBoundEmitter;
        }


        private bool BindRootBodySurface(
            EmitterPool pool,
            UnitVfxTarget target)
        {
            ParticleSystem particle =
                pool.templateParticle;

            if (particle == null)
                return false;

            RestoreTemplateShape(pool);

            // 根节点单对象特效只有一个 ParticleSystem，
            // 因此绑定目标中的第一个有效 Renderer。
            for (int i = 0; i < target.RendererCount; i++)
            {
                Renderer renderer = target.GetRenderer(i);

                if (renderer is not SkinnedMeshRenderer &&
                    renderer is not MeshRenderer)
                {
                    continue;
                }

                if (!BindRenderer(
                        particle,
                        renderer,
                        pool.template))
                {
                    continue;
                }

                PrepareEmitter(particle, false);

                pool.activeSurfaceCount = 1;
                pool.rootUsage = RootEmitterUsage.Surface;

                return true;
            }

            return false;
        }


        private bool BindBodyAnchors(
            EmitterPool pool,
            UnitVfxTarget target)
        {
            if (!target.HasAnchor)
                return false;

            if (pool.useRootEmitterDirectly)
                return BindRootBodyAnchor(pool, target);

            bool hasBoundEmitter = false;

            for (int i = 0; i < target.AnchorCount; i++)
            {
                Transform anchor = target.GetAnchor(i);

                if (anchor == null)
                    continue;

                ParticleSystem particle = GetAnchorEmitter(
                    pool,
                    pool.activeAnchorCount);

                if (particle == null)
                    continue;

                ResetEmitterTransform(
                    particle,
                    pool.template);

                ParticleSystem.MainModule main =
                    particle.main;

                // 防止 VFX / Unit 父级缩放重复影响粒子。
                main.scalingMode =
                    ParticleSystemScalingMode.Local;

                particle.transform.SetPositionAndRotation(
                    anchor.position,
                    anchor.rotation);

                PrepareEmitter(particle, true);

                anchorBindings.Add(new AnchorBinding
                {
                    particle = particle,
                    anchor = anchor
                });

                pool.activeAnchorCount++;
                hasBoundEmitter = true;
            }

            return hasBoundEmitter;
        }


        private bool BindRootBodyAnchor(
            EmitterPool pool,
            UnitVfxTarget target)
        {
            ParticleSystem particle =
                pool.templateParticle;

            if (particle == null)
                return false;

            RestoreTemplateShape(pool);

            // 根节点单对象特效只有一个 ParticleSystem，
            // 因此绑定目标中的第一个有效 Anchor。
            for (int i = 0; i < target.AnchorCount; i++)
            {
                Transform anchor = target.GetAnchor(i);

                if (anchor == null)
                    continue;

                ParticleSystem.MainModule main =
                    particle.main;

                main.scalingMode =
                    ParticleSystemScalingMode.Local;

                particle.transform.SetPositionAndRotation(
                    anchor.position,
                    anchor.rotation);

                PrepareEmitter(particle, false);

                anchorBindings.Add(new AnchorBinding
                {
                    particle = particle,
                    anchor = anchor
                });

                pool.activeAnchorCount = 1;
                pool.rootUsage = RootEmitterUsage.Anchor;

                return true;
            }

            return false;
        }


        private void UpdateAnchorBindings()
        {
            for (int i = 0; i < anchorBindings.Count; i++)
            {
                AnchorBinding binding = anchorBindings[i];

                if (binding?.particle == null ||
                    binding.anchor == null)
                {
                    continue;
                }

                binding.particle.transform.SetPositionAndRotation(
                    binding.anchor.position,
                    binding.anchor.rotation);
            }
        }


        private ParticleSystem GetSurfaceEmitter(
            EmitterPool pool,
            int index)
        {
            return GetEmitter(
                pool,
                pool.surfaceEmitters,
                index,
                "Surface");
        }


        private ParticleSystem GetAnchorEmitter(
            EmitterPool pool,
            int index)
        {
            return GetEmitter(
                pool,
                pool.anchorEmitters,
                index,
                "Anchor");
        }


        private ParticleSystem GetEmitter(
            EmitterPool pool,
            List<ParticleSystem> emitters,
            int index,
            string typeName)
        {
            if (index < emitters.Count)
                return emitters[index];

            GameObject instance = Instantiate(
                pool.template.gameObject,
                runtimeRoot,
                false);

            instance.name =
                $"{pool.template.name}_{typeName}Emitter_{emitters.Count}";

            BodyVfxEmitter marker =
                instance.GetComponent<BodyVfxEmitter>();

            if (marker != null)
                Destroy(marker);

            ParticleSystem particle =
                instance.GetComponent<ParticleSystem>();

            if (particle == null)
            {
                Destroy(instance);
                return null;
            }

            instance.SetActive(false);

            emitters.Add(particle);

            return particle;
        }


        private bool BindRenderer(
            ParticleSystem particle,
            Renderer renderer,
            BodyVfxEmitter template)
        {
            if (particle == null ||
                renderer == null ||
                template == null)
            {
                return false;
            }

            ParticleSystem.MainModule main =
                particle.main;

            // 粒子大小由本系统显式同步目标 Scale。
            // 不允许父级 Hierarchy Scale 再次重复影响。
            main.scalingMode =
                ParticleSystemScalingMode.Local;

            ParticleSystem.ShapeModule shape =
                particle.shape;

            shape.enabled = true;

            // 原 Shape 可能来自 Sphere / Cone / Box。
            // 切换 Renderer Shape 时清理旧 Shape 参数。
            shape.position = Vector3.zero;
            shape.rotation = Vector3.zero;
            shape.scale = Vector3.one;

            shape.meshShapeType =
                template.SurfaceType;

            shape.normalOffset =
                template.NormalOffset;

            switch (renderer)
            {
                case SkinnedMeshRenderer skinned:
                    shape.shapeType =
                        ParticleSystemShapeType.SkinnedMeshRenderer;

                    shape.meshRenderer = null;
                    shape.skinnedMeshRenderer = skinned;

                    return true;


                case MeshRenderer mesh:
                    shape.shapeType =
                        ParticleSystemShapeType.MeshRenderer;

                    shape.skinnedMeshRenderer = null;
                    shape.meshRenderer = mesh;

                    return true;


                default:
                    return false;
            }
        }


        private void UpdateDynamicScale(bool force)
        {
            if (currentTarget == null)
                return;

            float targetScale =
                currentTarget.GetScaleRatio();

            if (!force &&
                Mathf.Abs(
                    targetScale - lastScaleRatio) < 0.001f)
            {
                return;
            }

            lastScaleRatio = targetScale;

            for (int i = 0; i < emitterPools.Count; i++)
            {
                EmitterPool pool = emitterPools[i];

                if (pool?.template == null ||
                    pool.templateParticle == null)
                {
                    continue;
                }

                float scale =
                    pool.template.FollowTargetScale
                        ? targetScale *
                          pool.template.ScaleMultiplier
                        : pool.template.ScaleMultiplier;

                if (pool.useRootEmitterDirectly)
                {
                    if (pool.rootUsage != RootEmitterUsage.None)
                    {
                        ApplyScaleToParticle(
                            pool.templateParticle,
                            pool,
                            scale);
                    }

                    continue;
                }

                ApplyScale(
                    pool.surfaceEmitters,
                    pool.activeSurfaceCount,
                    pool,
                    scale);

                ApplyScale(
                    pool.anchorEmitters,
                    pool.activeAnchorCount,
                    pool,
                    scale);
            }
        }


        private void ApplyScale(
            List<ParticleSystem> emitters,
            int activeCount,
            EmitterPool pool,
            float scale)
        {
            int count = Mathf.Min(
                activeCount,
                emitters.Count);

            for (int i = 0; i < count; i++)
            {
                ApplyScaleToParticle(
                    emitters[i],
                    pool,
                    scale);
            }
        }


        private void ApplyScaleToParticle(
            ParticleSystem particle,
            EmitterPool pool,
            float scale)
        {
            if (particle == null ||
                pool == null)
            {
                return;
            }

            ParticleSystem.MainModule main =
                particle.main;

            if (pool.templateStartSize3D)
            {
                main.startSizeXMultiplier =
                    pool.templateStartSizeXMultiplier *
                    scale;

                main.startSizeYMultiplier =
                    pool.templateStartSizeYMultiplier *
                    scale;

                main.startSizeZMultiplier =
                    pool.templateStartSizeZMultiplier *
                    scale;
            }
            else
            {
                main.startSizeMultiplier =
                    pool.templateStartSizeMultiplier *
                    scale;
            }
        }


        private void PrepareEmitter(
            ParticleSystem particle,
            bool activateGameObject)
        {
            if (particle == null)
                return;

            // 子发射器可以单独激活；根发射器不能在 Bind 阶段
            // 提前激活整个池化 VFX 根对象。
            if (activateGameObject &&
                !particle.gameObject.activeSelf)
            {
                particle.gameObject.SetActive(true);
            }

            particle.Stop(
                true,
                ParticleSystemStopBehavior.StopEmittingAndClear);

            particle.Clear(true);
        }


        private void PlayEmitters(
            List<ParticleSystem> emitters,
            int activeCount)
        {
            int count = Mathf.Min(
                activeCount,
                emitters.Count);

            for (int i = 0; i < count; i++)
            {
                PlayEmitter(
                    emitters[i],
                    true);
            }
        }


        private void PlayEmitter(
            ParticleSystem particle,
            bool activateGameObject)
        {
            if (particle == null)
                return;

            if (activateGameObject &&
                !particle.gameObject.activeSelf)
            {
                particle.gameObject.SetActive(true);
            }

            particle.Clear(true);
            particle.Play(true);
        }


        private void ResetRootEmitter(
            EmitterPool pool)
        {
            if (pool?.templateParticle == null)
                return;

            ParticleSystem particle =
                pool.templateParticle;

            particle.Stop(
                true,
                ParticleSystemStopBehavior.StopEmittingAndClear);

            particle.Clear(true);

            RestoreTemplateShape(pool);
            RestoreRootEmitterTransform(pool);
        }


        private void RestoreTemplateShape(
            EmitterPool pool)
        {
            if (pool?.templateParticle == null)
                return;

            ParticleSystem.ShapeModule shape =
                pool.templateParticle.shape;

            shape.enabled =
                pool.templateShapeEnabled;

            shape.shapeType =
                pool.templateShapeType;

            shape.position =
                pool.templateShapePosition;

            shape.rotation =
                pool.templateShapeRotation;

            shape.scale =
                pool.templateShapeScale;

            shape.meshShapeType =
                pool.templateMeshShapeType;

            shape.normalOffset =
                pool.templateNormalOffset;

            shape.meshRenderer =
                pool.templateMeshRenderer;

            shape.skinnedMeshRenderer =
                pool.templateSkinnedMeshRenderer;
        }


        private void CaptureRootEmitterTransforms()
        {
            for (int i = 0; i < emitterPools.Count; i++)
            {
                EmitterPool pool = emitterPools[i];

                if (pool?.templateParticle == null ||
                    !pool.useRootEmitterDirectly)
                {
                    continue;
                }

                Transform particleT =
                    pool.templateParticle.transform;

                pool.rootSnapshotLocalPosition =
                    particleT.localPosition;

                pool.rootSnapshotLocalRotation =
                    particleT.localRotation;

                pool.rootSnapshotLocalScale =
                    particleT.localScale;

                pool.hasRootTransformSnapshot = true;
            }
        }


        private void RestoreRootEmitterTransform(
            EmitterPool pool)
        {
            if (pool?.templateParticle == null ||
                !pool.hasRootTransformSnapshot)
            {
                return;
            }

            Transform particleT =
                pool.templateParticle.transform;

            // 根对象的父级由 VFX 对象池管理，不能在这里改父级。
            particleT.localPosition =
                pool.rootSnapshotLocalPosition;

            particleT.localRotation =
                pool.rootSnapshotLocalRotation;

            particleT.localScale =
                pool.rootSnapshotLocalScale;

            pool.hasRootTransformSnapshot = false;
        }


        private void ResetSurfaceEmitters(
            EmitterPool pool)
        {
            for (int i = 0;
                 i < pool.surfaceEmitters.Count;
                 i++)
            {
                ParticleSystem particle =
                    pool.surfaceEmitters[i];

                if (particle == null)
                    continue;

                ParticleSystem.ShapeModule shape =
                    particle.shape;

                shape.meshRenderer = null;
                shape.skinnedMeshRenderer = null;

                DisableEmitter(particle);

                ResetEmitterTransform(
                    particle,
                    pool.template);
            }
        }


        private void ResetAnchorEmitters(
            EmitterPool pool)
        {
            for (int i = 0;
                 i < pool.anchorEmitters.Count;
                 i++)
            {
                ParticleSystem particle =
                    pool.anchorEmitters[i];

                if (particle == null)
                    continue;

                DisableEmitter(particle);

                ResetEmitterTransform(
                    particle,
                    pool.template);
            }
        }


        private void ResetEmitterTransform(
            ParticleSystem particle,
            BodyVfxEmitter template)
        {
            if (particle == null ||
                template == null ||
                runtimeRoot == null)
            {
                return;
            }

            Transform particleT = particle.transform;

            particleT.SetParent(runtimeRoot, false);

            particleT.localPosition = Vector3.zero;
            particleT.localRotation = Quaternion.identity;

            // 保留原粒子模板自己的 Scale。
            particleT.localScale =
                template.transform.localScale;
        }


        private void DisableEmitter(
            ParticleSystem particle)
        {
            if (particle == null)
                return;

            particle.Stop(
                true,
                ParticleSystemStopBehavior.StopEmittingAndClear);

            particle.Clear(true);
            particle.gameObject.SetActive(false);
        }
    }
}