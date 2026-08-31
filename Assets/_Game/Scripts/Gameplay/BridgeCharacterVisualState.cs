using UnityEngine;

namespace GameJamRAC.Gameplay
{
    /// <summary>Drives B's bridge-story animation states.</summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Animator))]
    public class BridgeCharacterVisualState : MonoBehaviour
    {
        private static readonly int BridgeState = Animator.StringToHash("BridgeState");

        [SerializeField] private Animator animator;

        private void Awake()
        {
            if (animator == null) animator = GetComponent<Animator>();
        }

        private void Start() { SetIdle(); }
        public void SetIdle() { SetState(0); }
        public void SetActive() { SetState(1); }
        public void SetDead() { SetState(2); }

        private void SetState(int state)
        {
            if (animator != null) animator.SetInteger(BridgeState, state);
        }
    }
}
