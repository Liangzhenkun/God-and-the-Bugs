using UnityEngine;
using UnityEngine.UI;

namespace GameJamRAC.UI
{
    /// <summary>
    /// 游戏主场景 HUD 控制器：管理魂穿按钮等游戏内 UI。
    /// 后续可在此扩展：暂停菜单、角色状态面板、小地图等。
    /// </summary>
    public class GameplayUIController : MonoBehaviour
    {
        [Header("魂穿系统引用")]
        [SerializeField] private Gameplay.SoulSwapManager soulSwapManager;
        [SerializeField] private Button swapButton;
        [SerializeField] private Text swapButtonLabel;
        [SerializeField] private Text statePrompt;

        [Header("状态标识")]
        [SerializeField] private GameObject panelAPossessing;
        [SerializeField] private GameObject panelBPossessing;

        private void Start()
        {
            // 监听状态变化以更新 UI
            if (soulSwapManager != null)
                soulSwapManager.onStateChanged += OnSoulSwapStateChanged;
        }

        private void OnDestroy()
        {
            if (soulSwapManager != null)
                soulSwapManager.onStateChanged -= OnSoulSwapStateChanged;
        }

        private void OnSoulSwapStateChanged(Gameplay.SoulSwapManager.SwapState state)
        {
            if (panelAPossessing != null)
                panelAPossessing.SetActive(state == Gameplay.SoulSwapManager.SwapState.PossessingA);

            if (panelBPossessing != null)
                panelBPossessing.SetActive(state == Gameplay.SoulSwapManager.SwapState.PossessingB);
        }
    }
}
