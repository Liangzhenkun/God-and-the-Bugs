using UnityEngine;
using UnityEngine.Tilemaps;

namespace GameJamRAC.Grid
{
    /// <summary>One-way portal: any entrance tile sends the unit to this layer's exit tile.</summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Tilemap))]
    public class BlackholePortal : MonoBehaviour
    {
        [Header("归属")]
        [SerializeField] private GridBoard board;
        [SerializeField] private GridUnitMover mover;
        [SerializeField] private Tilemap portalTilemap;

        [Header("瓦片")]
        [SerializeField] private TileBase entranceTile;
        [SerializeField] private TileBase exitTile;

        private Vector3Int exitCell;
        private bool hasExit;
        private bool teleporting;

        private void Awake()
        {
            ResolveReferences();
            RefreshExitCell();
        }

        private void OnEnable()
        {
            ResolveReferences();
            if (mover != null) mover.onCellReached += OnCellReached;
        }

        private void OnDisable()
        {
            if (mover != null) mover.onCellReached -= OnCellReached;
        }

        [ContextMenu("刷新出口格")]
        public void RefreshExitCell()
        {
            hasExit = false;
            if (portalTilemap == null || exitTile == null) return;

            foreach (Vector3Int cell in portalTilemap.cellBounds.allPositionsWithin)
            {
                if (portalTilemap.GetTile(cell) != exitTile) continue;
                exitCell = cell;
                exitCell.z = 0;
                hasExit = true;
                return;
            }
        }

        private void OnCellReached(Vector3Int cell)
        {
            if (teleporting || portalTilemap == null || entranceTile == null || mover == null) return;
            cell.z = 0;
            if (portalTilemap.GetTile(cell) != entranceTile) return;

            RefreshExitCell();
            if (!hasExit) return;

            teleporting = true;
            mover.TeleportToCell(exitCell);
            teleporting = false;
        }

        private void ResolveReferences()
        {
            if (portalTilemap == null) portalTilemap = GetComponent<Tilemap>();
            if (board == null) board = GetComponentInParent<GridBoard>();
            if (mover == null && board != null && board.Owner != null)
                mover = board.Owner.GetComponent<GridUnitMover>();
        }
    }
}
