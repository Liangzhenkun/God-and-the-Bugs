using UnityEngine;

namespace GameJamRAC.Camera
{
    /// <summary>
    /// 相机跟随：平滑追踪目标位置。
    /// 支持在 SoulSwapManager 中切换追踪目标，或回退到默认俯瞰视角。
    /// </summary>
    public class CameraFollow : MonoBehaviour
    {
        [Header("跟随目标（null 则保持当前位置）")]
        [SerializeField] private Transform target;
        [SerializeField] private CameraAnchor poseAnchor;

        [Header("跟随参数")]
        [SerializeField] private Vector3 offset = new Vector3(0, 8f, -6f);
        [SerializeField] private float smoothSpeed = 5f;
        [SerializeField] private bool lookAtTarget = true;

        [Header("俯瞰视角（默认站位）")]
        [SerializeField] private Vector3 overviewPosition = new Vector3(0, 12f, 0);
        [SerializeField] private Vector3 overviewRotation = new Vector3(90f, 0, 0);
        [SerializeField] private Transform[] overviewTargets;
        [SerializeField] private float overviewHeight = 22f;
        [SerializeField] private bool autoCenterOverview = false;

        private Vector3 anchorPositionVelocity;


        public Transform Target
        {
            get => target;
            set => target = value;
        }

        public void SetOverviewTargets(Transform[] targets)
        {
            overviewTargets = targets;
        }

        private void LateUpdate()
        {
            if (poseAnchor != null)
                FollowAnchor();
            else if (target != null)
                FollowTarget();
        }

        private void FollowAnchor()
        {
            Transform anchorTransform = poseAnchor.transform;
            Vector3 toAnchor = anchorTransform.position - transform.position;
            toAnchor.y = 0f;

            bool isInsideDeadZone = poseAnchor.UseDeadZone
                && toAnchor.sqrMagnitude <= poseAnchor.DeadZoneRadius * poseAnchor.DeadZoneRadius;

            if (!isInsideDeadZone)
            {
                float smoothTime = Mathf.Max(0.0001f, poseAnchor.PositionSmoothTime);
                transform.position = Vector3.SmoothDamp(
                    transform.position,
                    anchorTransform.position,
                    ref anchorPositionVelocity,
                    smoothTime);
            }

            float rotationT = 1f - Mathf.Exp(-poseAnchor.RotationFollowSpeed * Time.deltaTime);
            transform.rotation = Quaternion.Slerp(transform.rotation, anchorTransform.rotation, rotationT);
        }

        private void FollowTarget()
        {
            Vector3 desiredPos = target.position + offset;
            transform.position = Vector3.Lerp(transform.position, desiredPos, smoothSpeed * Time.deltaTime);

            if (lookAtTarget)
            {
                Quaternion desiredRot = Quaternion.LookRotation(target.position - transform.position, Vector3.up);
                transform.rotation = Quaternion.Slerp(transform.rotation, desiredRot, smoothSpeed * Time.deltaTime);
            }
        }

        /// <summary>切换到俯瞰视角（AutoPilot 状态）</summary>
        public void SwitchToOverview()
        {
            target = null;
            poseAnchor = null;
            anchorPositionVelocity = Vector3.zero;
            if (!autoCenterOverview) return;

            transform.position = GetOverviewPosition();
            transform.rotation = Quaternion.Euler(overviewRotation);
        }

        private Vector3 GetOverviewPosition()
        {
            if (overviewTargets == null || overviewTargets.Length == 0)
                return overviewPosition;

            Vector3 center = Vector3.zero;
            int validCount = 0;
            for (int i = 0; i < overviewTargets.Length; i++)
            {
                if (overviewTargets[i] == null) continue;
                center += overviewTargets[i].position;
                validCount++;
            }

            if (validCount == 0) return overviewPosition;
            center /= validCount;
            center.y += overviewHeight;
            return center;
        }

        /// <summary>切换到追踪目标</summary>
        public void SwitchToTarget(Transform newTarget)
        {
            poseAnchor = null;
            anchorPositionVelocity = Vector3.zero;
            target = newTarget;
            if (target == null) return;

            // 接管瞬间直接切到跟随视角，避免从俯瞰镜头缓慢滑落。
            transform.position = target.position + offset;
            if (lookAtTarget)
                transform.rotation = Quaternion.LookRotation(target.position - transform.position, Vector3.up);
        }

        /// <summary>切到角色自带的机位锚点。</summary>
        public void SwitchToAnchor(CameraAnchor newAnchor)
        {
            target = null;
            poseAnchor = newAnchor;
            anchorPositionVelocity = Vector3.zero;
            if (poseAnchor == null) return;

            if (!poseAnchor.SnapOnActivate) return;

            transform.position = poseAnchor.transform.position;
            transform.rotation = poseAnchor.transform.rotation;
        }

        private void Start()
        {
            if (target == null)
                SwitchToOverview();
        }
    }
}
