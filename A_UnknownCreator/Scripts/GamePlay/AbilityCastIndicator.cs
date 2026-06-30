using UnityEngine;
using WalldoffStudios.Indicators;
using UnityEngine.SceneManagement;

namespace UnknownCreator.Modules
{
    /*
     indicator.Init("TargetIndicator");
     indicator.SetRealTarget(6f);
     indicator.SetColors(Color.red, new Color(1f, 0.3f, 0.3f, 1f));
     indicator.Play(unit.transform, true);

     indicator.Init("ConeIndicator");
     indicator.SetCone(10f, 75f, 30);
     indicator.Play(unit.transform, true);

     indicator.Init("LineIndicator");
     indicator.SetLine(15f, 2.5f);
     indicator.Play(unit.transform, true);

     indicator.Init("ParabolicIndicator");
     indicator.SetParabolic(20f, 8f, 1.5f, 0.04f);
     indicator.Play(unit.transform, true);
     */

    public class AbilityCastIndicator
    {
        private string name;
        private GameObject obj;
        private bool isPlaying;

        public Transform objT { get; private set; }
        public IndicatorController ic { get; private set; }

        public bool isInited => obj != null;

        /// <summary>
        /// 是否正在显示指示器。
        /// 注意：隐藏不再使用 GameObject.SetActive(false)，而是使用 ToggleAim(false) 隐藏 MeshRenderer。
        /// </summary>
        public bool isEnable => obj != null && isPlaying;

        public void Init(string name)
        {
            if (obj != null)
            {
                Destroy();
            }

            this.name = name;

            obj = Mgr.GPool.Load(name, true, false);

            if (obj == null)
            {
                UCMDebug.LogError($"AbilityCastIndicator 初始化失败，无法从对象池加载 {name}");
                ClearRefs();
                return;
            }

            objT = obj.transform;
            ic = obj.GetComp<IndicatorController>();

            if (ic == null)
            {
                UCMDebug.LogError($"AbilityCastIndicator 初始化失败，物体 {name} 上没有 IndicatorController");

                Mgr.GPool.Release(name, obj);
                ClearRefs();
                return;
            }

            SceneManager.activeSceneChanged -= OnActiveSceneChanged;
            SceneManager.activeSceneChanged += OnActiveSceneChanged;

            // 关键：
            // 不要把整个指示器 GameObject 设为 inactive。
            // TargetIndicator 的 SetTarget / OnValuesUpdated 内部可能会 StartCoroutine，
            // inactive GameObject 上 StartCoroutine 会直接报错。
            EnsureRuntimeActive();

            ResetTransform();

            // 初始化后只隐藏渲染，不关闭 GameObject。
            HideIndicator(true);
        }

        public void Destroy()
        {
            SceneManager.activeSceneChanged -= OnActiveSceneChanged;

            if (obj == null)
                return;

            // 回收前安全隐藏并重置。
            if (ic != null)
            {
                HideIndicator(true);
                ic.IndicatorResetFillAmount();
            }

            if (objT != null)
            {
                objT.SetParent(null);
                ResetTransform();
            }

            // 只有真正回收到对象池时，才允许 inactive。
            obj.SetActive(false);

            Mgr.GPool.Release(name, obj);

            ClearRefs();
        }

        /// <summary>
        /// 显示指示器，并开始填充动画。
        /// </summary>
        public void Play(Transform parent = null, bool isReset = false)
        {
            if (!EnsureReady())
                return;

            if (parent != null)
            {
                objT.SetParent(parent, false);
                ResetTransform();
            }
            else
            {
                objT.SetParent(null);
            }

            // Play = 显示 + 播放填充。
            Show(true, isReset);
        }

        /// <summary>
        /// 完整停止：隐藏 + 脱离父物体 + 重置位置 + 重置填充。
        /// </summary>
        public void Stop()
        {
            if (obj == null || ic == null)
                return;

            // Stop 也不要 SetActive(false)，否则下一次 SetTarget 又可能报 inactive coroutine。
            EnsureRuntimeActive();

            HideIndicator(true);

            if (objT != null)
            {
                objT.SetParent(null);
                ResetTransform();
            }
        }

        /// <summary>
        /// 只隐藏指示器。
        /// 不脱离父物体，不重置位置。
        /// 适合临时隐藏。
        /// </summary>
        public void Hide(bool resetFill = true)
        {
            if (obj == null || ic == null)
                return;

            EnsureRuntimeActive();
            HideIndicator(resetFill);
        }

        /// <summary>
        /// 只显示指示器。
        /// 不重新挂父物体，不重置位置。
        /// playFill = true 时会播放填充动画。
        /// resetFill = true 时会先把填充归零。
        /// </summary>
        public void Show(bool playFill = false, bool resetFill = false)
        {
            if (obj == null || ic == null)
                return;

            EnsureRuntimeActive();

            if (resetFill)
            {
                ic.IndicatorResetFillAmount();
                ic.SetFillAmount(0f);
            }

            isPlaying = true;

            // 显示渲染，同时让 Line/Cone 开始检测协程。
            ic.ToggleAim(true);

            if (playFill)
            {
                ic.ShotIndicator();
            }
        }

        public void SetPosition(Vector3 pos)
        {
            if (!EnsureReady())
                return;

            ic.SetPosition(pos);

            if (!isPlaying)
                HideIndicator(false);
        }

        public void SetFillTime(float time)
        {
            if (!EnsureReady())
                return;

            ic.SetFillTime(time);

            if (!isPlaying)
                HideIndicator(false);
        }

        public void SetCone(float range, float fov, int raycasts)
        {
            if (!EnsureReady())
                return;

            ic.SetCone(range, fov, raycasts);

            if (!isPlaying)
                HideIndicator(false);
        }

        public void SetLine(float range, float width)
        {
            if (!EnsureReady())
                return;

            ic.SetLine(range, width);

            if (!isPlaying)
                HideIndicator(false);
        }

        /// <summary>
        /// 按真实线宽设置 Line。
        /// 例如检测半径是 2，那么线宽直径就是 4。
        /// </summary>
        public void SetRealLine(float range, float width)
        {
            if (!EnsureReady())
                return;

            ic.SetLine(range, width * 2f);

            if (!isPlaying)
                HideIndicator(false);
        }

        /// <summary>
        /// 原始 Target 设置。
        /// 注意：TargetIndicator 的 RadialSize 更接近直径，不是半径。
        /// </summary>
        public void SetTarget(float radius)
        {
            if (!EnsureReady())
                return;

            ic.SetTarget(radius);

            if (!isPlaying)
                HideIndicator(false);
        }

        /// <summary>
        /// 按真实圆形半径设置 Target。
        /// 你的项目里如果 2.15f 对得更准，就保留 2.15f。
        /// 标准直径算法一般是 radius * 2f。
        /// </summary>
        public void SetRealTarget(float radius)
        {
            if (!EnsureReady())
                return;

            ic.SetTarget(radius * 2.15f);

            if (!isPlaying)
                HideIndicator(false);
        }

        public void SetParabolic(float range, float height, float width, float resolution)
        {
            if (!EnsureReady())
                return;

            ic.SetParabolic(range, height, width, resolution);

            if (!isPlaying)
                HideIndicator(false);
        }

        public void SetColors(Color mainColor, Color fillColor)
        {
            if (!EnsureReady())
                return;

            ic.SetColors(mainColor, fillColor);

            if (!isPlaying)
                HideIndicator(false);
        }

        public void SetBrightness(float brightness)
        {
            if (!EnsureReady())
                return;

            ic.SetBrightness(brightness);

            if (!isPlaying)
                HideIndicator(false);
        }

        public void SetFillSpeed(float speed)
        {
            if (!EnsureReady())
                return;

            ic.SetFillSpeed(speed);

            if (!isPlaying)
                HideIndicator(false);
        }

        public void SetUseHitDetection(bool value)
        {
            if (!EnsureReady())
                return;

            ic.SetUseHitDetection(value);

            if (!isPlaying)
                HideIndicator(false);
        }

        public void SetDrawDebug(bool value)
        {
            if (!EnsureReady())
                return;

            ic.SetDrawDebug(value);

            if (!isPlaying)
                HideIndicator(false);
        }

        /// <summary>
        /// 确保控制器可以安全刷新参数。
        /// TARGET 类型刷新时可能会启动协程，所以 GameObject 不能是 inactive。
        /// </summary>
        private bool EnsureReady()
        {
            if (obj == null || ic == null)
                return false;

            EnsureRuntimeActive();
            return true;
        }

        private void EnsureRuntimeActive()
        {
            if (obj == null)
                return;

            if (!obj.activeSelf)
            {
                obj.SetActive(true);
            }

            // 防止对象池复用后子节点或列表状态不一致。
            if (ic != null)
            {
                ic.CollectChildIndicators();
            }
        }

        private void HideIndicator(bool resetFill)
        {
            isPlaying = false;

            if (ic == null)
                return;

            // 隐藏渲染，同时让 Line/Cone 的检测协程停掉。
            // 不要 SetActive(false)，否则 TargetIndicator 下次 SetTarget 可能又报 inactive coroutine。
            ic.ToggleAim(false);

            if (resetFill)
            {
                ic.SetFillAmount(0f);
                ic.IndicatorResetFillAmount();
            }
        }

        private void ResetTransform()
        {
            if (objT == null)
                return;

            objT.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            objT.localScale = Vector3.one;
        }

        private void OnActiveSceneChanged(Scene oldScene, Scene newScene)
        {
            Stop();
        }

        private void ClearRefs()
        {
            obj = null;
            objT = null;
            ic = null;
            name = null;
            isPlaying = false;
        }
    }
}