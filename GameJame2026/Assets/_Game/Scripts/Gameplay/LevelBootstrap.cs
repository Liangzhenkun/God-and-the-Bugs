using GameJamRAC.UI;
using UnityEngine;

namespace GameJamRAC.Gameplay
{
    /// <summary>
    /// 挂到场景根节点上，拖入关卡配置资产。
    /// Awake 时根据配置激活对应规则，替代之前的 GameObject.Find 推断逻辑。
    /// </summary>
    [DisallowMultipleComponent]
    public class LevelBootstrap : MonoBehaviour
    {
        [SerializeField] private LevelConfig config;

        private void Awake()
        {
            if (config == null)
            {
                Debug.LogWarning("[LevelBootstrap] 未配置 LevelConfig，关卡规则不会自动设置。");
                return;
            }

            ApplyConsumeRule();
            ApplyStory();
        }

        private void ApplyConsumeRule()
        {
            SoulBridgeSequence bridge = FindFirstObjectByType<SoulBridgeSequence>();

            switch (config.consumeRule)
            {
                case LevelConfig.ConsumeRule.Bridge:
                    // 桥接模式：保持 SoulBridgeSequence 运行
                    if (bridge == null)
                        Debug.LogError("[LevelBootstrap] consumeRule=Bridge 但场景中没有 SoulBridgeSequence！");
                    break;

                case LevelConfig.ConsumeRule.InteractionTile:
                    // 交互格模式：禁用桥接，靠场景中已有的 SceneThreeBConsumeSequence
                    if (bridge != null)
                    {
                        bridge.enabled = false;
                        Debug.Log("[LevelBootstrap] 已禁用 SoulBridgeSequence（InteractionTile 模式）。");
                    }
                    break;

                case LevelConfig.ConsumeRule.NeighborCheck:
                    // 近邻检测模式：禁用桥接，解锁魂穿，激活 D1 消耗序列
                    if (bridge != null)
                    {
                        bridge.enabled = false;
                        Debug.Log("[LevelBootstrap] 已禁用 SoulBridgeSequence（NeighborCheck 模式）。");
                    }
                    if (FindFirstObjectByType<D1BConsumeSequence>() == null)
                    {
                        Debug.LogWarning("[LevelBootstrap] consumeRule=NeighborCheck 但场景中没有 D1BConsumeSequence，已自动创建。建议在场景中手动放置。");
                        gameObject.AddComponent<D1BConsumeSequence>();
                    }
                    // NeighborCheck 模式下 A、B 均可独立操作，需要开局 + 复活后都保持魂穿
                    SoulSwapManager sm = FindFirstObjectByType<SoulSwapManager>();
                    if (sm != null)
                    {
                        sm.SetSoulSwapUnlocked(true);
                        sm.AlwaysAllowSwapAfterRespawn = true;
                    }
                    break;
            }
        }

        private void ApplyStory()
        {
            if (!config.hasStory)
            {
                StoryIntroController story = FindFirstObjectByType<StoryIntroController>();
                if (story != null)
                    story.gameObject.SetActive(false);
            }
        }
    }
}
