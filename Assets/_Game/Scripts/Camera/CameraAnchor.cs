using UnityEngine;
using Unity.Cinemachine;
using GameJamRAC.Gameplay;

namespace GameJamRAC.Camera
{
    /// <summary>角色可复用的接管机位。作为 Prefab 挂到任意角色下。</summary>
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public class CameraAnchor : MonoBehaviour
    {
        [Header("备用旧跟随（仅 Cinemachine 未启用时）")]
        [SerializeField] private bool useDeadZone = true;
        [SerializeField, Min(0f)] private float deadZoneRadius = 1.25f;

        [Header("备用旧跟随延迟")]
        [SerializeField, Min(0f)] private float positionSmoothTime = 0.18f;
        [SerializeField, Min(0f)] private float rotationFollowSpeed = 10f;
        [SerializeField] private bool snapOnActivate = true;

        public bool UseDeadZone => useDeadZone;
        public float DeadZoneRadius => deadZoneRadius;
        public float PositionSmoothTime => positionSmoothTime;
        public float RotationFollowSpeed => rotationFollowSpeed;
        public bool SnapOnActivate => snapOnActivate;
        public bool HasCinemachineCamera => GetCinemachineCamera() != null;

        [SerializeField, HideInInspector] private CinemachineCamera cinemachineCamera;

        public void ConfigureCinemachineFollow(Transform followTarget)
        {
            CinemachineCamera camera = GetCinemachineCamera();
            if (camera != null)
                camera.Follow = followTarget;
        }

        private CinemachineCamera GetCinemachineCamera()
        {
            if (cinemachineCamera == null)
                cinemachineCamera = GetComponent<CinemachineCamera>();

            return cinemachineCamera;
        }

        private void OnValidate()
        {
            GetCinemachineCamera();

            CharacterUnit owner = GetComponentInParent<CharacterUnit>();
            if (owner != null)
                ConfigureCinemachineFollow(owner.transform);
        }

        private void OnTransformParentChanged()
        {
            OnValidate();
        }

        public void SetCinemachineActive(bool isActive)
        {
            CinemachineCamera camera = GetCinemachineCamera();
            if (camera == null) return;

            camera.enabled = isActive;
            PrioritySettings priority = camera.Priority;
            priority.Enabled = true;
            priority.Value = 20;
            camera.Priority = priority;
        }

        private void OnDrawGizmosSelected()
        {
            if (!useDeadZone || deadZoneRadius <= 0f) return;

            Gizmos.color = new Color(0.25f, 0.8f, 1f, 0.8f);
            Gizmos.DrawWireSphere(transform.position, deadZoneRadius);
        }
    }
}
