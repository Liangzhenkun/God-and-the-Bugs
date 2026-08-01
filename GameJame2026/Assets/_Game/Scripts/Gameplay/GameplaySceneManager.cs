using System.Collections;
using System.Collections.Generic;
using GameJamRAC.UI;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace GameJamRAC.Gameplay
{
    /// <summary>统一管理关卡 UI、通关流程、下一关和最终证书。</summary>
    [DisallowMultipleComponent]
    public sealed class GameplaySceneManager : MonoBehaviour
    {
        private static bool hasAutomaticallyShownGuide;

        [Header("场景 UI 引用")]
        [SerializeField] private GameplayUIReferences ui;
        [SerializeField] private bool showGuideOnSceneStart = true;

        [Header("关卡顺序")]
        [SerializeField] private LevelSequence levelSequence;
        [SerializeField] private List<string> fallbackLevelOrder = new List<string>
        {
            "MainScene",
            "NextScene 2",
            "NextScene 3"
        };
        [SerializeField] private string fallbackTitleSceneName = "TitleScene";

        [Header("通关流程")]
        [SerializeField] private GameObject victoryPanel;
        [SerializeField] private Button nextLevelButton;

        [Header("最终关奖励")]
        [SerializeField] private GameObject certificatePanel;
        [SerializeField] private Button certificateCloseButton;
        [SerializeField] private Button certificateAcceptButton;

        private bool hasWon;

        public bool HasWon => hasWon;
        private bool IsFinalScene
        {
            get
            {
                string currentSceneName = SceneManager.GetActiveScene().name;
                if (levelSequence != null)
                    return levelSequence.IsFinalScene(currentSceneName);

                return fallbackLevelOrder.Count > 0 && fallbackLevelOrder[fallbackLevelOrder.Count - 1] == currentSceneName;
            }
        }

        private void Awake()
        {
            AudioSettingsState.Apply();
            ResolveReferences();
            WireGameplayButtons();
            WireVictoryButtons();
            CloseAllPanels();

            if (victoryPanel != null) victoryPanel.SetActive(false);
            if (certificatePanel != null) certificatePanel.SetActive(false);
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

        public static GameplaySceneManager GetActive()
        {
            GameplaySceneManager manager = FindFirstObjectByType<GameplaySceneManager>();
            if (manager != null) return manager;

            return null;
        }

        public void Win()
        {
            if (hasWon) return;

            hasWon = true;
            FreezeSceneMovement();

            if (IsFinalScene)
            {
                ShowCertificatePanel();
                return;
            }

            if (victoryPanel != null)
                victoryPanel.SetActive(true);
        }

        public void LoadNextLevel()
        {
            if (IsFinalScene)
            {
                ShowCertificatePanel();
                return;
            }

            string currentSceneName = SceneManager.GetActiveScene().name;
            if (TryGetNextSceneName(currentSceneName, out string nextSceneName))
                LoadSceneByName(nextSceneName);
            else
                Debug.LogError("Current scene has no configured next level: " + currentSceneName);
        }

        public void RestartCurrentScene()
        {
            PlayerPrefs.SetInt("GameJamRAC.SkipIntro", 1);
            PlayerPrefs.Save();
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        public void ReturnToTitle()
        {
            PlayerPrefs.DeleteKey("GameJamRAC.SkipIntro");
            PlayerPrefs.Save();

            string titleSceneName = levelSequence != null && !string.IsNullOrWhiteSpace(levelSequence.TitleSceneName)
                ? levelSequence.TitleSceneName
                : fallbackTitleSceneName;

            LoadSceneByName(titleSceneName);
        }

        private void ResolveReferences()
        {
            if (ui == null)
                ui = GetComponentInChildren<GameplayUIReferences>(true);

            if (victoryPanel == null)
                victoryPanel = FindSceneObject("VictoryPanel");

            if (nextLevelButton == null)
                nextLevelButton = FindButton("NextLevelButton", victoryPanel != null ? victoryPanel.transform : null);

            if (certificatePanel == null)
                certificatePanel = FindSceneObject("CertificateRewardPanel");

            if (certificateCloseButton == null)
                certificateCloseButton = FindButton("Close", certificatePanel != null ? certificatePanel.transform : null);

            if (certificateAcceptButton == null)
                certificateAcceptButton = FindButton("Accept", certificatePanel != null ? certificatePanel.transform : null);
        }

        private void WireGameplayButtons()
        {
            if (ui != null)
            {
                AddClick(ui.DialogueButton, ShowDialogue);
                AddClick(ui.CodexButton, ShowCodex);
                AddClick(ui.HelpButton, ShowGuide);
                AddClick(ui.SettingsButton, ShowSettings);
                AddClick(ui.SettingsCloseButton, CloseAllPanels);
                AddClick(ui.ReturnToTitleButton, ReturnToTitle);
                AddClick(ui.ExitGameButton, ExitGame);
                AddClick(ui.CreditsButton, ShowCredits);
                AddClick(ui.GuideCloseButton, CloseAllPanels);
                AddClick(ui.CodexCloseButton, CloseAllPanels);
                AddClick(ui.CreditsCloseButton, CloseAllPanels);
            }

            Button restartButton = FindButton("RestartButton", null);
            AddClick(restartButton, RestartCurrentScene);
        }

        private void WireVictoryButtons()
        {
            AddClick(nextLevelButton, LoadNextLevel);
            AddClick(certificateCloseButton, ReturnToTitle);
            AddClick(certificateAcceptButton, ReturnToTitle);
        }

        private void FreezeSceneMovement()
        {
            foreach (CharacterUnit character in FindObjectsByType<CharacterUnit>(FindObjectsSortMode.None))
                character.ReleaseControl();

            foreach (GameJamRAC.Grid.GridUnitMover mover in FindObjectsByType<GameJamRAC.Grid.GridUnitMover>(FindObjectsSortMode.None))
                mover.enabled = false;

            foreach (TurnActionManager turns in FindObjectsByType<TurnActionManager>(FindObjectsSortMode.None))
                turns.enabled = false;
        }

        private IEnumerator ShowGuideAfterStart()
        {
            yield return new WaitForSecondsRealtime(0.5f);
            ShowGuide();
        }

        private void ShowSettings() => OpenOnly(ui != null ? ui.SettingsPanel : null);
        private void ShowGuide() => OpenOnly(ui != null ? ui.GuidePanel : null);
        private void ShowCodex() => OpenOnly(ui != null ? ui.CodexPanel : null);
        private void ShowCredits() => OpenOnly(ui != null ? ui.CreditsPanel : null);

        private void ShowDialogue()
        {
            if (ui == null || ui.DialoguePanel == null) return;
            CloseAllPanels();
            ui.DialoguePanel.Show();
        }

        private void ShowCertificatePanel()
        {
            if (victoryPanel != null) victoryPanel.SetActive(false);
            if (certificatePanel != null) certificatePanel.SetActive(true);
        }

        private void OpenOnly(GameObject panel)
        {
            if (panel == null) return;
            CloseAllPanels();
            panel.SetActive(true);
            Time.timeScale = 0f;
        }

        private void CloseAllPanels()
        {
            if (ui != null)
            {
                if (ui.SettingsPanel != null) ui.SettingsPanel.SetActive(false);
                if (ui.GuidePanel != null) ui.GuidePanel.SetActive(false);
                if (ui.DialoguePanel != null) ui.DialoguePanel.Hide();
                if (ui.CodexPanel != null) ui.CodexPanel.SetActive(false);
                if (ui.CreditsPanel != null) ui.CreditsPanel.SetActive(false);
            }

            Time.timeScale = 1f;
        }

        private static void AddClick(Button button, UnityEngine.Events.UnityAction action)
        {
            if (button == null) return;
            button.onClick.RemoveListener(action);
            button.onClick.AddListener(action);
        }

        private static GameObject FindSceneObject(string objectName)
        {
            foreach (Transform transform in FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (transform.name == objectName)
                    return transform.gameObject;
            }

            return null;
        }

        private static Button FindButton(string objectName, Transform preferredRoot)
        {
            if (preferredRoot != null)
            {
                foreach (Button button in preferredRoot.GetComponentsInChildren<Button>(true))
                    if (button.name == objectName)
                        return button;
            }

            GameObject buttonObject = FindSceneObject(objectName);
            return buttonObject != null ? buttonObject.GetComponent<Button>() : null;
        }

        private bool TryGetNextSceneName(string currentSceneName, out string nextSceneName)
        {
            if (levelSequence != null && levelSequence.TryGetNextSceneName(currentSceneName, out nextSceneName))
                return true;

            for (int i = 0; i < fallbackLevelOrder.Count - 1; i++)
            {
                if (fallbackLevelOrder[i] == currentSceneName)
                {
                    nextSceneName = fallbackLevelOrder[i + 1];
                    return !string.IsNullOrWhiteSpace(nextSceneName);
                }
            }

            nextSceneName = string.Empty;
            return false;
        }

        private static void LoadSceneByName(string sceneName)
        {
            Time.timeScale = 1f;
            if (Application.CanStreamedLevelBeLoaded(sceneName))
                SceneManager.LoadScene(sceneName);
            else
                Debug.LogError("Scene is missing or not included in Build Settings: " + sceneName);
        }

        private static void ExitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
