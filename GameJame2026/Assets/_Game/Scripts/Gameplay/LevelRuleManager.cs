using GameJamRAC.UI;
using UnityEngine;

namespace GameJamRAC.Gameplay
{
    [DisallowMultipleComponent]
    public class LevelRuleManager : MonoBehaviour
    {
        [SerializeField] private LevelRuleConfig config;

        private void Awake()
        {
            if (config == null)
            {
                Debug.LogWarning("[LevelRuleManager] Missing LevelRuleConfig. Level rules were not applied.");
                return;
            }

            ApplyCommonGameplay();
            ValidateSceneRules();
            ApplyStory();
        }

        private void ApplyCommonGameplay()
        {
            SoulSwapManager soulSwap = FindFirstObjectByType<SoulSwapManager>();
            if (soulSwap == null) return;

            soulSwap.AlwaysAllowSwapAfterRespawn = config.keepSoulSwapAfterRespawn;
            soulSwap.SetSoulSwapUnlocked(config.unlockSoulSwapOnStart);
        }

        private void ValidateSceneRules()
        {
            switch (config.consumeRule)
            {
                case LevelRuleConfig.ConsumeRule.Bridge:
                    SetSceneComponentEnabled<BridgeConsumptionRule>(true);
                    SetSceneComponentEnabled<BInteractionConsumptionRule>(false);
                    SetSceneComponentEnabled<D1InteractionConsumptionRule>(false);
                    RequireSceneComponent<BridgeConsumptionRule>("consumeRule=Bridge");
                    break;

                case LevelRuleConfig.ConsumeRule.InteractionTile:
                    SetSceneComponentEnabled<BridgeConsumptionRule>(false);
                    SetSceneComponentEnabled<BInteractionConsumptionRule>(true);
                    SetSceneComponentEnabled<D1InteractionConsumptionRule>(false);
                    RequireSceneComponent<BInteractionConsumptionRule>("consumeRule=InteractionTile");
                    break;

                case LevelRuleConfig.ConsumeRule.NeighborCheck:
                    SetSceneComponentEnabled<BridgeConsumptionRule>(false);
                    SetSceneComponentEnabled<BInteractionConsumptionRule>(false);
                    SetSceneComponentEnabled<D1InteractionConsumptionRule>(true);
                    RequireSceneComponent<D1InteractionConsumptionRule>("consumeRule=NeighborCheck");
                    break;
            }
        }

        private static void SetSceneComponentEnabled<T>(bool enabled) where T : MonoBehaviour
        {
            foreach (T component in FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (component != null)
                    component.enabled = enabled;
            }
        }

        private static void RequireSceneComponent<T>(string ruleName) where T : MonoBehaviour
        {
            if (FindFirstObjectByType<T>(FindObjectsInactive.Include) != null) return;

            Debug.LogError("[LevelRuleManager] " + ruleName + " but the scene has no " + typeof(T).Name + ".");
        }

        private void ApplyStory()
        {
            if (config.hasStory) return;

            StoryIntroController story = FindFirstObjectByType<StoryIntroController>();
            if (story != null)
                story.gameObject.SetActive(false);
        }
    }
}
