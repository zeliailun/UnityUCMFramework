using UnityEngine;
using System;

namespace UnknownCreator.Modules
{
    [Serializable]
    public class SoundCfg
    {
        [JsonMark]
        [SerializeField]
        internal SoundClipInfo[] soundArray;

        [field: SerializeField]
        public bool isApplyTimeScale { private set; get; } = true;

        [field: SerializeField]
        public int playCount { private set; get; } = 10;

        [field: SerializeField]
        public string mixerGroup { private set; get; }

        [field: SerializeField]
        public bool isRandomPlay { private set; get; }

        [field: SerializeField]
        public bool bypassEffects { private set; get; }

        [field: SerializeField]
        public bool bypassListenerEffects { private set; get; }

        [field: SerializeField]
        public bool bypassReverbZones { private set; get; }

        [field: SerializeField]
        public bool isLoop { private set; get; }

        [field: SerializeField]
        public int priority { private set; get; } = 128;

        [field: SerializeField]
        public float volume { private set; get; } = 1F;

        [field: SerializeField]
        public float pitch { private set; get; } = 1F;

        [field: SerializeField]
        public float stereoPan { private set; get; } = 0F;

        [field: SerializeField]
        public float spatialBlend { private set; get; } = 0F;

        [field: SerializeField]
        public float reverbZoneMix { private set; get; } = 1F;

        [field: SerializeField]
        public float dopplerLevel { private set; get; } = 1F;

        [field: SerializeField]
        public float spread { private set; get; } = 0F;

        [field: SerializeField]
        public float minDistance { private set; get; } = 0.1F;

        [field: SerializeField]
        public float maxDistance { private set; get; } = 50F;

        [field: SerializeField]
        public AudioRolloffMode rolloffMode { private set; get; } = AudioRolloffMode.Logarithmic;

        [field: SerializeField]
        public AnimationCurve customRolloffCurve { private set; get; }

        [field: SerializeField]
        public AnimationCurve spatialBlendCurve { private set; get; }

        [field: SerializeField]
        public AnimationCurve reverbZoneMixCurve { private set; get; }

        [field: SerializeField]
        public AnimationCurve spreadCurve { private set; get; }
    }

    [Serializable]
    public class SoundClipInfo
    {
        [field: SerializeField]
        public string clip { internal set; get; }

        [field: SerializeField]
        public bool isUseCustomVolume { internal set; get; }

        [field: SerializeField]
        public float volume { internal set; get; }
    }
}