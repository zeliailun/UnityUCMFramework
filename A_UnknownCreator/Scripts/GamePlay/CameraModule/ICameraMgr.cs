using UnityEngine;
using Unity.Cinemachine;
namespace UnknownCreator.Modules
{
    public interface ICameraMgr : IDearMgr
    {
        IHBSMController hfsm { get; }
        GameObject target { get; }
        CinemachineBrain brain { get; }
        Camera mainCam { get; }
        Transform mainCamT { get; }
        Transform cameraRootT { get; }
        void CretaeMainCamera(string mainCameraName);
        void EnableAllCamera();
        void DisableAllCamera();
        void ClearAllCamera();
        void ReleaseAllCamera();
        void SetCameraTarget(GameObject target);
        void ChangeTarget(GameObject target);
    }
}