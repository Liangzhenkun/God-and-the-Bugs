using UnityEngine;

namespace GameJamRAC.Gameplay
{
    /// <summary>让 2D 角色视觉节点持续面向当前相机。</summary>
    [DisallowMultipleComponent]
    public class CameraFacingSprite : MonoBehaviour
    {
        [SerializeField] private UnityEngine.Camera targetCamera;
        [SerializeField] private Transform visualRoot;
        [SerializeField] private bool preserveInitialRotationOffset;

        private Quaternion initialRotationOffset = Quaternion.identity;
        private bool hasInitialRotationOffset;

        private void Awake()
        {
            ResolveVisualRoot();
            CaptureInitialRotationOffset();
        }

        private void OnEnable()
        {
            hasInitialRotationOffset = false;
            CaptureInitialRotationOffset();
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

            Quaternion facingRotation = Quaternion.LookRotation(directionToCamera, camera.transform.up);
            if (preserveInitialRotationOffset)
            {
                if (!hasInitialRotationOffset)
                    CaptureInitialRotationOffset(camera);

                visualRoot.rotation = facingRotation * initialRotationOffset;
                return;
            }

            // 只旋转美术节点，避免角色根节点带着相机或移动逻辑一起转。
            visualRoot.rotation = facingRotation;
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

        private void CaptureInitialRotationOffset()
        {
            UnityEngine.Camera camera = targetCamera != null ? targetCamera : UnityEngine.Camera.main;
            if (camera != null)
                CaptureInitialRotationOffset(camera);
        }

        private void CaptureInitialRotationOffset(UnityEngine.Camera camera)
        {
            if (!preserveInitialRotationOffset || camera == null) return;

            if (visualRoot == null)
                ResolveVisualRoot();
            if (visualRoot == null) return;

            Vector3 directionToCamera = camera.transform.position - visualRoot.position;
            if (directionToCamera.sqrMagnitude < 0.0001f) return;

            Quaternion facingRotation = Quaternion.LookRotation(directionToCamera, camera.transform.up);
            initialRotationOffset = Quaternion.Inverse(facingRotation) * visualRoot.rotation;
            hasInitialRotationOffset = true;
        }
    }
}
