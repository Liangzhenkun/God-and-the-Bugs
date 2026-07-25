using GameJamRAC.Camera;
using GameJamRAC.Grid;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using System;

namespace GameJamRAC.Gameplay
{
    /// <summary>角色生命、格子移动接管与定向魂穿数据。</summary>
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(GridUnitMover))]
    [RequireComponent(typeof(GridCenterPlacement))]
    public class CharacterUnit : MonoBehaviour
    {
        [Header("角色属性")]
        [SerializeField] private int initialLife = 10;
        [SerializeField] private string displayName = "角色";

        [Header("头顶标签")]
        [SerializeField] private ScoreLabelUI scoreLabel;

        [Header("接管机位")]
        [SerializeField] private CameraAnchor viewAnchor;

        [Header("魂穿指向")]
        [SerializeField] private CharacterUnit soulTransferTarget;

        [Header("事件")]
        public UnityEvent<int> onScoreChanged;
        public UnityEvent<int> onScoreReceived;

        private bool isPlayerControlled;
        private Rigidbody rb;
        private GridUnitMover gridMover;
        private int currentLife;
        private bool isDead;
        private bool wasPlayerControlledAtDeath;
        private bool suppressAutomaticRespawnOnDeath;
        private float walkedPathLength;
        private Vector3 spawnPosition;
        private Quaternion spawnRotation;
        private bool spawnCaptured;

        public int CurrentLife => currentLife;
        public bool IsDead => isDead;
        public bool WasPlayerControlledAtDeath => wasPlayerControlledAtDeath;
        public bool SuppressAutomaticRespawnOnDeath => suppressAutomaticRespawnOnDeath;
        public bool IsPlayerControlled => isPlayerControlled;
        public float WalkedPathLength => walkedPathLength;
        public string DisplayName => displayName;
        public CameraAnchor ViewAnchor
        {
            get
            {
                if (viewAnchor == null)
                    viewAnchor = GetComponentInChildren<CameraAnchor>(true);

                return viewAnchor;
            }
        }
        public CharacterUnit SoulTransferTarget => soulTransferTarget;
        public event Action<CharacterUnit> onDied;

        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
            rb.isKinematic = true;

            gridMover = GetComponent<GridUnitMover>();
            gridMover.onEnteredCell += CheckForExit;
            gridMover.onEnteredCell += SpendLife;
            gridMover.onPathLengthChanged += UpdateWalkedPathLength;

            CameraAnchor anchor = ViewAnchor;
            if (anchor != null)
                anchor.ConfigureCinemachineFollow(transform);
        }

        private void OnValidate()
        {
            if (Application.isPlaying || scoreLabel == null) return;

            int previewLife = Mathf.Max(0, initialLife);
            scoreLabel.SetLife(previewLife, displayName, previewLife == 0);
            scoreLabel.SetPathLength(0f);
        }

        private void OnDestroy()
        {
            if (gridMover != null)
            {
                gridMover.onEnteredCell -= CheckForExit;
                gridMover.onEnteredCell -= SpendLife;
                gridMover.onPathLengthChanged -= UpdateWalkedPathLength;
            }
        }

        private void Start()
        {
            currentLife = Mathf.Max(0, initialLife);
            isDead = currentLife == 0;
            onScoreChanged?.Invoke(currentLife);
            UpdateLifeLabel();
            StartCoroutine(CaptureSpawnAfterSetup());
        }

        private System.Collections.IEnumerator CaptureSpawnAfterSetup()
        {
            yield return new WaitForEndOfFrame();
            spawnPosition = transform.position;
            spawnRotation = transform.rotation;
            spawnCaptured = true;
        }

        private void Update()
        {
            if (isPlayerControlled && !isDead)
                ReadGridInput();
        }

        private void ReadGridInput()
        {
            if (gridMover.IsMoving) return;

            if (!Input.GetMouseButtonDown(0)) return;
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;

            gridMover.TryMoveFromScreen(UnityEngine.Camera.main, Input.mousePosition);
        }

        /// <summary>成功进入一个新格后扣除该格的生命消耗。</summary>
        private void SpendLife(int moveCost)
        {
            if (!isDead)
                SetLife(currentLife - Mathf.Max(1, moveCost));
        }

        private void CheckForExit(int _)
        {
            if (!isPlayerControlled) return;

            ExitGoal exitGoal = FindFirstObjectByType<ExitGoal>();
            if (exitGoal == null || !exitGoal.ContainsWorldPosition(transform.position)) return;

            VictoryFlowManager victoryFlow = FindFirstObjectByType<VictoryFlowManager>();
            if (victoryFlow != null)
                victoryFlow.Win();
        }

        /// <summary>将当前剩余生命全部交给指定角色，源角色死亡。</summary>
        public int TransferRemainingLifeTo(CharacterUnit target)
        {
            if (isDead || target == null || target.isDead) return 0;

            int transferredLife = currentLife;
            suppressAutomaticRespawnOnDeath = true;
            SetLife(0);
            target.ReceiveLife(transferredLife);
            return transferredLife;
        }

        public void AddLife(int amount)
        {
            if (isDead || amount <= 0) return;
            SetLife(currentLife + amount);
        }

        private void ReceiveLife(int amount)
        {
            if (isDead || amount <= 0) return;
            SetLife(currentLife + amount);
            onScoreReceived?.Invoke(amount);
        }

        private void SetLife(int value)
        {
            currentLife = Mathf.Max(0, value);
            onScoreChanged?.Invoke(currentLife);
            if (currentLife == 0)
            {
                bool wasDead = isDead;
                wasPlayerControlledAtDeath = isPlayerControlled;
                isDead = true;
                isPlayerControlled = false;
                if (!wasDead) onDied?.Invoke(this);
            }
            UpdateLifeLabel();
        }

        public void RespawnAtInitialPosition()
        {
            if (!spawnCaptured)
            {
                spawnPosition = transform.position;
                spawnRotation = transform.rotation;
                spawnCaptured = true;
            }

            transform.SetPositionAndRotation(spawnPosition, spawnRotation);
            gridMover?.ResetAtWorldPosition(spawnPosition);
            currentLife = Mathf.Max(1, initialLife);
            isDead = false;
            wasPlayerControlledAtDeath = false;
            suppressAutomaticRespawnOnDeath = false;
            isPlayerControlled = false;
            walkedPathLength = 0f;
            onScoreChanged?.Invoke(currentLife);
            UpdateLifeLabel();
        }

        private void UpdateLifeLabel()
        {
            if (scoreLabel != null)
            {
                scoreLabel.SetLife(currentLife, displayName, isDead);
                scoreLabel.SetPathLength(walkedPathLength);
            }
        }

        private void UpdateWalkedPathLength(float pathLength)
        {
            walkedPathLength = pathLength;
            if (scoreLabel != null)
                scoreLabel.SetPathLength(walkedPathLength);
        }

        public void TakeControl()
        {
            if (!isDead)
            {
                isPlayerControlled = true;
                gridMover.SetMoveTargetsVisible(true);
            }
        }

        public void ReleaseControl()
        {
            isPlayerControlled = false;
            gridMover.SetMoveTargetsVisible(false);
        }
    }
}
