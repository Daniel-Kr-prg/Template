using DanieloZ.Managers.Config;
using DG.Tweening;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Audio;

namespace DanieloZ.Managers.Sound
{
    public class SoundManager : SingletonManager<SoundManager>
    {
        [Header("Audio Mixer References")]
        [SerializeField] private AudioMixer masterMixer;
        [SerializeField] private SerializedDictionary<AudioMixerGroupName, AudioMixerGroup> mixerGroups;

        [Header("Volume Parameters")]
        private const string MasterVolumeParam = "MasterVolume";
        private const string EffectsVolumeParam = "EffectsVolume";
        private const string MusicVolumeParam = "MusicVolume";
        private const string VCVolumeParam = "VoiceChatVolume";


        public float MasterVolume => ConfigManager.Instance.configData.AudioSettings.MasterVolume;
        public float MusicVolume => ConfigManager.Instance.configData.AudioSettings.MusicVolume;
        public float EffectsVolume => ConfigManager.Instance.configData.AudioSettings.EffectsVolume;
        public float VoiceChatVolume => ConfigManager.Instance.configData.AudioSettings.VoiceChatVolume;

                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                         private static Vector2 MasterVolumeLimit => ConfigSettingsLimits.AudioMixer_MasterVolumeLimit;
        private static Vector2 EffectsVolumeLimit => ConfigSettingsLimits.AudioMixer_EffectsVolumeLimit;
        private static Vector2 MusicVolumeLimit => ConfigSettingsLimits.AudioMixer_AudioVolumeLimit;
        private static Vector2 VoiceChatVolumeLimit => ConfigSettingsLimits.AudioMixer_VoiceChatVolumeLimit;


        /// <summary>
        /// Used to play music sound right at player
        /// </summary>
        [Header("Global Audio Sources")]
        [SerializeField] private Transform globalSoundPoint;

        /// <summary>
        /// Used to play voice sound right at player
        /// </summary>
        [SerializeField] private AudioSource globalVoiceChatSource;

        private Transform globalFollowPoint;

        [Header("Music Sources")]
        [SerializeField] private List<AudioSource> globalMusicSources;
        private Dictionary<AudioMixerGroupName, AudioSource> musicSourceMap;

        [Header("Effect Sources")]
        [SerializeField] private List<AudioSource> globalEffectSources;
        private Dictionary<AudioMixerGroupName, AudioSource> effectSourceMap;

        [Header("Local Audio Settings")]
        [SerializeField] private SoundManager_SourcePool localAudioSourcePool;
        private bool poolInitialized = false;

        [Header("Sound Library Settings")]
        [SerializeField] private SoundLibrary soundLibrary;

        protected override void Awake()
        {
            base.Awake();
            if (masterMixer == null)
            {
                Debug.LogError("[SoundManager] Master Mixer is not assigned!");
            }

            localAudioSourcePool ??= GetComponentInChildren<SoundManager_SourcePool>();
            if (localAudioSourcePool != null)
            {
                localAudioSourcePool.Initialize(5, 
                    onGet: new Action<SoundManager_LocalAudioSource>(x => { x.Init(); }),
                    onReturn: new Action<SoundManager_LocalAudioSource>(x => { }));

                if (localAudioSourcePool.Initialized)
                    poolInitialized = true;
            }

            effectSourceMap = new Dictionary<AudioMixerGroupName, AudioSource>();
            foreach (var source in globalEffectSources)
            {
                if (source.outputAudioMixerGroup != null)
                {
                    var groupName = mixerGroups.FirstOrDefault(kvp => kvp.Value == source.outputAudioMixerGroup).Key;
                    if (groupName != AudioMixerGroupName.NONE && !effectSourceMap.ContainsKey(groupName))
                    {
                        effectSourceMap[groupName] = source;
                    }
                }
            }

            musicSourceMap = new Dictionary<AudioMixerGroupName, AudioSource>();
            foreach (var source in globalMusicSources)
            {
                if (source.outputAudioMixerGroup != null)
                {
                    var groupName = mixerGroups.FirstOrDefault(kvp => kvp.Value == source.outputAudioMixerGroup).Key;
                    if (groupName != AudioMixerGroupName.NONE && !musicSourceMap.ContainsKey(groupName))
                    {
                        musicSourceMap[groupName] = source;
                    }
                }
            }
        }

        private void Start()
        {
            // Additional handling before stage changing

            // Satisfy stage condition
            StagesManager.Instance.AppStages.currentStage.SatisfyCondition("StagesManager_SoundManagerReady");
        }

        /// <summary>
        /// Sets the master volume, clamping within limits.
        /// </summary>
        public static void SetMasterVolume(float value)
        {
            float clampedValue = Mathf.Clamp01(value);
            float dBValue = Mathf.Lerp(MasterVolumeLimit.x, MasterVolumeLimit.y, clampedValue);
            Instance.SetMixerVolume(MasterVolumeParam, dBValue);
            Debug.Log($"[SoundManager] Master volume set to {clampedValue} ({dBValue} dB)");
        }

        /// <summary>
        /// Sets the effects volume, clamping within limits.
        /// </summary>
        public static void SetEffectsVolume(float value)
        {
            float clampedValue = Mathf.Clamp01(value);
            float dBValue = Mathf.Lerp(EffectsVolumeLimit.x, EffectsVolumeLimit.y, clampedValue);
            Instance.SetMixerVolume(EffectsVolumeParam, dBValue);
            Debug.Log($"[SoundManager] Effects volume set to {clampedValue} ({dBValue} dB)");
        }

        /// <summary>
        /// Sets the music volume, clamping within limits.
        /// </summary>
        public static void SetMusicVolume(float value)
        {
            float clampedValue = Mathf.Clamp01(value);
            float dBValue = Mathf.Lerp(MusicVolumeLimit.x, MusicVolumeLimit.y, clampedValue);
            Instance.SetMixerVolume(MusicVolumeParam, dBValue);
            Debug.Log($"[SoundManager] Music volume set to {clampedValue} ({dBValue} dB)");
        }

        /// <summary>
        /// Sets the voice chat volume, clamping within limits.
        /// </summary>
        public static void SetVCVolume(float value)
        {
            float clampedValue = Mathf.Clamp01(value);
            float dBValue = Mathf.Lerp(VoiceChatVolumeLimit.x, VoiceChatVolumeLimit.y, clampedValue);
            Instance.SetMixerVolume(VCVolumeParam, dBValue);
            Debug.Log($"[SoundManager] Voice chat volume set to {clampedValue} ({dBValue} dB)");
        }

        /// <summary>
        /// Helper to set a parameter on the audio mixer.
        /// </summary>
        private void SetMixerVolume(string parameter, float dBValue)
        {
            if (masterMixer != null)
            {
                masterMixer.SetFloat(parameter, dBValue);
            }
            else
            {
                Debug.LogError($"[SoundManager] AudioMixer is not assigned or missing!");
            }
        }

        public void FadeAudio(AudioMixerGroupName groupName, float targetVolume, float duration, Ease ease)
        {
            if (musicSourceMap.TryGetValue(groupName, out var musicSource))
            {
                DOTween.To(() => musicSource.volume, x => musicSource.volume = x, targetVolume, duration).SetEase(ease);
            }
            else if (effectSourceMap.TryGetValue(groupName, out var effectSource))
            {
                DOTween.To(() => effectSource.volume, x => effectSource.volume = x, targetVolume, duration).SetEase(ease);
            }
            else
            {
                Debug.LogWarning($"[SoundManager] No AudioSource found for group {groupName} to fade volume.");
            }
        }

        public void FadeIn(AudioMixerGroupName groupName, float duration, Ease ease)
        {
            FadeAudio(groupName, 1f, duration, ease);
        }

        public void FadeOut(AudioMixerGroupName groupName, float duration, Ease ease)
        {
            FadeAudio(groupName, 0f, duration, ease);
        }

        /// <summary>
        /// Plays a sound effect.
        /// </summary>
        public void PlayGlobalEffect(SoundCategory category, SoundName soundName, AudioMixerGroupName groupName = AudioMixerGroupName.Effects)
        {
            var clip = soundLibrary.GetSound(category, soundName);

            if (clip == null)
            {
                Debug.LogWarning($"[SoundManager] Clip for {soundName} is null");
                return;
            }

            if (effectSourceMap.TryGetValue(groupName, out var source))
            {
                source.PlayOneShot(clip);
                Debug.Log($"[SoundManager] Playing effect: {soundName} in group: {groupName}");
            }
            else
            {
                Debug.LogWarning($"[SoundManager] No AudioSource mapped to group: {groupName}");
            }
        }

        /// <summary>
        /// Plays background music.
        /// </summary>
        public void PlayGlobalMusic(SoundCategory category, SoundName soundName, AudioMixerGroupName groupName = AudioMixerGroupName.Music, bool loop = true)
        {
            var clip = soundLibrary.GetSound(category, soundName);

            if (clip == null)
            {
                Debug.LogWarning($"[SoundManager] Clip for {soundName} is null");
                return;
            }

            if (musicSourceMap.TryGetValue(groupName, out var source))
            {
                source.clip = clip;
                source.loop = loop;
                source.Play();
                Debug.Log($"[SoundManager] Playing music: {soundName} in group: {groupName}");
            }
            else
            {
                Debug.LogWarning($"[SoundManager] No AudioSource mapped to music group: {groupName}");
            }
        }

        /// <summary>
        /// Stops background music.
        /// </summary>
        public void StopGlobalMusic()
        {
            foreach (var source in musicSourceMap.Values)
            {
                source.Stop();
            }
            Debug.Log("[SoundManager] Music stopped.");
        }

        ///// <summary>
        ///// Plays voice chat audio.
        ///// </summary>
        //public void PlayGlobalVoiceChat()
        //{
        //    if (globalVoiceChatSource != null && clip != null)
        //    {
        //        globalVoiceChatSource.PlayOneShot(clip);
        //        Debug.Log($"[SoundManager] Playing voice chat audio: {clip.name}");
        //    }
        //    else
        //    {
        //        Debug.LogWarning("[SoundManager] Voice chat source or clip is null!");
        //    }
        //}

        /// <summary>
        /// Stops all sound sources.
        /// </summary>
        public void StopAllGlobalSounds()
        {
            foreach (var source in musicSourceMap.Values)
            {
                source.Stop();
            }

            foreach (var source in effectSourceMap.Values)
            {
                source.Stop();
            }

            globalVoiceChatSource?.Stop();
            Debug.Log("[SoundManager] All sounds stopped.");
        }

        /// <summary>
        /// Mutes/unmutes all audio.
        /// </summary>
        public void ToggleMute(bool mute)
        {
            if (masterMixer != null)
            {
                masterMixer.SetFloat(MasterVolumeParam, mute ? -80f : 0f);
                Debug.Log($"[SoundManager] Audio {(mute ? "muted" : "unmuted")}.");
            }
        }

        public void SetGlobalPoint(Transform followTransform)
        {
            globalFollowPoint = followTransform;
        }

        public SoundManager_LocalAudioSource SpawnSound_AtPoint(SoundCategory category, SoundName soundName, Vector3 position, AudioMixerGroupName groupName = AudioMixerGroupName.NONE, AudioSourceSettings settings = null, SFXSettings SFX = null)
        {
            if (poolInitialized)
            {
                settings ??= new AudioSourceSettings() { loop = false, volume = 1f, group = GetAudioMixerGroup(groupName) };
                settings.group ??= GetAudioMixerGroup(groupName);

                return localAudioSourcePool.SpawnSound_AtPoint(soundLibrary.GetSound(category, soundName), position, settings, SFX);
            }

            return null;
        }

        public SoundManager_LocalAudioSource SpawnSound_FollowTransform(SoundCategory category, SoundName soundName, Transform transformToFollow, bool stopOnTransformDeactivate = false, AudioMixerGroupName groupName = AudioMixerGroupName.NONE, AudioSourceSettings settings = null, SFXSettings SFX = null)
        {
            if (poolInitialized)
            {
                settings ??= new AudioSourceSettings() { loop = false, volume = 1f, group = GetAudioMixerGroup(groupName) };
                settings.group ??= GetAudioMixerGroup(groupName);

                return localAudioSourcePool.SpawnSound_FollowTransform(soundLibrary.GetSound(category, soundName), transformToFollow, stopOnTransformDeactivate, settings, SFX);
            }

            return null;
        }

        public static AudioMixerGroup GetAudioMixerGroup(AudioMixerGroupName name)
        {
            if (Instance.mixerGroups.TryGetValue(name, out var group)) return group;

            return null;
        }

        private void Update()
        {
            if (globalFollowPoint != null)
            {
                globalSoundPoint.position = globalFollowPoint.position;
            }
        }
    }


    public class AudioSourceSettings
    {
        public float delayTime = 0;
        public bool loop = false;
        public float volume = 1f;
        public AudioMixerGroup group;
    }

    public class SFXSettings
    {
        public float fadeInTime = 0;
        public float fadeOutTime = 0;
    }

    public enum AudioMixerGroupName
    {
        Master,
        Effects,
        Effects_UI_1,
        Effects_UI_2,
        Music,
        Music_UI_1,
        VC,
        NONE
    }
}
