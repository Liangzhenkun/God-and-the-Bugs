using UnityEngine;

namespace GameJamRAC.Gameplay
{
    /// <summary>根据角色生命值驱动图片角色的 Animator 状态。</summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Animator))]
    public class CharacterSpriteState : MonoBehaviour
    {
        [Header("角色引用")]
        [SerializeField] private CharacterUnit character;
        [SerializeField] private Animator animator;

        [Header("低血量")]
        [SerializeField, Min(1)] private int lowHealthMaximum = 3;

        private static readonly int LifeState = Animator.StringToHash("LifeState");

        private void Awake()
        {
            if (character == null) character = GetComponentInParent<CharacterUnit>();
            if (animator == null) animator = GetComponent<Animator>();
        }

        private void OnEnable()
        {
            if (character != null) character.onScoreChanged.AddListener(ApplyLifeState);
        }

        private void Start()
        {
            if (character != null) ApplyLifeState(character.CurrentLife);
        }

        private void OnDisable()
        {
            if (character != null) character.onScoreChanged.RemoveListener(ApplyLifeState);
        }

        private void ApplyLifeState(int life)
        {
            if (animator == null) return;

            // 0: 正常（4+）；1: 低血（1-3）；2: 死亡（0）。
            int state = life <= 0 ? 2 : life <= lowHealthMaximum ? 1 : 0;
            int currentState = life <= 0 ? 2 : life <= lowHealthMaximum ? 1 : 0;
            animator.SetInteger(LifeState, currentState);
        }
    }
}
