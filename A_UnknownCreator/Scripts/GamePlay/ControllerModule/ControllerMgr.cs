using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace UnknownCreator.Modules
{
    public sealed class ControllerMgr : IControllerMgr
    {
        private IStateMachine sm;

        [SerializeField]
        private string defaultInputClass;

        [SerializeField]
        private string defaultInputAsset;

        public IHBSMController hfsm { private set; get; }

        public IInputActionCollection2 inputClass { private set; get; }

        public InputActionAsset inputAsset { private set; get; }

        public bool isActivated { private set; get; }

        public GameObject target { private set; get; }

        //private ControllerMgr() { }

        void IDearMgr.WorkWork()
        {
            hfsm = new HBSMController();
            if (!string.IsNullOrWhiteSpace(defaultInputClass))
                SetInput(ObjectUtils.CreateInstance<IInputActionCollection2>(defaultInputClass));
            if (!string.IsNullOrWhiteSpace(defaultInputClass))
                inputAsset = UnityGlobals.LoadSync<InputActionAsset>(defaultInputClass);
        }

        void IDearMgr.DoNothing()
        {
            ReleaseController();
            hfsm = null;
        }

        void IDearMgr.UpdateMGR()
        {
            if (inputClass is null) UCMDebug.LogError("没有设置自定义输入类");

            if (!isActivated || target == null) return;
            hfsm?.UpdateAllHBSM();
        }

        public void SetControllerTarget(GameObject target)
        {
            this.target = target;
        }

        public void SetInput(IInputActionCollection2 actionInput)
        {
            DestroyController();
            inputClass = actionInput;
        }

        public void SetInput<T>() where T : IInputActionCollection2, new()
        {
            DestroyController();
            inputClass = new T();
        }

        public T GetInput<T>() where T : IInputActionCollection2
        {
            return (T)inputClass;
        }

        public Vector3 GetControllerDir(string name)
        {
            if (sm is null || sm.stateName != name) sm = hfsm.GetHBSM(name);
            return ((IController)sm?.currentState)?.GetInputDir() ?? Vector3.zero;
        }

        public void ChangeTarget(GameObject target)
        {
            if (target != null && this.target != null && target.GetInstanceID() == this.target.GetInstanceID()) return;
            DisableController();
            SetControllerTarget(target);
            if (target != null) EnableController();
        }

        public void EnableController()
        {
            if (!isActivated)
            {
                inputClass.Enable();
                hfsm?.EnableAllHBSM();
                isActivated = true;
            }
        }

        public void DisableController()
        {
            if (isActivated)
            {
                isActivated = false;
                hfsm?.DisableAllHBSM();
                inputClass.Disable();
            }
        }

        public void ClearController()
        {
            DisableController();
            target = null;
        }

        public void ReleaseController()
        {
            ClearController();
            hfsm?.ReleaseAllHBSM();
            DestroyController();
        }

        public bool IsControllerTarget(GameObject target)
        => Mgr.Cntlr.target != null && target != null && target.GetInstanceID() == Mgr.Cntlr.target.GetInstanceID();

        private void DestroyController()
        {
            if (inputClass is not null &&
                inputClass is IDisposable disposable)
                disposable.Dispose();
        }
    }
}

