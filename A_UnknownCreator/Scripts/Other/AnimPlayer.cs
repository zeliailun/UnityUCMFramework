using System;
using System.Collections.Generic;
using Animancer;
using UnityEngine;
using UnknownCreator.Modules;

namespace UnknownCreator.Modules
{
    public class AnimPlayer : IReference
    {
        public AnimancerComponent anim { get; private set; }
        public AnimTransitionAsset currentClip { get; private set; }
        public AnimancerLayer animLayer { get; private set; }
        public AnimancerState state { get; private set; }
        public bool isFadeOutLayer { get; private set; }
        public bool isRandom { get; set; }
        public Action<AnimancerEvent.Sequence> onStart { get; set; }
        public Action onEnd { get; set; }

        public bool isPlaying
        => anim == null ? false : state.IsValid() ? anim.IsPlaying(state.Key) : anim.IsPlaying(currentClip.asset);

        public bool isLayerPlaying
         => animLayer.IsValid() && animLayer.IsAnyStatePlaying() && animLayer.IsPlayingAndNotEnding();

        public bool isFadedOut
        => isFadeOutLayer && animLayer != null && animLayer.Weight == 0;

        private List<AnimTransitionAsset> clipAssets;
        private int endWeight;
        private float endFade;

        void IReference.ObjRestart()
        {
            clipAssets = new();
            isRandom = false;
            isFadeOutLayer = false;
        }

        void IReference.ObjRelease()
        {
            ClearAllAnimAssets();
            onStart = null;
            onEnd = null;
            currentClip = null;
            anim = null;
            clipAssets = null;
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
            if (!isRandom || !clipAssets.IsValid()) return;

            currentClip = RVGlobals.RandomElement(clipAssets);
        }

        public void SetAnimAsset(string key, float baseSpeed = 1, bool isSetPlayAnim = true)
        {
            if (string.IsNullOrWhiteSpace(key)) return;

            var obj = Mgr.RPool.Load<AnimTransitionAsset>();
            obj.asset = UnityGlobals.LoadSync<TransitionAsset>(key);
            obj.baseSpeed = baseSpeed;
            clipAssets.Add(obj);
            if (isSetPlayAnim) currentClip = obj;
        }

        public void SetAnimAsset(List<AnimAsset> list)
        {
            if (!list.IsValid()) return;

            for (int i = 0; i < list.Count; i++)
            {
                var obj = Mgr.RPool.Load<AnimTransitionAsset>();
                obj.asset = UnityGlobals.LoadSync<TransitionAsset>(list[i].animKey);
                obj.baseSpeed = list[i].baseSpeed;
                clipAssets.Add(obj);
            }
        }


        public AnimancerState PlayByDefaultLayer(
            AnimancerComponent anim,
            float fade = 0.25f,
            FadeMode fadeMode = default,
            float sp = 1,
            Easing.Function fadeGroup = Easing.Function.Linear)
        {
            this.anim = anim;
            state = anim.Play(currentClip.asset, fade, fadeMode);
            state.Speed = sp / currentClip.baseSpeed;
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
            state = animLayer.Play(currentClip.asset, info.startFade, info.fadeMode);
            if (state.Events(this, out AnimancerEvent.Sequence evt))
            {
                onStart?.Invoke(evt);
                if (!info.skipFadeOutLayer) evt.OnEnd += FadeOutLayer;
                evt.OnEnd += onEnd;
            }
            state.FadeGroup.SetEasing(info.fadeGroup);
            state.Speed = info.sp / currentClip.baseSpeed;
            return state;
        }

        public void FadeOutLayer()
        {
            FadeOutLayer(endFade);
        }


        public void FadeOutLayer(float endFadeV)
        {
            if (!animLayer.IsValid() || isFadeOutLayer) return;
            if (state.IsValid()) state = null;
            animLayer.StartFade(endWeight, endFadeV);
            if (endFadeV <= 0) animLayer.Stop();
            isFadeOutLayer = true;
        }


        public void ClearCurrentAnimAssets()
        {
            if (currentClip == null) return;

            if (clipAssets.Contains(currentClip))
            {
                clipAssets.Remove(currentClip);
                Mgr.RPool.Release(currentClip);
                currentClip = null;
                ClearState();
            }
        }

        public void ClearAllAnimAssets()
        {
            if (clipAssets.IsValid())
            {
                for (int i = 0; i < clipAssets.Count; i++)
                    Mgr.RPool.Release(clipAssets[i]);
                clipAssets.Clear();
            }

            ClearState();
            currentClip = null;
        }

        public void ClearState()
        {

            if (state != null)
            {
                state.Destroy();
                state = null;
            }


            if (animLayer != null)
            {
                animLayer.Stop();
                animLayer.DestroyStates();
                animLayer = null;
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
        public bool skipFadeOutLayer;
    }


    public class AnimTransitionAsset : IReference
    {
        public TransitionAsset asset;
        public float baseSpeed;

        public void ObjRelease()
        {
            if (asset != null)
            {
                UnityGlobals.Release(asset);
                asset = null;
            }
        }
    }
}
