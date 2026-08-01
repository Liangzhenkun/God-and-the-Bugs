namespace GameJamRAC.Gameplay
{
    /// <summary>
    /// A 消耗 B 的序列统一接口。三个关卡各有一种实现，
    /// TurnActionManager 和 CharacterRespawnFlow 通过此接口统一处理。
    /// </summary>
    public interface IConsumeSequence
    {
        bool IsResolving { get; }
        bool WasBConsumed { get; }
        void ResetSequence(bool revealB);
        void RehideBAfterRespawn();
        void ClearConsumedFlag();
    }
}
