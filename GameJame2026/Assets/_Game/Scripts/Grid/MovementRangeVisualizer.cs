using UnityEngine;
using GameJamRAC.Gameplay;

namespace GameJamRAC.Grid
{
    /// <summary>
    /// 显示跟随角色的能力范围层。它不属于地图 Tilemap，始终是角色子物体，
    /// 只显示落在道路上的能力格。
    /// </summary>
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public class MovementRangeVisualizer : MonoBehaviour
    {
        [SerializeField] private MovementRange movementRange;
        [SerializeField] private GridUnitMover mover;
        private CharacterUnit owner;
        [Header("显示样式")]
        [Tooltip("能力范围地面指示格的颜色；A（透明度）越小，显示越淡。")]
        [SerializeField] private Color rangeColor = new Color(1f, 0.28f, 0.28f, 0.38f);
        [SerializeField, Min(0f)] private float heightOffset = 0.3f;
        [SerializeField] private bool showInEditMode = true;
        [SerializeField, Tooltip("已废弃：运行时显示由当前角色控制权决定。")] private bool alwaysShowInGame;

        private Material material;
        private bool requestedVisible;
        private bool needsRebuild = true;
        private Vector3 lastOwnerPosition;
        private Vector3Int lastCell;
        private int lastRangeHash;

        private void OnEnable()
        {
            if (Application.isPlaying)
                requestedVisible = false;
            needsRebuild = true;
            RebuildIfPossible();
        }

        private void OnValidate()
        {
            needsRebuild = true;
        }

        private void Start()
        {
            RebuildIfPossible();
        }

        private void Update()
        {
            ResolveReferences();
            if (Application.isPlaying && owner != null && requestedVisible != owner.IsPlayerControlled)
            {
                requestedVisible = owner.IsPlayerControlled;
                ApplyVisibility();
            }

            if (!Application.isPlaying && (mover == null || mover.transform.position != lastOwnerPosition))
                needsRebuild = true;

            GridBoard board = mover != null ? mover.Board : null;
            if (board != null)
            {
                Vector3Int cell = GetDisplayCell(board);
                if (cell != lastCell) needsRebuild = true;
            }

            if (movementRange != null && GetRangeHash() != lastRangeHash)
                needsRebuild = true;

            if (needsRebuild)
                RebuildIfPossible();
        }

        public void SetVisible(bool visible)
        {
            requestedVisible = visible;
            ApplyVisibility();
        }

        public void Refresh()
        {
            needsRebuild = true;
            RebuildIfPossible();
        }

        private void RebuildIfPossible()
        {
            ResolveReferences();
            if (movementRange == null || mover == null || mover.Board == null || mover.Board.Grid == null) return;

            EnsureMaterial();
            ClearCells();

            GridBoard board = mover.Board;
            Vector3Int originCell = GetDisplayCell(board);
            CreateCell(board, originCell, true);

            foreach (Vector2Int offset in movementRange.Offsets)
            {
                Vector3Int targetCell = originCell + new Vector3Int(offset.x, offset.y, 0);
                if (board.CanEnter(targetCell))
                    CreateCell(board, targetCell, false);
            }

            lastOwnerPosition = mover.transform.position;
            lastCell = originCell;
            lastRangeHash = GetRangeHash();
            needsRebuild = false;
            ApplyVisibility();
        }

        private void ResolveReferences()
        {
            if (movementRange == null) movementRange = GetComponentInParent<MovementRange>();
            if (mover == null) mover = GetComponentInParent<GridUnitMover>();
            if (owner == null) owner = GetComponentInParent<CharacterUnit>();
        }

        private Vector3Int GetDisplayCell(GridBoard board)
        {
            return Application.isPlaying
                ? mover.CurrentCell
                : board.WorldToCell(mover.transform.position);
        }

        private void EnsureMaterial()
        {
            if (material == null)
            {
                material = new Material(Shader.Find("Sprites/Default"));
                material.name = "AbilityRangeMaterial";
                material.hideFlags = HideFlags.DontSaveInEditor | HideFlags.DontSaveInBuild;
            }

            material.color = rangeColor;
            // Ability cells are transparent ground indicators. Keep them behind
            // character sprites instead of forcing them to render on top.
            material.renderQueue = 3000;
        }

        private void CreateCell(GridBoard board, Vector3Int cellPosition, bool isOrigin)
        {
            GameObject cell = GameObject.CreatePrimitive(PrimitiveType.Quad);
            cell.name = isOrigin ? "RangeOrigin" : "RangeCell_" + cellPosition.x + "_" + cellPosition.y;
            cell.hideFlags = HideFlags.DontSaveInEditor | HideFlags.DontSaveInBuild;
            cell.transform.SetParent(transform, true);
            cell.transform.position = board.Grid.GetCellCenterWorld(cellPosition) + Vector3.up * heightOffset;
            cell.transform.rotation = Quaternion.Euler(90f, 0f, 0f);

            float cellSize = board.Grid.cellSize.x;
            float scale = isOrigin ? 0.72f : 0.88f;
            cell.transform.localScale = new Vector3(cellSize * scale, cellSize * scale, 1f);
            Renderer renderer = cell.GetComponent<Renderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.allowOcclusionWhenDynamic = false;
            // Ground map: 0; road: 5; interaction: 6; exit: 8; character PNG: 15.
            // This keeps the ability hint visible over the ground but behind characters.
            renderer.sortingOrder = 10;

            Collider collider = cell.GetComponent<Collider>();
            if (Application.isPlaying) Destroy(collider);
            else DestroyImmediate(collider);
        }

        private void ClearCells()
        {
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                GameObject cell = transform.GetChild(i).gameObject;
                if (Application.isPlaying)
                {
                    // Destroy 会延迟到帧末；先隐藏旧格子，避免移动时出现一帧的红色拖影。
                    cell.SetActive(false);
                    Destroy(cell);
                }
                else DestroyImmediate(cell);
            }
        }

        private void ApplyVisibility()
        {
            bool visible = Application.isPlaying ? requestedVisible : showInEditMode;

            for (int i = 0; i < transform.childCount; i++)
                transform.GetChild(i).gameObject.SetActive(visible);
        }

        private int GetRangeHash()
        {
            if (movementRange == null) return 0;

            int hash = 17;
            foreach (Vector2Int offset in movementRange.Offsets)
                hash = hash * 31 + offset.GetHashCode();
            return hash;
        }

        private void OnDestroy()
        {
            if (material == null) return;

            if (Application.isPlaying) Destroy(material);
            else DestroyImmediate(material);
        }
    }
}
