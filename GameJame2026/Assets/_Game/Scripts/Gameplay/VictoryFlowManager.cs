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
        [SerializeField] private string nextSceneName = "NextScene 3";

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
            FreezeSceneMovement();
            if (victoryPanel != null)
                victoryPanel.SetActive(true);
        }

        private void FreezeSceneMovement()
        {
            foreach (CharacterUnit character in FindObjectsByType<CharacterUnit>(FindObjectsSortMode.None))
                character.ReleaseControl();

            foreach (Grid.GridUnitMover mover in FindObjectsByType<Grid.GridUnitMover>(FindObjectsSortMode.None))
                mover.enabled = false;

            foreach (TurnActionManager turns in FindObjectsByType<TurnActionManager>(FindObjectsSortMode.None))
                turns.enabled = false;
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
