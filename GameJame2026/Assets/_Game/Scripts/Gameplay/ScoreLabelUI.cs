using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace GameJamRAC.Gameplay
{
    /// <summary>角色头顶世界空间生命标签与魂穿提示。</summary>
    public class ScoreLabelUI : MonoBehaviour
    {
        [SerializeField] private Vector3 worldOffset = new Vector3(0, 2.5f, 0);
        [SerializeField] private Transform followTarget;
        [SerializeField] private Text nameText;
        [SerializeField] private Text scoreText;
        [SerializeField] private GameObject transferBanner;
        [SerializeField] private Text transferText;
        [SerializeField] private float transferDuration = 1.5f;
        [SerializeField, Min(0.001f)] private float visibleScale = 0.03f;
        [SerializeField] private int sortingOrder = 50;
        [SerializeField, Min(0f)] private float modelTopPadding = 0.5f;

        private Transform labelTransform;
        private Coroutine transferCoroutine;
        private UnityEngine.Camera mainCamera;
        private Canvas worldCanvas;

        private void Awake()
        {
            labelTransform = transform;
            mainCamera = UnityEngine.Camera.main;
            worldCanvas = GetComponent<Canvas>();
            ConfigureCanvas();
            if (transferBanner != null) transferBanner.SetActive(false);
        }

        private void LateUpdate()
        {
            if (followTarget != null)
                labelTransform.position = GetLabelPosition();

            if (mainCamera == null) mainCamera = UnityEngine.Camera.main;
            if (mainCamera != null)
            {
                ConfigureCanvas();
                labelTransform.rotation = mainCamera.transform.rotation;
            }
        }

        private void ConfigureCanvas()
        {
            if (worldCanvas == null || mainCamera == null) return;

            worldCanvas.renderMode = RenderMode.WorldSpace;
            worldCanvas.worldCamera = mainCamera;
            worldCanvas.overrideSorting = true;
            worldCanvas.sortingOrder = sortingOrder;
            labelTransform.localScale = Vector3.one * visibleScale;
        }

        private Vector3 GetLabelPosition()
        {
            Vector3 position = followTarget.position + worldOffset;
            Renderer[] renderers = followTarget.GetComponentsInChildren<Renderer>();
            float modelTop = float.NegativeInfinity;

            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer renderer = renderers[i];
                if (renderer.enabled)
                    modelTop = Mathf.Max(modelTop, renderer.bounds.max.y);
            }

            if (!float.IsNegativeInfinity(modelTop))
                position.y = Mathf.Max(position.y, modelTop + modelTopPadding);

            return position;
        }

        public void SetLife(int life, string displayName, bool isDead)
        {
            if (nameText != null) nameText.text = displayName;
            if (scoreText != null) scoreText.text = isDead ? "死亡" : "生命 " + life;
        }

        public void ShowTransfer(int amount)
        {
            if (transferCoroutine != null) StopCoroutine(transferCoroutine);
            transferCoroutine = StartCoroutine(TransferRoutine(amount));
        }

        private IEnumerator TransferRoutine(int amount)
        {
            if (transferBanner != null)
            {
                transferBanner.SetActive(true);
                if (transferText != null)
                    transferText.text = "+" + amount + " 生命";
            }

            yield return new WaitForSeconds(transferDuration);

            if (transferBanner != null)
                transferBanner.SetActive(false);
        }
    }
}
