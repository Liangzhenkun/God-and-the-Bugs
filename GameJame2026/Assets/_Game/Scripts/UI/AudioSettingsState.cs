using UnityEngine;

namespace GameJamRAC.UI
{
    /// <summary>统一保存并应用标题与关卡共用的声音设置。</summary>
    public static class AudioSettingsState
    {
        private const string VolumeKey = "GameJamRAC.AudioVolume.v2";
        private const string SoundEnabledKey = "GameJamRAC.SoundEnabled";
        private const float DefaultVolume = 0.35f;

        public static float Volume => Mathf.Clamp01(PlayerPrefs.GetFloat(VolumeKey, DefaultVolume));
        public static bool SoundEnabled => PlayerPrefs.GetInt(SoundEnabledKey, 1) == 1;

        public static void ToggleSound()
        {
            Set(Volume, !SoundEnabled);
        }

        public static void Set(float volume, bool soundEnabled)
        {
            PlayerPrefs.SetFloat(VolumeKey, Mathf.Clamp01(volume));
            PlayerPrefs.SetInt(SoundEnabledKey, soundEnabled ? 1 : 0);
            PlayerPrefs.Save();
            Apply();
        }

        public static void Apply()
        {
            AudioListener.volume = SoundEnabled ? Volume : 0f;
        }
    }
}
