using Animancer;
using UnknownCreator.Modules;
using UnityEngine;
using System.Collections.Generic;
using System;
namespace UnknownCreator.Modules
{
    public class AnimPlayer : IReference
    {
        public AnimancerComponent anim { get; private set; }
        public TransitionAsset clip { get; private set; }
        public AnimancerLayer animLayer { get; private set; }
        public AnimancerState state { get; private set; }
        public float baseSpeed { get; private set; }
        public bool isInitialized { get; private set; }
        public bool isFadeOutLayer { get; private set; }
        public bool isRandom { get; set; }
        public Action<AnimancerEvent.Sequence> onStart { get; set; }
        public Action onEnd { get; set; }

        public bool isPlaying
        => anim == null ? false : state.IsValid() ? anim.IsPlaying(state.Key) : anim.IsPlaying(clip.Key);

        public bool isLayerPlaying
         => animLayer.IsValid() && animLayer.IsAnyStatePlaying() && animLayer.IsPlayingAndNotEnding();

        public bool isFadedOut
        => isFadeOutLayer && animLayer != null && animLayer.Weight == 0;

        private List<AnimAsset> clipAssets;
        private bool canReleaseAsset, canReleaseAssets;
        private int endWeight;
        private float endFade;

        void IReference.ObjRestart()
        {
            clipAssets = new();
            isRandom = false;
            isFadeOutLayer = false;
            isInitialized = false;
        }

        void IReference.ObjRelease()
        {
            ClearAnimAsset();
            ClearAnimAssets();
            ClearState();

            onStart = null;
            onEnd = null;
            clip = null;
            anim = null;
        }

        public void SetPlayAnim(string name, float baseSpeed = 1)
        {

            if (string.IsNullOrWhiteSpace(name)) return;
            ClearAnimAsset();
            canReleaseAsset = true;
            clip = UnityGlobals.LoadSync<TransitionAsset>(name);
            this.baseSpeed = baseSpeed;
        }

        public void SetPlayAnim(TransitionAsset value, bool canRelease, float baseSpeed = 1)
        {
            if (value == null) return;
            ClearAnimAsset();
            canReleaseAsset = canRelease;
            clip = value;
            this.baseSpeed = baseSpeed;
        }

        public void SetAnimList(List<AnimAsset> list, bool canRelease)
        {
            if (list == null) return;
            ClearAnimAssets();
            this.canReleaseAssets = canRelease;
            clipAssets = list;
        }

        public void SetRandomPlayAnim()
        {
            if (!isRandom || clipAssets.Count < 2) return;
            ClearAnimAsset();
            canReleaseAsset = canReleaseAssets;
            state = null;
            var result = RVUtils.RandomElement(clipAssets);
            clip = result.asset;
            baseSpeed = result.baseSpeed;
        }



        public AnimancerState PlayByDefaultLayer(
            AnimancerComponent anim,
            float fade = 0.25f,
            FadeMode fadeMode = default,
            float sp = 1,
            Easing.Function fadeGroup = Easing.Function.Linear)
        {
            this.anim = anim;
            state = anim.Play(clip, fade, fadeMode);
            state.Speed = sp / baseSpeed;
            state.FadeGroup.SetEasing(fadeGroup);
            return state;
        }


        public AnimancerState Play(AnimPlayerInfo info)
        {

            isFadeOutLayer = false;
            this.anim = info.anim;
            this.endWeight = info.endWeight;
            this.endFade = info.endFade;

            if (anim == null) return null;

            animLayer = anim.Layers[info.startLayer];
            animLayer.Mask = info.mask;
            animLayer.ApplyAnimatorIK = true;
            animLayer.ApplyFootIK = true;
            state = animLayer.Play(clip, info.startFade, info.fadeMode);
            if (state.Events(this, out AnimancerEvent.Sequence evt))
            {
                onStart?.Invoke(evt);
                if (!info.fadeOutLayer)
                    evt.OnEnd += FadeOutLayer;
                evt.OnEnd += onEnd;
            }
            state.FadeGroup.SetEasing(info.fadeGroup);
            state.Speed = info.sp / baseSpeed;
            return state;
        }

        public void FadeOutLayer()
        {
            if (!animLayer.IsValid() || isFadeOutLayer) return;
            if (state.IsValid()) state = null;
            animLayer.StartFade(endWeight, endFade);
            if (endFade <= 0)
                animLayer.Stop();
            isFadeOutLayer = true;
        }

        public void ClearAnimAsset()
        {
            if (canReleaseAsset && clip != null)
            {
                UnityGlobals.Release(clip);
                canReleaseAsset = false;
            }
            state = null;
            clip = null;
        }

        public void ClearAnimAssets()
        {
            if (clipAssets?.Count < 1) return;

            if (canReleaseAssets)
            {

                foreach (var item in clipAssets)
                {
                    UnityGlobals.Release(item.asset);
                }
                canReleaseAssets = false;
            }

            clipAssets = null;
        }

        public void ClearState()
        {

            if (animLayer != null)
            {
                animLayer.Stop();
                animLayer.DestroyStates();
                animLayer = null;
            }

            if (state != null)
            {
                state.Events(this).Clear();
                state = null;
            }
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
        public bool fadeOutLayer;
    }
}
