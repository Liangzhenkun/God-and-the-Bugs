using GameJamRAC.Camera;
using GameJamRAC.Grid;
using UnityEngine;
using UnityEngine.Events;

namespace GameJamRAC.Gameplay
{
    /// <summary>角色生命、格子移动接管与定向魂穿数据。</summary>
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(GridUnitMover))]
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

        public int CurrentLife => currentLife;
        public bool IsDead => isDead;
        public bool IsPlayerControlled => isPlayerControlled;
        public string DisplayName => displayName;
        public CameraAnchor ViewAnchor => viewAnchor;
        public CharacterUnit SoulTransferTarget => soulTransferTarget;

        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
            rb.isKinematic = true;

            gridMover = GetComponent<GridUnitMover>();
            gridMover.onEnteredCell += SpendLife;

            if (viewAnchor != null)
                viewAnchor.ConfigureCinemachineFollow(transform);
        }

        private void OnDestroy()
        {
            if (gridMover != null)
                gridMover.onEnteredCell -= SpendLife;
        }

        private void Start()
        {
            currentLife = Mathf.Max(0, initialLife);
            isDead = currentLife == 0;
            UpdateLifeLabel();
        }

        private void Update()
        {
            if (isPlayerControlled && !isDead)
                ReadGridInput();
        }

        private void ReadGridInput()
        {
            if (gridMover.IsMoving) return;

            if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow))
                gridMover.TryMove(Vector2Int.up);
            else if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow))
                gridMover.TryMove(Vector2Int.down);
            else if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow))
                gridMover.TryMove(Vector2Int.left);
            else if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow))
                gridMover.TryMove(Vector2Int.right);
        }

        /// <summary>成功进入一个新格后扣除该格的生命消耗。</summary>
        private void SpendLife(int moveCost)
        {
            if (!isDead)
                SetLife(currentLife - Mathf.Max(1, moveCost));
        }

        /// <summary>将当前剩余生命全部交给指定角色，源角色死亡。</summary>
        public int TransferRemainingLifeTo(CharacterUnit target)
        {
            if (isDead || target == null || target.isDead) return 0;

            int transferredLife = currentLife;
            SetLife(0);
            target.ReceiveLife(transferredLife);
            return transferredLife;
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
                isDead = true;
                isPlayerControlled = false;
            }
            UpdateLifeLabel();
        }

        private void UpdateLifeLabel()
        {
            if (scoreLabel != null)
                scoreLabel.SetLife(currentLife, displayName, isDead);
        }

        public void TakeControl()
        {
            if (!isDead)
                isPlayerControlled = true;
        }

        public void ReleaseControl()
        {
            isPlayerControlled = false;
        }
    }
}
