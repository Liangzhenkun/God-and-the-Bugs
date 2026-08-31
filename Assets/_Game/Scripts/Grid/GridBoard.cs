using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Tilemaps;
using GameJamRAC.Gameplay;
using System;
using System.Collections.Generic;

namespace GameJamRAC.Grid
{
    /// <summary>关卡逻辑格盘：负责世界坐标、格子坐标与可行走 Tile 的转换。</summary>
    [DisallowMultipleComponent]
    public class GridBoard : MonoBehaviour
    {
        [Header("逻辑图层")]
        [SerializeField] private UnityEngine.Grid grid;
        [SerializeField] private Tilemap walkableTilemap;
        [SerializeField] private Tilemap interactionTilemap;

        [Header("道路归属")]
        [SerializeField] private CharacterUnit owner;

        [Header("特殊格事件")]
        [SerializeField] private UnityEvent<string> onInteractiveTileEntered;

        [Header("角色站位")]
        [SerializeField] private float unitHeight = 3f;
        [SerializeField, Min(1)] private int defaultMoveCost = 1;
        [SerializeField] private float roadLayerHeight = 0.05f;

        [Header("编辑器辅助显示")]
        [SerializeField] private bool drawGridGizmos = true;
        [SerializeField] private Color gridColor = new Color(0.2f, 0.85f, 1f, 0.35f);

        private readonly HashSet<Vector3Int> temporaryWalkableCells = new HashSet<Vector3Int>();
        private readonly Dictionary<Vector3Int, float> temporaryCellHeights = new Dictionary<Vector3Int, float>();

        public Tilemap WalkableTilemap => walkableTilemap;
        public Tilemap InteractionTilemap => interactionTilemap;
        public UnityEngine.Grid Grid => grid;
        public float UnitHeight => unitHeight;
        public CharacterUnit Owner => owner;
        public IEnumerable<Vector3Int> TemporaryWalkableCells => temporaryWalkableCells;
        public event Action<string> InteractiveTileEntered;

        private void Reset()
        {
            grid = GetComponentInChildren<UnityEngine.Grid>();
            foreach (Tilemap tilemap in GetComponentsInChildren<Tilemap>(true))
            {
                if (tilemap.name.StartsWith("WalkableTilemap")) walkableTilemap = tilemap;
                if (tilemap.name.StartsWith("InteractionTilemap")) interactionTilemap = tilemap;
            }
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
            float height = temporaryCellHeights.TryGetValue(cell, out float temporaryHeight)
                ? temporaryHeight
                : unitHeight;
            return grid.GetCellCenterWorld(cell) + Vector3.up * height;
        }

        public bool CanEnter(Vector3Int cell)
        {
            return (walkableTilemap != null && walkableTilemap.HasTile(cell))
                || temporaryWalkableCells.Contains(cell);
        }

        public bool TryGetCellFromScreen(UnityEngine.Camera camera, Vector3 screenPosition, out Vector3Int cell)
        {
            cell = default;
            if (camera == null || grid == null) return false;

            Ray ray = camera.ScreenPointToRay(screenPosition);
            Plane gridPlane = new Plane(grid.transform.forward, grid.transform.position);
            if (!gridPlane.Raycast(ray, out float distance)) return false;

            cell = WorldToCell(ray.GetPoint(distance));
            return true;
        }

        public int GetMoveCost(Vector3Int cell)
        {
            if (walkableTilemap == null) return defaultMoveCost;

            RouteTile routeTile = walkableTilemap.GetTile<RouteTile>(cell);
            return routeTile != null ? Mathf.Max(1, routeTile.moveCost) : defaultMoveCost;
        }

        public RouteTile GetRouteTile(Vector3Int cell)
        {
            return walkableTilemap != null ? walkableTilemap.GetTile<RouteTile>(cell) : null;
        }

        public bool HasInteraction(Vector3Int cell)
        {
            RouteTile tile = interactionTilemap != null
                ? interactionTilemap.GetTile<RouteTile>(cell)
                : null;
            return tile != null && tile.IsInteractive;
        }

        public void NotifyCellEntered(Vector3Int cell)
        {
            RouteTile tile = interactionTilemap != null
                ? interactionTilemap.GetTile<RouteTile>(cell)
                : null;
            if (tile != null && tile.IsInteractive)
            {
                onInteractiveTileEntered?.Invoke(tile.InteractionId);
                InteractiveTileEntered?.Invoke(tile.InteractionId);
            }
        }

        public void SetTemporaryWalkableCell(Vector3Int cell, bool enabled, float worldHeight)
        {
            cell.z = 0;
            if (enabled)
            {
                temporaryWalkableCells.Add(cell);
                temporaryCellHeights[cell] = worldHeight;
            }
            else
            {
                temporaryWalkableCells.Remove(cell);
                temporaryCellHeights.Remove(cell);
            }
        }

        public bool IsTemporaryWalkableCell(Vector3Int cell)
        {
            cell.z = 0;
            return temporaryWalkableCells.Contains(cell);
        }

        /// <summary>清除运行时临时道路（例如 B 作为桥梁时生成的格子）。</summary>
        public void ClearTemporaryWalkableCells()
        {
            temporaryWalkableCells.Clear();
            temporaryCellHeights.Clear();
        }

        [ContextMenu("对齐道路首格到所属角色")]
        public void AlignOriginToOwner()
        {
            if (owner == null || grid == null) return;

            Vector3 center = grid.GetCellCenterWorld(Vector3Int.zero);
            transform.position += new Vector3(
                owner.transform.position.x - center.x,
                roadLayerHeight - center.y,
                owner.transform.position.z - center.z);
            unitHeight = owner.transform.position.y - roadLayerHeight;
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
