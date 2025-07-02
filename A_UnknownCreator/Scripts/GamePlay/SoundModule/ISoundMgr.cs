using System.Collections.Generic;
using UnityEngine.Audio;

namespace UnknownCreator.Modules
{
    public interface ISoundMgr : IDearMgr
    {
        AudioMixer mixer { get; }
        int sameSoundCount { get; }
        int soundCount { get; }
        int soundGroupCount { get; }
        void SetSoundMixer(AudioMixer am);
        void AddSoundPlayCount(string name);
        void RemoveSoundPlayCount(string name);
        int CurrentSoundPlayCount(string name);
        T LoadSound<T>(string soundName, string soundGroupName)
        where T : class, ISound, new();
        void UnloadSound(ISound sound);
        void UnloadSound(int id);
        ISound GetSound(int id);
        void PlaySound(int id, bool isUseOneShot);
        void PauseSound(int id);
        void ResumeSound(int id);
        void StopSound(int id);
        void MuteSound(int id, bool isMute);
        void SetSoundGroup(int id, string soundGroupName);
        void RemoveSoundGroup(int id);
        void PauseSoundGroup(string soundGroupName);
        void ResumeSoundGroup(string soundGroupName);
        void StopSoundGroup(string soundGroupName);
        void ClearSoundGroup(string soundGroupName);
        void MuteSoundSound(string soundGroupName, bool isMute);
        ISoundGroup GetSoundGroup(string soundGroupName);
        bool HasSound(int id);
        bool HasSoundGroup(string soundGroupName);
        void PauseAllSound();
        void ResumeAllSound();
        void StopAllSound();
        void ClearAllSound();
        List<ISoundGroup> GetAllSoundGroup();
        List<ISound> GetAllSound();
    }
}