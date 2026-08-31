namespace GameJamRAC.Gameplay
{
    /// <summary>
    /// Shared contract for rules that resolve A consuming B.
    /// TurnActionManager and CharacterRespawnFlow use this to wait and reset consistently.
    /// </summary>
    public interface IConsumptionRule
    {
        bool IsResolving { get; }
        bool WasBConsumed { get; }
        void ResetSequence(bool revealB);
        void RehideBAfterRespawn();
        void ClearConsumedFlag();
    }

    public interface ICharacterAvailabilityRule
    {
        bool IsCharacterUnavailable(CharacterUnit character);
        bool IsUnavailableAsPrey(CharacterUnit character);
    }
}
