using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace GameJamRAC.Grid
{
    /// <summary>单向黑洞：入口 Tile 会把绑定的角色送到出口 Tile。</summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Tilemap))]
    public class BlackholePortal : MonoBehaviour
    {
        [Header("归属")]
        [SerializeField] private GridBoard board;
        [SerializeField] private GridUnitMover mover;
        [SerializeField] private Tilemap portalTilemap;

        [Header("额外触发者")]
        [SerializeField] private GridUnitMover[] additionalMovers;

        [Header("瓦片")]
        [SerializeField] private TileBase entranceTile;
        [SerializeField] private TileBase exitTile;

        private readonly Dictionary<GridUnitMover, Action<Vector3Int>> moverHandlers = new Dictionary<GridUnitMover, Action<Vector3Int>>();
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
            RefreshSubscriptions();
        }

        private void OnDisable()
        {
            ClearSubscriptions();
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

        private void RefreshSubscriptions()
        {
            ClearSubscriptions();
            Subscribe(mover);

            if (additionalMovers == null) return;
            foreach (GridUnitMover additionalMover in additionalMovers)
                Subscribe(additionalMover);
        }

        private void Subscribe(GridUnitMover targetMover)
        {
            if (targetMover == null || moverHandlers.ContainsKey(targetMover)) return;

            Action<Vector3Int> handler = _ => OnMoverCellReached(targetMover);
            moverHandlers.Add(targetMover, handler);
            targetMover.onCellReached += handler;
        }

        private void ClearSubscriptions()
        {
            foreach (KeyValuePair<GridUnitMover, Action<Vector3Int>> pair in moverHandlers)
                pair.Key.onCellReached -= pair.Value;

            moverHandlers.Clear();
        }

        private void OnMoverCellReached(GridUnitMover activeMover)
        {
            if (teleporting || activeMover == null || portalTilemap == null || entranceTile == null) return;
            if (!HasPortalAuthority(activeMover)) return;

            GridBoard activeBoard = activeMover.Board;
            if (activeBoard == null) return;

            Vector3Int portalCell = board != null
                ? board.WorldToCell(activeMover.transform.position)
                : activeBoard.WorldToCell(activeMover.transform.position);
            portalCell.z = 0;
            if (portalTilemap.GetTile(portalCell) != entranceTile) return;

            RefreshExitCell();
            if (!hasExit) return;

            Vector3 exitWorldPosition = board != null
                ? board.GetCellCenterWorld(exitCell)
                : portalTilemap.GetCellCenterWorld(exitCell);
            Vector3Int activeExitCell = activeBoard.WorldToCell(exitWorldPosition);

            teleporting = true;
            activeMover.TeleportToCell(activeExitCell);
            teleporting = false;
        }

        private static bool HasPortalAuthority(GridUnitMover activeMover)
        {
            GameJamRAC.Gameplay.CharacterUnit character =
                activeMover.GetComponent<GameJamRAC.Gameplay.CharacterUnit>();
            if (character == null) return true;

            GameJamRAC.Gameplay.SoulSwapManager soulSwap =
                FindFirstObjectByType<GameJamRAC.Gameplay.SoulSwapManager>();
            return soulSwap != null
                ? soulSwap.IsActiveControlledCharacter(character)
                : character.IsPlayerControlled;
        }

        private void ResolveReferences()
        {
            if (portalTilemap == null) portalTilemap = GetComponent<Tilemap>();

            GridBoard parentBoard = GetComponentInParent<GridBoard>();
            if (parentBoard != null) board = parentBoard;

            GridUnitMover ownerMover = board != null && board.Owner != null
                ? board.Owner.GetComponent<GridUnitMover>()
                : null;
            if (ownerMover != null) mover = ownerMover;
        }
    }
}
