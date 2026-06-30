using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace UnknownCreator.Modules
{
    public interface IControllerMgr : IDearMgr
    {
        bool hasTarget { get; }
        bool isActivated { get; }

        IReadOnlyList<GameObject> allTarget { get; }

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

        void SetControllerTargets(GameObject target);
        T GetInput<T>() where T : IInputActionCollection2;
        GameObject GetFirstTarget();
        GameObject GetTargetByIndex(int index);
        GameObject GetTargetByID(EntityId id);
        Vector3 GetControllerDir(string name);
        bool IsControllerTarget(GameObject obj);
    }
}
