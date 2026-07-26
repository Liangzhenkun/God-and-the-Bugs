using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

namespace GameJamRAC.UI
{
    /// <summary>
    /// 标题画面 / 主菜单 UI 控制器。
    /// 不生成任何 UI —— 只负责在 Play 模式下按名称找到场景中已有的按钮，接上点击事件。
    /// 所有主菜单相关的页面切换、弹窗控制都在这里统一管理。
    ///
    /// 需要的场景结构（手动或通过 MCP 搭建）：
    ///   Canvas/
    ///     ├── CoverArt (Image)
    ///     └── _GeneratedTitleUI/
    ///           ├── MenuPanel/
    ///           │     ├── GameTitle (Text)
    ///           │     ├── StartButton (Button)
    ///           │     ├── SettingsButton (Button)
    ///           │     └── ExitButton (Button)
    ///           ├── CreditsButton (Button, 底部居中)
    ///           ├── CreditsOverlay/...
    ///           ├── SettingsOverlay/...
    ///           └── GameplayOverlay/...
    /// </summary>
    [DisallowMultipleComponent]
    public class TitleScreenUIController : MonoBehaviour
    {
        [Header("场景引用（可拖拽赋值或让脚本按名称自动查找）")]
        [SerializeField] private Canvas targetCanvas;
        [SerializeField] private GameObject menuPanel;
        [SerializeField] private GameObject creditsOverlay;
        [SerializeField] private GameObject settingsOverlay;
        [SerializeField] private GameObject gameplayOverlay;

        [Header("音量控制（自动查找）")]

        [Header("流程配置")]
        [SerializeField] private string gameplaySceneName = "";

        private void OnEnable()
        {
            // 编辑器模式下不执行，只在 Play 时接线
            if (!Application.isPlaying) return;
            EnsureEventSystem();
            ResolveReferences();
            AudioSettingsState.Apply();
            UIRuntimeFont.ApplyTo(targetCanvas != null ? targetCanvas.transform : null);
            // 声音按钮改为场景层级组件，不再由脚本临时创建。
            WireAllButtons();
        }

        private void Start()
        {
            if (!Application.isPlaying) return;
            // OnEnable 通常已处理完毕，Start 是兜底
            if (menuPanel == null)
            {
                ResolveReferences();
                WireAllButtons();
            }
        }

        /// <summary>
        /// 按名称在场景中查找所有已知 UI 元素。Play 模式自动调用，也可在 Inspector 右键菜单手动触发。
        /// </summary>
        [ContextMenu("按名称查找引用")]
        public void ResolveReferences()
        {
            if (targetCanvas == null)
            {
                targetCanvas = GetComponentInParent<Canvas>();
                if (targetCanvas == null) targetCanvas = FindFirstObjectByType<Canvas>();
            }
            if (targetCanvas == null) return;

            var root = targetCanvas.transform.Find("_GeneratedTitleUI");
            if (root == null) return;

            menuPanel = FindChild(root.gameObject, "MenuPanel");
            creditsOverlay = FindChild(root.gameObject, "CreditsOverlay");
            settingsOverlay = FindChild(root.gameObject, "SettingsOverlay");
            gameplayOverlay = FindChild(root.gameObject, "GameplayOverlay");

        }

        /// <summary>
        /// 为所有已知名称的按钮绑定点击事件。Play 模式自动调用。
        /// 后续新增的按钮在这里追加 WireButtonIn / WireDeepButton 即可。
        /// </summary>
        [ContextMenu("绑定所有按钮事件")]
        public void WireAllButtons()
        {
            // 菜单面板按钮
            WireButtonIn(menuPanel, "StartButton", StartGame);
            WireButtonIn(menuPanel, "SettingsButton", OpenSettings);
            WireButtonIn(menuPanel, "ExitButton", ExitGame);

            // 底部独立的 Credits 按钮
            if (targetCanvas != null)
            {
                var root = targetCanvas.transform.Find("_GeneratedTitleUI");
                if (root != null)
                    WireButtonIn(root.gameObject, "CreditsButton", OpenCredits);
            }

            // 各弹窗的关闭按钮
            WireDeepButton(creditsOverlay, "CreditsCloseButton", CloseAllOverlays);
            WireDeepButton(gameplayOverlay, "BackToTitleButton", ShowTitle);

            // 音量控件
            ConfigureSettingsOverlay();
        }

        // ── 按钮回调 ────────────────────────

        public void StartGame()
        {
            // 如果配置了场景名且可加载，直接切换场景
            if (!string.IsNullOrWhiteSpace(gameplaySceneName) &&
                Application.CanStreamedLevelBeLoaded(gameplaySceneName))
            {
                SceneManager.LoadScene(gameplaySceneName);
                return;
            }
            // 否则显示游戏内占位覆盖层
            OpenExclusiveOverlay(gameplayOverlay);
        }

        public void ExitGame()
        {
#if UNITY_EDITOR
            // 编辑器内：退出 Play 模式
            UnityEditor.EditorApplication.isPlaying = false;
#else
            // 打包后：退出应用
            Application.Quit();
#endif
        }

        public void OpenCredits() => OpenOverlay(creditsOverlay);
        public void OpenSettings() => OpenOverlay(settingsOverlay);
        public void ShowTitle() => CloseAllOverlays();

        public void CloseAllOverlays()
        {
            SetActive(creditsOverlay, false);
            SetActive(settingsOverlay, false);
            SetActive(gameplayOverlay, false);
            SetActive(menuPanel, true);
        }

        /// <summary>打开覆盖层，菜单面板保持可见</summary>
        private void OpenOverlay(GameObject overlay)
        {
            SetActive(menuPanel, true);
            SetActive(creditsOverlay, false);
            SetActive(settingsOverlay, false);
            SetActive(gameplayOverlay, false);
            SetActive(overlay, true);
        }

        /// <summary>打开覆盖层并隐藏菜单面板（用于游戏中画面）</summary>
        private void OpenExclusiveOverlay(GameObject overlay)
        {
            SetActive(menuPanel, false);
            SetActive(creditsOverlay, false);
            SetActive(settingsOverlay, false);
            SetActive(gameplayOverlay, false);
            SetActive(overlay, true);
        }

        // ── 内部工具方法 ────────────────────

        private void ConfigureSettingsOverlay()
        {
            if (settingsOverlay == null) return;

            Transform existing = settingsOverlay.transform.Find("SimpleSettingsContent");
            if (existing != null)
            {
                existing.gameObject.SetActive(true);
                return;
            }

            foreach (Transform child in settingsOverlay.transform)
                child.gameObject.SetActive(false);

            GameObject content = CreateObject("SimpleSettingsContent", settingsOverlay.transform, typeof(Image));
            content.GetComponent<Image>().color = new Color(0.05f, 0.08f, 0.16f, 0.80f);
            Stretch(content.GetComponent<RectTransform>());
            GameObject card = CreateObject("Card", content.transform, typeof(Image));
            card.GetComponent<Image>().color = new Color(0.96f, 0.98f, 1f, 0.96f);
            SetRect(card.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(620f, 440f));
            CreateText(card.transform, "Title", "\u8BBE\u7F6E", new Vector2(0.5f, 0.84f), Vector2.zero, new Vector2(420f, 60f), 38, TextAnchor.MiddleCenter);
            CreateButton(card.transform, "MainMenu", "\u56DE\u5230\u4E3B\u83DC\u5355", new Vector2(0.5f, 0.59f), Vector2.zero, new Vector2(280f, 58f), CloseAllOverlays);
            CreateButton(card.transform, "ExitGame", "\u9000\u51FA\u6E38\u620F", new Vector2(0.5f, 0.39f), Vector2.zero, new Vector2(280f, 58f), ExitGame);
            CreateButton(card.transform, "Credits", "\u5236\u4F5C\u7EC4", new Vector2(0.5f, 0.19f), Vector2.zero, new Vector2(280f, 58f), OpenCredits);
        }

        private static GameObject CreateObject(string name, Transform parent, params System.Type[] components)
        {
            GameObject gameObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer));
            foreach (System.Type component in components) gameObject.AddComponent(component);
            gameObject.transform.SetParent(parent, false);
            return gameObject;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static void SetRect(RectTransform rect, Vector2 anchor, Vector2 position, Vector2 size)
        {
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        private static Text CreateText(Transform parent, string name, string value, Vector2 anchor, Vector2 position, Vector2 size, int fontSize, TextAnchor alignment)
        {
            GameObject textObject = CreateObject(name, parent, typeof(Text));
            Text text = textObject.GetComponent<Text>();
            text.font = UIRuntimeFont.Resolve();
            text.text = value;
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.color = new Color(0.12f, 0.16f, 0.23f, 1f);
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            SetRect(text.rectTransform, anchor, position, size);
            return text;
        }

        private static Button CreateButton(Transform parent, string name, string label, Vector2 anchor, Vector2 position, Vector2 size, UnityEngine.Events.UnityAction action)
        {
            GameObject buttonObject = CreateObject(name, parent, typeof(Image), typeof(Button));
            buttonObject.GetComponent<Image>().color = new Color(0.18f, 0.47f, 0.78f, 0.94f);
            SetRect(buttonObject.GetComponent<RectTransform>(), anchor, position, size);
            Button button = buttonObject.GetComponent<Button>();
            button.onClick.AddListener(action);
            Text text = CreateText(buttonObject.transform, "Label", label, new Vector2(0.5f, 0.5f), Vector2.zero, size, 25, TextAnchor.MiddleCenter);
            text.color = Color.white;
            return button;
        }

        private static void SetActive(GameObject go, bool active)
        {
            if (go != null) go.SetActive(active);
        }

        /// <summary>在当前层级的直接子对象中按名称查找</summary>
        private static GameObject FindChild(GameObject parent, string childName)
        {
            if (parent == null) return null;
            var t = parent.transform.Find(childName);
            return t != null ? t.gameObject : null;
        }

        /// <summary>在父对象的直接子级中按名称找按钮并绑定事件</summary>
        private static void WireButtonIn(GameObject parent, string name, UnityEngine.Events.UnityAction action)
        {
            var go = FindChild(parent, name);
            if (go == null) return;
            var btn = go.GetComponent<Button>();
            if (btn != null)
            {
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(action);
            }
        }

        /// <summary>在根对象的全部后代中按名称深度查找按钮并绑定事件</summary>
        private static void WireDeepButton(GameObject root, string name, UnityEngine.Events.UnityAction action)
        {
            if (root == null) return;
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
            {
                if (t.name == name)
                {
                    var btn = t.GetComponent<Button>();
                    if (btn != null)
                    {
                        btn.onClick.RemoveAllListeners();
                        btn.onClick.AddListener(action);
                    }
                    return;
                }
            }
        }

        /// <summary>确保场景中存在 EventSystem，没有则创建一个</summary>
        private static void EnsureEventSystem()
        {
            if (FindFirstObjectByType<EventSystem>() != null) return;
            var go = new GameObject("EventSystem", typeof(EventSystem));
#if ENABLE_INPUT_SYSTEM
            go.AddComponent<InputSystemUIInputModule>();
#else
            go.AddComponent<StandaloneInputModule>();
#endif
        }
    }
}
