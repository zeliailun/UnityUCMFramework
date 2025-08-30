using UnityEngine.UIElements;
using System;
using UnityEngine.SceneManagement;

namespace UnknownCreator.Modules
{
    public class UITKOpenHideView : IReference
    {
        private UITKOpenInfo showInfo = new();
        private UITKHideInfo hideInfo = new();
        private ITimer startTween, endTween, delayTween;
        private VisualElement view;
        private Action<float> changeOpacity;
        private Action<TimerTween> finalOpacity;
        private Action<bool, float, TimerTween<bool>> changeOpacity2;
        private Action<bool, TimerTween<bool>> hideView;
        private Action<TimerCountCycle> autoHideView;

        public void Init(VisualElement view)
        {
            this.view = view;
            view.style.display = DisplayStyle.None;
            view.style.opacity = 0;
            changeOpacity = SetOpacity;
            changeOpacity2 = SetOpacity;
            finalOpacity = ApplyFinalOpacity;
            hideView = HideView;
            autoHideView = AutoHideHandle;
            Mgr.Event.Add(Reset, CustomGameEvents.OnBackMainMenu, -1, CustomEvtOrder.order1);
            SceneManager.activeSceneChanged += OnSceneChanged;
        }

        public void ObjRelease()
        {
            SceneManager.activeSceneChanged -= OnSceneChanged;
            Mgr.Event.Remove(Reset, CustomGameEvents.OnBackMainMenu, -1);
            Reset();
            showInfo = default;
            hideInfo = default;
            changeOpacity = null;
            finalOpacity = null;
            hideView = null;
            autoHideView = null;
            changeOpacity = null;
            changeOpacity2 = null;
            view = null;
        }

        public void Show(UITKOpenInfo info)
        {
            DestroyTween();
            showInfo = info;

            SetOpacity(0);
            view.style.display = DisplayStyle.Flex;

            showInfo.onShow?.Invoke(view);
            showInfo.onShow = null;

            if (showInfo.startDuration <= 0)
                ApplyFinalOpacity(null);
            else
                startTween = Mgr.Timer.Custom(0F, 1F, showInfo.startDuration, 1, false, changeOpacity, finalOpacity, EaseTypes.Linear, showInfo.isTimeScale);

            if (showInfo.isAutoHide)
            {
                if (showInfo.delay <= 0)
                    AutoHideHandle(null);
                else
                    delayTween = Mgr.Timer.CycleCount(1, showInfo.delay + showInfo.startDuration, false, autoHideView, null, showInfo.isTimeScale);
            }
        }

        public void Hide(UITKHideInfo info)
        {
            DestroyTween();
            hideInfo = info;
            float currentValue = view.style.opacity.value;
            if (info.hideDuration <= 0)
                HideView(false, null);
            else
                endTween = Mgr.Timer.Custom<bool>(false, currentValue, 0F, info.hideDuration, 1, false, changeOpacity2, hideView, EaseTypes.Linear, info.isTimeScale);
        }

        private void AutoHideHandle(TimerCountCycle cycle)
        {
            if (showInfo.endDuration <= 0)
                HideView(true, null);
            else
                endTween = Mgr.Timer.Custom<bool>(true, 1F, 0F, showInfo.endDuration, 1, false, changeOpacity2, hideView, EaseTypes.Linear, showInfo.isTimeScale);
        }

        private void HideView(bool isAuto, TimerTween tt)
        {
            if (isAuto)
            {
                showInfo.onAutoHide?.Invoke(view);
                showInfo.onAutoHide = null;
            }
            else
            {
                hideInfo.onHide?.Invoke(view);
                hideInfo.onHide = null;
            }
            SetOpacity(0);
            view.style.display = DisplayStyle.None;
            UITKMgr.OnUIHide?.Invoke(hideInfo.uidName, hideInfo.uiName);
        }

        private void OnSceneChanged(Scene oldScene, Scene newScene)
        {
            if (showInfo.isChangeSceneHide)
                Reset();
        }

        private void DestroyTween()
        {
            startTween.DestroySelf();
            startTween = null;
            delayTween.DestroySelf();
            delayTween = null;
            endTween.DestroySelf();
            endTween = null;
        }

        private void Reset()
        {
            DestroyTween();
            SetOpacity(0);
            view.style.display = DisplayStyle.None;
        }

        private void SetOpacity(float value)
        {
            view.style.opacity = value;
        }

        private void SetOpacity(bool b,float v, TimerTween<bool> tt)
        {
            SetOpacity(v);
        }

        private void ApplyFinalOpacity(TimerTween tt)
        {
            SetOpacity(1);
            UITKMgr.OnUIOpen?.Invoke(showInfo.uidName, showInfo.uiName);
        }
    }
}