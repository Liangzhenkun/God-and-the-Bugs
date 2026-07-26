using System.Collections;
using GameJamRAC.Gameplay;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace GameJamRAC.UI
{
    /// <summary>Creates the prototype menu, help, dialogue and codex UI under the gameplay canvas.</summary>
    [DisallowMultipleComponent]
    public class GameUIHub : MonoBehaviour
    {
        // Static so a scene reload from restart does not show the first-run guide again.
        private static bool hasAutomaticallyShownGuide;

        [Header("Replaceable artwork slots")]
        [SerializeField] private Sprite[] codexPortraitSlots;
        [SerializeField] private Sprite[] dialoguePortraitSlots;
        [SerializeField] private bool showGuideOnSceneStart = true;

        private Font font;
        private GameObject settingsPanel;
        private GameObject mainMenuPanel;
        private GameObject guidePanel;
        private GameObject dialoguePanel;
        private GameObject codexPanel;
        private Text dialogueBody;
        private Text dialogueSpeaker;
        private Image dialoguePortrait;
        private int dialogueIndex;
        private readonly string[] dialogueLines =
        {
            "\u8FD9\u7247\u68CB\u76D8\u4F1A\u8BB0\u4F4F\u6BCF\u4E00\u6B21\u9009\u62E9\u3002",
            "\u6211\u5FC5\u987B\u627E\u5230\u901A\u5F80\u51FA\u53E3\u7684\u9053\u8DEF\u3002",
            "\u501F\u6765\u7684\u751F\u547D\uFF0C\u4E5F\u4F1A\u7559\u4E0B\u75D5\u8FF9\u3002"
        };

        private void Awake()
        {
            Text existingText = GetComponentInChildren<Text>(true);
            font = existingText != null ? existingText.font : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            BuildUI();
        }

        private void Start()
        {
            if (showGuideOnSceneStart
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

        public void ShowSettings() { OpenOnly(settingsPanel); }
        public void ShowGuide() { OpenOnly(guidePanel); }
        public void ShowCodex() { RefreshCodex(); OpenOnly(codexPanel); }
        public void ShowDialogue() { dialogueIndex = 0; RefreshDialogue(); OpenOnly(dialoguePanel); }

        private void BuildUI()
        {
            Transform oldRoot = transform.Find("GameUIHubRuntime");
            if (oldRoot != null) Destroy(oldRoot.gameObject);

            GameObject root = CreateObject("GameUIHubRuntime", transform);
            Stretch(root.GetComponent<RectTransform>());
            BuildTopBar(root.transform);
            settingsPanel = BuildSettings(root.transform);
            mainMenuPanel = BuildMainMenu(root.transform);
            guidePanel = BuildGuide(root.transform);
            dialoguePanel = BuildDialogue(root.transform);
            codexPanel = BuildCodex(root.transform);
            CloseAll();
        }

        private void BuildTopBar(Transform parent)
        {
            CreateButton(parent, "DialogueButton", "\u5267\u60C5", new Vector2(0f, 1f), new Vector2(212f, -54f), new Vector2(92f, 64f), ShowDialogue);
            CreateButton(parent, "CodexButton", "\u56FE\u9274", new Vector2(1f, 1f), new Vector2(-250f, -54f), new Vector2(92f, 64f), ShowCodex);
            CreateButton(parent, "HelpButton", "?", new Vector2(1f, 1f), new Vector2(-122f, -54f), new Vector2(64f, 64f), ShowGuide);
            CreateButton(parent, "SettingsButton", "\u2699", new Vector2(1f, 1f), new Vector2(-34f, -54f), new Vector2(64f, 64f), ShowSettings);
            CreateText(parent, "CameraHint", "\u9F20\u6807\u53F3\u952E\u7F29\u653E\u89C6\u89D2", new Vector2(1f, 0f), new Vector2(-220f, 34f), new Vector2(420f, 52f), 28, TextAnchor.MiddleRight).color = new Color(1f, 1f, 1f, 0.92f);
        }

        private GameObject BuildSettings(Transform parent)
        {
            GameObject panel = CreatePanel(parent, "SettingsPanel", new Vector2(760f, 500f));
            CreateText(panel.transform, "Title", "\u8BBE\u7F6E", new Vector2(0.5f, 1f), new Vector2(0f, -55f), new Vector2(500f, 60f), 38, TextAnchor.MiddleCenter);
            CreateButton(panel.transform, "Close", "\u00D7", new Vector2(1f, 1f), new Vector2(-30f, -30f), new Vector2(56f, 56f), CloseAll, new Color(0.85f, 0.18f, 0.18f, 1f));
            CreateText(panel.transform, "VolumeLabel", "\u97F3\u91CF", new Vector2(0.5f, 0.62f), new Vector2(-130f, 0f), new Vector2(150f, 44f), 28, TextAnchor.MiddleRight);
            GameObject sliderObject = CreateObject("VolumeSlider", panel.transform, typeof(Slider), typeof(Image));
            RectTransform sliderRect = sliderObject.GetComponent<RectTransform>();
            SetRect(sliderRect, new Vector2(0.5f, 0.62f), new Vector2(80f, 0f), new Vector2(320f, 36f));
            Slider slider = sliderObject.GetComponent<Slider>(); slider.minValue = 0f; slider.maxValue = 1f; slider.value = AudioListener.volume;
            sliderObject.GetComponent<Image>().color = new Color(0.2f, 0.35f, 0.55f, 0.75f);
            slider.onValueChanged.AddListener(value => AudioListener.volume = value);
            CreateButton(panel.transform, "Reset", "\u91CD\u65B0\u5F00\u59CB", new Vector2(0.5f, 0.44f), Vector2.zero, new Vector2(260f, 58f), RestartScene);
            CreateButton(panel.transform, "MainMenu", "\u8FD4\u56DE\u4E3B\u83DC\u5355", new Vector2(0.5f, 0.30f), Vector2.zero, new Vector2(260f, 58f), ShowMainMenu);
            CreateButton(panel.transform, "ExitSettings", "\u9000\u51FA\u8BBE\u7F6E", new Vector2(0.5f, 0.16f), Vector2.zero, new Vector2(260f, 58f), CloseAll);
            return panel;
        }

        private GameObject BuildMainMenu(Transform parent)
        {
            GameObject panel = CreatePanel(parent, "MainMenuPanel", new Vector2(700f, 440f));
            CreateText(panel.transform, "Title", "\u4E3B\u83DC\u5355", new Vector2(0.5f, 0.78f), Vector2.zero, new Vector2(400f, 60f), 40, TextAnchor.MiddleCenter);
            CreateButton(panel.transform, "Continue", "\u7EE7\u7EED\u6E38\u620F", new Vector2(0.5f, 0.50f), Vector2.zero, new Vector2(260f, 58f), CloseAll);
            CreateButton(panel.transform, "Restart", "\u91CD\u65B0\u5F00\u59CB", new Vector2(0.5f, 0.32f), Vector2.zero, new Vector2(260f, 58f), RestartScene);
            CreateButton(panel.transform, "Back", "\u8FD4\u56DE\u8BBE\u7F6E", new Vector2(0.5f, 0.14f), Vector2.zero, new Vector2(260f, 58f), ShowSettings);
            return panel;
        }

        private GameObject BuildGuide(Transform parent)
        {
            GameObject panel = CreatePanel(parent, "GuidePanel", new Vector2(960f, 620f));
            CreateText(panel.transform, "Title", "\u6E38\u620F\u5E2E\u52A9", new Vector2(0.5f, 1f), new Vector2(0f, -55f), new Vector2(600f, 60f), 40, TextAnchor.MiddleCenter);
            CreateButton(panel.transform, "Close", "\u00D7", new Vector2(1f, 1f), new Vector2(-30f, -30f), new Vector2(56f, 56f), CloseAll, new Color(0.85f, 0.18f, 0.18f, 1f));
            CreateText(panel.transform, "GuideText", "\u70B9\u51FB\u53EF\u884C\u8D70\u683C\u5B50\u79FB\u52A8\u89D2\u8272\u3002\n\u5B8C\u6210 A \u7684\u4EA4\u4E92\u540E\u53EF\u542F\u7528\u9B42\u7A7F\u3002\n\u9F20\u6807\u53F3\u952E\u5728\u89D2\u8272\u89C6\u89D2\u4E0E\u5168\u5C40\u89C6\u89D2\u95F4\u5207\u6362\u3002\n\u4E0D\u540C\u89D2\u8272\u7684\u884C\u52A8\u8303\u56F4\u4E0D\u540C\uFF0C\u76EE\u6807\u662F\u627E\u5230\u51FA\u53E3\u3002", new Vector2(0.5f, 0.48f), Vector2.zero, new Vector2(760f, 340f), 28, TextAnchor.UpperLeft);
            return panel;
        }

        private GameObject BuildDialogue(Transform parent)
        {
            GameObject panel = CreatePanel(parent, "DialoguePanel", new Vector2(1260f, 720f));
            dialoguePortrait = CreateImage(panel.transform, "PortraitSlot", new Vector2(0.25f, 0.59f), Vector2.zero, new Vector2(430f, 420f), new Color(0.28f, 0.34f, 0.45f, 0.85f));
            dialogueSpeaker = CreateText(panel.transform, "Speaker", "\u795E\u79D8\u4EBA", new Vector2(0.12f, 0.20f), Vector2.zero, new Vector2(220f, 52f), 28, TextAnchor.MiddleCenter);
            GameObject box = CreatePanel(panel.transform, "DialogueBox", new Vector2(980f, 180f));
            SetRect(box.GetComponent<RectTransform>(), new Vector2(0.56f, 0.20f), Vector2.zero, new Vector2(980f, 180f));
            dialogueBody = CreateText(box.transform, "DialogueText", string.Empty, new Vector2(0.5f, 0.55f), Vector2.zero, new Vector2(860f, 110f), 28, TextAnchor.UpperLeft);
            CreateButton(panel.transform, "Next", "\u4E0B\u4E00\u6B65", new Vector2(0.88f, 0.08f), Vector2.zero, new Vector2(150f, 54f), NextDialogue);
            CreateButton(panel.transform, "Close", "\u00D7", new Vector2(1f, 1f), new Vector2(-30f, -30f), new Vector2(56f, 56f), CloseAll, new Color(0.85f, 0.18f, 0.18f, 1f));
            return panel;
        }

        private GameObject BuildCodex(Transform parent)
        {
            GameObject panel = CreatePanel(parent, "CodexPanel", new Vector2(1300f, 760f));
            CreateText(panel.transform, "Title", "\u56FE\u9274 - \u5DF2\u9047\u89C1\u89D2\u8272", new Vector2(0.5f, 1f), new Vector2(0f, -55f), new Vector2(800f, 60f), 40, TextAnchor.MiddleCenter);
            CreateButton(panel.transform, "Close", "\u00D7", new Vector2(1f, 1f), new Vector2(-30f, -30f), new Vector2(56f, 56f), CloseAll, new Color(0.85f, 0.18f, 0.18f, 1f));
            GameObject content = CreateObject("Cards", panel.transform);
            SetRect(content.GetComponent<RectTransform>(), new Vector2(0.5f, 0.48f), Vector2.zero, new Vector2(1120f, 520f));
            return panel;
        }

        private void RefreshCodex()
        {
            if (codexPanel == null) return;
            Transform cards = codexPanel.transform.Find("Cards");
            for (int i = cards.childCount - 1; i >= 0; i--) Destroy(cards.GetChild(i).gameObject);
            CharacterUnit[] characters = FindObjectsByType<CharacterUnit>(FindObjectsSortMode.None);
            const int columns = 5;
            const float columnSpacing = 220f;
            const float rowSpacing = 235f;
            for (int i = 0; i < characters.Length; i++)
            {
                CharacterUnit character = characters[i];
                int column = i % columns;
                int row = i / columns;
                float x = (column - (columns - 1) * 0.5f) * columnSpacing;
                float y = -130f - row * rowSpacing;
                GameObject card = CreatePanel(cards, "CodexCard_" + character.name, new Vector2(200f, 210f));
                SetRect(card.GetComponent<RectTransform>(), new Vector2(0.5f, 1f), new Vector2(x, y), new Vector2(200f, 210f));
                Sprite portrait = codexPortraitSlots != null && i < codexPortraitSlots.Length ? codexPortraitSlots[i] : null;
                Image image = CreateImage(card.transform, "PortraitSlot", new Vector2(0.5f, 0.65f), Vector2.zero, new Vector2(130f, 120f), new Color(0.3f, 0.36f, 0.48f, 0.9f));
                image.sprite = portrait;
                CreateText(card.transform, "Name", character.DisplayName, new Vector2(0.5f, 0.29f), Vector2.zero, new Vector2(180f, 40f), 23, TextAnchor.MiddleCenter);
                CreateText(card.transform, "Info", "\u5DF2\u9047\u89C1\n\u56FE\u7247\u4E0E\u4ECB\u7ECD\u53EF\u5728\nGameUIHub \u4E2D\u66FF\u6362", new Vector2(0.5f, 0.10f), Vector2.zero, new Vector2(185f, 62f), 15, TextAnchor.MiddleCenter);
            }
        }

        private void RefreshDialogue()
        {
            if (dialogueBody == null) return;
            dialogueSpeaker.text = dialogueIndex == 0 ? "\u795E\u79D8\u4EBA" : dialogueIndex == 1 ? "\u89D2\u8272 A" : "\u65C1\u767D";
            dialogueBody.text = dialogueLines[dialogueIndex];
            dialoguePortrait.sprite = dialoguePortraitSlots != null && dialogueIndex < dialoguePortraitSlots.Length ? dialoguePortraitSlots[dialogueIndex] : null;
        }

        private void NextDialogue()
        {
            dialogueIndex++;
            if (dialogueIndex >= dialogueLines.Length) { CloseAll(); return; }
            RefreshDialogue();
        }

        private void ShowMainMenu() { OpenOnly(mainMenuPanel); }
        private void RestartScene() { Time.timeScale = 1f; SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex); }
        private void OpenOnly(GameObject panel) { CloseAll(); panel.SetActive(true); Time.timeScale = 0f; }
        private void CloseAll()
        {
            if (settingsPanel != null) settingsPanel.SetActive(false);
            if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
            if (guidePanel != null) guidePanel.SetActive(false);
            if (dialoguePanel != null) dialoguePanel.SetActive(false);
            if (codexPanel != null) codexPanel.SetActive(false);
            Time.timeScale = 1f;
        }

        private GameObject CreatePanel(Transform parent, string name, Vector2 size)
        {
            GameObject panel = CreateObject(name, parent, typeof(Image));
            panel.GetComponent<Image>().color = new Color(0.96f, 0.98f, 1f, 0.92f);
            SetRect(panel.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), Vector2.zero, size);
            return panel;
        }

        private Button CreateButton(Transform parent, string name, string label, Vector2 anchor, Vector2 position, Vector2 size, UnityEngine.Events.UnityAction click, Color? color = null)
        {
            GameObject buttonObject = CreateObject(name, parent, typeof(Image), typeof(Button));
            buttonObject.GetComponent<Image>().color = color ?? new Color(0.18f, 0.47f, 0.78f, 0.94f);
            SetRect(buttonObject.GetComponent<RectTransform>(), anchor, position, size);
            Button button = buttonObject.GetComponent<Button>(); button.onClick.AddListener(click);
            CreateText(buttonObject.transform, "Label", label, new Vector2(0.5f, 0.5f), Vector2.zero, size, 25, TextAnchor.MiddleCenter).color = Color.white;
            return button;
        }

        private Image CreateImage(Transform parent, string name, Vector2 anchor, Vector2 position, Vector2 size, Color color)
        {
            GameObject imageObject = CreateObject(name, parent, typeof(Image));
            Image image = imageObject.GetComponent<Image>(); image.color = color;
            SetRect(imageObject.GetComponent<RectTransform>(), anchor, position, size);
            return image;
        }

        private Text CreateText(Transform parent, string name, string value, Vector2 anchor, Vector2 position, Vector2 size, int fontSize, TextAnchor alignment)
        {
            GameObject textObject = CreateObject(name, parent, typeof(Text));
            Text text = textObject.GetComponent<Text>();
            text.font = font; text.text = value; text.fontSize = fontSize; text.alignment = alignment; text.color = new Color(0.12f, 0.16f, 0.23f, 1f); text.horizontalOverflow = HorizontalWrapMode.Wrap; text.verticalOverflow = VerticalWrapMode.Overflow;
            SetRect(textObject.GetComponent<RectTransform>(), anchor, position, size);
            return text;
        }

        private static GameObject CreateObject(string name, Transform parent, params System.Type[] components)
        {
            GameObject gameObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer));
            foreach (System.Type component in components) gameObject.AddComponent(component);
            gameObject.transform.SetParent(parent, false);
            return gameObject;
        }

        private static void SetRect(RectTransform rect, Vector2 anchor, Vector2 position, Vector2 size)
        {
            rect.anchorMin = anchor; rect.anchorMax = anchor; rect.pivot = new Vector2(0.5f, 0.5f); rect.anchoredPosition = position; rect.sizeDelta = size;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = Vector2.zero; rect.offsetMax = Vector2.zero;
        }
    }
}
