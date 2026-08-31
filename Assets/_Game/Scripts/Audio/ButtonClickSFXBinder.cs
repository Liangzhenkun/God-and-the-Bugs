using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace GameJamRAC.Audio
{
    /// <summary>
    /// 自动给场景里的所有 Button 添加点击音效，避免逐个在 Inspector 里绑定。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ButtonClickSFXBinder : MonoBehaviour
    {
        [SerializeField] private SFXPlayer clickPlayer;
        [SerializeField] private bool includeInactiveButtons = true;

        private Coroutine bindRoutine;

        private void Awake()
        {
            if (clickPlayer == null)
                clickPlayer = GetComponent<SFXPlayer>();
        }

        private void OnEnable()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
            QueueBind();
        }

        private void Start() => QueueBind();

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            if (bindRoutine != null)
            {
                StopCoroutine(bindRoutine);
                bindRoutine = null;
            }
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode) => QueueBind();

        public void BindNow()
        {
            if (clickPlayer == null)
                clickPlayer = GetComponent<SFXPlayer>();

            foreach (Button button in FindObjectsByType<Button>(
                         includeInactiveButtons ? FindObjectsInactive.Include : FindObjectsInactive.Exclude,
                         FindObjectsSortMode.None))
            {
                ButtonClickSFXHandler handler = button.GetComponent<ButtonClickSFXHandler>();
                if (handler == null)
                    handler = button.gameObject.AddComponent<ButtonClickSFXHandler>();

                handler.Configure(clickPlayer);
            }
        }

        private void QueueBind()
        {
            if (!isActiveAndEnabled) return;
            if (bindRoutine != null) StopCoroutine(bindRoutine);
            bindRoutine = StartCoroutine(BindAfterUiScripts());
        }

        private IEnumerator BindAfterUiScripts()
        {
            yield return null;
            BindNow();
            bindRoutine = null;
        }
    }

}
