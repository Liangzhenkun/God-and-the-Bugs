using System;
using System.Collections;
using GameJamRAC.Camera;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.UI;

namespace GameJamRAC.Gameplay
{
    /// <summary>管理开局总览、子机位接管与定向生命转移。</summary>
    public class SoulSwapManager : MonoBehaviour
    {
        [Header("角色顺序")]
        [SerializeField] private CharacterUnit[] characters;

        [Header("相机")]
        [SerializeField] private CameraFollow cameraFollow;
        [SerializeField] private UnityEngine.Camera mainCamera;

        private CinemachineBrain cinemachineBrain;
        private Vector3 mainCameraStartPosition;
        private Quaternion mainCameraStartRotation;

        [Header("UI")]
        [SerializeField] private Button swapButton;
        [SerializeField] private Text swapButtonLabel;
        [SerializeField] private Text statePrompt;
        [SerializeField] private string swapButtonAutoPilotText = "魂穿";
        [SerializeField] private string swapButtonPossessingText = "转移并魂穿";

        [Header("转移参数")]
        [SerializeField, Min(0f)] private float transferDuration = 1.5f;

        public enum SwapState
        {
            AutoPilot,
            PossessingA,
            TransferringAB,
            PossessingB
        }

        private SwapState currentState = SwapState.AutoPilot;
        public SwapState CurrentState => currentState;
        public event Action<SwapState> onStateChanged;

        private void Awake()
        {
            if (mainCamera == null)
                mainCamera = UnityEngine.Camera.main;

            if (mainCamera == null) return;

            cinemachineBrain = mainCamera.GetComponent<CinemachineBrain>();
            mainCameraStartPosition = mainCamera.transform.position;
            mainCameraStartRotation = mainCamera.transform.rotation;
        }

        private void Start()
        {
            if (cameraFollow != null && characters != null)
            {
                var overviewTargets = new Transform[characters.Length];
                for (int i = 0; i < characters.Length; i++)
                    overviewTargets[i] = characters[i] != null ? characters[i].transform : null;
                cameraFollow.SetOverviewTargets(overviewTargets);
            }

            if (swapButton != null)
                swapButton.onClick.AddListener(OnSwapButtonPressed);

            // 开局：主摄像机总览，同时已能控制 A。
            EnterState(SwapState.AutoPilot);
        }

        private void OnDestroy()
        {
            if (swapButton != null)
                swapButton.onClick.RemoveListener(OnSwapButtonPressed);
        }

        public void OnSwapButtonPressed()
        {
            switch (currentState)
            {
                case SwapState.AutoPilot:
                    EnterState(SwapState.PossessingA);
                    break;
                case SwapState.PossessingA:
                    TransferAndPossessB();
                    break;
            }
        }

        private void EnterState(SwapState newState)
        {
            currentState = newState;
            onStateChanged?.Invoke(newState);

            switch (newState)
            {
                case SwapState.AutoPilot:
                    PossessCharacterFromMainCamera(0);
                    UpdateUI("控制 A｜按下魂穿进入 A 的角色视角");
                    SetButtonActive(true, swapButtonAutoPilotText);
                    break;

                case SwapState.PossessingA:
                    PossessCharacter(0);
                    UpdateUI("控制 A｜再次按下，转移生命并魂穿至目标角色");
                    SetButtonActive(CanTransferFrom(0), swapButtonPossessingText);
                    break;

                case SwapState.TransferringAB:
                    SetButtonActive(false, "");
                    UpdateUI("A → B：生命转移中…");
                    PerformTransfer(0, () => EnterState(SwapState.PossessingB));
                    break;

                case SwapState.PossessingB:
                    PossessCharacter(1);
                    UpdateUI("控制 B｜已接收 A 的剩余生命");
                    SetButtonActive(false, "");
                    break;
            }
        }

        /// <summary>开局总览：控制角色，但保持主摄像机视角。</summary>
        private void PossessCharacterFromMainCamera(int index)
        {
            SetControlledCharacter(index);

            if (cameraFollow != null)
            {
                cameraFollow.SwitchToOverview();
                cameraFollow.enabled = false;
            }

            if (cinemachineBrain != null)
                cinemachineBrain.enabled = false;

            if (mainCamera != null)
            {
                mainCamera.transform.SetPositionAndRotation(
                    mainCameraStartPosition,
                    mainCameraStartRotation);
            }
        }

        /// <summary>接管角色：启用该角色的 Cinemachine 子机位。</summary>
        private void PossessCharacter(int index)
        {
            SetControlledCharacter(index);

            CameraAnchor anchor = characters[index].ViewAnchor;
            if (anchor != null && anchor.HasCinemachineCamera)
            {
                if (cameraFollow != null)
                    cameraFollow.enabled = false;
                if (cinemachineBrain != null)
                    cinemachineBrain.enabled = true;
                anchor.SetCinemachineActive(true);
            }
            else if (anchor != null)
            {
                if (cinemachineBrain != null)
                    cinemachineBrain.enabled = false;
                if (cameraFollow != null)
                {
                    cameraFollow.enabled = true;
                    cameraFollow.SwitchToAnchor(anchor);
                }
            }
            else
            {
                if (cinemachineBrain != null)
                    cinemachineBrain.enabled = false;
                if (cameraFollow != null)
                {
                    cameraFollow.enabled = true;
                    cameraFollow.SwitchToTarget(characters[index].transform);
                }
            }
        }

        private void SetControlledCharacter(int index)
        {
            for (int i = 0; i < characters.Length; i++)
            {
                if (characters[i].ViewAnchor != null)
                    characters[i].ViewAnchor.SetCinemachineActive(false);

                if (i == index)
                    characters[i].TakeControl();
                else
                    characters[i].ReleaseControl();
            }
        }

        private void PerformTransfer(int from, Action onComplete)
        {
            CharacterUnit source = characters[from];
            CharacterUnit target = source.SoulTransferTarget;
            int transferredLife = source.TransferRemainingLifeTo(target);

            ScoreLabelUI sourceLabel = source.GetComponentInChildren<ScoreLabelUI>();
            ScoreLabelUI targetLabel = target.GetComponentInChildren<ScoreLabelUI>();
            if (sourceLabel != null) sourceLabel.ShowTransfer(transferredLife);
            if (targetLabel != null) targetLabel.ShowTransfer(transferredLife);

            StartCoroutine(WaitThenCallback(transferDuration, onComplete));
        }

        private void TransferAndPossessB()
        {
            if (CanTransferFrom(0))
            {
                CharacterUnit source = characters[0];
                CharacterUnit target = source.SoulTransferTarget;
                int transferredLife = source.TransferRemainingLifeTo(target);

                ScoreLabelUI sourceLabel = source.GetComponentInChildren<ScoreLabelUI>();
                ScoreLabelUI targetLabel = target.GetComponentInChildren<ScoreLabelUI>();
                if (sourceLabel != null) sourceLabel.ShowTransfer(transferredLife);
                if (targetLabel != null) targetLabel.ShowTransfer(transferredLife);
            }

            EnterState(SwapState.PossessingB);
        }

        private bool CanTransferFrom(int index)
        {
            if (characters == null || index < 0 || index >= characters.Length) return false;

            CharacterUnit source = characters[index];
            return source != null
                && !source.IsDead
                && source.SoulTransferTarget != null
                && !source.SoulTransferTarget.IsDead;
        }

        private IEnumerator WaitThenCallback(float delay, Action callback)
        {
            yield return new WaitForSeconds(delay);
            callback?.Invoke();
        }

        private void UpdateUI(string prompt)
        {
            if (statePrompt != null) statePrompt.text = prompt;
        }

        private void SetButtonActive(bool active, string label)
        {
            if (swapButton != null) swapButton.interactable = active;
            if (swapButtonLabel != null) swapButtonLabel.text = label;
        }
    }
}
