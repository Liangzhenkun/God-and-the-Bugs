using UnityEngine;
using UnityEngine.UI;

namespace GameJamRAC.UI
{
    /// <summary>Scene-owned UI references. All visible UI stays editable under the Canvas hierarchy.</summary>
    [DisallowMultipleComponent]
    public class GameplayUIReferences : MonoBehaviour
    {
        [Header("顶部按钮")]
        [SerializeField] private Button dialogueButton;
        [SerializeField] private Button codexButton;
        [SerializeField] private Button helpButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private SoundToggleButton soundToggle;

        [Header("弹出面板")]
        [SerializeField] private GameObject settingsPanel;
        [SerializeField] private GameObject guidePanel;
        [SerializeField] private DialoguePanelController dialoguePanel;
        [SerializeField] private GameObject codexPanel;
        [SerializeField] private GameObject creditsPanel;

        [Header("面板按钮")]
        [SerializeField] private Button settingsCloseButton;
        [SerializeField] private Button returnToTitleButton;
        [SerializeField] private Button exitGameButton;
        [SerializeField] private Button creditsButton;
        [SerializeField] private Button guideCloseButton;
        [SerializeField] private Button codexCloseButton;
        [SerializeField] private Button creditsCloseButton;

        public Button DialogueButton => dialogueButton;
        public Button CodexButton => codexButton;
        public Button HelpButton => helpButton;
        public Button SettingsButton => settingsButton;
        public SoundToggleButton SoundToggle => soundToggle;
        public GameObject SettingsPanel => settingsPanel;
        public GameObject GuidePanel => guidePanel;
        public DialoguePanelController DialoguePanel => dialoguePanel;
        public GameObject CodexPanel => codexPanel;
        public GameObject CreditsPanel => creditsPanel;
        public Button SettingsCloseButton => settingsCloseButton;
        public Button ReturnToTitleButton => returnToTitleButton;
        public Button ExitGameButton => exitGameButton;
        public Button CreditsButton => creditsButton;
        public Button GuideCloseButton => guideCloseButton;
        public Button CodexCloseButton => codexCloseButton;
        public Button CreditsCloseButton => creditsCloseButton;

        public void Configure(
            Button newDialogueButton, Button newCodexButton, Button newHelpButton, Button newSettingsButton, SoundToggleButton newSoundToggle,
            GameObject newSettingsPanel, GameObject newGuidePanel, DialoguePanelController newDialoguePanel, GameObject newCodexPanel, GameObject newCreditsPanel,
            Button newSettingsCloseButton, Button newReturnToTitleButton, Button newExitGameButton, Button newCreditsButton,
            Button newGuideCloseButton, Button newCodexCloseButton, Button newCreditsCloseButton)
        {
            dialogueButton = newDialogueButton;
            codexButton = newCodexButton;
            helpButton = newHelpButton;
            settingsButton = newSettingsButton;
            soundToggle = newSoundToggle;
            settingsPanel = newSettingsPanel;
            guidePanel = newGuidePanel;
            dialoguePanel = newDialoguePanel;
            codexPanel = newCodexPanel;
            creditsPanel = newCreditsPanel;
            settingsCloseButton = newSettingsCloseButton;
            returnToTitleButton = newReturnToTitleButton;
            exitGameButton = newExitGameButton;
            creditsButton = newCreditsButton;
            guideCloseButton = newGuideCloseButton;
            codexCloseButton = newCodexCloseButton;
            creditsCloseButton = newCreditsCloseButton;
        }
    }
}
