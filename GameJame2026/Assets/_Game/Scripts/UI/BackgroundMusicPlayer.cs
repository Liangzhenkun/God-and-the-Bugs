using UnityEngine;

namespace GameJamRAC.UI
{
    /// <summary>跨场景保留同一个背景音乐播放器，避免关卡切换造成音乐中断。</summary>
    [DisallowMultipleComponent]
    public class BackgroundMusicPlayer : MonoBehaviour
    {
        private static BackgroundMusicPlayer instance;

        [SerializeField] private AudioClip musicClip;
        [SerializeField, Range(0f, 1f)] private float volume = 0.35f;

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
            source = GetComponent<AudioSource>();
            if (source == null) source = gameObject.AddComponent<AudioSource>();
            source.clip = musicClip;
            source.loop = true;
            source.playOnAwake = false;
            source.spatialBlend = 0f;
            source.volume = volume;
            AudioSettingsState.Apply();
            if (source.clip != null && !source.isPlaying) source.Play();
        }

        private void OnDestroy()
        {
            if (instance == this) instance = null;
        }
    }
}
