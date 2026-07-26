using System.Collections.Generic;
using GameJamRAC.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace GameJamRAC.Editor
{
    /// <summary>Creates the editable gameplay UI hierarchy for every playable level scene.</summary>
    public static class GameplayUIHierarchyGenerator
    {
        private static readonly string[] ScenePaths =
        {
            "Assets/Scenes/MainScene.unity",
            "Assets/Scenes/NextScene 2.unity",
            "Assets/Scenes/NextScene 3.unity"
        };

        [MenuItem("GameJam/UI/Create Editable Gameplay UI")]
        public static void CreateEditableGameplayUi()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            string activeScenePath = SceneManager.GetActiveScene().path;
            foreach (string scenePath in ScenePaths)
            {
                Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
                GameObject canvas = GameObject.Find("GamePlayCanvas");
                if (canvas == null)
                {
                    Debug.LogWarning("GamePlayCanvas was not found in " + scenePath);
                    continue;
                }

                if (canvas.GetComponentInChildren<GameplayUIReferences>(true) == null)
                {
                    CreateUi(canvas.transform);
                    EditorSceneManager.MarkSceneDirty(scene);
                    EditorSceneManager.SaveScene(scene);
                }
            }

            if (!string.IsNullOrEmpty(activeScenePath))
            {
                EditorSceneManager.OpenScene(activeScenePath, OpenSceneMode.Single);
            }
        }

        private static void CreateUi(Transform canvas)
        {
            GameObject root = CreateUiObject("GameplayUI", canvas, typeof(GameplayUIReferences));
            Stretch(root.GetComponent<RectTransform>());

            Button dialogue = CreateIconButton(root.transform, "DialogueButton", "剧情", new Vector2(0f, 1f), new Vector2(156f, -54f), LoadSprite("Assets/_Game/Art/UI/Menu.png"));
            Button codex = CreateIconButton(root.transform, "CodexButton", "图鉴", new Vector2(1f, 1f), new Vector2(-250f, -54f), LoadSprite("Assets/_Game/Art/UI/book.png"));
            Button help = CreateIconButton(root.transform, "HelpButton", "?", new Vector2(1f, 1f), new Vector2(-122f, -54f), LoadSprite("Assets/_Game/Art/UI/Help.png"));
            Button settings = CreateIconButton(root.transform, "SettingsButton", "设置", new Vector2(1f, 1f), new Vector2(-34f, -54f), LoadSprite("Assets/_Game/Art/UI/Setting.png"));
            CreateText(root.transform, "CameraHint", "鼠标右键缩放视角", new Vector2(1f, 0f), new Vector2(-220f, 34f), new Vector2(420f, 52f), 28, TextAnchor.MiddleRight);

            SoundToggleButton soundToggle = CreateSoundToggle(root.transform);
            PanelButtons settingsPanel = CreateSettingsPanel(root.transform);
            PanelButtons guidePanel = CreateGuidePanel(root.transform);
            DialoguePanelController dialoguePanel = CreateDialoguePanel(root.transform);
            PanelButtons codexPanel = CreateCodexPanel(root.transform);
            PanelButtons creditsPanel = CreateCreditsPanel(root.transform);

            root.GetComponent<GameplayUIReferences>().Configure(
                dialogue, codex, help, settings, soundToggle,
                settingsPanel.Panel, guidePanel.Panel, dialoguePanel, codexPanel.Panel, creditsPanel.Panel,
                settingsPanel.Close, settingsPanel.Primary, settingsPanel.Secondary, settingsPanel.Tertiary,
                guidePanel.Close, codexPanel.Close, creditsPanel.Close);
        }

        private static SoundToggleButton CreateSoundToggle(Transform parent)
        {
            GameObject root = CreateUiObject("SoundToggleButton", parent, typeof(Image), typeof(Button), typeof(SoundToggleButton));
            SetRect(root.GetComponent<RectTransform>(), new Vector2(0f, 1f), new Vector2(54f, -54f), new Vector2(72f, 72f));
            root.GetComponent<Image>().color = Color.clear;
            Image icon = CreateImage(root.transform, "Icon", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(64f, 64f));
            icon.raycastTarget = false;
            SoundToggleButton toggle = root.GetComponent<SoundToggleButton>();
            toggle.ConfigureForAuthoring(icon, LoadSprite("Assets/_Game/Art/UI/sounds on.png"), LoadSprite("Assets/_Game/Art/UI/mute.png"));
            return toggle;
        }

        private static PanelButtons CreateSettingsPanel(Transform parent)
        {
            GameObject panel = CreatePanel(parent, "SettingsPanel", new Vector2(760f, 440f));
            CreateText(panel.transform, "Title", "设置", new Vector2(0.5f, 1f), new Vector2(0f, -55f), new Vector2(500f, 60f), 38, TextAnchor.MiddleCenter);
            Button close = CreateCloseButton(panel.transform);
            Button menu = CreateTextButton(panel.transform, "ReturnToTitle", "回到主菜单", new Vector2(0.5f, 0.58f), Vector2.zero, new Vector2(300f, 62f));
            Button exit = CreateTextButton(panel.transform, "ExitGame", "退出游戏", new Vector2(0.5f, 0.38f), Vector2.zero, new Vector2(300f, 62f));
            Button credits = CreateTextButton(panel.transform, "Credits", "制作组", new Vector2(0.5f, 0.18f), Vector2.zero, new Vector2(300f, 62f));
            panel.SetActive(false);
            return new PanelButtons(panel, close, menu, exit, credits);
        }

        private static PanelButtons CreateGuidePanel(Transform parent)
        {
            GameObject panel = CreatePanel(parent, "GuidePanel", new Vector2(960f, 620f));
            Image image = panel.GetComponent<Image>();
            Sprite board = LoadSprite("Assets/_Game/Art/UI/helpboard.png");
            if (board != null)
            {
                image.sprite = board;
                image.preserveAspect = true;
            }
            CreateText(panel.transform, "Title", "游戏帮助", new Vector2(0.5f, 1f), new Vector2(0f, -55f), new Vector2(600f, 60f), 40, TextAnchor.MiddleCenter);
            CreateText(panel.transform, "GuideText", "在此直接编辑游戏帮助内容。", new Vector2(0.5f, 0.48f), Vector2.zero, new Vector2(760f, 340f), 28, TextAnchor.UpperLeft);
            Button close = CreateCloseButton(panel.transform);
            panel.SetActive(false);
            return new PanelButtons(panel, close);
        }

        private static DialoguePanelController CreateDialoguePanel(Transform parent)
        {
            GameObject panel = CreateUiObject("DialoguePanel", parent, typeof(Image), typeof(DialoguePanelController));
            Stretch(panel.GetComponent<RectTransform>());
            panel.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.68f);

            GameObject card = CreatePanel(panel.transform, "DialogueCard", new Vector2(1260f, 720f));
            card.GetComponent<Image>().color = new Color(0.12f, 0.15f, 0.24f, 0.96f);
            GameObject pagesRoot = CreateUiObject("Pages", card.transform);
            Stretch(pagesRoot.GetComponent<RectTransform>());
            List<GameObject> pages = new List<GameObject>();
            for (int i = 0; i < 3; i++) pages.Add(CreateDialoguePage(pagesRoot.transform, i + 1));
            Button close = CreateCloseButton(card.transform);
            Button next = CreateTextButton(card.transform, "Next", "下一步", new Vector2(1f, 0f), new Vector2(-115f, 52f), new Vector2(180f, 64f));
            DialoguePanelController controller = panel.GetComponent<DialoguePanelController>();
            controller.ConfigureForAuthoring(pages.ToArray(), next, close);
            panel.SetActive(false);
            return controller;
        }

        private static GameObject CreateDialoguePage(Transform parent, int number)
        {
            GameObject page = CreateUiObject("Page_" + number.ToString("00"), parent);
            Stretch(page.GetComponent<RectTransform>());
            Image portrait = CreateImage(page.transform, "Portrait", new Vector2(0f, 0.5f), new Vector2(235f, 78f), new Vector2(380f, 470f));
            portrait.color = new Color(0.31f, 0.40f, 0.56f, 1f);
            CreateText(page.transform, "Speaker", "角色名称（可替换）", new Vector2(0f, 0f), new Vector2(250f, 155f), new Vector2(420f, 58f), 32, TextAnchor.MiddleCenter);
            Text body = CreateText(page.transform, "DialogueText", "在这里填写第 " + number + " 页的剧情文本。", new Vector2(0.5f, 0f), new Vector2(95f, 155f), new Vector2(1000f, 180f), 30, TextAnchor.UpperLeft);
            body.horizontalOverflow = HorizontalWrapMode.Wrap;
            body.verticalOverflow = VerticalWrapMode.Overflow;
            return page;
        }

        private static PanelButtons CreateCodexPanel(Transform parent)
        {
            GameObject panel = CreatePanel(parent, "CodexPanel", new Vector2(1300f, 760f));
            CreateText(panel.transform, "Title", "图鉴", new Vector2(0.5f, 1f), new Vector2(0f, -55f), new Vector2(800f, 60f), 40, TextAnchor.MiddleCenter);
            Button close = CreateCloseButton(panel.transform);
            GameObject cards = CreateUiObject("Cards", panel.transform);
            SetRect(cards.GetComponent<RectTransform>(), new Vector2(0.5f, 0.48f), Vector2.zero, new Vector2(1120f, 520f));
            for (int i = 0; i < 4; i++) CreateCodexCard(cards.transform, i + 1);
            panel.SetActive(false);
            return new PanelButtons(panel, close);
        }

        private static void CreateCodexCard(Transform parent, int number)
        {
            GameObject card = CreatePanel(parent, "CodexCard_" + number.ToString("00"), new Vector2(220f, 260f));
            int column = (number - 1) % 4;
            SetRect(card.GetComponent<RectTransform>(), new Vector2(0f, 1f), new Vector2(135f + column * 285f, -150f), new Vector2(220f, 260f));
            Image portrait = CreateImage(card.transform, "Portrait", new Vector2(0.5f, 0.67f), Vector2.zero, new Vector2(145f, 135f));
            portrait.color = new Color(0.31f, 0.40f, 0.56f, 1f);
            CreateText(card.transform, "Name", "角色名称", new Vector2(0.5f, 0.29f), Vector2.zero, new Vector2(200f, 40f), 24, TextAnchor.MiddleCenter);
            CreateText(card.transform, "Info", "在这里填写角色资料", new Vector2(0.5f, 0.10f), Vector2.zero, new Vector2(195f, 48f), 17, TextAnchor.MiddleCenter);
        }

        private static PanelButtons CreateCreditsPanel(Transform parent)
        {
            GameObject panel = CreatePanel(parent, "CreditsPanel", new Vector2(780f, 500f));
            CreateText(panel.transform, "Title", "制作组", new Vector2(0.5f, 0.78f), Vector2.zero, new Vector2(500f, 70f), 44, TextAnchor.MiddleCenter);
            CreateText(panel.transform, "CreditsText", "在这里填写制作人员与感谢内容。", new Vector2(0.5f, 0.46f), Vector2.zero, new Vector2(610f, 220f), 28, TextAnchor.MiddleCenter);
            Button close = CreateCloseButton(panel.transform);
            panel.SetActive(false);
            return new PanelButtons(panel, close);
        }

        private static GameObject CreatePanel(Transform parent, string name, Vector2 size)
        {
            GameObject panel = CreateUiObject(name, parent, typeof(Image));
            SetRect(panel.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), Vector2.zero, size);
            panel.GetComponent<Image>().color = new Color(0.08f, 0.10f, 0.18f, 0.94f);
            return panel;
        }

        private static Button CreateIconButton(Transform parent, string name, string label, Vector2 anchor, Vector2 position, Sprite sprite)
        {
            GameObject button = CreateUiObject(name, parent, typeof(Image), typeof(Button));
            SetRect(button.GetComponent<RectTransform>(), anchor, position, new Vector2(64f, 64f));
            Image image = button.GetComponent<Image>();
            image.sprite = sprite;
            image.preserveAspect = sprite != null;
            image.color = sprite != null ? Color.white : new Color(0.18f, 0.48f, 0.92f, 1f);
            if (sprite == null) CreateText(button.transform, "Label", label, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(64f, 64f), 20, TextAnchor.MiddleCenter);
            return button.GetComponent<Button>();
        }

        private static Button CreateTextButton(Transform parent, string name, string label, Vector2 anchor, Vector2 position, Vector2 size)
        {
            GameObject button = CreateUiObject(name, parent, typeof(Image), typeof(Button));
            SetRect(button.GetComponent<RectTransform>(), anchor, position, size);
            button.GetComponent<Image>().color = new Color(0.18f, 0.48f, 0.92f, 1f);
            CreateText(button.transform, "Label", label, new Vector2(0.5f, 0.5f), Vector2.zero, size, 28, TextAnchor.MiddleCenter);
            return button.GetComponent<Button>();
        }

        private static Button CreateCloseButton(Transform parent)
        {
            return CreateIconButton(parent, "Close", "×", new Vector2(1f, 1f), new Vector2(-38f, -38f), LoadSprite("Assets/_Game/Art/UI/X Only panel.png"));
        }

        private static Image CreateImage(Transform parent, string name, Vector2 anchor, Vector2 position, Vector2 size)
        {
            GameObject image = CreateUiObject(name, parent, typeof(Image));
            SetRect(image.GetComponent<RectTransform>(), anchor, position, size);
            return image.GetComponent<Image>();
        }

        private static Text CreateText(Transform parent, string name, string value, Vector2 anchor, Vector2 position, Vector2 size, int fontSize, TextAnchor alignment)
        {
            GameObject textObject = CreateUiObject(name, parent, typeof(Text));
            SetRect(textObject.GetComponent<RectTransform>(), anchor, position, size);
            Text text = textObject.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.text = value;
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.color = Color.white;
            return text;
        }

        private static GameObject CreateUiObject(string name, Transform parent, params System.Type[] components)
        {
            GameObject created = new GameObject(name, components);
            created.transform.SetParent(parent, false);
            return created;
        }

        private static Sprite LoadSprite(string assetPath)
        {
            Object[] assets = AssetDatabase.LoadAllAssetsAtPath(assetPath);
            foreach (Object asset in assets)
            {
                if (asset is Sprite sprite) return sprite;
            }
            return null;
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

        private readonly struct PanelButtons
        {
            public readonly GameObject Panel;
            public readonly Button Close;
            public readonly Button Primary;
            public readonly Button Secondary;
            public readonly Button Tertiary;

            public PanelButtons(GameObject panel, Button close = null, Button primary = null, Button secondary = null, Button tertiary = null)
            {
                Panel = panel;
                Close = close;
                Primary = primary;
                Secondary = secondary;
                Tertiary = tertiary;
            }
        }
    }
}
