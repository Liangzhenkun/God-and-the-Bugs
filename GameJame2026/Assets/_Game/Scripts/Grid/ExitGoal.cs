using UnityEngine;
using UnityEngine.Tilemaps;

namespace GameJamRAC.Grid
{
    /// <summary>关卡出口：紫色 Tilemap 上的任意格都可作为胜利目标。</summary>
    [DisallowMultipleComponent]
    public class ExitGoal : MonoBehaviour
    {
        [SerializeField] private UnityEngine.Grid grid;
        [SerializeField] private Tilemap exitTilemap;

        public Tilemap ExitTilemap => exitTilemap;

        private void Reset()
        {
            grid = GetComponent<UnityEngine.Grid>();
            exitTilemap = GetComponentInChildren<Tilemap>();
        }

        public bool ContainsWorldPosition(Vector3 worldPosition)
        {
            if (grid == null || exitTilemap == null) return false;

            Vector3Int cell = grid.WorldToCell(worldPosition);
            cell.z = 0;
            return exitTilemap.HasTile(cell);
        }
    }
}
