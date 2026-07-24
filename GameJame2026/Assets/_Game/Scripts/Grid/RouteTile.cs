using UnityEngine;
using UnityEngine.Tilemaps;

namespace GameJamRAC.Grid
{
    /// <summary>可绘制的路线格。不同 Tile 可设置不同的移动生命消耗。</summary>
    [CreateAssetMenu(fileName = "RouteTile", menuName = "GameJamRAC/路线格")]
    public class RouteTile : Tile
    {
        [Min(1)] public int moveCost = 1;
    }
}
