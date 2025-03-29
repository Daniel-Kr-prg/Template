using UnityEngine;

public class ConfigSettingsLimits
{
    public static Vector2 FOVLimit = new Vector2Int(30, 240);
    public static Vector2 BrightnessLimit = new Vector2(-4f, 4f);
    public static Vector2 DrawDistanceLimit = new Vector2(50, 500);
    public static Vector2 ShadowsDistanceLimit = new Vector2(30, 150);
    public static Vector2 LightDistanceLimit = new Vector2(0, 1);
    public static Vector2 vSyncLimit = new Vector2(0, 1);

    public static Vector2 AudioMixer_MasterVolumeLimit = new Vector2(-80, 20);
    public static Vector2 AudioMixer_EffectsVolumeLimit = new Vector2(-80, 20);
    public static Vector2 AudioMixer_AudioVolumeLimit = new Vector2(-80, 20);
    public static Vector2 AudioMixer_VoiceChatVolumeLimit = new Vector2(-80, 20);
}
