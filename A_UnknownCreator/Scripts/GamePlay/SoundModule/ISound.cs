using UnityEngine;

namespace UnknownCreator.Modules
{
    public interface ISound
    {

        EntityId id { get; }
        string soundName { get; }
        string groupName { set; get; }
        bool isPlayEndUnload { set; get; }
        bool isRelease { get; }
        GameObject soundObj { get; }
        Transform soundT { get; }
        AudioSource source { get; }
        SoundCfg soundCfg { get; }
       

        void Init(string soundName, GameObject soundObj);
        bool HasGroup();
        void SetPosition(Vector3 position);
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
        bool HasSound(EntityId id);
        void AddSound(EntityId id, ISound sound);
        void RemoveSound(EntityId id);
        void PauseAllSounds();
        void ResumeAllSound();
        void StopAllSound();
        void MuteAllSound(bool isMute);
        void ClearSounds();
    }
}
