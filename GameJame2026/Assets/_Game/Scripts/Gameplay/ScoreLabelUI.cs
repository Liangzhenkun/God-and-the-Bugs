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

        private Transform labelTransform;
        private Coroutine transferCoroutine;
        private UnityEngine.Camera mainCamera;

        private void Awake()
        {
            labelTransform = transform;
            mainCamera = UnityEngine.Camera.main;
            if (transferBanner != null) transferBanner.SetActive(false);
        }

        private void LateUpdate()
        {
            if (followTarget != null)
                labelTransform.position = followTarget.position + worldOffset;

            if (mainCamera == null) mainCamera = UnityEngine.Camera.main;
            if (mainCamera != null)
                labelTransform.rotation = mainCamera.transform.rotation;
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
