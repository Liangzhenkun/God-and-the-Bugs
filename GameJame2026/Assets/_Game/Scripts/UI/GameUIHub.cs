using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GameJamRAC.UI
{
    /// <summary>Wires scene-authored UI together without creating any visible UI at runtime.</summary>
    [DisallowMultipleComponent]
    public class GameUIHub : MonoBehaviour
    {
        private static bool hasAutomaticallyShownGuide;

        [Header("场景 UI 引用")]
        [SerializeField] private GameplayUIReferences ui;
        [SerializeField] private bool showGuideOnSceneStart = true;

        private void Awake()
        {
            AudioSettingsState.Apply();
            if (ui == null) ui = GetComponentInChildren<GameplayUIReferences>(true);
            if (ui == null)
            {
                Debug.LogWarning("GameplayUIReferences is missing. Use GameJam/UI/Create Editable Gameplay UI.");
                return;
            }

            WireButtons();
            CloseAll();
        }

        private void Start()
        {
            if (ui != null
                && showGuideOnSceneStart
                && SceneManager.GetActiveScene().name == "MainScene"
                && !hasAutomaticallyShownGuide)
            {
                hasAutomaticallyShownGuide = true;
                StartCoroutine(ShowGuideAfterStart());
            }
        }

        private IEnumerator ShowGuideAfterStart()
        {
            yield return new WaitForSecondsRealtime(0.5f);
            ShowGuide();
        }

        public void ShowSettings() => OpenOnly(ui != null ? ui.SettingsPanel : null);
        public void ShowGuide() => OpenOnly(ui != null ? ui.GuidePanel : null);
        public void ShowCodex() => OpenOnly(ui != null ? ui.CodexPanel : null);

        public void ShowDialogue()
        {
            if (ui == null || ui.DialoguePanel == null) return;
            CloseAll();
            ui.DialoguePanel.Show();
        }

        private void WireButtons()
        {
            AddClick(ui.DialogueButton, ShowDialogue);
            AddClick(ui.CodexButton, ShowCodex);
            AddClick(ui.HelpButton, ShowGuide);
            AddClick(ui.SettingsButton, ShowSettings);
            AddClick(ui.SettingsCloseButton, CloseAll);
            AddClick(ui.ReturnToTitleButton, ReturnToTitle);
            AddClick(ui.ExitGameButton, ExitGame);
            AddClick(ui.CreditsButton, ShowCredits);
            AddClick(ui.GuideCloseButton, CloseAll);
            AddClick(ui.CodexCloseButton, CloseAll);
            AddClick(ui.CreditsCloseButton, CloseAll);
        }

        private static void AddClick(UnityEngine.UI.Button button, UnityEngine.Events.UnityAction action)
        {
            if (button != null) button.onClick.AddListener(action);
        }

        private void ShowCredits() => OpenOnly(ui != null ? ui.CreditsPanel : null);

        private void ReturnToTitle()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene("TitleScene");
        }

        private void ExitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private void OpenOnly(GameObject panel)
        {
            if (panel == null) return;
            CloseAll();
            panel.SetActive(true);
            Time.timeScale = 0f;
        }

        private void CloseAll()
        {
            if (ui == null) return;
            if (ui.SettingsPanel != null) ui.SettingsPanel.SetActive(false);
            if (ui.GuidePanel != null) ui.GuidePanel.SetActive(false);
            if (ui.DialoguePanel != null) ui.DialoguePanel.Hide();
            if (ui.CodexPanel != null) ui.CodexPanel.SetActive(false);
            if (ui.CreditsPanel != null) ui.CreditsPanel.SetActive(false);
            Time.timeScale = 1f;
        }
    }
}
