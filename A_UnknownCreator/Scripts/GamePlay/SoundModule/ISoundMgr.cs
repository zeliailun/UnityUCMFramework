using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

namespace UnknownCreator.Modules
{
    public interface ISoundMgr : IDearMgr
    {
        AudioMixer mixer { get; }
        int sameSoundCount { get; }
        int soundCount { get; }
        int soundGroupCount { get; }
        void SetSoundMixer(string am);
        void IncreaseSoundPlayCount(string name);
        void DecreaseSoundPlayCount(string name);
        int CurrentSoundPlayCount(string name);
        T LoadSound<T>(string soundName, string soundGroupName)
        where T : class, ISound, new();
        void UnloadSound(ISound sound);
        void UnloadSound(EntityId id);
        ISound GetSound(EntityId id);
        void PlaySound(EntityId id, bool isUseOneShot);
        void SetSoundPosition(EntityId id, Vector3 position);
        void PauseSound(EntityId id);
        void ResumeSound(EntityId id);
        void StopSound(EntityId id);
        void MuteSound(EntityId id, bool isMute);
        void SetSoundGroup(EntityId id, string soundGroupName);
        void RemoveSoundGroup(EntityId id);
        void PauseSoundGroup(string soundGroupName);
        void ResumeSoundGroup(string soundGroupName);
        void StopSoundGroup(string soundGroupName);
        void ClearSoundGroup(string soundGroupName);
        void MuteSoundGroup(string soundGroupName, bool isMute);
        void MuteSoundSound(string soundGroupName, bool isMute);
        ISoundGroup GetSoundGroup(string soundGroupName);
        bool HasSound(EntityId id);
        bool HasSoundGroup(string soundGroupName);
        void PauseAllSound();
        void ResumeAllSound();
        void StopAllSound();
        void ClearAllSound();
        List<ISoundGroup> GetAllSoundGroup();
        List<ISound> GetAllSound();
    }
}
