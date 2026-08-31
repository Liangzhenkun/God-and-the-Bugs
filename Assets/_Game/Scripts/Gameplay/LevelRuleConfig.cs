using UnityEngine;

namespace GameJamRAC.Gameplay
{
    [CreateAssetMenu(menuName = "GameJamRAC/LevelRuleConfig")]
    public class LevelRuleConfig : ScriptableObject
    {
        public enum ConsumeRule
        {
            Bridge,
            InteractionTile,
            NeighborCheck
        }

        [Header("Consumption rule")]
        public ConsumeRule consumeRule;

        [Header("Common gameplay")]
        public bool unlockSoulSwapOnStart;
        public bool keepSoulSwapAfterRespawn;

        [Header("Intro")]
        public bool hasStory = true;
        public bool hasCameraHint = true;
        public float cameraHintDuration = 1.5f;
    }
}
