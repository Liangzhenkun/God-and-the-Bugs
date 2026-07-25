using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace GameJamRAC.Gameplay
{
    /// <summary>显示胜利面板并加载下一关场景。</summary>
    [DisallowMultipleComponent]
    public class VictoryFlowManager : MonoBehaviour
    {
        [SerializeField] private GameObject victoryPanel;
        [SerializeField] private Button nextLevelButton;
        [SerializeField] private string nextSceneName = "NextScene";

        private bool hasWon;
        public bool HasWon => hasWon;

        private void Awake()
        {
            if (victoryPanel != null)
                victoryPanel.SetActive(false);

            if (nextLevelButton != null)
                nextLevelButton.onClick.AddListener(LoadNextLevel);
        }

        private void OnDestroy()
        {
            if (nextLevelButton != null)
                nextLevelButton.onClick.RemoveListener(LoadNextLevel);
        }

        public void Win()
        {
            if (hasWon) return;

            hasWon = true;
            if (victoryPanel != null)
                victoryPanel.SetActive(true);
        }

        public void LoadNextLevel()
        {
            if (Application.CanStreamedLevelBeLoaded(nextSceneName))
                SceneManager.LoadScene(nextSceneName);
            else
                Debug.LogError("找不到下一关场景：" + nextSceneName);
        }
    }
}
