using UnityEngine;
using UnityEngine.UI;

namespace GameJamRAC.UI
{
    /// <summary>
    /// 挂到任意 Text/Image 上，激活时自动呼吸闪烁。
    /// 物体 SetActive(true) 时自动开始，SetActive(false) 时自动停止。
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Graphic))]
    public class TextBreathingEffect : MonoBehaviour
    {
        [Header("呼吸速度")]
        [SerializeField, Range(0.5f, 5f)] private float speed = 1.8f;

        [Header("最小透明度")]
        [SerializeField, Range(0f, 1f)] private float minAlpha = 0.25f;

        [Header("最大透明度")]
        [SerializeField, Range(0f, 1f)] private float maxAlpha = 1f;

        [Header("开始呼吸前保持全亮的秒数")]
        [SerializeField] private float startDelay = 0.8f;

        [Header("持续秒数后自动隐藏（0 = 永久，直到外部关闭）")]
        [SerializeField] private float duration = 0f;

        private CanvasGroup canvasGroup;
        private Coroutine breathing;

        private void Awake()
        {
            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();
            canvasGroup.alpha = maxAlpha;
        }

        private void OnEnable()
        {
            canvasGroup.alpha = maxAlpha;
            StartBreathing();
        }

        private void OnDisable()
        {
            StopBreathing();
        }

        private void StartBreathing()
        {
            StopBreathing();
            breathing = StartCoroutine(Pulse());
        }

        private void StopBreathing()
        {
            if (breathing != null)
            {
                StopCoroutine(breathing);
                breathing = null;
            }
        }

        private System.Collections.IEnumerator Pulse()
        {
            canvasGroup.alpha = maxAlpha;

            if (startDelay > 0f)
                yield return new WaitForSeconds(startDelay);

            // 用 Cos 而非 Sin：cos(0)=1，从 maxAlpha 平滑开始，无跳变
            float startTime = Time.time;
            while (true)
            {
                float elapsed = Time.time - startTime;
                if (duration > 0f && elapsed >= duration)
                {
                    canvasGroup.alpha = maxAlpha;
                    yield return null;
                    gameObject.SetActive(false);
                    yield break;
                }

                float t = (Mathf.Cos(elapsed * speed) + 1f) * 0.5f;
                canvasGroup.alpha = Mathf.Lerp(minAlpha, maxAlpha, t);
                yield return null;
            }
        }
    }
}
