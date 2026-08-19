using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

namespace UnknownCreator.Modules
{
    [Serializable]
    public sealed class SoundMgr : ISoundMgr
    {
        internal Dictionary<string, int> soundCountDict = new();

        internal Dictionary<EntityId, ISound> soundDict = new();

        internal List<ISound> soundList = new();

        internal Dictionary<string, ISoundGroup> soundGroupDict = new();

        internal List<ISoundGroup> soundGroupList = new();

        [field: SerializeField]
        private string mixerName;

        public AudioMixer mixer { internal set; get; }

        public int soundCount => soundDict.Count;

        public int soundGroupCount => soundGroupDict.Count;

        public int sameSoundCount => soundCountDict.Count;

        void IDearMgr.WorkWork()
        {
            soundCountDict ??= new();
            soundGroupDict ??= new();
            soundDict ??= new();
            soundGroupList ??= new();
            soundList ??= new();

            if (!string.IsNullOrWhiteSpace(mixerName))
                mixer = UnityGlobals.LoadSync<AudioMixer>(mixerName);
        }

        void IDearMgr.DoNothing()
        {
            ClearAllSound();
            UnityGlobals.Release(mixer);
            mixer = null;
        }

        void IDearMgr.UpdateMGR()
        {
            for (int i = soundList.Count - 1; i >= 0; i--)
                soundList[i]?.UpdateSound();
        }

        public void SetSoundMixer(string am)
        {
            mixerName = am;
            UnityGlobals.Release(mixer);
            mixer = null;

            if (!string.IsNullOrWhiteSpace(am))
                mixer = UnityGlobals.LoadSync<AudioMixer>(am);
        }

        public void IncreaseSoundPlayCount(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return;

            soundCountDict.TryGetValue(name, out var count);
            soundCountDict[name] = count + 1;
        }

        public void DecreaseSoundPlayCount(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return;
            if (!soundCountDict.TryGetValue(name, out var count)) return;

            soundCountDict[name] = count <= 1 ? 0 : count - 1;
        }

        public int CurrentSoundPlayCount(string name)
        {
            return soundCountDict.TryGetValue(name, out var count) ? count : 0;
        }

        public T LoadSound<T>(string soundName, string soundGroupName)
        where T : class, ISound, new()
        {
            var info = Mgr.GPool.GetNewGameObject(SoundGlobals.SoundObj);
            if (info.Item2)
            {
                info.Item1.AddComponent<AudioSource>();
                info.Item1.layer = 2;
            }

            var sound = Mgr.RPool.Load<T>();
            sound.Init(soundName, info.Item1);
            soundDict.Add(sound.id, sound);
            soundList.Add(sound);

            if (!string.IsNullOrWhiteSpace(soundGroupName))
                SetSoundGroup(sound.id, soundGroupName);

            return sound;
        }

        public void UnloadSound(ISound sound)
        {
            if (sound is null) return;
            UnloadSound(sound.id);
        }

        public void UnloadSound(EntityId id)
        {
            if (!soundDict.Remove(id, out var sound)) return;

            if (sound.HasGroup())
                GetSoundGroup(sound.groupName)?.RemoveSound(id);

            soundList.Remove(sound);
            Mgr.RPool.Release(sound);
        }

        public ISound GetSound(EntityId id)
        => soundDict.TryGetValue(id, out var value) ? value : null;

        public bool HasSound(EntityId id)
        => soundDict.TryGetValue(id, out _);

        public void PlaySound(EntityId id, bool isUseOneShot)
        {
            GetSound(id)?.PlaySound(isUseOneShot);
        }

        public void SetSoundPosition(EntityId id, Vector3 position)
        {
            GetSound(id)?.SetPosition(position);
        }

        public void PauseSound(EntityId id)
        {
            GetSound(id)?.PauseSound();
        }

        public void ResumeSound(EntityId id)
        {
            GetSound(id)?.ResumeSound();
        }

        public void StopSound(EntityId id)
        {
            GetSound(id)?.StopSound();
        }

        public void MuteSound(EntityId id, bool isMute)
        {
            GetSound(id)?.MuteSound(isMute);
        }

        public List<ISound> GetAllSound()
        => soundList.CopyToNewList();

        public bool HasSoundGroup(string soundGroupName)
        => soundGroupDict.TryGetValue(soundGroupName, out _);

        public void SetSoundGroup(EntityId id, string soundGroupName)
        {
            if (string.IsNullOrWhiteSpace(soundGroupName))
            {
                UCMDebug.LogError("无法设置空声音组");
                return;
            }

            var sound = GetSound(id);
            if (sound is null) return;

            if (sound.HasGroup())
            {
                if (sound.groupName == soundGroupName) return;
                GetSoundGroup(sound.groupName)?.RemoveSound(id);
            }

            var group = GetSoundGroup(soundGroupName);
            if (group is null)
            {
                group = Mgr.RPool.Load<SoundGroup>();
                group.groupName = soundGroupName;
                soundGroupDict.Add(soundGroupName, group);
                soundGroupList.Add(group);
            }

            group.AddSound(id, sound);
        }

        public void RemoveSoundGroup(EntityId id)
        {
            var sound = GetSound(id);
            if (sound is null) return;

            if (sound.HasGroup())
                GetSoundGroup(sound.groupName)?.RemoveSound(id);
        }

        public void PauseSoundGroup(string soundGroupName)
        {
            GetSoundGroup(soundGroupName)?.PauseAllSounds();
        }

        public void ResumeSoundGroup(string soundGroupName)
        {
            GetSoundGroup(soundGroupName)?.ResumeAllSound();
        }

        public void StopSoundGroup(string soundGroupName)
        {
            GetSoundGroup(soundGroupName)?.StopAllSound();
        }

        public void ClearSoundGroup(string soundGroupName)
        {
            var group = GetSoundGroup(soundGroupName);
            if (group is null) return;

            soundGroupDict.Remove(soundGroupName);
            soundGroupList.Remove(group);
            Mgr.RPool.Release(group);
        }

        public void MuteSoundGroup(string soundGroupName, bool isMute)
        {
            GetSoundGroup(soundGroupName)?.MuteAllSound(isMute);
        }

        public void MuteSoundSound(string soundGroupName, bool isMute)
        {
            MuteSoundGroup(soundGroupName, isMute);
        }

        public ISoundGroup GetSoundGroup(string soundGroupName)
        => soundGroupDict.TryGetValue(soundGroupName, out var value) ? value : null;

        public List<ISoundGroup> GetAllSoundGroup()
        => soundGroupList.CopyToNewList();

        public void PauseAllSound()
        {
            for (int i = soundList.Count - 1; i >= 0; i--)
                soundList[i]?.PauseSound();
        }

        public void ResumeAllSound()
        {
            for (int i = soundList.Count - 1; i >= 0; i--)
                soundList[i]?.ResumeSound();
        }

        public void StopAllSound()
        {
            for (int i = soundList.Count - 1; i >= 0; i--)
                soundList[i]?.StopSound();
        }

        public void ClearAllSound()
        {
            soundCountDict.Clear();
            soundGroupDict.Clear();
            soundDict.Clear();

            for (int i = soundGroupList.Count - 1; i >= 0; i--)
            {
                var soundGroup = soundGroupList[i];
                soundGroupList.RemoveAt(i);
                Mgr.RPool.Release(soundGroup);
            }

            for (int i = soundList.Count - 1; i >= 0; i--)
            {
                var sound = soundList[i];
                soundList.RemoveAt(i);
                Mgr.RPool.Release(sound);
            }
        }
    }
}
