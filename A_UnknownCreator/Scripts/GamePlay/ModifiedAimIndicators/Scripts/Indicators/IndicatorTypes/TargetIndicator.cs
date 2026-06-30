using System.Collections;
using UnityEngine;

namespace WalldoffStudios.Indicators
{
    public class TargetIndicator : IndicatorBase
    {
        private Vector4[] vertPoints;
        private static readonly int VertPoints = Shader.PropertyToID("_VertPoints");

        private float cachedRadialSize;
        private Coroutine meshUpdatingRoutine;

        // 物体 inactive 时收到尺寸刷新，先记下来，等激活后再执行
        private bool pendingSmoothUpdate;

        protected override void SetIndicatorType() => IndicatorType = IndicatorType.TARGET;

        protected override void Start()
        {
            base.Start();

            CreateMesh();

            // 初始化时不要开协程，直接同步到当前半径，避免 inactive/初始化顺序问题
            cachedRadialSize = settings.RadialSize;
            UpdateRadialSizeImmediate(cachedRadialSize);

            ToggleAimRenderer(settings.AlwaysDisplayIndicator);
        }

        private void OnEnable()
        {
            // inactive 时如果收到过刷新请求，激活后再补一次
            if (!pendingSmoothUpdate)
                return;

            pendingSmoothUpdate = false;
            StartMeshUpdateSafe();
        }

        private void OnDisable()
        {
            // 物体禁用时停止协程，避免对象池复用时协程状态残留
            if (meshUpdatingRoutine != null)
            {
                StopCoroutine(meshUpdatingRoutine);
                meshUpdatingRoutine = null;
            }
        }

        private void CreateMesh()
        {
            meshFilter.mesh = MeshGenerator.CreateTargetMesh(settings.RadialSize, Is2D);
        }

        protected override void RebuildMesh()
        {
            CreateMesh();

            cachedRadialSize = settings.RadialSize;
            UpdateRadialSizeImmediate(cachedRadialSize);
        }

        public override void OnValuesUpdated()
        {
            base.OnValuesUpdated();
            StartMeshUpdateSafe();
        }

        private void StartMeshUpdateSafe()
        {
            // StartCoroutine 不能在 inactive GameObject 上调用
            // 所以 inactive 时直接同步数据，不启动协程
            if (!isActiveAndEnabled || !gameObject.activeInHierarchy)
            {
                pendingSmoothUpdate = true;

                cachedRadialSize = settings.RadialSize;
                UpdateRadialSizeImmediate(cachedRadialSize);
                return;
            }

            if (meshUpdatingRoutine != null)
            {
                StopCoroutine(meshUpdatingRoutine);
            }

            meshUpdatingRoutine = StartCoroutine(UpdateMeshRoutine());
        }

        private IEnumerator UpdateMeshRoutine()
        {
            while (Mathf.Abs(cachedRadialSize - settings.RadialSize) > 0.01f)
            {
                cachedRadialSize = Mathf.Lerp(
                    cachedRadialSize,
                    settings.RadialSize,
                    settings.LerpTime
                );

                UpdateRadialSizeImmediate(cachedRadialSize);
                yield return null;
            }

            cachedRadialSize = settings.RadialSize;
            UpdateRadialSizeImmediate(cachedRadialSize);

            meshUpdatingRoutine = null;
        }

        private void UpdateRadialSizeImmediate(float size)
        {
            if (Is2D)
            {
                vertPoints = new Vector4[4]
                {
                    new Vector4(-0.5f, -0.5f, 0.0f, 0.0f) * size,
                    new Vector4( 0.5f, -0.5f, 0.0f, 0.0f) * size,
                    new Vector4(-0.5f,  0.5f, 0.0f, 0.0f) * size,
                    new Vector4( 0.5f,  0.5f, 0.0f, 0.0f) * size
                };
            }
            else
            {
                vertPoints = new Vector4[4]
                {
                    new Vector4(-0.5f, 0.0f, -0.5f, 0.0f) * size,
                    new Vector4( 0.5f, 0.0f, -0.5f, 0.0f) * size,
                    new Vector4(-0.5f, 0.0f,  0.5f, 0.0f) * size,
                    new Vector4( 0.5f, 0.0f,  0.5f, 0.0f) * size
                };
            }

            UpdateVertices();
        }

        private void UpdateVertices()
        {
            if (meshRenderer == null || matPropertyBlock == null || vertPoints == null)
                return;

            meshRenderer.GetPropertyBlock(matPropertyBlock);
            matPropertyBlock.SetVectorArray(VertPoints, vertPoints);
            meshRenderer.SetPropertyBlock(matPropertyBlock);
        }

        public override void ToggleAim(bool toggle)
        {
            ToggleAimRenderer(toggle);
        }
    }
}