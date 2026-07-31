using GameJamRAC.UI;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace GameJamRAC.Gameplay
{
    /// <summary>处理胜利、下一关，以及最终关的证书奖励流程。</summary>
    [DisallowMultipleComponent]
    public class VictoryFlowManager : MonoBehaviour
    {
        [SerializeField] private GameObject victoryPanel;
        [SerializeField] private Button nextLevelButton;
        [SerializeField] private SceneFlowManager sceneFlowManager;
        [SerializeField] private string nextSceneName = "NextScene 3";

        [Header("最终关奖励")]
        [SerializeField] private string finalSceneName = "NextScene 3";
        [SerializeField] private GameObject certificatePanel;
        [SerializeField] private Image certificateArtwork;
        [SerializeField] private Button certificateCloseButton;
        [SerializeField] private Button certificateAcceptButton;

        private bool hasWon;
        private Font uiFont;

        public bool HasWon => hasWon;
        private bool IsFinalScene => sceneFlowManager != null
            ? sceneFlowManager.IsFinalScene
            : SceneManager.GetActiveScene().name == finalSceneName;

        private void Awake()
        {
            ResolveSceneReferences();

            uiFont = UIRuntimeFont.Resolve();
            if (victoryPanel != null)
            {
                victoryPanel.SetActive(false);
                UIRuntimeFont.ApplyTo(victoryPanel.transform);
            }

            if (nextLevelButton != null)
            {
                if (nextLevelButton.onClick.GetPersistentEventCount() == 0)
                    nextLevelButton.onClick.AddListener(LoadNextLevel);

                if (IsFinalScene)
                    SetButtonLabel(nextLevelButton, "确定");
            }

            if (IsFinalScene)
                ConfigureCertificatePanel();

            if (certificatePanel != null)
                certificatePanel.SetActive(false);
        }

        private void ResolveSceneReferences()
        {
            if (sceneFlowManager == null)
                sceneFlowManager = SceneFlowManager.GetOrCreate();

            if (victoryPanel == null)
                victoryPanel = FindSceneObject("VictoryPanel");

            if (nextLevelButton == null)
            {
                if (victoryPanel != null)
                    nextLevelButton = FindButtonInChildren(victoryPanel.transform, "NextLevelButton");

                if (nextLevelButton == null)
                {
                    GameObject buttonObject = FindSceneObject("NextLevelButton");
                    if (buttonObject != null)
                        nextLevelButton = buttonObject.GetComponent<Button>();
                }
            }
        }

        private void OnValidate()
        {
            if (Application.isPlaying || gameObject.scene.name != finalSceneName) return;
            if (certificatePanel == null) CreateCertificatePanelInCanvas();
        }

        [ContextMenu("在 Canvas 中创建证书奖励面板")]
        private void CreateCertificatePanelForEditing()
        {
            if (Application.isPlaying || certificatePanel != null) return;
            CreateCertificatePanelInCanvas();
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

        private void FreezeSceneMovement()
        {
            foreach (CharacterUnit character in FindObjectsByType<CharacterUnit>(FindObjectsSortMode.None))
                character.ReleaseControl();

            foreach (GameJamRAC.Grid.GridUnitMover mover in FindObjectsByType<GameJamRAC.Grid.GridUnitMover>(FindObjectsSortMode.None))
                mover.enabled = false;

            foreach (TurnActionManager turns in FindObjectsByType<TurnActionManager>(FindObjectsSortMode.None))
                turns.enabled = false;
        }

        public void LoadNextLevel()
        {
            if (IsFinalScene)
            {
                ShowCertificatePanel();
                return;
            }

            if (sceneFlowManager != null)
            {
                sceneFlowManager.LoadNextLevel();
                return;
            }

            if (Application.CanStreamedLevelBeLoaded(nextSceneName))
                SceneManager.LoadScene(nextSceneName);
            else
                Debug.LogError("找不到下一关场景：" + nextSceneName);
        }

        private void CreateCertificatePanelInCanvas()
        {
            Canvas canvas = victoryPanel != null
                ? victoryPanel.GetComponentInParent<Canvas>()
                : FindFirstObjectByType<Canvas>();
            if (canvas == null) return;

            certificatePanel = CreateOverlay(canvas.transform, "CertificateRewardPanel");
            GameObject certificateCard = CreateCard(certificatePanel.transform, "CertificateCard", new Vector2(900f, 620f));
            CreateText(certificateCard.transform, "Title", "通关纪念证书", new Vector2(0.5f, 0.91f), Vector2.zero, new Vector2(620f, 66f), 42, TextAnchor.MiddleCenter, new Color(0.22f, 0.13f, 0.3f, 1f));
            certificateCloseButton = CreateButton(certificateCard.transform, "Close", "×", new Vector2(1f, 1f), new Vector2(-34f, -34f), new Vector2(58f, 58f), ReturnToTitle, new Color(0.82f, 0.16f, 0.2f, 1f));
            certificateCloseButton.GetComponentInChildren<Text>().fontSize = 34;

            certificateArtwork = CreateImage(certificateCard.transform, "CertificateArtwork", new Vector2(0.5f, 0.52f), Vector2.zero, new Vector2(680f, 330f), new Color(1f, 0.95f, 0.75f, 1f));
            certificateArtwork.preserveAspect = true;
            CreateText(certificateArtwork.transform, "CertificateText", "恭喜完成全部关卡！\n\n此证书属于勇敢抵达终点的你。", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(580f, 220f), 30, TextAnchor.MiddleCenter, new Color(0.28f, 0.17f, 0.12f, 1f));
            certificateAcceptButton = CreateButton(certificateCard.transform, "Accept", "收下", new Vector2(0.5f, 0.08f), Vector2.zero, new Vector2(220f, 62f), ReturnToTitle);
        }

        private void ConfigureCertificatePanel()
        {
            if (certificateCloseButton != null)
            {
                certificateCloseButton.onClick.RemoveAllListeners();
                certificateCloseButton.onClick.AddListener(ReturnToTitle);
            }

            if (certificateAcceptButton != null)
            {
                certificateAcceptButton.onClick.RemoveAllListeners();
                certificateAcceptButton.onClick.AddListener(ReturnToTitle);
            }
        }

        private void ShowCertificatePanel()
        {
            if (victoryPanel != null) victoryPanel.SetActive(false);
            if (certificatePanel != null) certificatePanel.SetActive(true);
        }

        private void ReturnToTitle()
        {
            if (sceneFlowManager != null)
                sceneFlowManager.ReturnToTitle();
            else
            {
                Time.timeScale = 1f;
                SceneManager.LoadScene("TitleScene");
            }
        }

        private GameObject CreateOverlay(Transform parent, string name)
        {
            GameObject overlay = CreateObject(name, parent, typeof(Image));
            overlay.GetComponent<Image>().color = new Color(0.04f, 0.02f, 0.08f, 0.72f);
            Stretch(overlay.GetComponent<RectTransform>());
            return overlay;
        }

        private GameObject CreateCard(Transform parent, string name, Vector2 size)
        {
            GameObject card = CreateObject(name, parent, typeof(Image));
            card.GetComponent<Image>().color = new Color(0.98f, 0.95f, 0.86f, 0.98f);
            SetRect(card.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), Vector2.zero, size);
            return card;
        }

        private Button CreateButton(Transform parent, string name, string label, Vector2 anchor, Vector2 position, Vector2 size, UnityEngine.Events.UnityAction action, Color? color = null)
        {
            GameObject buttonObject = CreateObject(name, parent, typeof(Image), typeof(Button));
            buttonObject.GetComponent<Image>().color = color ?? new Color(0.17f, 0.47f, 0.9f, 1f);
            SetRect(buttonObject.GetComponent<RectTransform>(), anchor, position, size);
            Button button = buttonObject.GetComponent<Button>();
            button.onClick.AddListener(action);
            CreateText(buttonObject.transform, "Label", label, new Vector2(0.5f, 0.5f), Vector2.zero, size, 26, TextAnchor.MiddleCenter, Color.white);
            return button;
        }

        private Image CreateImage(Transform parent, string name, Vector2 anchor, Vector2 position, Vector2 size, Color color)
        {
            GameObject imageObject = CreateObject(name, parent, typeof(Image));
            Image image = imageObject.GetComponent<Image>();
            image.color = color;
            SetRect(imageObject.GetComponent<RectTransform>(), anchor, position, size);
            return image;
        }

        private Text CreateText(Transform parent, string name, string value, Vector2 anchor, Vector2 position, Vector2 size, int fontSize, TextAnchor alignment, Color color)
        {
            GameObject textObject = CreateObject(name, parent, typeof(Text));
            Text text = textObject.GetComponent<Text>();
            text.font = uiFont != null ? uiFont : UIRuntimeFont.Resolve();
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.color = color;
            text.text = value;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            SetRect(text.rectTransform, anchor, position, size);
            return text;
        }

        private static GameObject CreateObject(string name, Transform parent, params System.Type[] components)
        {
            GameObject gameObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer));
            foreach (System.Type component in components) gameObject.AddComponent(component);
            gameObject.transform.SetParent(parent, false);
            return gameObject;
        }

        private static void SetButtonLabel(Button button, string value)
        {
            Text text = button.GetComponentInChildren<Text>(true);
            if (text != null) text.text = value;
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

        private static Button FindButtonInChildren(Transform root, string objectName)
        {
            foreach (Button button in root.GetComponentsInChildren<Button>(true))
            {
                if (button.name == objectName)
                    return button;
            }

            return null;
        }

        private static void SetRect(RectTransform rect, Vector2 anchor, Vector2 position, Vector2 size)
        {
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}
