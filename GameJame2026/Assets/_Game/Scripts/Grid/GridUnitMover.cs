using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Tilemaps;

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
        private bool showMoveTargets;
        private MovementRange movementRange;
        private MovementRangeVisualizer rangeVisualizer;

        public event Action<int> onEnteredCell;
        public GridBoard Board => board;
        public Vector3Int CurrentCell => currentCell;
        public bool IsMoving => isMoving;

        private void Awake()
        {
            if (board == null)
                board = FindFirstObjectByType<GridBoard>();

            movementRange = GetComponent<MovementRange>();
            rangeVisualizer = GetComponent<MovementRangeVisualizer>();

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

        public bool TryMoveToCell(Vector3Int targetCell)
        {
            if (board == null || isMoving) return false;

            targetCell.z = 0;
            Vector2Int offset = new Vector2Int(targetCell.x - currentCell.x, targetCell.y - currentCell.y);
            if (!CanMoveByAbility(offset) || !board.CanEnter(targetCell)) return false;

            StartCoroutine(MoveToCell(targetCell));
            return true;
        }

        public bool TryMoveFromScreen(UnityEngine.Camera camera, Vector3 screenPosition)
        {
            return board != null
                && board.TryGetCellFromScreen(camera, screenPosition, out Vector3Int targetCell)
                && TryMoveToCell(targetCell);
        }

        public void SetMoveTargetsVisible(bool visible)
        {
            showMoveTargets = visible;
            if (rangeVisualizer != null)
                rangeVisualizer.SetVisible(visible);
            RefreshMoveTargets();
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
            RefreshMoveTargets();
            board.NotifyCellEntered(targetCell);
            onEnteredCell?.Invoke(board.GetMoveCost(targetCell));
        }

        private void RefreshMoveTargets()
        {
            if (board == null || board.WalkableTilemap == null) return;

            if (rangeVisualizer != null)
                rangeVisualizer.Refresh();

            Tilemap tilemap = board.WalkableTilemap;
            BoundsInt bounds = tilemap.cellBounds;
            foreach (Vector3Int cell in bounds.allPositionsWithin)
            {
                if (!tilemap.HasTile(cell)) continue;

                tilemap.SetTileFlags(cell, TileFlags.None);
                tilemap.SetColor(cell, Color.white);
            }

            if (!showMoveTargets) return;

            foreach (Vector2Int offset in GetAbilityOffsets())
            {
                Vector3Int cell = currentCell + new Vector3Int(offset.x, offset.y, 0);
                if (board.CanEnter(cell))
                    tilemap.SetColor(cell, new Color(1f, 0.92f, 0.2f, 1f));
            }
        }

        private bool CanMoveByAbility(Vector2Int offset)
        {
            if (movementRange == null)
                return Mathf.Abs(offset.x) + Mathf.Abs(offset.y) == 1;

            return movementRange.Contains(offset);
        }

        private System.Collections.Generic.IReadOnlyList<Vector2Int> GetAbilityOffsets()
        {
            if (movementRange != null)
                return movementRange.Offsets;

            return DefaultOffsets;
        }

        private static readonly Vector2Int[] DefaultOffsets =
        {
            Vector2Int.right,
            Vector2Int.left,
            Vector2Int.up,
            Vector2Int.down
        };
    }
}
