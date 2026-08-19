using System;
using System.Collections.Generic;
using Animancer;
using UnityEngine;
using UnknownCreator.Modules;

public class AnimPlayer : IReference
{
    private const float FadeOutWeightEpsilon = 0.0001f;

    public AnimancerComponent anim { get; private set; }
    public AnimTransitionAsset currentClip { get; private set; }
    public AnimancerLayer animLayer { get; private set; }
    public AnimancerState state { get; private set; }

    public bool isFadeOutLayer { get; private set; }
    public bool isRandom { get; set; }

    public Action<AnimancerEvent.Sequence> onStart { get; set; }
    public Action onEnd { get; set; }

    public bool isPlaying
    {
        get
        {
            if (anim == null)
                return false;

            if (state != null && state.IsValid())
                return state.IsPlaying;

            return currentClip?.asset != null &&
                   anim.IsPlaying(currentClip.asset);
        }
    }

    public bool isLayerPlaying =>
        animLayer != null &&
        animLayer.IsValid() &&
        animLayer.IsAnyStatePlaying() &&
        animLayer.IsPlayingAndNotEnding();

    public bool isFadedOut =>
        isFadeOutLayer &&
        (animLayer == null ||
         !animLayer.IsValid() ||
         animLayer.Weight <= FadeOutWeightEpsilon);


    private List<AnimTransitionAsset> clipAssets;
    private HashSet<AnimancerState> ownedStates;
    private AnimTransitionAsset playedDefaultClip;

    private int endWeight;
    private float endFade;

    void IReference.ObjRestart()
    {
        clipAssets = new List<AnimTransitionAsset>();
        ownedStates = new HashSet<AnimancerState>();

        anim = null;
        currentClip = null;
        animLayer = null;
        state = null;
        playedDefaultClip = null;

        onStart = null;
        onEnd = null;

        isRandom = false;
        isFadeOutLayer = false;

        endWeight = 0;
        endFade = 0;
    }

    void IReference.ObjRelease()
    {
        // 先取消外部回调，再销毁状态，避免对象池复用后旧回调残留。
        onStart = null;
        onEnd = null;

        // 必须先销毁 AnimancerState，再释放 TransitionAsset。
        ClearAllAnimAssets();

        anim = null;
        currentClip = null;
        animLayer = null;
        state = null;
        playedDefaultClip = null;

        clipAssets = null;
        ownedStates = null;

        isRandom = false;
        isFadeOutLayer = false;

        endWeight = 0;
        endFade = 0;
    }

    public void SetPlayAnim(int index = -1)
    {
        if (!clipAssets.IsValid())
            return;

        int targetIndex = index < 0
            ? clipAssets.Count - 1
            : Mathf.Clamp(index, 0, clipAssets.Count - 1);

        currentClip = clipAssets[targetIndex];
    }

    public void SetRandomPlayAnim()
    {
        if (!isRandom || !clipAssets.IsValid())
            return;

        currentClip = RVGlobals.RandomElement(clipAssets);
    }

    public void SetAnimAsset(
        string key,
        float baseSpeed = 1f,
        bool isSetPlayAnim = true)
    {
        if (string.IsNullOrWhiteSpace(key))
            return;

        clipAssets ??= new List<AnimTransitionAsset>();

        TransitionAsset transitionAsset =
            UnityGlobals.LoadSync<TransitionAsset>(key);

        if (transitionAsset == null)
            return;

        AnimTransitionAsset obj =
            Mgr.RPool.Load<AnimTransitionAsset>();

        obj.asset = transitionAsset;
        obj.baseSpeed = NormalizeBaseSpeed(baseSpeed);

        clipAssets.Add(obj);

        if (isSetPlayAnim)
            currentClip = obj;
    }

    public void SetAnimAsset(List<AnimAsset> list)
    {
        if (!list.IsValid())
            return;

        clipAssets ??= new List<AnimTransitionAsset>();

        for (int i = 0; i < list.Count; i++)
        {
            AnimAsset animAsset = list[i];

            if (string.IsNullOrWhiteSpace(animAsset.animKey))
                continue;

            TransitionAsset transitionAsset =
                UnityGlobals.LoadSync<TransitionAsset>(animAsset.animKey);

            if (transitionAsset == null)
                continue;

            AnimTransitionAsset obj =
                Mgr.RPool.Load<AnimTransitionAsset>();

            obj.asset = transitionAsset;
            obj.baseSpeed = NormalizeBaseSpeed(animAsset.baseSpeed);

            clipAssets.Add(obj);
        }
    }

    public AnimancerState PlayByDefaultLayer(
        AnimancerComponent anim,
        float fade = 0.25f,
        FadeMode fadeMode = default,
        float sp = 1f,
        Easing.Function fadeGroup = Easing.Function.Linear)
    {
        if (!CanPlay(anim))
            return null;

        // 移动和待机状态会逐帧调用本方法。同一动画仍是默认层当前状态时，
        // 只同步播放速度，避免反复发起相同的 Play 和淡入操作。
        if (ReferenceEquals(this.anim, anim) &&
            ReferenceEquals(playedDefaultClip, currentClip) &&
            state != null &&
            state.IsValid() &&
            state.IsPlaying &&
            ReferenceEquals(anim.Layers[0].CurrentState, state))
        {
            state.Speed = GetPlaybackSpeed(sp);
            return state;
        }

        isFadeOutLayer = false;
        this.anim = anim;

        state = anim.Play(currentClip.asset, fade, fadeMode);
        playedDefaultClip = currentClip;
        TrackState(state);

        if (state.FadeGroup != null)
            state.FadeGroup.SetEasing(fadeGroup);

        state.Speed = GetPlaybackSpeed(sp);

        return state;
    }

    public AnimancerState Play(AnimPlayerInfo info)
    {
        if (!CanPlay(info.anim))
            return null;

        isFadeOutLayer = false;
        playedDefaultClip = null;

        anim = info.anim;
        endWeight = info.endWeight;
        endFade = info.endFade;

        animLayer = anim.Layers[info.startLayer];
        animLayer.Mask = info.mask;
        if (!animLayer.ApplyAnimatorIK)
            animLayer.ApplyAnimatorIK = true;
        if (!animLayer.ApplyFootIK)
            animLayer.ApplyFootIK = true;

        state = animLayer.Play(
            currentClip.asset,
            info.startFade,
            info.fadeMode);

        TrackState(state);

        /*
         * Events(this, ...) 会把当前 AnimPlayer 作为事件拥有者。
         * ClearState 会销毁本播放器用过的全部 State，
         * 因此下一个 AnimPlayer 不会再次撞上旧 Owner。
         */
        if (state.Events(this, out AnimancerEvent.Sequence evt))
        {
            // 防止同一个状态被当前 AnimPlayer 重播时重复叠加结束回调。
            evt.OnEnd = null;

            onStart?.Invoke(evt);

            if (!info.skipFadeOutLayer)
                evt.OnEnd += FadeOutLayer;

            if (onEnd != null)
                evt.OnEnd += onEnd;
        }

        if (state.FadeGroup != null)
            state.FadeGroup.SetEasing(info.fadeGroup);

        state.Speed = GetPlaybackSpeed(info.sp);

        return state;
    }

    public void FadeOutLayer()
    {
        FadeOutLayer(endFade);
    }

    public void FadeOutLayer(float endFadeV)
    {
        if (animLayer == null ||
            !animLayer.IsValid() ||
            isFadeOutLayer)
        {
            return;
        }

        animLayer.StartFade(endWeight, endFadeV);

        if (endFadeV <= 0f)
            animLayer.Stop();

        isFadeOutLayer = true;
    }

    public void ClearCurrentAnimAssets()
    {
        if (currentClip == null)
            return;

        AnimTransitionAsset targetClip = currentClip;

        // State 仍然引用 TransitionAsset，所以必须先清理 State。
        ClearState();

        if (clipAssets != null && clipAssets.Remove(targetClip))
            Mgr.RPool.Release(targetClip);

        currentClip = null;
    }

    public void ClearAllAnimAssets()
    {
        // 旧版本只销毁最后一个 state，SitDown 会残留在 Graph 中。
        // 现在会销毁这个 AnimPlayer 播放过的全部状态。
        ClearState();

        if (clipAssets.IsValid())
        {
            for (int i = 0; i < clipAssets.Count; i++)
            {
                AnimTransitionAsset clipAsset = clipAssets[i];

                if (clipAsset != null)
                    Mgr.RPool.Release(clipAsset);
            }

            clipAssets.Clear();
        }

        currentClip = null;
    }

    public void ClearState()
    {
        if (ownedStates != null && ownedStates.Count > 0)
        {
            // 先复制并清空集合，避免 Destroy 期间发生重入。
            var states = new List<AnimancerState>(ownedStates);
            ownedStates.Clear();

            for (int i = 0; i < states.Count; i++)
            {
                AnimancerState ownedState = states[i];

                if (ownedState != null &&
                    ownedState.IsValid() &&
                    ownedState.Graph.IsValidOrDispose())
                {
                    ownedState.Destroy();
                }
            }
        }
        else if (state != null &&
                 state.IsValid() &&
                 state.Graph.IsValidOrDispose())
        {
            // 兜底：防止某个状态因为异常流程没有加入 ownedStates。
            state.Destroy();
        }

        state = null;
        animLayer = null;
        playedDefaultClip = null;
        isFadeOutLayer = false;
    }

    private bool CanPlay(AnimancerComponent targetAnim)
    {
        return targetAnim != null &&
               currentClip != null &&
               currentClip.asset != null;
    }

    private void TrackState(AnimancerState targetState)
    {
        if (targetState == null || !targetState.IsValid())
            return;

        ownedStates ??= new HashSet<AnimancerState>();
        ownedStates.Add(targetState);
    }

    private float GetPlaybackSpeed(float speed)
    {
        return speed / NormalizeBaseSpeed(currentClip.baseSpeed);
    }

    private static float NormalizeBaseSpeed(float speed)
    {
        return Mathf.Approximately(speed, 0f) ? 1f : speed;
    }
}

public struct AnimPlayerInfo
{
    public AnimancerComponent anim;
    public float startFade;
    public float endFade;
    public int startLayer;
    public int endWeight;
    public float sp;
    public AvatarMask mask;
    public FadeMode fadeMode;
    public Easing.Function fadeGroup;
    public bool skipFadeOutLayer;
}

public class AnimTransitionAsset : IReference
{
    public TransitionAsset asset;
    public float baseSpeed;

    public void ObjRestart()
    {
        asset = null;
        baseSpeed = 1f;
    }

    public void ObjRelease()
    {
        if (asset != null)
        {
            UnityGlobals.Release(asset);
            asset = null;
        }

        baseSpeed = 1f;
    }
}
