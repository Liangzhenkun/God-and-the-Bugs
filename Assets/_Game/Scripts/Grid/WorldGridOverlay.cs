using System.Collections.Generic;
using UnityEngine;

namespace GameJamRAC.Grid
{
    /// <summary>
    /// 在地图表面绘制与道路完全同格的棋盘提示线，帮助玩家辨认可点击的格子中心。
    /// </summary>
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public class WorldGridOverlay : MonoBehaviour
    {
        [Header("参考")]
        [SerializeField] private UnityEngine.Grid grid;
        [SerializeField] private Renderer groundRenderer;

        [Header("显示")]
        [SerializeField] private Color lineColor = new Color(0.03f, 0.1f, 0.12f, 0.38f);
        [SerializeField, Min(0.005f)] private float lineWidth = 0.035f;
        [SerializeField, Min(0f)] private float heightOffset = 0.015f;
        [SerializeField] private int sortingOrder = 1;

        private MeshFilter meshFilter;
        private MeshRenderer meshRenderer;
        private Mesh mesh;
        private Material material;

        private void OnEnable()
        {
            EnsureComponents();
            Refresh();
        }

        private void OnValidate()
        {
            EnsureComponents();
            Refresh();
        }

        [ContextMenu("刷新世界格子提示")]
        public void Refresh()
        {
            if (grid == null || groundRenderer == null || mesh == null) return;

            Bounds bounds = groundRenderer.bounds;
            Vector3Int minCell = grid.WorldToCell(new Vector3(bounds.min.x, grid.transform.position.y, bounds.min.z));
            Vector3Int maxCell = grid.WorldToCell(new Vector3(bounds.max.x, grid.transform.position.y, bounds.max.z));

            int minX = Mathf.Min(minCell.x, maxCell.x) - 1;
            int maxX = Mathf.Max(minCell.x, maxCell.x) + 1;
            int minY = Mathf.Min(minCell.y, maxCell.y) - 1;
            int maxY = Mathf.Max(minCell.y, maxCell.y) + 1;
            float cellSize = grid.cellSize.x;
            float y = bounds.max.y + heightOffset;

            Vector3 minCenter = grid.GetCellCenterWorld(new Vector3Int(minX, minY, 0));
            float startX = minCenter.x - cellSize * 0.5f;
            float startZ = minCenter.z - cellSize * 0.5f;
            float endX = startX + (maxX - minX + 2) * cellSize;
            float endZ = startZ + (maxY - minY + 2) * cellSize;

            List<Vector3> vertices = new List<Vector3>();
            List<int> triangles = new List<int>();
            for (int x = minX; x <= maxX + 1; x++)
            {
                float lineX = startX + (x - minX) * cellSize;
                AddLine(vertices, triangles, new Vector3(lineX, y, startZ), new Vector3(lineX, y, endZ));
            }

            for (int cellY = minY; cellY <= maxY + 1; cellY++)
            {
                float lineZ = startZ + (cellY - minY) * cellSize;
                AddLine(vertices, triangles, new Vector3(startX, y, lineZ), new Vector3(endX, y, lineZ));
            }

            mesh.Clear();
            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateBounds();
            meshFilter.sharedMesh = mesh;

            material.color = lineColor;
            material.renderQueue = 3100;
            meshRenderer.sortingOrder = sortingOrder;
        }

        private void EnsureComponents()
        {
            if (meshFilter == null) meshFilter = GetComponent<MeshFilter>();
            if (meshFilter == null) meshFilter = gameObject.AddComponent<MeshFilter>();
            if (meshRenderer == null) meshRenderer = GetComponent<MeshRenderer>();
            if (meshRenderer == null) meshRenderer = gameObject.AddComponent<MeshRenderer>();

            if (mesh == null)
            {
                mesh = new Mesh { name = "WorldGridOverlayMesh" };
                mesh.hideFlags = HideFlags.DontSaveInEditor | HideFlags.DontSaveInBuild;
            }

            if (material == null)
            {
                material = new Material(Shader.Find("Sprites/Default"));
                material.name = "WorldGridOverlayMaterial";
                material.hideFlags = HideFlags.DontSaveInEditor | HideFlags.DontSaveInBuild;
            }

            meshRenderer.sharedMaterial = material;
            meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            meshRenderer.receiveShadows = false;
            meshRenderer.allowOcclusionWhenDynamic = false;
        }

        private void AddLine(List<Vector3> vertices, List<int> triangles, Vector3 start, Vector3 end)
        {
            Vector3 direction = (end - start).normalized;
            Vector3 perpendicular = new Vector3(-direction.z, 0f, direction.x) * (lineWidth * 0.5f);
            int index = vertices.Count;
            vertices.Add(start - perpendicular);
            vertices.Add(start + perpendicular);
            vertices.Add(end + perpendicular);
            vertices.Add(end - perpendicular);
            triangles.Add(index);
            triangles.Add(index + 1);
            triangles.Add(index + 2);
            triangles.Add(index);
            triangles.Add(index + 2);
            triangles.Add(index + 3);
        }

        private void OnDestroy()
        {
            if (mesh != null)
            {
                if (Application.isPlaying) Destroy(mesh);
                else DestroyImmediate(mesh);
            }

            if (material != null)
            {
                if (Application.isPlaying) Destroy(material);
                else DestroyImmediate(material);
            }
        }
    }
}
