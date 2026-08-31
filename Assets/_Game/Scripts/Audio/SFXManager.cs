using System.Collections;
using GameJamRAC.Grid;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GameJamRAC.Audio
{
    /// <summary>
    /// 移动音效：自动接 GridUnitMover 的移动事件，切场景自动重连。
    /// </summary>
    public class SFXManager : MonoBehaviour
    {
        [SerializeField] private AudioClip sourceClip;
        [SerializeField, Range(0f, 1f)] private float volume = 1f;
        [SerializeField] private float segmentStart = 0f;
        [SerializeField] private float segmentDuration = 0.4f;
        [SerializeField, Min(0f)] private float playDelay = 0.02f;
        [SerializeField] private bool playWhenMoveStarts = true;

        private AudioSource audioSrc;
        private Coroutine stopRoutine;
        private Coroutine delayRoutine;

        private void Awake()
        {
            audioSrc = gameObject.AddComponent<AudioSource>();
            audioSrc.playOnAwake = false;
        }

        private void OnEnable()  => SceneManager.sceneLoaded += OnSceneLoaded;
        private void OnDisable() => SceneManager.sceneLoaded -= OnSceneLoaded;

        private void Start() => HookAllMovers();

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode) => StartCoroutine(HookNextFrame());
        private IEnumerator HookNextFrame() { yield return null; HookAllMovers(); }

        private void HookAllMovers()
        {
            foreach (GridUnitMover mover in FindObjectsByType<GridUnitMover>(FindObjectsSortMode.None))
            {
                mover.onMoveStarted -= OnMove;
                mover.onCellReached -= OnMove;
                if (playWhenMoveStarts)
                    mover.onMoveStarted += OnMove;
                else
                    mover.onCellReached += OnMove;
            }
        }

        private void OnMove(Vector3Int _)
        {
            if (delayRoutine != null) StopCoroutine(delayRoutine);
            delayRoutine = StartCoroutine(PlayAfterDelay());
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

        private IEnumerator PlayAfterDelay()
        {
            if (playDelay > 0f)
                yield return new WaitForSeconds(playDelay);

            Play();
            delayRoutine = null;
        }

        private IEnumerator StopAfter(float delay)
        {
            yield return new WaitForSeconds(delay);
            audioSrc.Stop();
            audioSrc.clip = null;
            stopRoutine = null;
        }
    }
}
