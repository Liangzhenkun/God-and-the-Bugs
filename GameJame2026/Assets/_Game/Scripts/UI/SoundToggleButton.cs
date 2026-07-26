using UnityEngine;
using UnityEngine.UI;

namespace GameJamRAC.UI
{
    /// <summary>全场景共用的左上角声音开关按钮。</summary>
    [DisallowMultipleComponent]
    public class SoundToggleButton : MonoBehaviour
    {
        private const string SoundOnPath = "UI/sounds on";
        private const string SoundOffPath = "UI/mute";
        private static Sprite soundOnSprite;
        private static Sprite soundOffSprite;

        private Image icon;
        private Button button;

        public static SoundToggleButton Create(Transform parent)
        {
            Transform existing = parent.Find("GlobalSoundToggleButton");
            if (existing != null)
            {
                SoundToggleButton existingButton = existing.GetComponent<SoundToggleButton>();
                if (existingButton != null) return existingButton;
            }

            GameObject root = new GameObject("GlobalSoundToggleButton", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button), typeof(SoundToggleButton));
            root.transform.SetParent(parent, false);
            RectTransform rootRect = root.GetComponent<RectTransform>();
            rootRect.anchorMin = new Vector2(0f, 1f);
            rootRect.anchorMax = new Vector2(0f, 1f);
            rootRect.pivot = new Vector2(0.5f, 0.5f);
            rootRect.anchoredPosition = new Vector2(54f, -54f);
            rootRect.sizeDelta = new Vector2(72f, 72f);

            Image background = root.GetComponent<Image>();
            background.color = new Color(0.13f, 0.47f, 0.78f, 0.92f);
            Button button = root.GetComponent<Button>();
            button.targetGraphic = background;

            GameObject iconObject = new GameObject("Icon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            iconObject.transform.SetParent(root.transform, false);
            RectTransform iconRect = iconObject.GetComponent<RectTransform>();
            iconRect.anchorMin = new Vector2(0.5f, 0.5f);
            iconRect.anchorMax = new Vector2(0.5f, 0.5f);
            iconRect.pivot = new Vector2(0.5f, 0.5f);
            iconRect.sizeDelta = new Vector2(52f, 52f);
            iconObject.GetComponent<Image>().raycastTarget = false;
            SoundToggleButton soundToggleButton = root.GetComponent<SoundToggleButton>();
            soundToggleButton.RefreshIcon();
            return soundToggleButton;
        }

        private void Awake()
        {
            button = GetComponent<Button>();
            Transform iconTransform = transform.Find("Icon");
            icon = iconTransform != null ? iconTransform.GetComponent<Image>() : null;
            if (button != null) button.onClick.AddListener(ToggleSound);
        }

        private void OnEnable()
        {
            RefreshIcon();
        }

        private void OnDestroy()
        {
            if (button != null) button.onClick.RemoveListener(ToggleSound);
        }

        private void ToggleSound()
        {
            AudioSettingsState.ToggleSound();
            RefreshIcon();
        }

        private void RefreshIcon()
        {
            if (icon == null)
            {
                Transform iconTransform = transform.Find("Icon");
                icon = iconTransform != null ? iconTransform.GetComponent<Image>() : null;
            }
            if (icon == null) return;
            icon.sprite = AudioSettingsState.SoundEnabled ? GetSoundOnSprite() : GetSoundOffSprite();
            icon.preserveAspect = true;
            icon.color = Color.white;
        }

        private static Sprite GetSoundOnSprite()
        {
            if (soundOnSprite == null) soundOnSprite = CreateSprite(SoundOnPath);
            return soundOnSprite;
        }

        private static Sprite GetSoundOffSprite()
        {
            if (soundOffSprite == null) soundOffSprite = CreateSprite(SoundOffPath);
            return soundOffSprite;
        }

        private static Sprite CreateSprite(string resourcePath)
        {
            Texture2D texture = Resources.Load<Texture2D>(resourcePath);
            if (texture == null) return null;
            return Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f);
        }
    }
}
