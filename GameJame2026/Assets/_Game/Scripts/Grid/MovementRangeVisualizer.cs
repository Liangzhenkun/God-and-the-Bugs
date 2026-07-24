using UnityEngine;

namespace GameJamRAC.Grid
{
    /// <summary>在角色脚下显示红色能力范围格，仅用于当前受控角色。</summary>
    [DisallowMultipleComponent]
    public class MovementRangeVisualizer : MonoBehaviour
    {
        [SerializeField] private MovementRange movementRange;
        [SerializeField] private GridUnitMover mover;
        [SerializeField] private Color rangeColor = new Color(1f, 0.15f, 0.15f, 0.45f);
        [SerializeField, Min(0f)] private float heightOffset = 0.04f;

        private GameObject root;
        private Material material;
        private bool isVisible;

        private void Awake()
        {
            if (movementRange == null) movementRange = GetComponent<MovementRange>();
            if (mover == null) mover = GetComponent<GridUnitMover>();
            Build();
            SetVisible(false);
        }

        public void SetVisible(bool visible)
        {
            isVisible = visible;
            if (root != null)
                root.SetActive(visible);
        }

        public void Refresh()
        {
            if (root == null) return;

            for (int i = root.transform.childCount - 1; i >= 0; i--)
            {
                GameObject child = root.transform.GetChild(i).gameObject;
                child.SetActive(false);
                Destroy(child);
            }

            BuildCells();
            root.SetActive(isVisible);
        }

        private void Build()
        {
            if (movementRange == null || mover == null || root != null) return;

            root = new GameObject("AbilityRangeOverlay");
            root.transform.SetParent(transform, false);
            material = new Material(Shader.Find("Sprites/Default"));
            material.color = rangeColor;

            BuildCells();
        }

        private void BuildCells()
        {
            if (movementRange == null || mover == null || root == null) return;

            GridBoard board = mover.Board;
            if (board == null || board.Grid == null) return;

            float cellSize = board.Grid.cellSize.x;
            float footOffset = -board.UnitHeight;
            CreateCell(Vector2Int.zero, cellSize, footOffset, true);

            foreach (Vector2Int offset in movementRange.Offsets)
            {
                Vector3Int targetCell = mover.CurrentCell + new Vector3Int(offset.x, offset.y, 0);
                if (board.CanEnter(targetCell))
                    CreateCell(offset, cellSize, footOffset, false);
            }
        }

        private void CreateCell(Vector2Int offset, float cellSize, float footOffset, bool isCenter)
        {
            GameObject cell = GameObject.CreatePrimitive(PrimitiveType.Quad);
            cell.name = isCenter ? "RangeOrigin" : $"RangeCell_{offset.x}_{offset.y}";
            cell.transform.SetParent(root.transform, false);
            cell.transform.localPosition = new Vector3(offset.x * cellSize, footOffset + heightOffset, offset.y * cellSize);
            cell.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            float scale = isCenter ? 0.72f : 0.88f;
            cell.transform.localScale = new Vector3(cellSize * scale, cellSize * scale, 1f);
            cell.GetComponent<Renderer>().sharedMaterial = material;
            Destroy(cell.GetComponent<Collider>());
        }

        private void OnDestroy()
        {
            if (material != null)
                Destroy(material);
        }
    }
}
