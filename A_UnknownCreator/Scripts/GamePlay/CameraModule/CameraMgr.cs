using Unity.Cinemachine;
using UnityEngine;

namespace UnknownCreator.Modules
{
    public sealed class CameraMgr : ICameraMgr
    {
        public IHBSMController hfsm { private set; get; }

        public GameObject target { private set; get; }

        public Camera mainCam { private set; get; }

        public Transform mainCamT { private set; get; }

        public CinemachineBrain brain { private set; get; }

        public Transform cameraRootT { private set; get; }

        private GameObject cameraRootCache;
        private GameObject cameraCache;
        private string mainCameraName;
        private string cameraRootName;

        //private CameraMgr() { }

        void IDearMgr.WorkWork()
        {
            cameraRootName = "CustomCameraRoot";
            hfsm ??= new HBSMController();
        }

        void IDearMgr.DoNothing()
        {
            ReleaseAllCamera();
            hfsm = null;
        }

        void IDearMgr.UpdateMGR()
        {
            hfsm?.UpdateAllHBSM();
        }

        void IDearMgr.LateUpdateMGR()
        {
            hfsm?.LateUpdateAllHBSM();
        }

        void IDearMgr.FixedUpdateMGR()
        {
            hfsm?.FixedUpdateAllHBSM();
        }

        public void ChangeTarget(GameObject newTarget, bool isReactivate = true)
        {
            // 目标相同，不替换
            if (newTarget != null && target != null && newTarget.GetEntityId() == target.GetEntityId())
            {
                return;
            }

            // 新目标为空：直接清空目标并关闭相机，无视 isReactivate
            if (newTarget == null)
            {
                target = null;
                DisableAllCamera();
                return;
            }

            // 需要重新激活相机：先关闭，再换目标，再开启
            if (isReactivate)
            {
                DisableAllCamera();
                target = newTarget;
                EnableAllCamera();
                return;
            }

            // 不重新激活相机：只替换目标，然后刷新状态机
            target = newTarget;
            hfsm?.RefreshAllHBSM();
        }

        public void CreateMainCamera(string mainCameraName)
        {
            if (cameraCache != null)
            {
                UCMDebug.LogWarning("已有主相机");
                return;
            }
            this.mainCameraName = mainCameraName;
            cameraRootCache = Mgr.GPool.GetNewGameObject(cameraRootName).Item1;
            cameraRootT = cameraRootCache.GetComponent<Transform>();
            cameraCache = Mgr.GPool.Load(mainCameraName, true, false);
            brain = cameraCache.GetComp<CinemachineBrain>();
            mainCam = cameraCache.GetComp<Camera>();
            mainCamT = cameraCache.GetComp<Transform>();
            mainCamT.SetParent(cameraRootT);
            DisableAllCamera();


        }

        public void EnableAllCamera()
        {
            if (cameraCache != null && !cameraCache.activeSelf)
            {
                cameraCache.SetActive(true);
                hfsm?.EnableAllHBSM();
            }
        }

        public void DisableAllCamera()
        {
            if (cameraCache != null && cameraCache.activeSelf)
            {
                hfsm?.DisableAllHBSM();
                cameraCache.SetActive(false);
            }
        }

        public void ClearAllCamera()
        {
            DisableAllCamera();
            target = null;
        }

        public void ReleaseAllCamera()
        {
            if (cameraCache != null)
            {
                hfsm.ReleaseAllHBSM();
                Mgr.GPool.Release(mainCameraName, cameraCache, false);
                hfsm = null;
                brain = null;
                target = null;
                mainCam = null;
                mainCamT = null;
                cameraCache = null;
                mainCameraName = null;
            }

            if (cameraRootCache != null)
            {
                Mgr.GPool.Release(cameraRootName, cameraRootCache, false);
                cameraRootT = null;
                cameraRootCache = null;
            }
        }
    }
}