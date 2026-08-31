using UnityEngine;

namespace GameJamRAC.Grid
{
    /// <summary>
    /// 将任意场景物体锁定在统一道路格的中心。
    /// 适合尚未配置移动/道路逻辑的普通角色模型或摆放对象。
    /// </summary>
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public class GridCenterPlacement : MonoBehaviour
    {
        [Header("格心基准")]
        [SerializeField] private UnityEngine.Grid referenceGrid;
        [SerializeField] private bool snapInEditMode = true;
        [SerializeField] private bool preserveHeight = true;

        private Vector3 lastPosition;
        private bool isSnapping;

        private void OnEnable()
        {
            ResolveReferenceGrid();
            SnapToCellCenter();
        }

        private void OnValidate()
        {
            if (Application.isPlaying) return;

            ResolveReferenceGrid();
            SnapToCellCenter();
        }

        private void Update()
        {
            if (Application.isPlaying || !snapInEditMode || isSnapping) return;
            if ((transform.position - lastPosition).sqrMagnitude < 0.000001f) return;

            SnapToCellCenter();
        }

        [ContextMenu("吸附到统一格心")]
        public void SnapToCellCenter()
        {
            ResolveReferenceGrid();
            if (referenceGrid == null)
            {
                lastPosition = transform.position;
                return;
            }

            isSnapping = true;
            Vector3Int cell = referenceGrid.WorldToCell(transform.position);
            cell.z = 0;

            Vector3 snappedPosition = referenceGrid.GetCellCenterWorld(cell);
            if (preserveHeight)
                snappedPosition.y = transform.position.y;

            transform.position = snappedPosition;
            lastPosition = transform.position;
            isSnapping = false;
        }

        private void ResolveReferenceGrid()
        {
            if (referenceGrid != null) return;

            GameObject primaryRoadGrid = GameObject.Find("RouteGrid_A");
            if (primaryRoadGrid != null)
                referenceGrid = primaryRoadGrid.GetComponent<UnityEngine.Grid>();

            if (referenceGrid == null)
                referenceGrid = FindFirstObjectByType<UnityEngine.Grid>();
        }
    }
}
