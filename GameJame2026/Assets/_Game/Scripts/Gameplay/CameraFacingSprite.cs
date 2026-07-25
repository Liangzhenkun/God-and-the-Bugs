using UnityEngine;

namespace GameJamRAC.Gameplay
{
    /// <summary>Keeps a 2D sprite facing the camera that is currently rendering the scene.</summary>
    [DisallowMultipleComponent]
    public class CameraFacingSprite : MonoBehaviour
    {
        [SerializeField] private UnityEngine.Camera targetCamera;

        private void LateUpdate()
        {
            UnityEngine.Camera camera = targetCamera != null ? targetCamera : UnityEngine.Camera.main;
            if (camera == null) return;

            Vector3 directionToCamera = camera.transform.position - transform.position;
            if (directionToCamera.sqrMagnitude < 0.0001f) return;

            transform.rotation = Quaternion.LookRotation(directionToCamera, camera.transform.up);
        }
    }
}
