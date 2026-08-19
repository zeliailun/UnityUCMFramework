using System;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace UnknownCreator.Modules
{
    public class UITKOpenHideView : IReference
    {
        private UITKBuilder builder;
        private UITKOpenInfo showInfo = new();
        private UITKHideInfo hideInfo = new();
        private long endTween, startTween, delayTween;
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
            SceneManager.sceneLoaded += OnSceneChanged;
            UITKMgr.OnUIReload += Refresh;
        }


        public void ObjRelease()
        {
            DestroyTween();
            UITKMgr.OnUIReload -= Refresh;
            SceneManager.sceneLoaded -= OnSceneChanged;
            showInfo = default;
            hideInfo = default;
            changeOpacity = null;
            finalOpacity = null;
            hideView = null;
            autoHideView = null;
            changeOpacity = null;
            changeOpacity2 = null;
            view = null;
            builder = null;
        }

        private void Refresh(UITKBuilder ub)
        {
            if (builder.idName == ub.idName)
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
                startTween = Mgr.Timer.CustomID(0F, 1F, showInfo.startDuration, 1, false, changeOpacity, finalOpacity, EaseTypes.Linear, showInfo.isTimeScale);

            if (showInfo.isAutoHide)
            {
                if (showInfo.delay <= 0)
                    AutoHideHandle(null);
                else
                    delayTween = Mgr.Timer.CycleCountID(1, showInfo.delay + showInfo.startDuration, false, autoHideView, null, showInfo.isTimeScale);
            }
        }

        public void Hide(UITKHideInfo info)
        {
            // if (EqualityComparer<UITKHideInfo>.Default.Equals(info, default)) return;
            DestroyTween();

            showOrHide = false;
            hideInfo = info;
            view = builder.root.Q<VisualElement>(hideInfo.uiName);

            float currentValue = view.resolvedStyle.opacity;
            if (info.hideDuration <= 0)
                HideView(false, null);
            else
                endTween = Mgr.Timer.CustomID<bool>(false, currentValue, 0F, info.hideDuration, 1, false, changeOpacity2, hideView, EaseTypes.Linear, info.isTimeScale);
        }

        private void AutoHideHandle(TimerCountCycle cycle)
        {
            if (showInfo.endDuration <= 0)
                HideView(true, null);
            else
                endTween = Mgr.Timer.CustomID<bool>(true, 1F, 0F, showInfo.endDuration, 1, false, changeOpacity2, hideView, EaseTypes.Linear, showInfo.isTimeScale);
        }

        private void HideView(bool isAuto, TimerTween tt)
        {
            SetOpacity(0);
            view.style.display = DisplayStyle.None;

            if (isAuto)
            {
                showInfo.onAutoHide?.Invoke(view);
                UITKMgr.OnUIHide?.Invoke(showInfo.prName, showInfo.uiName);
            }
            else
            {
                hideInfo.onHide?.Invoke(view);
                UITKMgr.OnUIHide?.Invoke(hideInfo.prName, hideInfo.uiName);
            }

            showInfo = default;
            hideInfo = default;
        }

        private void OnSceneChanged(Scene scene, LoadSceneMode mode)
        {
            if (showInfo.isChangeSceneHide)
            {
                showInfo.onChangeScene?.Invoke(scene, view);
                DestroyTween();
                SetOpacity(0);
                showInfo = default;
                hideInfo = default;
            }
        }

        private void DestroyTween()
        {
            startTween.DestroySelf();
            delayTween.DestroySelf();
            endTween.DestroySelf();
        }

        private void SetOpacity(float value)
        {

            if (view != null && view.panel != null)
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
