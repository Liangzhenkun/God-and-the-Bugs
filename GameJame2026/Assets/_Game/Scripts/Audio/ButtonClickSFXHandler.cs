using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace GameJamRAC.Audio
{
    /// <summary>
    /// 每个按钮上的轻量触发器；不依赖 Button.onClick，所以不会被 UI 脚本清空。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ButtonClickSFXHandler : MonoBehaviour, IPointerDownHandler, ISubmitHandler
    {
        [SerializeField] private SFXPlayer clickPlayer;
        [SerializeField] private string clickPlayerId = "";

        [SerializeField, HideInInspector] private bool usesDefaultClickPlayer = true;

        private Button button;

        public void Configure(SFXPlayer player)
        {
            if ((clickPlayer != null || !string.IsNullOrWhiteSpace(clickPlayerId)) && !usesDefaultClickPlayer)
                return;

            clickPlayer = player;
            clickPlayerId = "";
            usesDefaultClickPlayer = true;
            if (button == null)
                button = GetComponent<Button>();
        }

        public void SetCustomClickPlayer(SFXPlayer player)
        {
            clickPlayer = player;
            usesDefaultClickPlayer = false;
            if (button == null)
                button = GetComponent<Button>();
        }

        public void SetCustomClickPlayerId(string playerId)
        {
            clickPlayer = null;
            clickPlayerId = playerId;
            usesDefaultClickPlayer = false;
            if (button == null)
                button = GetComponent<Button>();
        }

        private void OnValidate()
        {
            if (clickPlayer != null || !string.IsNullOrWhiteSpace(clickPlayerId))
                usesDefaultClickPlayer = false;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            PlayIfReady();
        }

        public void OnSubmit(BaseEventData eventData)
        {
            PlayIfReady();
        }

        private void PlayIfReady()
        {
            SFXPlayer player = ResolvePlayer();
            if (player == null)
                return;

            if (button == null)
                button = GetComponent<Button>();

            if (button != null && (!button.isActiveAndEnabled || !button.interactable))
                return;

            player.Play();
        }

        private SFXPlayer ResolvePlayer()
        {
            if (clickPlayer != null)
                return clickPlayer;

            return SFXPlayer.TryGetById(clickPlayerId, out SFXPlayer player) ? player : null;
        }
    }
}
