using UnityEngine;
using UnityEngine.Tilemaps;

namespace GameJamRAC.Grid
{
    /// <summary>关卡逻辑格盘：负责世界坐标、格子坐标与可行走 Tile 的转换。</summary>
    [DisallowMultipleComponent]
    public class GridBoard : MonoBehaviour
    {
        [Header("逻辑图层")]
        [SerializeField] private UnityEngine.Grid grid;
        [SerializeField] private Tilemap walkableTilemap;

        [Header("角色站位")]
        [SerializeField] private float unitHeight = 3f;
        [SerializeField, Min(1)] private int defaultMoveCost = 1;

        [Header("编辑器辅助显示")]
        [SerializeField] private bool drawGridGizmos = true;
        [SerializeField] private Color gridColor = new Color(0.2f, 0.85f, 1f, 0.35f);

        public Tilemap WalkableTilemap => walkableTilemap;

        private void Reset()
        {
            grid = GetComponentInChildren<UnityEngine.Grid>();
            walkableTilemap = GetComponentInChildren<Tilemap>();
        }

        public Vector3Int WorldToCell(Vector3 worldPosition)
        {
            if (grid == null) return Vector3Int.zero;

            Vector3Int cell = grid.WorldToCell(worldPosition);
            cell.z = 0;
            return cell;
        }

        public Vector3 GetCellCenterWorld(Vector3Int cell)
        {
            if (grid == null) return transform.position;

            cell.z = 0;
            return grid.GetCellCenterWorld(cell) + Vector3.up * unitHeight;
        }

        public bool CanEnter(Vector3Int cell)
        {
            return walkableTilemap != null && walkableTilemap.HasTile(cell);
        }

        public int GetMoveCost(Vector3Int cell)
        {
            if (walkableTilemap == null) return defaultMoveCost;

            RouteTile routeTile = walkableTilemap.GetTile<RouteTile>(cell);
            return routeTile != null ? Mathf.Max(1, routeTile.moveCost) : defaultMoveCost;
        }

        private void OnDrawGizmosSelected()
        {
            if (!drawGridGizmos || walkableTilemap == null || grid == null) return;

            Gizmos.color = gridColor;
            BoundsInt bounds = walkableTilemap.cellBounds;
            foreach (Vector3Int cell in bounds.allPositionsWithin)
            {
                if (!walkableTilemap.HasTile(cell)) continue;

                Vector3 center = GetCellCenterWorld(cell);
                Vector3 size = new Vector3(grid.cellSize.x, 0.02f, grid.cellSize.y);
                Gizmos.DrawWireCube(center - Vector3.up * unitHeight + Vector3.up * 0.03f, size);
            }
        }
    }
}
