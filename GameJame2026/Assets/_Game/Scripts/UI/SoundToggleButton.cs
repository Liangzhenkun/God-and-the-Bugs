using UnityEngine;
using UnityEngine.UI;

namespace GameJamRAC.UI
{
    /// <summary>Scene-authored global sound toggle.</summary>
    [DisallowMultipleComponent]
    public class SoundToggleButton : MonoBehaviour
    {
        [SerializeField] private Image icon;
        [SerializeField] private Sprite soundOnSprite;
        [SerializeField] private Sprite soundOffSprite;
        [SerializeField] private bool replaceExistingOnClick = true;

        private Button button;

        private void Awake()
        {
            button = GetComponent<Button>();
            ResolveReferences();

            if (button != null)
            {
                if (replaceExistingOnClick)
                    button.onClick = new Button.ButtonClickedEvent();

                button.onClick.AddListener(ToggleSound);
            }

            RefreshIcon();
        }

        private void OnEnable() => RefreshIcon();

        private void OnDestroy()
        {
            if (button != null) button.onClick.RemoveListener(ToggleSound);
        }

        public void ConfigureForAuthoring(Image newIcon, Sprite newSoundOnSprite, Sprite newSoundOffSprite)
        {
            icon = newIcon;
            soundOnSprite = newSoundOnSprite;
            soundOffSprite = newSoundOffSprite;
        }

        public static SoundToggleButton Create(Transform parent)
        {
            if (parent == null) return null;

            SoundToggleButton existing = parent.GetComponentInChildren<SoundToggleButton>(true);
            if (existing != null) return existing;

            Transform volumeTransform = parent.Find("音量");
            return volumeTransform != null ? volumeTransform.GetComponent<SoundToggleButton>() : null;
        }

        private void ToggleSound()
        {
            AudioSettingsState.ToggleSound();
            RefreshIcon();
        }

        private void RefreshIcon()
        {
            ResolveReferences();
            if (icon == null) return;

            icon.sprite = AudioSettingsState.SoundEnabled ? soundOnSprite : soundOffSprite;
            icon.preserveAspect = true;
            icon.color = Color.white;
        }

        private void ResolveReferences()
        {
            if (icon == null) icon = GetComponent<Image>();
            if (soundOnSprite == null) soundOnSprite = LoadSprite("UI/sounds on");
            if (soundOffSprite == null) soundOffSprite = LoadSprite("UI/mute");
        }

        private static Sprite LoadSprite(string resourcePath)
        {
            Sprite[] sprites = Resources.LoadAll<Sprite>(resourcePath);
            return sprites != null && sprites.Length > 0 ? sprites[0] : Resources.Load<Sprite>(resourcePath);
        }
    }
}
