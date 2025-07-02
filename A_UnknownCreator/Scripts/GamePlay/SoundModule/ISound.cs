using UnityEngine;

namespace UnknownCreator.Modules
{
    public interface ISound
    {

        int id { get; }
        string soundName { get; }
        GameObject soundObj { get; }
        Transform soundT { get; }
        AudioSource source { get; }
        SoundCfg soundCfg { get; }
        string groupName { set; get; }
        void Init(string soundName, GameObject soundObj);
        bool HasGroup();
        void PlaySound(bool isOneShot);
        void MuteSound(bool isMute);
        void StopSound();
        void StopSound(float fadeDuration);
        void PauseSound();
        void ResumeSound();
        void UpdateSound();
    }

    public interface ISoundGroup
    {
        string groupName { get; set; }
        int soundCount { get; }
        bool HasSound(int id);
        void AddSound(int id, ISound sound);
        void RemoveSound(int id);
        void PauseAllSounds();
        void ResumeAllSound();
        void StopAllSound();
        void MuteAllSound(bool isMute);
        void ClearSounds();
    }
}