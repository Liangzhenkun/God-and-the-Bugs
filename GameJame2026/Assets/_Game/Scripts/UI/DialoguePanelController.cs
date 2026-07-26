using UnityEngine;
using UnityEngine.UI;

namespace GameJamRAC.UI
{
    /// <summary>Controls a scene-authored dialogue panel. Dialogue content stays on the UI hierarchy.</summary>
    [DisallowMultipleComponent]
    public class DialoguePanelController : MonoBehaviour
    {
        [Header("页面组件")]
        [SerializeField] private GameObject[] pages;
        [SerializeField] private Button nextButton;
        [SerializeField] private Button closeButton;

        private int pageIndex;

        private void Awake()
        {
            if (nextButton != null) nextButton.onClick.AddListener(ShowNextPage);
            if (closeButton != null) closeButton.onClick.AddListener(Hide);
        }

        public void Show()
        {
            pageIndex = 0;
            gameObject.SetActive(true);
            RefreshPages();
            Time.timeScale = 0f;
        }

        public void Hide()
        {
            gameObject.SetActive(false);
            Time.timeScale = 1f;
        }

        public void ConfigureForAuthoring(GameObject[] newPages, Button newNextButton, Button newCloseButton)
        {
            pages = newPages;
            nextButton = newNextButton;
            closeButton = newCloseButton;
        }

        private void ShowNextPage()
        {
            if (pages == null || pages.Length == 0)
            {
                Hide();
                return;
            }

            pageIndex++;
            if (pageIndex >= pages.Length)
            {
                Hide();
                Time.timeScale = 1f;
                return;
            }

            RefreshPages();
        }

        private void RefreshPages()
        {
            if (pages == null) return;

            for (int i = 0; i < pages.Length; i++)
            {
                if (pages[i] != null) pages[i].SetActive(i == pageIndex);
            }
        }
    }
}
