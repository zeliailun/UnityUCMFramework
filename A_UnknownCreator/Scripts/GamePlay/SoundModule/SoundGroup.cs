using System.Collections.Generic;
using UnityEngine;

namespace UnknownCreator.Modules
{
    public class SoundGroup : ISoundGroup, IReference
    {
        private Dictionary<EntityId, ISound> soundDict = new();

        private List<ISound> soundList = new();

        public int soundCount => soundDict.Count;

        public string groupName { get; set; }

        public bool HasSound(EntityId id) => soundDict.TryGetValue(id, out _);

        public void AddSound(EntityId id, ISound sound)
        {
            if (sound is null) return;
            if (soundDict.TryGetValue(id, out _)) return;

            sound.groupName = groupName;
            soundDict.Add(id, sound);
            soundList.Add(sound);
        }

        public void RemoveSound(EntityId id)
        {
            if (!soundDict.Remove(id, out var sound)) return;

            soundList.Remove(sound);
            sound.groupName = string.Empty;
        }

        public void PauseAllSounds()
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

        public void MuteAllSound(bool isMute)
        {
            for (int i = soundList.Count - 1; i >= 0; i--)
                soundList[i]?.MuteSound(isMute);
        }

        public void ClearSounds()
        {
            for (int i = soundList.Count - 1; i >= 0; i--)
            {
                if (soundList[i] != null)
                    soundList[i].groupName = string.Empty;
            }

            soundDict.Clear();
            soundList.Clear();
        }

        void IReference.ObjRelease()
        {
            ClearSounds();
            groupName = string.Empty;
        }
    }
}
