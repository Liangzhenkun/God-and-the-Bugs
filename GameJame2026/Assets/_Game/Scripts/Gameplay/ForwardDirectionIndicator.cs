using GameJamRAC.Grid;
using UnityEngine;

namespace GameJamRAC.Gameplay
{
    /// <summary>在角色脚下显示其默认前方（本地 +Z / 能力网格 +Y）。</summary>
    [DisallowMultipleComponent]
    public class ForwardDirectionIndicator : MonoBehaviour
    {
        [SerializeField] private Color color = new Color(1f, 0.85f, 0.1f, 0.95f);
        [SerializeField, Min(0f)] private float heightAboveRoad = 0.08f;
        [SerializeField, Min(0f)] private float forwardOffset = 0.9f;
        [SerializeField, Min(0.1f)] private float length = 1.35f;
        [SerializeField, Min(0.1f)] private float width = 0.8f;

        private GridUnitMover mover;
        private Mesh arrowMesh;
        private Material arrowMaterial;

        private void Awake()
        {
            EnsureVisual();
            AlignToFeet();
        }

        private void EnsureVisual()
        {
            MeshFilter meshFilter = GetComponent<MeshFilter>();
            if (meshFilter == null) meshFilter = gameObject.AddComponent<MeshFilter>();

            MeshRenderer meshRenderer = GetComponent<MeshRenderer>();
            if (meshRenderer == null) meshRenderer = gameObject.AddComponent<MeshRenderer>();

            if (arrowMesh == null)
            {
                arrowMesh = new Mesh { name = "ForwardDirectionArrow" };
                meshFilter.sharedMesh = arrowMesh;
            }

            if (arrowMaterial == null)
            {
                arrowMaterial = new Material(Shader.Find("Sprites/Default"));
                arrowMaterial.color = color;
                meshRenderer.sharedMaterial = arrowMaterial;
                meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                meshRenderer.receiveShadows = false;
            }

            BuildMesh();
        }

        private void BuildMesh()
        {
            if (arrowMesh == null) return;

            float halfWidth = width * 0.5f;
            float halfLength = length * 0.5f;
            float shoulder = length * 0.05f;
            Vector3[] vertices =
            {
                new Vector3(-halfWidth * 0.45f, 0f, -halfLength),
                new Vector3(halfWidth * 0.45f, 0f, -halfLength),
                new Vector3(halfWidth * 0.45f, 0f, shoulder),
                new Vector3(halfWidth, 0f, shoulder),
                new Vector3(0f, 0f, halfLength),
                new Vector3(-halfWidth, 0f, shoulder),
                new Vector3(-halfWidth * 0.45f, 0f, shoulder)
            };

            int[] triangles = { 0, 2, 1, 0, 3, 2, 0, 4, 3, 0, 5, 4, 0, 6, 5 };
            arrowMesh.Clear();
            arrowMesh.vertices = vertices;
            arrowMesh.triangles = triangles;
            arrowMesh.RecalculateNormals();
            arrowMesh.RecalculateBounds();
        }

        private void AlignToFeet()
        {
            if (mover == null) mover = GetComponentInParent<GridUnitMover>();
            GridBoard board = mover != null ? mover.Board : null;
            if (board == null) return;

            transform.localPosition = new Vector3(0f, -board.UnitHeight + heightAboveRoad, forwardOffset);
            transform.localRotation = Quaternion.identity;
        }

        private void OnDrawGizmos()
        {
            if (Application.isPlaying) return;

            Vector3 basePoint = transform.position - transform.forward * (length * 0.5f);
            Vector3 tip = transform.position + transform.forward * (length * 0.5f);
            Vector3 wingBase = tip - transform.forward * (length * 0.42f);
            Gizmos.color = color;
            Gizmos.DrawLine(basePoint, tip);
            Gizmos.DrawLine(tip, wingBase + transform.right * (width * 0.5f));
            Gizmos.DrawLine(tip, wingBase - transform.right * (width * 0.5f));
        }

        private void OnDestroy()
        {
            if (arrowMesh != null)
            {
                if (Application.isPlaying) Destroy(arrowMesh);
                else DestroyImmediate(arrowMesh);
            }

            if (arrowMaterial != null)
            {
                if (Application.isPlaying) Destroy(arrowMaterial);
                else DestroyImmediate(arrowMaterial);
            }
        }
    }
}
