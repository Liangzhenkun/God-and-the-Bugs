using System;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace GameJamRAC.Gameplay
{
    /// <summary>Configured level order. Drag scenes in the Inspector and reorder the list.</summary>
    [CreateAssetMenu(menuName = "GameJamRAC/Level Sequence", fileName = "LevelSequence")]
    public sealed class LevelSequence : ScriptableObject
    {
        [SerializeField] private string titleSceneName = "TitleScene";
        [SerializeField] private List<SceneEntry> levels = new List<SceneEntry>();

        public string TitleSceneName => titleSceneName;

        public bool IsFinalScene(string sceneName)
        {
            return levels.Count > 0 && levels[levels.Count - 1].SceneName == sceneName;
        }

        public bool TryGetNextSceneName(string currentSceneName, out string nextSceneName)
        {
            for (int i = 0; i < levels.Count - 1; i++)
            {
                if (levels[i].SceneName == currentSceneName)
                {
                    nextSceneName = levels[i + 1].SceneName;
                    return !string.IsNullOrWhiteSpace(nextSceneName);
                }
            }

            nextSceneName = string.Empty;
            return false;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            foreach (SceneEntry level in levels)
                level.SyncSceneName();
        }
#endif

        [Serializable]
        public sealed class SceneEntry
        {
#if UNITY_EDITOR
            [SerializeField] private SceneAsset sceneAsset;
#endif
            [SerializeField] private string sceneName;

            public string SceneName => sceneName;

#if UNITY_EDITOR
            public void SyncSceneName()
            {
                if (sceneAsset != null)
                    sceneName = sceneAsset.name;
            }
#endif
        }
    }
}
