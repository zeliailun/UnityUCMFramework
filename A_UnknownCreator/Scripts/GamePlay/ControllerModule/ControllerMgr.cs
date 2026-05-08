using System;
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

        public Dictionary<EntityId, GameObject> targetDict { private set; get; }
        public List<GameObject> targets { private set; get; }

        public IReadOnlyList<GameObject> allTarget => targets;

        public bool hasTarget => targetDict != null && targetDict.Count > 0;

        void IDearMgr.WorkWork()
        {
            hfsm = new HBSMController();
            targets = new();
            targetDict = new();
            if (!string.IsNullOrWhiteSpace(defaultInputClass))
                SetInput(ObjectGlobals.CreateInstance<IInputActionCollection2>(defaultInputClass));
            if (!string.IsNullOrWhiteSpace(defaultInputAsset))
                inputAsset = UnityGlobals.LoadSync<InputActionAsset>(defaultInputAsset);
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
            bool wasActivated = isActivated;

            if (wasActivated)
                DisableController();

            DestroyController();
            inputClass = actionInput;

            if (wasActivated)
                EnableController();
        }

        public void SetInput<T>() where T : IInputActionCollection2, new()
        {
            SetInput(new T());
        }

        public T GetInput<T>() where T : IInputActionCollection2
        {
            return (T)inputClass;
        }



        public void AddControllerTarget(GameObject target)
        {
            if (target == null) return;

            var id = target.GetEntityId();
            if (targetDict.TryGetValue(id, out _)) return;

            targetDict[id] = target;
            targets.Add(target);

        }

        public void RemoveControllerTarget(GameObject target)
        {
            if (target == null) return;

            var id = target.GetEntityId();
            if (targetDict.Remove(id, out _))
                targets.Remove(target);
        }

        public GameObject GetFirstTarget()
        => targets.IsValid() ? targets[0] : null;

        public GameObject GetTargetByIndex(int index)
        {
            if (targets == null || index < 0 || index >= targets.Count)
                return null;

            return targets[index];
        }

        public GameObject GetTargetByID(EntityId id)
        {
            if (targetDict.TryGetValue(id, out var target))
            {
                return target;
            }
            return null;
        }

        public Vector3 GetControllerDir(string name)
        {
            if (hfsm == null || string.IsNullOrWhiteSpace(name))
                return Vector3.zero;

            if (sm == null || sm.stateName != name)
                sm = hfsm.GetHBSM(name);

            if (sm?.currentState is IController controller)
                return controller.GetInputDir();

            return Vector3.zero;
        }

        public void SetControllerTargets(GameObject target)
        {
            if (target == null || targetDict.TryGetValue(target.GetEntityId(), out _)) return;

            DisableController();
            AddControllerTarget(target);
            EnableController();
        }

        public void EnableController()
        {
            if (isActivated) return;

            if (inputClass == null)
            {
                UCMDebug.LogError("ControllerMgr 启用失败：inputClass 为空");
                return;
            }

            inputClass.Enable();
            hfsm?.EnableAllHBSM();
            isActivated = true;
        }


        public void DisableController()
        {
            if (isActivated)
            {
                isActivated = false;
                hfsm?.DisableAllHBSM();
                inputClass?.Disable();
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
        => target != null && targetDict.TryGetValue(target.GetEntityId(), out _);

        private void DestroyController()
        {
            if (inputClass is IDisposable disposable)
                disposable.Dispose();

            inputClass = null;
        }
    }
}
