using System;
using System.Collections;
using UnityEngine;

namespace GameJamRAC.Grid
{
    /// <summary>格子移动器：只允许进入路线 Tile，并在抵达新格时发出事件。</summary>
    [DisallowMultipleComponent]
    public class GridUnitMover : MonoBehaviour
    {
        [Header("格盘")]
        [SerializeField] private GridBoard board;

        [Header("移动")]
        [SerializeField, Min(0.01f)] private float stepDuration = 0.22f;
        [SerializeField] private bool snapToGridOnStart = true;

        private Vector3Int currentCell;
        private bool isMoving;

        public event Action<int> onEnteredCell;
        public Vector3Int CurrentCell => currentCell;
        public bool IsMoving => isMoving;

        private void Awake()
        {
            if (board == null)
                board = FindFirstObjectByType<GridBoard>();

            if (board != null)
                currentCell = board.WorldToCell(transform.position);
        }

        private void Start()
        {
            if (snapToGridOnStart && board != null && board.CanEnter(currentCell))
                transform.position = board.GetCellCenterWorld(currentCell);
        }

        public bool TryMove(Vector2Int direction)
        {
            if (board == null || isMoving) return false;
            if (Mathf.Abs(direction.x) + Mathf.Abs(direction.y) != 1) return false;

            Vector3Int targetCell = currentCell + new Vector3Int(direction.x, direction.y, 0);
            if (!board.CanEnter(targetCell)) return false;

            StartCoroutine(MoveToCell(targetCell));
            return true;
        }

        private IEnumerator MoveToCell(Vector3Int targetCell)
        {
            isMoving = true;
            Vector3 start = transform.position;
            Vector3 destination = board.GetCellCenterWorld(targetCell);
            float elapsed = 0f;

            while (elapsed < stepDuration)
            {
                elapsed += Time.deltaTime;
                transform.position = Vector3.Lerp(start, destination, elapsed / stepDuration);
                yield return null;
            }

            transform.position = destination;
            currentCell = targetCell;
            isMoving = false;
            onEnteredCell?.Invoke(board.GetMoveCost(targetCell));
        }
    }
}
