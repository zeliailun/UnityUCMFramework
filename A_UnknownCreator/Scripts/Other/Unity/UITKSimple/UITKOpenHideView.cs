using System;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace UnknownCreator.Modules
{
    public class UITKOpenHideView : IReference
    {
        private UITKBuilder builder;
        private UITKOpenInfo showInfo = new();
        private UITKHideInfo hideInfo = new();
        private ITimer startTween, endTween, delayTween;
        private VisualElement view;
        private Action<float> changeOpacity;
        private Action<TimerTween> finalOpacity;
        private Action<bool, float, TimerTween<bool>> changeOpacity2;
        private Action<bool, TimerTween<bool>> hideView;
        private Action<TimerCountCycle> autoHideView;
        private bool showOrHide;


        public void Init(UITKBuilder builder)
        {
            this.builder = builder;
            changeOpacity = SetOpacity;
            changeOpacity2 = SetOpacity;
            finalOpacity = ApplyFinalOpacity;
            hideView = HideView;
            autoHideView = AutoHideHandle;
            Mgr.Event.Add(Reset, CGE.OnBackMainMenu, default, CustomEvtOrder.order1);
            SceneManager.activeSceneChanged += OnSceneChanged;
            UITKMgr.OnUIReload += Refresh;
        }


        public void ObjRelease()
        {
            UITKMgr.OnUIReload -= Refresh;
            SceneManager.activeSceneChanged -= OnSceneChanged;
            Mgr.Event.Remove(Reset, CGE.OnBackMainMenu, default);
            Reset();
            changeOpacity = null;
            finalOpacity = null;
            hideView = null;
            autoHideView = null;
            changeOpacity = null;
            changeOpacity2 = null;
            view = null;
        }

        private void Refresh(UITKBuilder ub)
        {
            if(builder.idName== ub.idName)
            {
                if (showOrHide)
                    view = ub.root.Q<VisualElement>(showInfo.uiName);
                else
                    view = ub.root.Q<VisualElement>(hideInfo.uiName);
            }
        }

        public void Show(UITKOpenInfo info)
        {
            //  if (EqualityComparer<UITKOpenInfo>.Default.Equals(info, default)) return;

            DestroyTween();
            
            showOrHide = true;
            showInfo = info;
            view = builder.root.Q<VisualElement>(showInfo.uiName);
   
            SetOpacity(0);
            view.style.display = DisplayStyle.Flex;

            showInfo.onShow?.Invoke(view);
            //  showInfo.onShow = null;

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
            // if (EqualityComparer<UITKHideInfo>.Default.Equals(info, default)) return;
            DestroyTween();

            showOrHide = false;
            hideInfo = info;
            view = builder.root.Q<VisualElement>(hideInfo.uiName);

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
                //  showInfo.onAutoHide = null;
            }
            else
            {
                hideInfo.onHide?.Invoke(view);
                //  hideInfo.onHide = null;
            }
            SetOpacity(0);
            view.style.display = DisplayStyle.None;
            UITKMgr.OnUIHide?.Invoke(hideInfo.uidName, hideInfo.uiName);
            showInfo = default;
            hideInfo = default;
        }

        private void OnSceneChanged(Scene oldScene, Scene newScene)
        {
            if (showInfo.isChangeSceneHide)
            {
                showInfo.onChangeScene?.Invoke(oldScene, newScene, view);
                Reset();
            }
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
            showInfo = default;
            hideInfo = default;
        }

        private void SetOpacity(float value)
        {
            view.style.opacity = value;
        }

        private void SetOpacity(bool b, float v, TimerTween<bool> tt)
        {
            SetOpacity(v);
        }

        private void ApplyFinalOpacity(TimerTween tt)
        {
            SetOpacity(1);
            UITKMgr.OnUIOpen?.Invoke(showInfo.prName, showInfo.uiName);
        }
    }
}