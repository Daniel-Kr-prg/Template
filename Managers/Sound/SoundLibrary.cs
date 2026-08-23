using UnityEngine;

namespace DanieloZ.Managers.Sound
{
    [CreateAssetMenu(fileName = "Sound Library", menuName = "Audio/Sound Library")]
    [System.Serializable]
    public class SoundLibrary : ScriptableObject
    {
        [SerializeField] private SerializedDictionary<SoundCategory, SoundList> categories;

        public void SetSoundList(SoundCategory category, SoundList soundList)
        {
            categories ??= new SerializedDictionary<SoundCategory, SoundList>();
            categories[category] = soundList;
        }

        public AudioClip GetSound(SoundCategory categoryEnum, SoundName soundName)
        {
            if (categories != null && categories.TryGetValue(categoryEnum, out SoundList soundList) && soundList != null)
            {
                if (soundList.sounds != null && soundList.sounds.TryGetValue(soundName, out AudioClip clip))
                {
                    return clip;
                }
            }

            return null;
        }
    }
}
