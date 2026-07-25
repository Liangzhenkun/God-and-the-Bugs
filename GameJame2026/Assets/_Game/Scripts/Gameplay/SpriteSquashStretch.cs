using UnityEngine;

namespace GameJamRAC.Gameplay
{
    [DisallowMultipleComponent]
    public class SpriteSquashStretch : MonoBehaviour
    {
        [SerializeField, Range(0f, 0.35f)] private float scaleAmount = 0.08f;
        [SerializeField, Range(0f, 0.25f)] private float verticalBobAmount = 0.06f;
        [SerializeField, Min(0.01f)] private float frequency = 2.1f;
        [SerializeField] private bool useUniquePhase = true;
        [SerializeField] private float phaseOffset;

        private Vector3 baseScale;
        private Vector3 basePosition;

        private void Awake()
        {
            baseScale = transform.localScale;
            basePosition = transform.localPosition;
            if (useUniquePhase) phaseOffset += (GetInstanceID() & 1023) * 0.017f;
        }

        private void OnEnable()
        {
            baseScale = transform.localScale;
            basePosition = transform.localPosition;
        }

        private void Update()
        {
            float wave = Mathf.Sin((Time.time * frequency + phaseOffset) * Mathf.PI * 2f);
            float wide = 1f + wave * scaleAmount;
            float tall = 1f - wave * scaleAmount * 0.75f;
            transform.localScale = new Vector3(baseScale.x * wide, baseScale.y * tall, baseScale.z);
            transform.localPosition = basePosition + Vector3.up * (wave * verticalBobAmount);
        }
    }
}
