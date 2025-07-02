using UnityEngine;
using UnityEngine.InputSystem;

namespace UnknownCreator.Modules
{
    public interface IControllerMgr : IDearMgr
    {
        bool isActivated { get; }

        IHBSMController hfsm { get; }

        GameObject target { get; }

        InputActionAsset inputAsset { get; }

        IInputActionCollection2 inputClass { get; }

        void EnableController();

        void DisableController();

        void ClearController();

        void ReleaseController();

        void SetControllerTarget(GameObject target);
        void ChangeTarget(GameObject target);
        void SetInput(IInputActionCollection2 actionInput);
        void SetInput<T>() where T : IInputActionCollection2, new();
        T GetInput<T>() where T : IInputActionCollection2;
        Vector3 GetControllerDir(string name);
        bool IsControllerTarget(GameObject obj);
    }
}
