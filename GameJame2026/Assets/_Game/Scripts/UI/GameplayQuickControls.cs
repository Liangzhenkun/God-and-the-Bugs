using GameJamRAC.Gameplay;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace GameJamRAC.UI
{
    [DisallowMultipleComponent]
    public class GameplayQuickControls : MonoBehaviour
    {
        [SerializeField] private SoulSwapManager soulSwapManager;
        [SerializeField] private Button focusButton;
        [SerializeField] private Button overviewButton;
        [SerializeField] private Button restartButton;

        private void Awake()
        {
            if (soulSwapManager == null) soulSwapManager = FindFirstObjectByType<SoulSwapManager>();
            if (overviewButton != null) overviewButton.onClick.AddListener(Overview);
            if (restartButton != null) restartButton.onClick.AddListener(Restart);

            // 镜头聚焦已经改为鼠标右键切换，保留旧引用仅避免旧场景序列化丢失。
            if (focusButton != null) focusButton.gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            if (overviewButton != null) overviewButton.onClick.RemoveListener(Overview);
            if (restartButton != null) restartButton.onClick.RemoveListener(Restart);
        }

        private void Overview() { if (soulSwapManager != null) soulSwapManager.ReturnToOverview(); }
        private void Restart() { SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex); }
    }
}
