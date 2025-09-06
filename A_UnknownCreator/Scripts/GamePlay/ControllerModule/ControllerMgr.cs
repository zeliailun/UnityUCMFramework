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

        public Dictionary<int, GameObject> targetDict { private set; get; }
        public List<GameObject> targets { private set; get; }

        public bool hasTarget => targetDict.Count > 0;

        void IDearMgr.WorkWork()
        {
            hfsm = new HBSMController();
            targets = new();
            targetDict = new();
            if (!string.IsNullOrWhiteSpace(defaultInputClass))
                SetInput(ObjectGlobals.CreateInstance<IInputActionCollection2>(defaultInputClass));
            if (!string.IsNullOrWhiteSpace(defaultInputClass))
                inputAsset = UnityGlobals.LoadSync<InputActionAsset>(defaultInputClass);
        }

        void IDearMgr.DoNothing()
        {
            ReleaseController();
            hfsm = null;
            targets = null;
            targetDict = null;
        }

        void IDearMgr.UpdateMGR()
        {
            if (inputClass is null) UCMDebug.LogError("没有设置自定义输入类");

            if (!isActivated || !targets.IsValid()) return;
            hfsm?.UpdateAllHBSM();
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



        public void AddControllerTarget(GameObject target)
        {
            if (target == null) return;

            var id = target.GetInstanceID();
            if (targetDict.TryGetValue(id, out _)) return;

            targetDict[id] = target;
            targets.Add(target);
     
        }

        public void RemoveControllerTarget(GameObject target)
        {
            if (target == null) return;

            var id = target.GetInstanceID();
            if (targetDict.Remove(id, out _))
                targets.Remove(target);
        }

        public GameObject GetFirstTarget()
        => targets.IsValid() ? targets[0] : null;

        public GameObject GetTargetByIndex(int index)
        {
            if (targets.IsValid() && index < targets.Count && index >= 0)
            {
                return targets[0];
            }
            return null;
        }

        public GameObject GetTargetByID(int id)
        {
            if (targetDict.TryGetValue(id, out var target))
            {
                return target;
            }
            return null;
        }

        public List<GameObject> GetAllTarget()
        {
            return targets;
        }


        public Vector3 GetControllerDir(string name)
        {
            if (sm is null || sm.stateName != name) sm = hfsm.GetHBSM(name);
            return ((IController)sm?.currentState)?.GetInputDir() ?? Vector3.zero;
        }

        public void ChangeTarget(GameObject target)
        {
            if (target == null || targetDict.TryGetValue(target.GetInstanceID(), out _)) return;

            DisableController();
            AddControllerTarget(target);
            EnableController();
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
            targetDict.Clear();
            targets.Clear();
        }

        public void ReleaseController()
        {
            ClearController();
            hfsm?.ReleaseAllHBSM();
            DestroyController();
        }

        public bool IsControllerTarget(GameObject target)
        => target != null && targetDict.TryGetValue(target.GetInstanceID(), out _);

        private void DestroyController()
        {
            if (inputClass is not null &&
                inputClass is IDisposable disposable)
                disposable.Dispose();
        }
    }
}
