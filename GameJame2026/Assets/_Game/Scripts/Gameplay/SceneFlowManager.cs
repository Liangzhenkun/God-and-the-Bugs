using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GameJamRAC.Gameplay
{
    /// <summary>Scene flow manager for title, next level, and configured level order.</summary>
    [DisallowMultipleComponent]
    public sealed class SceneFlowManager : MonoBehaviour
    {
        [SerializeField] private LevelSequence levelSequence;
        [SerializeField] private List<string> fallbackLevelOrder = new List<string>
        {
            "MainScene",
            "NextScene 2",
            "NextScene 3"
        };
        [SerializeField] private string fallbackTitleSceneName = "TitleScene";

        public bool IsFinalScene
        {
            get
            {
                string currentSceneName = SceneManager.GetActiveScene().name;
                if (levelSequence != null)
                    return levelSequence.IsFinalScene(currentSceneName);

                return fallbackLevelOrder.Count > 0 && fallbackLevelOrder[fallbackLevelOrder.Count - 1] == currentSceneName;
            }
        }

        public static SceneFlowManager GetOrCreate()
        {
            SceneFlowManager manager = FindFirstObjectByType<SceneFlowManager>();
            if (manager != null)
                return manager;

            GameObject gameObject = new GameObject("SceneFlowManager");
            return gameObject.AddComponent<SceneFlowManager>();
        }

        public void LoadNextLevel()
        {
            string currentSceneName = SceneManager.GetActiveScene().name;
            if (!TryGetNextSceneName(currentSceneName, out string nextSceneName))
            {
                Debug.LogError("Current scene has no configured next level: " + currentSceneName);
                return;
            }

            LoadSceneByName(nextSceneName);
        }

        public void ReturnToTitle()
        {
            if (levelSequence == null || string.IsNullOrWhiteSpace(levelSequence.TitleSceneName))
            {
                LoadSceneByName(fallbackTitleSceneName);
                return;
            }

            LoadSceneByName(levelSequence.TitleSceneName);
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
    }
}
