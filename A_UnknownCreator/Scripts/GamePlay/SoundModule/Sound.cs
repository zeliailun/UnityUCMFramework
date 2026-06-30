using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnknownCreator.Modules
{
    public sealed class Sound : ISound, IReference
    {
        public SoundCfg soundCfg { private set; get; }

        public AudioSource source { private set; get; }

        public GameObject soundObj { private set; get; }

        public Transform soundT { private set; get; }

        public EntityId id { private set; get; }

        public string soundName { private set; get; }

        public string groupName { set; get; }

        public bool isPlayEndUnload { set; get; }

        public bool isRelease { private set; get; }

        private int index;
        private int currentSoundNum;
        private float time;
        private float fadeDuration;
        private float playVolume;
        private float currentVolume;
        private bool isFadingOut;
        private bool isMainSoundCounted;
        private AudioClip[] clips;
        private readonly List<long> ids = new();
        private readonly Dictionary<long, float> oneShotVolumeDict = new();
        private EvtSoundPlayEnd soundEvt;
        private TimerHandle<TimerCountCycle> soundEndTimer;

        public void Init(string name, GameObject go)
        {
            soundName = name;
            soundObj = go;
            id = soundObj.GetEntityId();
            soundT = soundObj.GetComponent<Transform>();
            source = soundObj.GetComponent<AudioSource>();
            source ??= soundObj.AddComponent<AudioSource>();

            soundCfg = GetSoundCfg(soundName);
            CheckSoundCfg(soundName, soundCfg);

            ApplyAudioSourceCfg();
            LoadClips();

            currentSoundNum = 0;
            playVolume = soundCfg.volume;
            soundEvt = new();
            groupName = string.Empty;
            isPlayEndUnload = false;
            isRelease = false;
            isFadingOut = false;
            isMainSoundCounted = false;
            time = 0;
            fadeDuration = 0;
            ids.Clear();
            oneShotVolumeDict.Clear();
            soundEndTimer.Destroy();
        }

        public void PlaySound(bool isOneShot)
        {
            if (isRelease || source == null || soundCfg == null || clips == null || clips.Length == 0) return;
            if (source.isPlaying && !isOneShot) return;
            if (Mgr.Sound.CurrentSoundPlayCount(soundName) >= soundCfg.playCount) return;

            if (isFadingOut)
            {
                isFadingOut = false;
                time = 0;
            }

            if (soundCfg.isRandomPlay)
            {
                index = RVGlobals.RandomInt(0, clips.Length, false);
            }
            else
            {
                index = currentSoundNum;
                currentSoundNum = (currentSoundNum + 1) % clips.Length;
            }

            var clip = clips[index];
            if (clip == null) return;

            var info = soundCfg.soundArray[index];
            playVolume = info.isUseCustomVolume ? info.volume : soundCfg.volume;
            source.volume = playVolume;
            source.clip = clip;

            Mgr.Sound.IncreaseSoundPlayCount(soundName);

            if (!soundObj.activeSelf)
                soundObj.SetActive(true);

            if (isOneShot)
            {
                source.PlayOneShot(clip, 1);
                var timer = Mgr.Timer.CycleCountHandle(1, clip.length, false, SoundEndEvt, SoundCompleted, soundCfg.isApplyTimeScale);
                ids.Add(timer.idValue);
                oneShotVolumeDict[timer.idValue] = playVolume;
            }
            else
            {
                isMainSoundCounted = true;
                source.Play();

                if (soundCfg.isLoop)
                {
                    ClearMainEndTimer();
                }
                else
                {
                    ResetMainEndTimer(clip.length);
                }
            }
        }

        public void PauseSound()
        {
            if (isRelease || source == null || !source.isPlaying) return;
            source.Pause();
        }

        public void ResumeSound()
        {
            if (isRelease || source == null || source.isPlaying) return;
            source.UnPause();
        }

        public void StopSound()
        {
            if (isRelease || source == null) return;

            bool hasAnyPlayingSound = isMainSoundCounted || ids.Count > 0 || source.isPlaying;

            isFadingOut = false;
            time = 0;

            source.Stop();
            ClearMainEndTimer();
            ClearOneShotTimers(true);

            if (isMainSoundCounted)
            {
                isMainSoundCounted = false;
                Mgr.Sound.DecreaseSoundPlayCount(soundName);
            }

            if (!hasAnyPlayingSound) return;

            SendSoundEndEvent(playVolume);

            if (isPlayEndUnload)
                Mgr.Sound.UnloadSound(this);
        }

        public void StopSound(float fadeDuration)
        {
            if (isRelease || source == null || !source.isPlaying || isFadingOut) return;

            if (fadeDuration <= 0f)
            {
                StopSound();
                return;
            }

            isFadingOut = true;
            currentVolume = source.volume;
            this.fadeDuration = fadeDuration;
            time = 0;
        }

        public void MuteSound(bool isMute)
        {
            if (isRelease || source == null) return;
            source.mute = isMute;
        }

        public void UpdateSound()
        {
            FadeSound();
        }

        public bool HasGroup()
        => !string.IsNullOrWhiteSpace(groupName);

        private static SoundCfg GetSoundCfg(string soundName)
        {
            var dict = Mgr.JD.GetData<Dictionary<string, SoundCfg>>(JsonCfgKeyGlobals.SoundJson);
            if (dict == null || !dict.TryGetValue(soundName, out var cfg) || cfg == null)
                throw new Exception($"声音配置不存在：{soundName}");

            return cfg;
        }

        private static void CheckSoundCfg(string soundName, SoundCfg cfg)
        {
            if (cfg.soundArray == null || cfg.soundArray.Length == 0)
                throw new Exception($"声音配置没有可播放的 AudioClip：{soundName}");
        }

        private void ApplyAudioSourceCfg()
        {
            source.mute = false;
            source.playOnAwake = false;
            source.outputAudioMixerGroup = null;

            if (Mgr.Sound.mixer != null && !string.IsNullOrWhiteSpace(soundCfg.mixerGroup))
            {
                var groups = Mgr.Sound.mixer.FindMatchingGroups(soundCfg.mixerGroup);
                if (groups != null && groups.Length > 0)
                    source.outputAudioMixerGroup = groups[0];
            }

            source.bypassEffects = soundCfg.bypassEffects;
            source.bypassListenerEffects = soundCfg.bypassListenerEffects;
            source.bypassReverbZones = soundCfg.bypassReverbZones;
            source.loop = soundCfg.isLoop;
            source.priority = soundCfg.priority;
            source.pitch = soundCfg.pitch;
            source.panStereo = soundCfg.stereoPan;
            source.spatialBlend = soundCfg.spatialBlend;
            source.reverbZoneMix = soundCfg.reverbZoneMix;
            source.spread = soundCfg.spread;
            source.dopplerLevel = soundCfg.dopplerLevel;
            source.maxDistance = soundCfg.maxDistance;
            source.minDistance = soundCfg.minDistance;
            source.rolloffMode = soundCfg.rolloffMode;

            if (source.rolloffMode == AudioRolloffMode.Custom)
            {
                SetCustomCurveIfNotNull(AudioSourceCurveType.Spread, soundCfg.spreadCurve);
                SetCustomCurveIfNotNull(AudioSourceCurveType.SpatialBlend, soundCfg.spatialBlendCurve);
                SetCustomCurveIfNotNull(AudioSourceCurveType.ReverbZoneMix, soundCfg.reverbZoneMixCurve);
                SetCustomCurveIfNotNull(AudioSourceCurveType.CustomRolloff, soundCfg.customRolloffCurve);
            }
        }

        private void SetCustomCurveIfNotNull(AudioSourceCurveType curveType, AnimationCurve curve)
        {
            if (curve != null)
                source.SetCustomCurve(curveType, curve);
        }

        private void LoadClips()
        {
            clips = new AudioClip[soundCfg.soundArray.Length];
            for (int i = 0; i < soundCfg.soundArray.Length; i++)
                clips[i] = UnityGlobals.LoadSync<AudioClip>(soundCfg.soundArray[i].clip);
        }

        private void FadeSound()
        {
            if (isRelease || source == null || !source.isPlaying || !isFadingOut) return;

            if (fadeDuration <= 0f)
            {
                StopSound();
                return;
            }

            time += CustomTime.DeltaTime();
            float progress = Mathf.Clamp01(time / fadeDuration);
            source.volume = Mathf.Lerp(currentVolume, 0, progress);

            if (progress >= 1f)
            {
                time = 0;
                isFadingOut = false;
                StopSound();
            }
        }

        private void SoundEndEvt(TimerCountCycle cycle)
        {
            if (isRelease) return;

            if (cycle != null && ids.Contains(cycle.id))
            {
                float eventVolume = oneShotVolumeDict.TryGetValue(cycle.id, out var volume) ? volume : playVolume;
                Mgr.Sound.DecreaseSoundPlayCount(soundName);
                SendSoundEndEvent(eventVolume);

                if (isPlayEndUnload)
                    Mgr.Sound.UnloadSound(this);

                return;
            }

            if (!isMainSoundCounted) return;

            isMainSoundCounted = false;
            Mgr.Sound.DecreaseSoundPlayCount(soundName);
            SendSoundEndEvent(playVolume);

            if (isPlayEndUnload)
                Mgr.Sound.UnloadSound(this);
        }

        private void SoundCompleted(TimerCountCycle countCycle)
        {
            if (countCycle == null) return;

            if (ids.Remove(countCycle.id))
            {
                oneShotVolumeDict.Remove(countCycle.id);
                Mgr.Timer.RemoveTimer(countCycle.id);
            }
        }

        private void SendSoundEndEvent(float volume)
        {
            soundEvt.volume = volume;
            soundEvt.name = soundName;
            soundEvt.position = soundT != null ? soundT.position : Vector3.zero;
            Mgr.Event.Send(soundEvt, SoundGlobals.OnSoundPlayEnd);
        }

        private void ResetMainEndTimer(float delay)
        {
            if (!soundEndTimer.isValid)
            {
                soundEndTimer = Mgr.Timer.CycleCountHandle(1, delay, false, SoundEndEvt, null, soundCfg.isApplyTimeScale);
                return;
            }

            if (soundEndTimer.TryGet(out var timer))
            {
                timer.delay = delay;
                timer.Reset();
            }
        }

        private void ClearMainEndTimer()
        {
            soundEndTimer.Destroy();
        }

        private void ClearOneShotTimers(bool decreasePlayCount)
        {
            for (int i = ids.Count - 1; i >= 0; i--)
            {
                Mgr.Timer.RemoveTimer(ids[i]);

                if (decreasePlayCount)
                    Mgr.Sound.DecreaseSoundPlayCount(soundName);
            }

            ids.Clear();
            oneShotVolumeDict.Clear();
        }

        void IReference.ObjRelease()
        {
            if (isRelease) return;

            isRelease = true;
            isPlayEndUnload = false;
            isFadingOut = false;

            ClearMainEndTimer();

            if (isMainSoundCounted)
            {
                isMainSoundCounted = false;
                Mgr.Sound.DecreaseSoundPlayCount(soundName);
            }

            ClearOneShotTimers(true);

            if (source != null)
            {
                source.Stop();
                source.clip = null;
                source = null;
            }

            if (soundObj != null)
            {
                if (soundT != null)
                    Mgr.GPool.SetRoot(soundT, true);

                Mgr.GPool.ReleaseNewGameObject(SoundGlobals.SoundObj, soundObj);
                soundObj = null;
                soundT = null;
            }

            if (clips != null)
            {
                for (int i = 0; i < clips.Length; i++)
                    UnityGlobals.Release(clips[i]);
            }

            clips = null;
            soundCfg = null;
            groupName = string.Empty;
            soundName = string.Empty;
        }
    }
}
