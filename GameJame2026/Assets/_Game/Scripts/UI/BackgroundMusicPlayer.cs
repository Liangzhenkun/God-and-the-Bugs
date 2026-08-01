using UnityEngine;
using UnityEngine.Serialization;

namespace GameJamRAC.UI
{
    /// <summary>跨场景保留同一个背景音乐播放器，音量跟随 AudioSettingsState 统一管理。</summary>
    [DisallowMultipleComponent]
    public class BackgroundMusicPlayer : MonoBehaviour
    {
        private static BackgroundMusicPlayer instance;

        [SerializeField] private AudioClip musicClip;
        [FormerlySerializedAs("volume")]
        [SerializeField, Range(0f, 1f), Tooltip("初始音量（编辑器下始终生效，打包后仅首次生效）")]
        private float initialVolume = 0.35f;

        private AudioSource source;

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);
            // 始终新建 AudioSource，避免跟 SFXManager/SFXPlayer 抢夺同一个源
            source = gameObject.AddComponent<AudioSource>();
            source.clip = musicClip;
            source.loop = true;
            source.playOnAwake = false;
            source.spatialBlend = 0f;

            // 编辑器下始终同步 Inspector 初始音量，方便调试，但保留静音偏好
            // 打包后仅在首次运行时使用初始值，之后读取玩家偏好
#if UNITY_EDITOR
            AudioSettingsState.Set(initialVolume, AudioSettingsState.SoundEnabled);
#else
            if (!PlayerPrefs.HasKey("GameJamRAC.AudioVolume.v2"))
                AudioSettingsState.Set(initialVolume, true);
#endif

            source.volume = 1f;
            ApplySoundEnabled();
            AudioSettingsState.OnSettingsChanged += SyncVolume;

            if (source.clip != null && !source.isPlaying) source.Play();
        }

        private void OnDestroy()
        {
            AudioSettingsState.OnSettingsChanged -= SyncVolume;
            if (instance == this) instance = null;
        }

        private void OnApplicationQuit()
        {
            // 退出前抢先静音，防止 Unity 清理 AudioListener 时音量跳变
            AudioListener.volume = 0f;
        }

        private void ApplySoundEnabled()
        {
            if (source == null) return;
            if (AudioSettingsState.SoundEnabled)
            {
                source.UnPause();
                if (!source.isPlaying && source.clip != null)
                    source.Play();
            }
            else
            {
                source.Pause();
            }
            AudioListener.volume = AudioSettingsState.SoundEnabled ? AudioSettingsState.Volume : 0f;
        }

        /// <summary>声音开关切换时暂停/恢复音乐，不触发 Apply（避免循环）。</summary>
        private void SyncVolume()
        {
            ApplySoundEnabled();
        }
    }
}
