using System.Collections.Generic;
using UnityEngine;

namespace GameJamRAC.Grid
{
    /// <summary>角色单步可选目标。坐标以角色脚下格为原点，前方为正 Y。</summary>
    [DisallowMultipleComponent]
    public class MovementRange : MonoBehaviour
    {
        public enum Preset
        {
            Custom,
            EightNeighbors,
            CardinalTwoCells,
            SideOneForwardTwo,
            ForwardTriangle
        }

        [SerializeField] private Preset preset = Preset.EightNeighbors;
        [SerializeField] private List<Vector2Int> offsets = new List<Vector2Int>();

        public IReadOnlyList<Vector2Int> Offsets => offsets;

        private void Reset()
        {
            ApplyPreset();
        }

        private void OnValidate()
        {
            if (preset != Preset.Custom)
                ApplyPreset();
        }

        public bool Contains(Vector2Int offset)
        {
            return offsets.Contains(offset);
        }

        [ContextMenu("应用当前预设")]
        public void ApplyPreset()
        {
            if (preset == Preset.Custom) return;

            offsets.Clear();
            switch (preset)
            {
                case Preset.EightNeighbors:
                    Add(-1, -1); Add(0, -1); Add(1, -1);
                    Add(-1, 0);               Add(1, 0);
                    Add(-1, 1);  Add(0, 1);  Add(1, 1);
                    break;
                case Preset.CardinalTwoCells:
                    Add(0, 2); Add(0, -2); Add(2, 0); Add(-2, 0);
                    break;
                case Preset.SideOneForwardTwo:
                    Add(-1, 0); Add(1, 0); Add(0, 1); Add(0, 2);
                    break;
                case Preset.ForwardTriangle:
                    Add(-1, 1); Add(0, 1); Add(1, 1);
                    Add(-2, 2); Add(-1, 2); Add(0, 2); Add(1, 2); Add(2, 2);
                    break;
            }
        }

        private void Add(int x, int y)
        {
            offsets.Add(new Vector2Int(x, y));
        }
    }
}
