using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace UnknownCreator.Modules
{
    public interface IControllerMgr : IDearMgr
    {
        bool hasTarget { get; }
        bool isActivated { get; }

        IHBSMController hfsm { get; }

        List<GameObject> targets { get; }

        InputActionAsset inputAsset { get; }

        IInputActionCollection2 inputClass { get; }

        void EnableController();

        void DisableController();

        void ClearController();

        void ReleaseController();

        void AddControllerTarget(GameObject target);

        void RemoveControllerTarget(GameObject target);

        void ChangeTarget(GameObject target);
        void SetInput(IInputActionCollection2 actionInput);
        void SetInput<T>() where T : IInputActionCollection2, new();
        T GetInput<T>() where T : IInputActionCollection2;
        List<GameObject> GetAllTarget();
        GameObject GetFirstTarget();
        GameObject GetTargetByIndex(int index);
        GameObject GetTargetByID(EntityId id);
        Vector3 GetControllerDir(string name);
        bool IsControllerTarget(GameObject obj);
    }
}
