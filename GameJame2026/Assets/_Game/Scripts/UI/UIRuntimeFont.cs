using UnityEngine;
using UnityEngine.UI;

namespace GameJamRAC.UI
{
    /// <summary>为运行时 UI 提供随包携带的中英文字体，避免 WebGL 依赖编辑器电脑的系统字体。</summary>
    public static class UIRuntimeFont
    {
        // 使用固定的 Regular 实例。Unity 的旧版 Text 会把可变字体按默认
        // Thin 字重导入，导致 WebGL 字体发虚，且动态字图集可能遗漏字形。
        private const string ResourcePath = "Fonts/NotoSansSC-Regular";
        private static Font cachedFont;

        public static Font Resolve()
        {
            if (cachedFont == null)
                cachedFont = Resources.Load<Font>(ResourcePath);

            return cachedFont != null
                ? cachedFont
                : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }

        public static void ApplyTo(Transform root)
        {
            if (root == null) return;

            Font font = Resolve();
            Text[] texts = root.GetComponentsInChildren<Text>(true);
            foreach (Text text in texts)
                text.font = font;
        }
    }
}
