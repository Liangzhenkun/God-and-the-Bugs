using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace GameJamRAC.Grid
{
    /// <summary>格子移动器：只允许进入路线 Tile，并在抵达新格时发出事件。</summary>
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public class GridUnitMover : MonoBehaviour
    {
        [Header("格盘")]
        [SerializeField] private GridBoard board;

        [Header("移动")]
        [SerializeField, Min(0.01f)] private float stepDuration = 0.22f;
        [SerializeField] private bool snapToGridOnStart = true;

        [Header("编辑器站位")]
        [SerializeField] private bool snapToRoadInEditMode = true;

        private Vector3Int currentCell;
        private bool isMoving;
        private bool showMoveTargets;
        private float totalPathLength;
        private MovementRange movementRange;
        private MovementRangeVisualizer rangeVisualizer;
        private Vector3 lastEditorPosition;
        private bool isSnappingInEditor;

        public event Action<int> onEnteredCell;
        public event Action<Vector3Int> onCellReached;
        public event Action<float> onMovedDistance;
        public event Action<float> onPathLengthChanged;
        public GridBoard Board => board;
        public Vector3Int CurrentCell => currentCell;
        public bool IsMoving => isMoving;
        public float TotalPathLength => totalPathLength;

        private void Awake()
        {
            if (board == null)
                board = FindFirstObjectByType<GridBoard>();

            movementRange = GetComponent<MovementRange>();
            rangeVisualizer = GetComponentInChildren<MovementRangeVisualizer>(true);

            if (board != null)
                currentCell = board.WorldToCell(transform.position);
        }

        private void OnEnable()
        {
            if (!Application.isPlaying)
                SnapToRoadInEditor();

            lastEditorPosition = transform.position;
        }

        private void OnValidate()
        {
            if (!Application.isPlaying)
                SnapToRoadInEditor();
        }

        private void Update()
        {
            if (Application.isPlaying || !snapToRoadInEditMode || isSnappingInEditor) return;
            if ((transform.position - lastEditorPosition).sqrMagnitude < 0.000001f) return;

            SnapToRoadInEditor();
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
            return TryMoveToCell(targetCell);
        }

        public bool TryMoveToCell(Vector3Int targetCell)
        {
            return TryMoveToCellInternal(targetCell, false, board != null ? board.GetMoveCost(targetCell) : 1);
        }

        /// <summary>AI movement that can use a custom life cost and ignore character ability overlays.</summary>
        public bool TryMoveToCellAsAi(Vector3Int targetCell, int lifeCost)
        {
            return TryMoveToCellInternal(targetCell, true, Mathf.Max(1, lifeCost));
        }

        private bool TryMoveToCellInternal(Vector3Int targetCell, bool ignoreAbility, int lifeCost)
        {
            // 禁用移动器代表该角色本回合不能再行动；仍可被外部事件传送或复位。
            if (!isActiveAndEnabled || board == null || isMoving) return false;

            targetCell.z = 0;
            Vector2Int offset = new Vector2Int(targetCell.x - currentCell.x, targetCell.y - currentCell.y);
            if ((!ignoreAbility && !CanMoveByAbility(offset)) || !board.CanEnter(targetCell)) return false;
            if (!HasContinuousRoad(currentCell, targetCell)) return false;

            StartCoroutine(MoveToCell(targetCell, GetGridDistance(offset), lifeCost));
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

        public void ResetAtWorldPosition(Vector3 worldPosition)
        {
            StopAllCoroutines();
            isMoving = false;
            totalPathLength = 0f;
            transform.position = worldPosition;
            if (board != null) currentCell = board.WorldToCell(worldPosition);
            RefreshMoveTargets();
            onPathLengthChanged?.Invoke(totalPathLength);
        }

        /// <summary>Places the unit at a valid cell without adding walking distance.</summary>
        public bool TeleportToCell(Vector3Int targetCell)
        {
            if (board == null) return false;

            targetCell.z = 0;
            if (!board.CanEnter(targetCell)) return false;

            StopAllCoroutines();
            isMoving = false;
            currentCell = targetCell;
            transform.position = board.GetCellCenterWorld(targetCell);
            RefreshMoveTargets();
            return true;
        }

        private IEnumerator MoveToCell(Vector3Int targetCell, float pathDistance, int lifeCost)
        {
            isMoving = true;
            Vector3 start = transform.position;
            Vector3 destination = board.GetCellCenterWorld(targetCell);
            float elapsed = 0f;
            float duration = Mathf.Max(stepDuration, stepDuration * pathDistance);

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                transform.position = Vector3.Lerp(start, destination, elapsed / duration);
                yield return null;
            }

            transform.position = destination;
            currentCell = targetCell;
            isMoving = false;
            totalPathLength += pathDistance;
            RefreshMoveTargets();
            board.NotifyCellEntered(targetCell);
            onEnteredCell?.Invoke(Mathf.Max(1, lifeCost));
            onCellReached?.Invoke(targetCell);
            onMovedDistance?.Invoke(pathDistance);
            onPathLengthChanged?.Invoke(totalPathLength);
        }

        public void RefreshMoveTargets()
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
            Vector3Int targetCell = currentCell + new Vector3Int(offset.x, offset.y, 0);
            if (board != null && board.IsTemporaryWalkableCell(targetCell))
                return true;

            if (movementRange == null)
                return Mathf.Abs(offset.x) + Mathf.Abs(offset.y) == 1;

            return movementRange.Contains(offset);
        }

        private bool HasContinuousRoad(Vector3Int start, Vector3Int target)
        {
            bool isStartCell = true;
            foreach (Vector3Int cell in GetLineCells(start, target))
            {
                // A bridge tile can disappear immediately after the unit arrives on it.
                // The current standing cell must not prevent movement to a valid next cell.
                if (isStartCell)
                {
                    isStartCell = false;
                    continue;
                }

                if (!board.CanEnter(cell)) return false;
            }

            return true;
        }

        private static float GetGridDistance(Vector2Int offset)
        {
            return Mathf.Sqrt(offset.x * offset.x + offset.y * offset.y);
        }

        private static IEnumerable<Vector3Int> GetLineCells(Vector3Int start, Vector3Int target)
        {
            int x = start.x;
            int y = start.y;
            int dx = Mathf.Abs(target.x - x);
            int dy = Mathf.Abs(target.y - y);
            int stepX = x < target.x ? 1 : -1;
            int stepY = y < target.y ? 1 : -1;
            int error = dx - dy;

            while (true)
            {
                yield return new Vector3Int(x, y, 0);
                if (x == target.x && y == target.y) yield break;

                int doubledError = error * 2;
                if (doubledError > -dy)
                {
                    error -= dy;
                    x += stepX;
                }

                if (doubledError < dx)
                {
                    error += dx;
                    y += stepY;
                }
            }
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

        [ContextMenu("吸附到最近的道路格心")]
        private void SnapToRoadInEditor()
        {
            if (Application.isPlaying || !snapToRoadInEditMode || board == null || board.WalkableTilemap == null)
            {
                lastEditorPosition = transform.position;
                return;
            }

            isSnappingInEditor = true;
            Vector3Int bestCell = board.WorldToCell(transform.position);

            if (!board.CanEnter(bestCell))
            {
                float bestDistance = float.PositiveInfinity;
                BoundsInt bounds = board.WalkableTilemap.cellBounds;
                foreach (Vector3Int cell in bounds.allPositionsWithin)
                {
                    if (!board.WalkableTilemap.HasTile(cell)) continue;

                    Vector3 center = board.GetCellCenterWorld(cell);
                    float distance = (new Vector2(center.x, center.z) - new Vector2(transform.position.x, transform.position.z)).sqrMagnitude;
                    if (distance >= bestDistance) continue;

                    bestDistance = distance;
                    bestCell = cell;
                }
            }

            if (board.CanEnter(bestCell))
            {
                // 编辑器吸附只约束地面 X/Z；角色模型的高度由现有摆放决定，
                // 不应在拖动时被道路层高度重写。
                Vector3 snappedPosition = board.GetCellCenterWorld(bestCell);
                snappedPosition.y = transform.position.y;
                transform.position = snappedPosition;
            }

            lastEditorPosition = transform.position;
            isSnappingInEditor = false;
        }
    }
}
