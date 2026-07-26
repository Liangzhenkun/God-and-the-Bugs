using UnityEngine;
using UnityEngine.UI;

namespace GameJamRAC.UI
{
    /// <summary>Scene-authored sound toggle. Icon and sprites are assigned in the Inspector.</summary>
    [DisallowMultipleComponent]
    public class SoundToggleButton : MonoBehaviour
    {
        [SerializeField] private Image icon;
        [SerializeField] private Sprite soundOnSprite;
        [SerializeField] private Sprite soundOffSprite;

        private Button button;

        private void Awake()
        {
            button = GetComponent<Button>();
            if (button != null) button.onClick.AddListener(ToggleSound);
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

        // 兼容旧场景调用：只查找已存在的层级组件，不再创建任何运行时 UI。
        public static SoundToggleButton Create(Transform parent)
        {
            return parent != null ? parent.GetComponentInChildren<SoundToggleButton>(true) : null;
        }

        private void ToggleSound()
        {
            AudioSettingsState.ToggleSound();
            RefreshIcon();
        }

        private void RefreshIcon()
        {
            if (icon == null) return;
            icon.sprite = AudioSettingsState.SoundEnabled ? soundOnSprite : soundOffSprite;
            icon.preserveAspect = true;
            icon.color = Color.white;
        }
    }
}
