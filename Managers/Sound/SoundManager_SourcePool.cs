using DanieloZ.Managers;
using System;
using UnityEngine;
using UnityEngine.Audio;

namespace DanieloZ.Managers.Sound
{
    public class SoundManager_SourcePool : MonoBehaviour
    {
        [SerializeField] private SoundManager_LocalAudioSource LocalAudioSourcePrefab;
        private Pool<SoundManager_LocalAudioSource> pool;
        public bool Initialized { get; private set; }

        public bool Initialize(int initialSize = 5, Action<SoundManager_LocalAudioSource> onGet = null, Action<SoundManager_LocalAudioSource> onReturn = null)
        {
            Initialized = false;

            if (LocalAudioSourcePrefab == null)
                return false;

            pool = new Pool<SoundManager_LocalAudioSource>(LocalAudioSourcePrefab, 5, transform, onGet, onReturn);
            if (pool == null)
                return false;

            Initialized = true;
            return true;
        }

        public SoundManager_LocalAudioSource SpawnSound_AtPoint(AudioClip clip, Vector3 position, AudioSourceSettings settings = null, SFXSettings SFX = null)
        {
            var audioSource = pool.Get();

            if (clip != null)
            {
                audioSource.PlayClip_AtPoint(clip, position, settings, SFX, new System.Action(() =>
                {
                    pool.ReturnToPool(audioSource);
                }));

                return audioSource;
            }

            return null;
        }

        public SoundManager_LocalAudioSource SpawnSound_FollowTransform(AudioClip clip, Transform transformToFollow, bool stopOnTransformDeactivate = false, AudioSourceSettings settings = null, SFXSettings SFX = null)
        {
            var audioSource = pool.Get();

            if (clip != null)
            {
                audioSource.PlayClip_FollowTransform(clip, transformToFollow, stopOnTransformDeactivate, settings, SFX, new System.Action(() =>
                {
                    pool.ReturnToPool(audioSource);
                }));

                return audioSource;
            }

            return null;
        }
    }
}
