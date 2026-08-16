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

        public bool Initialize(int initialSize = 5, int? maxSize = null, Action<SoundManager_LocalAudioSource> onGet = null, Action<SoundManager_LocalAudioSource> onReturn = null)
        {
            Initialized = false;

            if (LocalAudioSourcePrefab == null)
                return false;

            pool = new Pool<SoundManager_LocalAudioSource>(
                factoryMethod: () => Instantiate(LocalAudioSourcePrefab, transform),
                initialSize: initialSize,
                maxSize: maxSize,
                onGet: onGet,
                onReturn: onReturn
            );
            if (pool == null)
                return false;

            Initialized = true;
            return true;
        }

        public SoundManager_LocalAudioSource SpawnSound_AtPoint(AudioClip clip, Vector3 position, AudioSourceSettings settings = null, SFXSettings SFX = null)
        {
            if (!Initialized || pool == null || clip == null) return null;

            var audioSource = pool.Get();
            audioSource.PlayClip_AtPoint(clip, position, settings, SFX, new System.Action(() =>
            {
                pool.ReturnToPool(audioSource);
            }));
            return audioSource;
        }

        public SoundManager_LocalAudioSource SpawnSound_FollowTransform(AudioClip clip, Transform transformToFollow, bool stopOnTransformDeactivate = false, AudioSourceSettings settings = null, SFXSettings SFX = null)
        {
            if (!Initialized || pool == null || clip == null || transformToFollow == null) return null;

            var audioSource = pool.Get();
            audioSource.PlayClip_FollowTransform(clip, transformToFollow, stopOnTransformDeactivate, settings, SFX, new System.Action(() =>
            {
                pool.ReturnToPool(audioSource);
            }));
            return audioSource;
        }
    }
}
