using UnityEngine;

namespace GameJamRAC.Grid
{
    /// <summary>
    /// 统一 FixedRoads 下所有道路与出口 Grid 的坐标基准。
    /// X/Z 只能落在同一套 4×4 格点上；道路与出口仅通过 Y 轴分层。
    /// </summary>
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public class FixedRoadGridAlignment : MonoBehaviour
    {
        [Header("世界网格")]
        [SerializeField, Min(0.01f)] private float cellSize = 4f;
        [SerializeField] private Vector2 cellCenterOffset = Vector2.zero;
        [SerializeField] private bool autoSnapInEditMode = true;

        [Header("图层高度（相对 FixedRoads）")]
        [SerializeField] private float roadGridLocalHeight = 0.68f;
        [SerializeField] private float exitGridLocalHeight = 0.76f;
        [SerializeField, Min(0f)] private float interactionLayerHeightOffset = 0.025f;

        private bool isAligning;
        private bool needsAlignment = true;

        private void OnEnable()
        {
            needsAlignment = true;
        }

        private void OnValidate()
        {
            // OnValidate 期间修改 Tilemap 会触发 Unity 的内部 SendMessage 警告，
            // 因此延迟到下一次编辑器 Update 再进行实际对齐。
            needsAlignment = true;
        }

        private void Update()
        {
            if (Application.isPlaying || !autoSnapInEditMode || isAligning) return;

            if (needsAlignment)
            {
                SnapAllLayers();
                return;
            }

            foreach (UnityEngine.Grid grid in GetComponentsInChildren<UnityEngine.Grid>(true))
            {
                if (!grid.transform.hasChanged) continue;

                SnapAllLayers();
                return;
            }
        }

        [ContextMenu("对齐所有道路与出口图层")]
        public void SnapAllLayers()
        {
            if (isAligning) return;

            isAligning = true;
            foreach (UnityEngine.Grid grid in GetComponentsInChildren<UnityEngine.Grid>(true))
            {
                bool isExitLayer = grid.GetComponent<ExitGoal>() != null;
                AlignGrid(grid, isExitLayer ? exitGridLocalHeight : roadGridLocalHeight);
            }
            isAligning = false;
            needsAlignment = false;
        }

        private void AlignGrid(UnityEngine.Grid grid, float localHeight)
        {
            Transform gridTransform = grid.transform;
            Vector3 localPosition = gridTransform.localPosition;
            localPosition.x = SnapGridOrigin(localPosition.x, cellCenterOffset.x);
            localPosition.y = localHeight;
            localPosition.z = SnapGridOrigin(localPosition.z, cellCenterOffset.y);

            SetIfDifferent(gridTransform.localPosition, localPosition, value => gridTransform.localPosition = value);
            Quaternion localRotation = Quaternion.Euler(90f, 0f, 0f);
            if (Quaternion.Angle(gridTransform.localRotation, localRotation) > 0.001f)
                gridTransform.localRotation = localRotation;
            SetIfDifferent(gridTransform.localScale, Vector3.one, value => gridTransform.localScale = value);
            SetIfDifferent(grid.cellSize, new Vector3(cellSize, cellSize, 1f), value => grid.cellSize = value);
            SetIfDifferent(grid.cellGap, Vector3.zero, value => grid.cellGap = value);

            foreach (var tilemap in grid.GetComponentsInChildren<UnityEngine.Tilemaps.Tilemap>(true))
            {
                Transform tilemapTransform = tilemap.transform;
                Vector3 tilemapLocalPosition = tilemap.name.StartsWith("InteractionTilemap")
                    ? new Vector3(0f, 0f, -interactionLayerHeightOffset)
                    : Vector3.zero;
                SetIfDifferent(tilemapTransform.localPosition, tilemapLocalPosition, value => tilemapTransform.localPosition = value);
                if (Quaternion.Angle(tilemapTransform.localRotation, Quaternion.identity) > 0.001f)
                    tilemapTransform.localRotation = Quaternion.identity;
                SetIfDifferent(tilemapTransform.localScale, Vector3.one, value => tilemapTransform.localScale = value);
            }

            gridTransform.hasChanged = false;
        }

        private float SnapGridOrigin(float position, float centerOffset)
        {
            // Grid 的第 0 格中心相对根节点偏移半个单元；令格心始终对齐同一坐标系。
            float originOffset = centerOffset - cellSize * 0.5f;
            return originOffset + Mathf.Round((position - originOffset) / cellSize) * cellSize;
        }

        private static void SetIfDifferent(Vector3 current, Vector3 desired, System.Action<Vector3> setValue)
        {
            if ((current - desired).sqrMagnitude > 0.000001f)
                setValue(desired);
        }
    }
}
