using UnityEngine;

namespace GameJamRAC.Gameplay
{
    /// <summary>Keeps a 2D sprite facing the camera that is currently rendering the scene.</summary>
    [DisallowMultipleComponent]
    public class CameraFacingSprite : MonoBehaviour
    {
        [SerializeField] private UnityEngine.Camera targetCamera;
        [SerializeField] private Transform visualRoot;

        private void Awake()
        {
            ResolveVisualRoot();
        }

        private void OnValidate()
        {
            if (!Application.isPlaying)
                ResolveVisualRoot();
        }

        private void LateUpdate()
        {
            UnityEngine.Camera camera = targetCamera != null ? targetCamera : UnityEngine.Camera.main;
            if (camera == null) return;

            if (visualRoot == null)
                ResolveVisualRoot();
            if (visualRoot == null) return;

            Vector3 directionToCamera = camera.transform.position - visualRoot.position;
            if (directionToCamera.sqrMagnitude < 0.0001f) return;

            // 只能旋转美术节点，不能旋转角色根节点；根节点下有 CameraRig 时，
            // 旋转根节点会反过来旋转相机，形成“相机追角色、角色追相机”的死循环。
            visualRoot.rotation = Quaternion.LookRotation(directionToCamera, camera.transform.up);
        }

        private void ResolveVisualRoot()
        {
            if (visualRoot != null) return;

            SpriteRenderer ownSprite = GetComponent<SpriteRenderer>();
            if (ownSprite != null)
            {
                visualRoot = ownSprite.transform;
                return;
            }

            SpriteRenderer childSprite = GetComponentInChildren<SpriteRenderer>(true);
            if (childSprite != null)
                visualRoot = childSprite.transform;
        }
    }
}
