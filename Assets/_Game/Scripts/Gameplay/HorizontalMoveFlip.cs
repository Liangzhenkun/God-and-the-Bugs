using System.Collections;
using GameJamRAC.Grid;
using UnityEngine;

namespace GameJamRAC.Gameplay
{
    /// <summary>
    /// 根据左右移动方向自动翻转 2D 角色视觉，并用短动画隐藏瞬间镜像。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class HorizontalMoveFlip : MonoBehaviour
    {
        private enum FacingDirection
        {
            Right = 1,
            Left = -1
        }

        [SerializeField] private GridUnitMover mover;
        [SerializeField] private Transform animationRoot;
        [SerializeField] private SpriteRenderer[] spriteRenderers;
        [SerializeField] private FacingDirection initialDirection = FacingDirection.Right;
        [SerializeField, Min(0f)] private float flipDuration = 0.12f;
        [SerializeField, Range(0.05f, 1f)] private float thinnestScale = 0.18f;
        [SerializeField] private bool includeInactiveSprites = true;

        private Vector3 baseScale = Vector3.one;
        private bool isMirrored;
        private bool hasTemporaryFacing;
        private bool savedMirrored;
        private Coroutine flipRoutine;

        private void Awake()
        {
            ResolveReferences();
            if (animationRoot != null)
                baseScale = animationRoot.localScale;
        }

        private void OnEnable()
        {
            ResolveReferences();
            if (mover != null)
                mover.onMoveStarted += OnMoveStarted;
        }

        private void OnDisable()
        {
            if (mover != null)
                mover.onMoveStarted -= OnMoveStarted;
        }

        private void OnValidate()
        {
            if (!Application.isPlaying)
                ResolveReferences();
        }

        [ContextMenu("刷新视觉引用")]
        private void ResolveReferences()
        {
            if (mover == null)
                mover = GetComponent<GridUnitMover>();
            if (mover == null)
                mover = GetComponentInParent<GridUnitMover>();

            if (spriteRenderers == null || spriteRenderers.Length == 0)
                spriteRenderers = GetComponentsInChildren<SpriteRenderer>(includeInactiveSprites);

            if (animationRoot == null && spriteRenderers != null && spriteRenderers.Length > 0)
                animationRoot = spriteRenderers[0].transform;
        }

        private void OnMoveStarted(Vector3Int targetCell)
        {
            if (mover == null) return;

            int horizontalDelta = targetCell.x - mover.CurrentCell.x;
            if (horizontalDelta == 0) return;

            int moveDirection = horizontalDelta > 0 ? 1 : -1;
            bool shouldMirror = moveDirection != (int)initialDirection;
            SetMirrored(shouldMirror);
        }

        private void SetMirrored(bool mirrored)
        {
            if (isMirrored == mirrored && flipRoutine == null)
                return;

            if (flipRoutine != null)
                StopCoroutine(flipRoutine);

            flipRoutine = StartCoroutine(AnimateFlip(mirrored));
        }

        public IEnumerator FaceWorldPositionTemporarily(Vector3 targetWorldPosition)
        {
            if (!hasTemporaryFacing)
            {
                savedMirrored = isMirrored;
                hasTemporaryFacing = true;
            }

            float horizontalDelta = targetWorldPosition.x - transform.position.x;
            if (Mathf.Abs(horizontalDelta) < 0.001f)
                yield break;

            int targetDirection = horizontalDelta > 0f ? 1 : -1;
            bool shouldMirror = targetDirection != (int)initialDirection;
            yield return SetMirroredAndWait(shouldMirror);
        }

        public IEnumerator RestoreTemporaryFacing()
        {
            if (!hasTemporaryFacing)
                yield break;

            bool restoreMirrored = savedMirrored;
            hasTemporaryFacing = false;
            yield return SetMirroredAndWait(restoreMirrored);
        }

        private IEnumerator SetMirroredAndWait(bool mirrored)
        {
            SetMirrored(mirrored);
            while (flipRoutine != null)
                yield return null;
        }

        private IEnumerator AnimateFlip(bool mirrored)
        {
            if (animationRoot == null)
            {
                ApplyMirror(mirrored);
                flipRoutine = null;
                yield break;
            }

            if (flipDuration <= 0f)
            {
                ApplyMirror(mirrored);
                animationRoot.localScale = baseScale;
                flipRoutine = null;
                yield break;
            }

            float halfDuration = flipDuration * 0.5f;
            yield return ScaleWidth(baseScale, thinnestScale, halfDuration);
            ApplyMirror(mirrored);
            yield return ScaleWidth(baseScale, 1f, halfDuration);
            animationRoot.localScale = baseScale;
            flipRoutine = null;
        }

        private IEnumerator ScaleWidth(Vector3 scale, float widthMultiplier, float duration)
        {
            Vector3 start = animationRoot.localScale;
            Vector3 end = new Vector3(scale.x * widthMultiplier, scale.y, scale.z);
            if (duration <= 0f)
            {
                animationRoot.localScale = end;
                yield break;
            }

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                t = 1f - (1f - t) * (1f - t);
                animationRoot.localScale = Vector3.Lerp(start, end, t);
                yield return null;
            }

            animationRoot.localScale = end;
        }

        private void ApplyMirror(bool mirrored)
        {
            isMirrored = mirrored;
            if (spriteRenderers == null) return;

            foreach (SpriteRenderer spriteRenderer in spriteRenderers)
            {
                if (spriteRenderer != null)
                    spriteRenderer.flipX = mirrored;
            }
        }
    }
}
