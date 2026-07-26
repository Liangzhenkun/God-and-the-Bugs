using System;
using GameJamRAC.Camera;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;

namespace GameJamRAC.Gameplay
{
    /// <summary>
    /// Toggles player control and camera focus between character A and B.
    /// </summary>
    public class SoulSwapManager : MonoBehaviour
    {
        [Header("Characters: index 0 is A, index 1 is B")]
        [SerializeField] private CharacterUnit[] characters;

        [Header("Camera")]
        [SerializeField] private CameraFollow cameraFollow;
        [SerializeField] private UnityEngine.Camera mainCamera;

        [Header("UI")]
        [SerializeField] private Button swapButton;
        [SerializeField] private Text swapButtonLabel;
        [SerializeField] private Text statePrompt;
        [SerializeField] private string swapButtonText = "\u9B42\u7A7F";
        [SerializeField] private Color lockedButtonColor = new Color(0.42f, 0.42f, 0.42f, 1f);
        [SerializeField] private Color unlockedButtonColor = new Color(0.16f, 0.52f, 1f, 1f);
        [SerializeField] private bool allowSwapImmediately;
        [SerializeField, Min(0.05f)] private float overviewBlendDuration = 0.55f;
        [SerializeField, Min(0.05f)] private float characterBlendDuration = 0.55f;

        private CinemachineBrain cinemachineBrain;
        private Vector3 mainCameraStartPosition;
        private Quaternion mainCameraStartRotation;
        private float mainCameraStartFieldOfView;
        private float mainCameraStartOrthographicSize;
        private bool mainCameraStartOrthographic;
        private bool soulSwapUnlocked;
        private Coroutine cameraBlendCoroutine;
        private Coroutine bDefeatRecoveryCoroutine;
        private bool awaitingStartInput = true;
        private bool isOverviewView = true;

        public enum SwapState
        {
            PossessingA,
            PossessingB
        }

        private SwapState currentState = SwapState.PossessingA;
        public SwapState CurrentState => currentState;
        public event Action<SwapState> onStateChanged;

        private void Awake()
        {
            if (mainCamera == null)
                mainCamera = UnityEngine.Camera.main;

            if (mainCamera == null)
                return;

            cinemachineBrain = mainCamera.GetComponent<CinemachineBrain>();
            mainCameraStartPosition = mainCamera.transform.position;
            mainCameraStartRotation = mainCamera.transform.rotation;
            mainCameraStartFieldOfView = mainCamera.fieldOfView;
            mainCameraStartOrthographicSize = mainCamera.orthographicSize;
            mainCameraStartOrthographic = mainCamera.orthographic;
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

            if (HasCharacter(1))
                characters[1].onDied += OnSecondaryCharacterDied;

            EnterInitialAState();
            if (allowSwapImmediately)
                SetSoulSwapUnlocked(true);
        }

        private void Update()
        {
            if (awaitingStartInput)
            {
                if (HasStartInput())
                    BeginCharacterControl();
                return;
            }

            if (soulSwapUnlocked)
                SetButtonActive(CanPossessCharacter(GetOtherCharacterIndex()));

            if (Input.GetMouseButtonDown(1))
            {
                if (isOverviewView)
                    FocusCurrentCharacter();
                else
                    ReturnToOverview();
            }
        }

        private void OnDestroy()
        {
            if (swapButton != null)
                swapButton.onClick.RemoveListener(OnSwapButtonPressed);
            if (HasCharacter(1))
                characters[1].onDied -= OnSecondaryCharacterDied;
        }

        public void OnSwapButtonPressed()
        {
            int targetIndex = GetOtherCharacterIndex();
            if (awaitingStartInput || !soulSwapUnlocked || !CanPossessCharacter(targetIndex))
                return;

            EnterState(targetIndex == 0 ? SwapState.PossessingA : SwapState.PossessingB);
        }

        private void EnterInitialAState()
        {
            if (!HasCharacter(0))
            {
                SetButtonActive(false);
                return;
            }

            currentState = SwapState.PossessingA;
            PossessCharacterFromMainCamera(0);
            SetControlledCharacter(-1);
            onStateChanged?.Invoke(currentState);
            ShowStartPrompt();
            SetButtonActive(false);
        }

        private void EnterState(SwapState newState)
        {
            int index = newState == SwapState.PossessingA ? 0 : 1;
            if (!CanPossessCharacter(index))
                return;

            currentState = newState;
            PossessCharacter(index);
            isOverviewView = false;
            onStateChanged?.Invoke(currentState);
            UpdateUI(newState == SwapState.PossessingA
                ? "\u63A7\u5236 A - \u70B9\u51FB\u9B42\u7A7F\u5207\u6362\u5230 B"
                : "\u63A7\u5236 B - \u70B9\u51FB\u9B42\u7A7F\u5207\u6362\u5230 A");
            SetButtonActive(soulSwapUnlocked && CanPossessCharacter(GetOtherCharacterIndex()));
        }

        private void PossessCharacterFromMainCamera(int index)
        {
            StopCameraBlend();
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
                mainCamera.transform.SetPositionAndRotation(mainCameraStartPosition, mainCameraStartRotation);
                RestoreMainCameraLens();
            }

            isOverviewView = true;
        }

        private void PossessCharacter(int index)
        {
            SetControlledCharacter(index);

            CharacterUnit character = characters[index];
            CameraAnchor anchor = character.ViewAnchor;
            if (anchor != null && anchor.HasCinemachineCamera)
            {
                BeginBlendToCharacterCamera(anchor);
                return;
            }

            if (cinemachineBrain != null)
                cinemachineBrain.enabled = false;

            if (cameraFollow == null)
                return;

            cameraFollow.enabled = true;
            if (anchor != null)
                cameraFollow.SwitchToAnchor(anchor);
            else
                cameraFollow.SwitchToTarget(character.transform);
        }

        private void SetControlledCharacter(int index)
        {
            if (characters == null)
                return;

            for (int i = 0; i < characters.Length; i++)
            {
                CharacterUnit character = characters[i];
                if (character == null)
                    continue;

                if (character.ViewAnchor != null)
                    character.ViewAnchor.SetCinemachineActive(false);

                if (i == index)
                    character.TakeControl();
                else
                    character.ReleaseControl();
            }
        }

        private bool HasCharacter(int index)
        {
            return characters != null && index >= 0 && index < characters.Length && characters[index] != null;
        }

        private bool CanPossessCharacter(int index)
        {
            if (!HasCharacter(index) || characters[index].IsDead)
                return false;

            SceneThreeBConsumeSequence sceneThreeConsume = FindFirstObjectByType<SceneThreeBConsumeSequence>();
            if (sceneThreeConsume != null && index == 1 && sceneThreeConsume.IsBUnavailable)
                return false;

            D1BConsumeSequence d1Consume = FindFirstObjectByType<D1BConsumeSequence>();
            return d1Consume == null || index != 1 || !d1Consume.IsBUnavailable;
        }

        private int GetOtherCharacterIndex()
        {
            return currentState == SwapState.PossessingA ? 1 : 0;
        }

        public bool IsPrimaryCharacter(CharacterUnit character)
        {
            return HasCharacter(0) && characters[0] == character;
        }

        /// <summary>B 被捕食时不是失败：等捕食动画结束后自动回到仍存活的 A。</summary>
        public bool RecoverToAAfterBDefeated(CharacterUnit defeatedCharacter)
        {
            if (!HasCharacter(1) || characters[1] != defeatedCharacter)
                return false;
            if (currentState != SwapState.PossessingB && !defeatedCharacter.WasPlayerControlledAtDeath)
                return false;
            if (!HasCharacter(0) || characters[0].IsDead)
                return false;

            if (bDefeatRecoveryCoroutine != null)
                StopCoroutine(bDefeatRecoveryCoroutine);
            bDefeatRecoveryCoroutine = StartCoroutine(RecoverToAAfterDelay());
            return true;
        }

        private void OnSecondaryCharacterDied(CharacterUnit defeatedCharacter)
        {
            RecoverToAAfterBDefeated(defeatedCharacter);
        }

        private IEnumerator RecoverToAAfterDelay()
        {
            yield return new WaitForSeconds(1f);
            bDefeatRecoveryCoroutine = null;
            if (HasCharacter(0) && !characters[0].IsDead)
                EnterState(SwapState.PossessingA);
        }

        private void UpdateUI(string prompt)
        {
            if (statePrompt != null && !awaitingStartInput)
                statePrompt.text = prompt;
        }

        private void SetButtonActive(bool active)
        {
            if (swapButton != null)
            {
                swapButton.interactable = active;
                if (swapButton.image != null)
                    swapButton.image.color = active ? unlockedButtonColor : lockedButtonColor;
            }
            if (swapButtonLabel != null)
                swapButtonLabel.text = swapButtonText;
        }

        public void SetSoulSwapUnlocked(bool unlocked)
        {
            soulSwapUnlocked = unlocked && HasCharacter(0) && HasCharacter(1);
            SetButtonActive(soulSwapUnlocked && CanPossessCharacter(GetOtherCharacterIndex()));
        }

        public void FocusCurrentCharacter()
        {
            if (awaitingStartInput) return;
            int index = currentState == SwapState.PossessingB ? 1 : 0;
            if (HasCharacter(index))
            {
                PossessCharacter(index);
                isOverviewView = false;
            }
        }

        public void ReturnToOverview()
        {
            if (awaitingStartInput) return;
            StopCameraBlend();
            cameraBlendCoroutine = StartCoroutine(BlendToOverview());
            isOverviewView = true;
        }

        public void PlayRespawnCameraSequence(CharacterUnit character)
        {
            if (character != null) StartCoroutine(RespawnCameraSequence(character));
        }

        private IEnumerator RespawnCameraSequence(CharacterUnit character)
        {
            PreviewCharacterCamera(character);
            yield return new WaitForSeconds(0.75f);
            ReturnToOverview();
        }

        /// <summary>死亡复活时重置关卡进度，但不重新显示开场输入提示。</summary>
        public void ResetProgressForRespawn()
        {
            awaitingStartInput = false;
            currentState = SwapState.PossessingA;
            SetSoulSwapUnlocked(false);
            SetControlledCharacter(-1);
            isOverviewView = true;
        }

        /// <summary>复活完成后恢复开局的 A 控制权。</summary>
        public void RestoreInitialControlAfterRespawn()
        {
            awaitingStartInput = false;
            currentState = SwapState.PossessingA;
            SetControlledCharacter(0);
            if (allowSwapImmediately)
                SetSoulSwapUnlocked(true);
            UpdateUI("\u63A7\u5236 A");
        }

        private void PreviewCharacterCamera(CharacterUnit character)
        {
            if (character == null) return;

            for (int i = 0; i < characters.Length; i++)
                if (characters[i] != null && characters[i].ViewAnchor != null)
                    characters[i].ViewAnchor.SetCinemachineActive(false);

            CameraAnchor anchor = character.ViewAnchor;
            if (anchor != null && anchor.HasCinemachineCamera)
            {
                BeginBlendToCharacterCamera(anchor);
            }
            else if (cameraFollow != null)
            {
                if (cinemachineBrain != null) cinemachineBrain.enabled = false;
                cameraFollow.enabled = true;
                cameraFollow.SwitchToAnchor(anchor);
            }

            isOverviewView = false;
        }

        private IEnumerator BlendToOverview()
        {
            for (int i = 0; i < characters.Length; i++)
                if (characters[i] != null && characters[i].ViewAnchor != null)
                    characters[i].ViewAnchor.SetCinemachineActive(false);

            if (cinemachineBrain != null) cinemachineBrain.enabled = false;
            if (cameraFollow != null)
            {
                cameraFollow.SwitchToOverview();
                cameraFollow.enabled = false;
            }
            if (mainCamera == null) yield break;

            Vector3 fromPosition = mainCamera.transform.position;
            Quaternion fromRotation = mainCamera.transform.rotation;
            float fromFieldOfView = mainCamera.fieldOfView;
            float fromOrthographicSize = mainCamera.orthographicSize;
            mainCamera.orthographic = mainCameraStartOrthographic;
            float elapsed = 0f;
            while (elapsed < overviewBlendDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, elapsed / overviewBlendDuration);
                mainCamera.transform.SetPositionAndRotation(
                    Vector3.Lerp(fromPosition, mainCameraStartPosition, t),
                    Quaternion.Slerp(fromRotation, mainCameraStartRotation, t));
                mainCamera.fieldOfView = Mathf.Lerp(fromFieldOfView, mainCameraStartFieldOfView, t);
                mainCamera.orthographicSize = Mathf.Lerp(fromOrthographicSize, mainCameraStartOrthographicSize, t);
                yield return null;
            }
            mainCamera.transform.SetPositionAndRotation(mainCameraStartPosition, mainCameraStartRotation);
            RestoreMainCameraLens();
            cameraBlendCoroutine = null;
        }

        private void BeginBlendToCharacterCamera(CameraAnchor anchor)
        {
            if (anchor == null || mainCamera == null) return;

            StopCameraBlend();
            cameraBlendCoroutine = StartCoroutine(BlendToCharacterCamera(anchor));
        }

        private IEnumerator BlendToCharacterCamera(CameraAnchor anchor)
        {
            for (int i = 0; i < characters.Length; i++)
                if (characters[i] != null && characters[i].ViewAnchor != null)
                    characters[i].ViewAnchor.SetCinemachineActive(false);

            if (cameraFollow != null) cameraFollow.enabled = false;
            if (cinemachineBrain != null) cinemachineBrain.enabled = false;

            CinemachineCamera characterCamera = anchor.GetComponent<CinemachineCamera>();
            float targetFieldOfView = characterCamera != null
                ? characterCamera.Lens.FieldOfView
                : mainCamera.fieldOfView;
            Vector3 fromPosition = mainCamera.transform.position;
            Quaternion fromRotation = mainCamera.transform.rotation;
            float fromFieldOfView = mainCamera.fieldOfView;
            float elapsed = 0f;
            while (elapsed < characterBlendDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, elapsed / characterBlendDuration);
                mainCamera.transform.SetPositionAndRotation(
                    Vector3.Lerp(fromPosition, anchor.transform.position, t),
                    Quaternion.Slerp(fromRotation, anchor.transform.rotation, t));
                mainCamera.fieldOfView = Mathf.Lerp(fromFieldOfView, targetFieldOfView, t);
                yield return null;
            }

            mainCamera.transform.SetPositionAndRotation(anchor.transform.position, anchor.transform.rotation);
            mainCamera.fieldOfView = targetFieldOfView;
            anchor.SetCinemachineActive(true);
            if (cinemachineBrain != null) cinemachineBrain.enabled = true;
            cameraBlendCoroutine = null;
        }

        private void StopCameraBlend()
        {
            if (cameraBlendCoroutine == null) return;
            StopCoroutine(cameraBlendCoroutine);
            cameraBlendCoroutine = null;
        }

        private void RestoreMainCameraLens()
        {
            if (mainCamera == null) return;
            mainCamera.orthographic = mainCameraStartOrthographic;
            mainCamera.fieldOfView = mainCameraStartFieldOfView;
            mainCamera.orthographicSize = mainCameraStartOrthographicSize;
        }

        private bool HasStartInput()
        {
            return Input.anyKeyDown
                || Input.GetMouseButtonDown(0)
                || Input.GetMouseButtonDown(1)
                || Input.GetMouseButtonDown(2);
        }

        private void BeginCharacterControl()
        {
            awaitingStartInput = false;
            if (statePrompt != null) statePrompt.gameObject.SetActive(false);
            currentState = SwapState.PossessingA;
            PossessCharacter(0);
            isOverviewView = false;
            onStateChanged?.Invoke(currentState);
        }

        private void ShowStartPrompt()
        {
            if (statePrompt == null) return;

            RectTransform promptTransform = statePrompt.rectTransform;
            promptTransform.anchorMin = new Vector2(0.5f, 0.12f);
            promptTransform.anchorMax = new Vector2(0.5f, 0.12f);
            promptTransform.pivot = new Vector2(0.5f, 0.5f);
            promptTransform.anchoredPosition = Vector2.zero;
            statePrompt.alignment = TextAnchor.MiddleCenter;
            statePrompt.text = "\u6309\u4EFB\u610F\u6309\u952E\u8FDB\u5165\u89D2\u8272\u63A7\u5236";
            statePrompt.gameObject.SetActive(true);
        }
    }
}
