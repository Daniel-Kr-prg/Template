using UnityEngine;

namespace DanieloZ.Managers.Sound
{
    [CreateAssetMenu(fileName = "Sound List", menuName = "Audio/Sound List")]
    [System.Serializable]
    public class SoundList : ScriptableObject
    {
        public SerializedDictionary<SoundName, AudioClip> sounds;
    }
}
