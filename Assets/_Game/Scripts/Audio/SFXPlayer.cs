using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GameJamRAC.Audio
{
    /// <summary>
    /// 播放一段音频片段。拖到 Button 的 onClick 上 → 选 Play()。
    /// </summary>
    public class SFXPlayer : MonoBehaviour
    {
        private static readonly Dictionary<string, SFXPlayer> PlayersById = new Dictionary<string, SFXPlayer>();

        [SerializeField] private string playerId = "";
        [SerializeField] private AudioClip sourceClip;
        [SerializeField, Range(0f, 1f)] private float volume = 1f;
        [SerializeField] private float segmentStart = 0f;
        [SerializeField] private float segmentDuration = 0.4f;

        private AudioSource audioSrc;
        private Coroutine stopRoutine;

        private void Awake()
        {
            audioSrc = gameObject.AddComponent<AudioSource>();
            audioSrc.playOnAwake = false;
        }

        private void OnEnable()
        {
            Register();
        }

        private void OnDisable()
        {
            Unregister();
        }

        public static bool TryGetById(string id, out SFXPlayer player)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                player = null;
                return false;
            }

            return PlayersById.TryGetValue(id.Trim(), out player) && player != null;
        }

        public void Play()
        {
            if (sourceClip == null) return;
            if (stopRoutine != null) StopCoroutine(stopRoutine);
            audioSrc.Stop();
            audioSrc.volume = volume;
            audioSrc.clip = sourceClip;
            audioSrc.Play();
            audioSrc.time = Mathf.Clamp(segmentStart, 0f, sourceClip.length - 0.01f);
            stopRoutine = StartCoroutine(StopAfter(Mathf.Min(segmentDuration, sourceClip.length - segmentStart)));
        }

        private IEnumerator StopAfter(float delay)
        {
            yield return new WaitForSeconds(delay);
            audioSrc.Stop();
            audioSrc.clip = null;
            stopRoutine = null;
        }

        private void Register()
        {
            if (string.IsNullOrWhiteSpace(playerId)) return;
            PlayersById[playerId.Trim()] = this;
        }

        private void Unregister()
        {
            if (string.IsNullOrWhiteSpace(playerId)) return;

            string key = playerId.Trim();
            if (PlayersById.TryGetValue(key, out SFXPlayer player) && player == this)
                PlayersById.Remove(key);
        }
    }
}
