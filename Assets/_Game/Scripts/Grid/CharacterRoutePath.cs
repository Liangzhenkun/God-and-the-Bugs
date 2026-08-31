using UnityEngine;

namespace GameJamRAC.Grid
{
    /// <summary>角色专属路线。编辑时跟随角色，运行时保持在世界中供角色沿格移动。</summary>
    [DisallowMultipleComponent]
    public class CharacterRoutePath : MonoBehaviour
    {
        [SerializeField] private bool detachWhenPlayStarts = true;

        private void Start()
        {
            if (detachWhenPlayStarts)
                transform.SetParent(null, true);
        }
    }
}
