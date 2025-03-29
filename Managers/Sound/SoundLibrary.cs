using UnityEngine;

namespace DanieloZ.Managers.Sound
{
    [CreateAssetMenu(fileName = "Sound Library", menuName = "Audio/Sound Library")]
    [System.Serializable]
    public class SoundLibrary : ScriptableObject
    {
        [SerializeField] private SerializedDictionary<SoundCategory, SoundList> categories;

        public AudioClip GetSound(SoundCategory categoryEnum, SoundName soundName)
        {
            if (categories.TryGetValue(categoryEnum, out SoundList soundList))
            {
                if (soundList.sounds.TryGetValue(soundName, out AudioClip clip))
                {
                    return clip;
                }
            }

            return null;
        }
    }
}
