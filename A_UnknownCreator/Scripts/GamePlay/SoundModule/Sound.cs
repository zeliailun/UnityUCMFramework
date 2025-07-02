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

        public int id { private set; get; }

        public string soundName { private set; get; }

        public string groupName { set; get; }


        public bool isRelease { private set; get; }


        private int index;
        private int currentSoundNum;
        private float time;
        private float fadeDuration;
        private float playVolume;
        private float currentVolume;
        private bool isFadingOut;
        private AudioClip[] clips;
        private List<long> ids = new();
        private EvtSoundPlayEnd soundEvt;
        private ITimer soundEndTimer;
        private Action<TimerCountCycle> soundEnd;
        private Action<TimerCountCycle> soundCompleted;


        public void Init(string name, GameObject go)
        {
            soundName = name;
            soundObj = go;
            soundCfg = Mgr.JD.GetData<Dictionary<string, SoundCfg>>(JsonCfgNameGlobals.SoundJson)[soundName];
            id = soundObj.GetInstanceID();
            soundT = soundObj.GetComponent<Transform>();
            source = soundObj.GetComponent<AudioSource>();
            source.mute = false;
            source.playOnAwake = false;
            source.outputAudioMixerGroup = Mgr.Sound.mixer.FindMatchingGroups(soundCfg.mixerGroup)[0];
            source.bypassEffects = soundCfg.bypassReverbZones;
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
                source.SetCustomCurve(AudioSourceCurveType.Spread, soundCfg.spreadCurve);
                source.SetCustomCurve(AudioSourceCurveType.SpatialBlend, soundCfg.spatialBlendCurve);
                source.SetCustomCurve(AudioSourceCurveType.ReverbZoneMix, soundCfg.reverbZoneMixCurve);
                source.SetCustomCurve(AudioSourceCurveType.CustomRolloff, soundCfg.customRolloffCurve);
            }

            clips = new AudioClip[soundCfg.soundArray.Length];
            for (int i = 0; i < soundCfg.soundArray.Length; i++)
                clips[i] = UnityGlobals.LoadSync<AudioClip>(soundCfg.soundArray[i].clip);

            currentSoundNum = 0;
            soundCompleted = SoundCompleted;
            soundEnd = SoundEndEvt;
            soundEvt = new();
            isRelease = isFadingOut = false;
        }

        public void PlaySound(bool isOneShot)
        {
            if (isRelease ||
                (source.isPlaying && !isOneShot) ||
                Mgr.Sound.CurrentSoundPlayCount(soundName) >= soundCfg.playCount)
                return;

            Mgr.Sound.AddSoundPlayCount(soundName);

            if (!soundObj.activeSelf) soundObj.SetActive(true);

            if (isFadingOut)
            {
                isFadingOut = false;
                time = 0;
            }

            if (soundCfg.isRandomPlay)
            {
                index = RVUtils.RandomInt(0, clips.Length, false);
            }
            else
            {
                index = currentSoundNum;
                currentSoundNum = (currentSoundNum + 1) % clips.Length;
            }

            var clip = clips[index];
            var info = soundCfg.soundArray[index];
            source.clip = clip;
            source.volume = playVolume = info.isUseCustomVolume ? info.volume : soundCfg.volume;


            if (isOneShot)
            {

                source.PlayOneShot(clip, source.volume);
                ids.Add(Mgr.Timer.CycleCount(1, clip.length, false, soundEnd, soundCompleted, soundCfg.isApplyTimeScale).id);
            }
            else
            {
                source.Play();
                if (soundEndTimer == null)
                {
                    soundEndTimer = Mgr.Timer.CycleCount(1, clip.length, false, soundEnd, null, soundCfg.isApplyTimeScale);
                }
                else
                {
                    (soundEndTimer as TimerCountCycle).delay = clip.length;
                    soundEndTimer.Reset();
                }
            }


        }

        public void PauseSound()
        {
            if (isRelease || !source.isPlaying) return;
            source.Pause();
        }

        public void ResumeSound()
        {
            if (isRelease || source.isPlaying) return;
            source.UnPause();
        }

        public void StopSound()
        {
            if (isRelease || !source.isPlaying) return;
            if (isFadingOut)
            {
                isFadingOut = false;
                time = 0;
            }

            source.Stop();
            SoundEndEvt(null);
        }

        public void StopSound(float fadeDuration)
        {
            if (isRelease || !source.isPlaying || isFadingOut) return;
            isFadingOut = true;
            currentVolume = source.volume;
            this.fadeDuration = fadeDuration;
        }

        public void MuteSound(bool isMute)
        {
            if (isRelease) return;
            source.mute = isMute;
        }

        public void UpdateSound()
        {
            FadeSound();
        }

        public bool HasGroup()
        => !string.IsNullOrWhiteSpace(groupName);

        private void FadeSound()
        {
            if (isRelease || !source.isPlaying || !isFadingOut) return;

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

            Mgr.Sound.RemoveSoundPlayCount(soundName);

            soundEvt.volume = playVolume;
            soundEvt.name = soundName;
            soundEvt.position = soundT.position;
            Mgr.Event.Send(soundEvt, SoundGlobals.OnSoundPlayEnd);
        }

        private void SoundCompleted(TimerCountCycle countCycle)
        {
            if(ids.Remove(countCycle.id))
            {
                Mgr.Timer.RemoveTimer(countCycle.id);
            }
        }

        void IReference.ObjRelease()
        {
            if (isRelease) return;

            isRelease = true;

            soundEndTimer.DestroySelf();
            soundEndTimer = null;

            for (int i = ids.Count - 1; i >= 0; i--)
                Mgr.Timer.RemoveTimer(ids[i]);
            ids.Clear();

            Mgr.Sound.RemoveSoundPlayCount(soundName);

            if (source != null)
            {
                source.Stop();
                source.clip = null;
                source = null;
            }

            if (soundObj != null)
            {
                Mgr.GPool.SetRoot(soundT, true);
                Mgr.GPool.ReleaseNewGameObject(SoundGlobals.SoundObj, soundObj);
                soundObj = null;
                soundT = null;
            }

            for (int i = 0; i < clips.Length; i++)
                UnityGlobals.Release(clips[i]);

            clips = null;
            soundCompleted = null;
            soundEnd = null;
            soundCfg = null;
        }
    }
}
