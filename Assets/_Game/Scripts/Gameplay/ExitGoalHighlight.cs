using System.Collections.Generic;
using GameJamRAC.Grid;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace GameJamRAC.Gameplay
{
    [DisallowMultipleComponent]
    public class ExitGoalHighlight : MonoBehaviour
    {
        [SerializeField] private ExitGoal exitGoal;
        [SerializeField] private Color glowColor = new Color(0.75f, 0.25f, 1f, 1f);
        [SerializeField, Min(0.01f)] private float pulseSpeed = 2f;
        [SerializeField, Min(0.001f)] private float minWidth = 0.07f;
        [SerializeField, Min(0.001f)] private float maxWidth = 0.16f;
        [SerializeField] private float heightOffset = 0.06f;

        private readonly List<LineRenderer> borders = new List<LineRenderer>();
        private Material material;

        private void Start()
        {
            if (exitGoal == null) exitGoal = GetComponent<ExitGoal>();
            BuildBorders();
        }

        private void Update()
        {
            float pulse = (Mathf.Sin(Time.time * pulseSpeed * Mathf.PI * 2f) + 1f) * 0.5f;
            Color color = glowColor;
            color.a = Mathf.Lerp(0.38f, 1f, pulse);
            float width = Mathf.Lerp(minWidth, maxWidth, pulse);
            foreach (LineRenderer border in borders)
            {
                if (border == null) continue;
                border.startColor = color;
                border.endColor = color;
                border.widthMultiplier = width;
            }
        }

        private void BuildBorders()
        {
            if (exitGoal == null || exitGoal.ExitTilemap == null) return;
            Tilemap tilemap = exitGoal.ExitTilemap;
            material = new Material(Shader.Find("Sprites/Default"));
            material.hideFlags = HideFlags.DontSaveInBuild | HideFlags.DontSaveInEditor;
            material.renderQueue = 3000;

            foreach (Vector3Int cell in tilemap.cellBounds.allPositionsWithin)
            {
                RouteTile tile = tilemap.GetTile<RouteTile>(cell);
                if (tile == null || !tile.IsExit) continue;
                GameObject border = new GameObject("ExitGlow_" + cell.x + "_" + cell.y);
                border.transform.SetParent(transform, false);
                LineRenderer line = border.AddComponent<LineRenderer>();
                line.sharedMaterial = material;
                line.useWorldSpace = true;
                line.loop = true;
                line.positionCount = 4;
                line.numCornerVertices = 3;
                line.numCapVertices = 3;
                line.sortingOrder = 12;
                Vector3 center = tilemap.GetCellCenterWorld(cell) + Vector3.up * heightOffset;
                Vector3 size = tilemap.layoutGrid.cellSize * 0.78f;
                line.SetPositions(new[]
                {
                    center + new Vector3(-size.x, 0f, -size.y) * 0.5f,
                    center + new Vector3(-size.x, 0f, size.y) * 0.5f,
                    center + new Vector3(size.x, 0f, size.y) * 0.5f,
                    center + new Vector3(size.x, 0f, -size.y) * 0.5f
                });
                borders.Add(line);
            }
        }

        private void OnDestroy()
        {
            if (material == null) return;
            if (Application.isPlaying) Destroy(material); else DestroyImmediate(material);
        }
    }
}
