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
        private string cameraRootName = "CustomCmaeraRoot";

        //private CameraMgr() { }

        void IDearMgr.WorkWork()
        {
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

        public void SetCameraTarget(GameObject target)
        {
            this.target = target;
        }

        public void ChangeTarget(GameObject target)
        {
            if (target != null && this.target != null && target.GetInstanceID() == this.target.GetInstanceID()) return;
            DisableAllCamera();
            SetCameraTarget(target);
            if (target != null) EnableAllCamera();
        }

        public void CretaeMainCamera(string mainCameraName)
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
                hfsm.EnableAllHBSM();
            }
        }

        public void DisableAllCamera()
        {
            if (cameraCache != null && cameraCache.activeSelf)
            {
                hfsm.DisableAllHBSM();
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
                Mgr.GPool.Release(mainCameraName, cameraCache);
                hfsm = null;
                brain = null;
                target = null;
                mainCam = null;
                mainCamT = null;
                cameraCache = null;
            }

            if (cameraRootCache != null)
            {
                Mgr.GPool.Release(cameraRootName, cameraRootCache);
                cameraRootT = null;
                cameraRootCache = null;
            }
        }
    }
}