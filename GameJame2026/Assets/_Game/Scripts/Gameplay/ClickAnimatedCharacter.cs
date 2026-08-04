using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace GameJamRAC.Gameplay
{
    /// <summary>Plays a short action animation when this world character is clicked.</summary>
    [DisallowMultipleComponent]
    public class ClickAnimatedCharacter : MonoBehaviour
    {
        [Header("Animation")]
        [SerializeField] private Animator animator;
        [SerializeField] private string idleStateName = "IdleC";
        [SerializeField] private string actionStateName = "ActC";
        [SerializeField, Min(0.05f)] private float actionDuration = 1f;

        private Coroutine returnToIdleCoroutine;

        private void Awake()
        {
            if (animator == null)
                animator = GetComponentInChildren<Animator>(true);
        }

        private void Start()
        {
            PlayIdle();
        }

        private void Update()
        {
            if (Mouse.current == null || !Mouse.current.leftButton.wasPressedThisFrame) return;
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;

            UnityEngine.Camera camera = UnityEngine.Camera.main;
            if (camera == null) return;

            Ray ray = camera.ScreenPointToRay(Mouse.current.position.ReadValue());
            if (!Physics.Raycast(ray, out RaycastHit hit)) return;
            if (hit.transform != transform && !hit.transform.IsChildOf(transform)) return;

            PlayAction();
        }

        public void PlayAction()
        {
            if (animator == null) return;

            animator.Play(actionStateName, 0, 0f);
            if (returnToIdleCoroutine != null) StopCoroutine(returnToIdleCoroutine);
            returnToIdleCoroutine = StartCoroutine(ReturnToIdleAfterDelay());
        }

        private IEnumerator ReturnToIdleAfterDelay()
        {
            yield return new WaitForSeconds(actionDuration);
            PlayIdle();
            returnToIdleCoroutine = null;
        }

        private void PlayIdle()
        {
            if (animator != null)
                animator.Play(idleStateName, 0, 0f);
        }
    }
}
