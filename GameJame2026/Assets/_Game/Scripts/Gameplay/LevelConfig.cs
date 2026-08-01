using UnityEngine;

namespace GameJamRAC.Gameplay
{
    /// <summary>
    /// 每关卡一份的配置资产，声明该关卡用哪套消耗规则和开场流程。
    /// 右键 Project → Create → GameJamRAC → LevelConfig 创建。
    /// </summary>
    [CreateAssetMenu(menuName = "GameJamRAC/LevelConfig")]
    public class LevelConfig : ScriptableObject
    {
        public enum ConsumeRule
        {
            /// <summary>场景 1：A 踩桥离开后吸收 B</summary>
            Bridge,
            /// <summary>场景 2：B 进入交互格后 A 踏入交互区域吃掉</summary>
            InteractionTile,
            /// <summary>场景 3：B 视觉死亡后 A 近邻检测吃掉</summary>
            NeighborCheck
        }

        [Header("消耗规则")]
        public ConsumeRule consumeRule;

        [Header("开场流程")]
        public bool hasStory = true;
        public bool hasCameraHint = true;
        public float cameraHintDuration = 1.5f;
    }
}
