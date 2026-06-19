using System;
using UnityEngine;

namespace WalldoffStudios.Indicators
{
    public class IndicatorSettings : MonoBehaviour
    {



        public IndicatorType IndicatorType;
        
        [SerializeField] private bool alwaysDisplayIndicator = false;
        [SerializeField] private Transform endPointTarget;
        [SerializeField] private Texture2D mainTex;
        [SerializeField] private bool renderEdges = true;
        [SerializeField] private Texture2D edgeTex;
        [SerializeField] private float range = 40.0f;
        [SerializeField] private float edgePadding = 0.5f;
        [SerializeField] private float radialSize = 2.2f;
        [SerializeField] private float fov = 60.0f;
        [SerializeField] private int raycasts = 45;
        [SerializeField] private float timeBetweenRaycasts;
        [SerializeField] private float minDistanceForUpdate = 0.001f;
        [SerializeField] private float lerpTime = 0.1f;
        [SerializeField] private LayerMask obstacleMask;
        [SerializeField] private bool useHitDetection = true;
        [SerializeField] private bool drawDebug = true;
        [SerializeField] private float height = 30.0f;
        [SerializeField] private float meshWidth = 4.0f;
        [SerializeField] private float resolution = 0.02f;
        [SerializeField] private float distortion = 0.017f;
        [SerializeField] private float offset = 1.0f;
        [SerializeField] private float brightness = 1.0f;
        [SerializeField] private Color mainColor = Color.white;
        [SerializeField] private bool useFillEffect = true;
        [SerializeField] private Color fillColor = Color.cyan;
        [SerializeField] private float fillSpeed = 1f;


        public bool AlwaysDisplayIndicator => alwaysDisplayIndicator;
        public Transform EndPointTarget => endPointTarget;
        public Texture2D MainTex
        {
            get => mainTex;
            set => mainTex = value;
        }

        public bool RenderEdges => renderEdges;

        public Texture2D EdgeTex
        {
            get => edgeTex;
            set => edgeTex = value;
        }

        public float Range => range;
        public float EdgePadding => edgePadding;
        public float RadialSize => radialSize;
        public float FOV => fov;
        public int Raycasts => raycasts;
        public float TimeBetweenRaycasts => timeBetweenRaycasts;
        public float MinDistanceForUpdate => minDistanceForUpdate;
        public float LerpTime => lerpTime;
        public LayerMask ObstacleMask => obstacleMask;
        public bool UseHitDetection => useHitDetection;
        public bool DrawDebug => drawDebug;
        public float Height => height;
        public float MeshWidth => meshWidth;
        public float Resolution => resolution;
        public float Distortion => distortion;
        public float Offset => offset;
        public float Brightness => brightness;
        public Color MainColor => mainColor;
        public bool UseFillEffect => useFillEffect;
        public Color FillColor => fillColor;
        public float FillSpeed { get => fillSpeed; set => fillSpeed = value; }


        private IndicatorBase indicator;
        private bool isBatching;
        private bool needMaterialRefresh;
        private bool needMeshRebuild;


        private void Awake()
        {
            indicator = GetComponent<IndicatorBase>();

            if (endPointTarget == null && IndicatorType == IndicatorType.PARABOLIC)
            {
                throw new SystemException($"endPointTarget is null on gameObject {gameObject.name}");
            }
        }


        #region 运行时刷新控制

        public void BeginRuntimeUpdate()
        {
            isBatching = true;
            needMaterialRefresh = false;
            needMeshRebuild = false;
        }

        public void EndRuntimeUpdate()
        {
            isBatching = false;

            if (indicator == null)
            {
                indicator = GetComponent<IndicatorBase>();
            }

            if (needMeshRebuild)
            {
                indicator?.RefreshIndicator(true);
            }
            else if (needMaterialRefresh)
            {
                indicator?.RefreshIndicator(false);
            }

            needMaterialRefresh = false;
            needMeshRebuild = false;
        }

        private void RequestRefresh(bool rebuildMesh)
        {
            if (isBatching)
            {
                if (rebuildMesh)
                {
                    needMeshRebuild = true;
                }
                else
                {
                    needMaterialRefresh = true;
                }

                return;
            }

            if (indicator == null)
            {
                indicator = GetComponent<IndicatorBase>();
            }

            indicator?.RefreshIndicator(rebuildMesh);
        }

        private static bool SameFloat(float a, float b)
        {
            return Mathf.Abs(a - b) < 0.0001f;
        }

        private bool NeedRebuildWhenRangeChanged()
        {
            return IndicatorType == IndicatorType.LINE ||
                   IndicatorType == IndicatorType.PARABOLIC;
        }

        private bool NeedRebuildWhenMeshWidthChanged()
        {
            return IndicatorType == IndicatorType.LINE ||
                   IndicatorType == IndicatorType.PARABOLIC;
        }

        #endregion


        #region 运行时修改 - 显示参数

        public void SetMainColor(Color value)
        {
            if (mainColor == value)
                return;

            mainColor = value;
            RequestRefresh(false);
        }

        public void SetFillColor(Color value)
        {
            if (fillColor == value)
                return;

            fillColor = value;
            RequestRefresh(false);
        }

        public void SetColors(Color main, Color fill)
        {
            bool changed = mainColor != main || fillColor != fill;

            if (!changed)
                return;

            mainColor = main;
            fillColor = fill;

            RequestRefresh(false);
        }

        public void SetBrightness(float value)
        {
            value = Mathf.Max(0f, value);

            if (SameFloat(brightness, value))
                return;

            brightness = value;
            RequestRefresh(false);
        }

        public void SetMainTexture(Texture2D value)
        {
            if (mainTex == value)
                return;

            mainTex = value;

            if (indicator == null)
            {
                indicator = GetComponent<IndicatorBase>();
            }

            indicator?.SetMaterial(value);
        }

        public void SetEdgeTexture(Texture2D value)
        {
            if (edgeTex == value)
                return;

            edgeTex = value;
            RequestRefresh(false);
        }

        public void SetRenderEdges(bool value)
        {
            if (renderEdges == value)
                return;

            renderEdges = value;
            RequestRefresh(false);
        }

        public void SetDistortion(float value)
        {
            value = Mathf.Max(0f, value);

            if (SameFloat(distortion, value))
                return;

            distortion = value;
            RequestRefresh(false);
        }

        public void SetFillSpeed(float value)
        {
            fillSpeed = Mathf.Max(0.01f, value);
        }

        public void SetUseFillEffect(bool value)
        {
            if (useFillEffect == value)
                return;

            useFillEffect = value;
            RequestRefresh(false);
        }

        #endregion


        #region 运行时修改 - 形状参数

        public void SetRange(float value)
        {
            value = Mathf.Max(0.01f, value);

            if (SameFloat(range, value))
                return;

            range = value;
            RequestRefresh(NeedRebuildWhenRangeChanged());
        }

        public void SetEdgePadding(float value)
        {
            value = Mathf.Max(0f, value);

            if (SameFloat(edgePadding, value))
                return;

            edgePadding = value;

            // 只影响 Line 的射线检测宽度，不需要重建 Mesh
        }

        public void SetRadialSize(float value)
        {
            value = Mathf.Max(0.01f, value);

            if (SameFloat(radialSize, value))
                return;

            radialSize = value;

            // TargetIndicator 自己会在 OnValuesUpdated 里开协程平滑更新
            RequestRefresh(false);
        }

        public void SetFOV(float value)
        {
            value = Mathf.Clamp(value, 1f, 360f);

            if (SameFloat(fov, value))
                return;

            fov = value;

            // 只有 Cone 用 FOV
            RequestRefresh(IndicatorType == IndicatorType.CONE);
        }

        public void SetRaycasts(int value)
        {
            value = Mathf.Max(1, value);

            if (raycasts == value)
                return;

            raycasts = value;

            // Cone 的 lengths 数组长度依赖 Raycasts，所以 Cone 必须重建
            RequestRefresh(IndicatorType == IndicatorType.CONE);
        }

        public void SetHeight(float value)
        {
            value = Mathf.Max(0.01f, value);

            if (SameFloat(height, value))
                return;

            height = value;

            // 只有 Parabolic 用 Height 生成路径
            RequestRefresh(IndicatorType == IndicatorType.PARABOLIC);
        }

        public void SetMeshWidth(float value)
        {
            value = Mathf.Max(0.01f, value);

            if (SameFloat(meshWidth, value))
                return;

            meshWidth = value;

            RequestRefresh(NeedRebuildWhenMeshWidthChanged());
        }

        public void SetResolution(float value)
        {
            value = Mathf.Max(0.001f, value);

            if (SameFloat(resolution, value))
                return;

            resolution = value;

            // 抛物线点数量依赖 Resolution
            RequestRefresh(IndicatorType == IndicatorType.PARABOLIC);
        }

        public void SetOffset(float value)
        {
            if (SameFloat(offset, value))
                return;

            offset = value;

            // 只影响命中距离修正，不需要重建 Mesh
        }

        #endregion



        #region 运行时修改 - 检测参数

        public void SetTimeBetweenRaycasts(float value)
        {
            value = Mathf.Max(0f, value);

            if (SameFloat(timeBetweenRaycasts, value))
                return;

            timeBetweenRaycasts = value;
        }

        public void SetMinDistanceForUpdate(float value)
        {
            value = Mathf.Max(0f, value);

            if (SameFloat(minDistanceForUpdate, value))
                return;

            minDistanceForUpdate = value;
        }

        public void SetLerpTime(float value)
        {
            value = Mathf.Clamp01(value);

            if (SameFloat(lerpTime, value))
                return;

            lerpTime = value;
        }

        public void SetObstacleMask(LayerMask value)
        {
            obstacleMask = value;
        }

        public void SetUseHitDetection(bool value)
        {
            useHitDetection = value;
        }

        public void SetDrawDebug(bool value)
        {
            drawDebug = value;
        }

        public void SetEndPointTarget(Transform value)
        {
            if (endPointTarget == value)
                return;

            endPointTarget = value;

            if (IndicatorType == IndicatorType.PARABOLIC)
            {
                RequestRefresh(true);
            }
        }

        public void SetAlwaysDisplayIndicator(bool value)
        {
            alwaysDisplayIndicator = value;
        }

        #endregion


    }
}