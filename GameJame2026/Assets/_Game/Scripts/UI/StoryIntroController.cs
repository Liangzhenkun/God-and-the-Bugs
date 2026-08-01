using UnityEngine;
using UnityEngine.UI;

namespace GameJamRAC.UI
{
    /// <summary>
    /// 挂到 Story 父物体上。5 个独立的页面 GameObject，点击依次显示，看完自动隐藏。
    /// </summary>
    [DisallowMultipleComponent]
    public class StoryIntroController : MonoBehaviour
    {
        [Header("按顺序拖入 5 个页面 GameObject")]
        [SerializeField] private GameObject[] storyPages;

        [Header("点击推进的 Button（全屏热区）")]
        [SerializeField] private Button advanceButton;

        [Header("Story 根物体（最后隐藏它）")]
        [SerializeField] private GameObject storyRoot;

        [Header("首帧保护（启动后忽略点击的秒数）")]
        [SerializeField] private float startupDelay = 0.3f;

        [Header("防连点间隔")]
        [SerializeField] private float cooldownSeconds = 0.4f;

        // OnStoryEnd 由 SoulSwapManager 代码自动连接，不在 Inspector 显示
        [System.NonSerialized]
        public System.Action OnStoryEnd;

        private int currentPage;
        private float readyTime;

        private void Awake()
        {
            // 重启关卡 → 跳过 Story
            if (PlayerPrefs.GetInt("GameJamRAC.SkipIntro", 0) == 1)
            {
                gameObject.SetActive(false);
                return;
            }

            // 确保初始只有第一页可见
            if (storyPages != null)
            {
                for (int i = 0; i < storyPages.Length; i++)
                {
                    if (storyPages[i] != null)
                        storyPages[i].SetActive(i == 0);
                }
            }

            currentPage = 0;
        }

        private void Start()
        {
            // 首帧保护：启动后 startupDelay 秒内不响应点击
            readyTime = Time.realtimeSinceStartup + startupDelay;

            if (advanceButton != null)
            {
                advanceButton.onClick.RemoveAllListeners();
                advanceButton.onClick.AddListener(Advance);

                // 禁用自动导航提交，避免按键意外触发
                var nav = advanceButton.navigation;
                nav.mode = Navigation.Mode.None;
                advanceButton.navigation = nav;
            }
        }

        private void OnDestroy()
        {
            if (advanceButton != null)
                advanceButton.onClick.RemoveListener(Advance);
        }

        public void Advance()
        {
            // 启动保护 & 防连点
            if (Time.realtimeSinceStartup < readyTime) return;
            readyTime = Time.realtimeSinceStartup + cooldownSeconds;

            // 隐藏当前页
            if (storyPages != null && currentPage < storyPages.Length && storyPages[currentPage] != null)
                storyPages[currentPage].SetActive(false);

            currentPage++;

            // 所有页看完 → 隐藏 Story，通知外部
            if (storyPages == null || currentPage >= storyPages.Length)
            {
                (storyRoot != null ? storyRoot : gameObject).SetActive(false);
                OnStoryEnd?.Invoke();
                return;
            }

            // 显示下一页
            if (storyPages[currentPage] != null)
                storyPages[currentPage].SetActive(true);
        }
    }
}
