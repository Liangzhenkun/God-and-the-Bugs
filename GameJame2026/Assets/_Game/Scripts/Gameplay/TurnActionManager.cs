using System;
using System.Collections;
using System.Collections.Generic;
using GameJamRAC.Grid;
using UnityEngine;

namespace GameJamRAC.Gameplay
{
    /// <summary>玩家每完成一次格子移动，只推进一次捕食者回合。</summary>
    [DisallowMultipleComponent]
    public class TurnActionManager : MonoBehaviour
    {
        [SerializeField, Min(0f)] private float playerToAiDelay = 0.08f;

        private static TurnActionManager activeInstance;

        private readonly Dictionary<GridUnitMover, Action<Vector3Int>> subscribedMovers = new Dictionary<GridUnitMover, Action<Vector3Int>>();
        private readonly Dictionary<GridUnitMover, int> processedMoveCounts = new Dictionary<GridUnitMover, int>();
        private Coroutine turnCoroutine;
        private bool autonomousTurnActive;
        private SoulSwapManager soulSwapManager;

        public bool IsAutonomousTurnActive => autonomousTurnActive;

        private void OnEnable()
        {
            if (!Application.isPlaying) return;

            if (activeInstance != null && activeInstance != this)
            {
                enabled = false;
                return;
            }

            activeInstance = this;
            soulSwapManager = FindFirstObjectByType<SoulSwapManager>();
            SubscribeMovers();
        }

        private void OnDisable()
        {
            if (!Application.isPlaying) return;

            UnsubscribeMovers();
            if (activeInstance == this)
                activeInstance = null;

            if (turnCoroutine != null) StopCoroutine(turnCoroutine);
            turnCoroutine = null;
            autonomousTurnActive = false;
        }

        private void SubscribeMovers()
        {
            foreach (CharacterUnit unit in FindObjectsByType<CharacterUnit>(FindObjectsSortMode.None))
            {
                GridUnitMover mover = unit.GetComponent<GridUnitMover>();
                if (mover == null || subscribedMovers.ContainsKey(mover)) continue;

                Action<Vector3Int> handler = _ => OnUnitCellReached(unit, mover);
                mover.onCellReached += handler;
                subscribedMovers[mover] = handler;
                processedMoveCounts[mover] = mover.CompletedMoveCount;
            }
        }

        private void UnsubscribeMovers()
        {
            foreach (KeyValuePair<GridUnitMover, Action<Vector3Int>> subscription in subscribedMovers)
            {
                if (subscription.Key != null)
                    subscription.Key.onCellReached -= subscription.Value;
            }

            subscribedMovers.Clear();
            processedMoveCounts.Clear();
        }

        private void OnUnitCellReached(CharacterUnit unit, GridUnitMover mover)
        {
            if (unit == null || mover == null || !unit.IsPlayerControlled || unit.IsDead) return;
            if (soulSwapManager != null && !soulSwapManager.IsActiveControlledCharacter(unit)) return;
            if (turnCoroutine != null) return;

            int moveCount = mover.CompletedMoveCount;
            if (processedMoveCounts.TryGetValue(mover, out int processedCount) && processedCount == moveCount) return;

            processedMoveCounts[mover] = moveCount;
            turnCoroutine = StartCoroutine(RunAutonomousTurns(unit, mover));
        }

        private IEnumerator RunAutonomousTurns(CharacterUnit player, GridUnitMover playerMover)
        {
            yield return null;

            if (playerMover != null)
                playerMover.enabled = false;

            if (playerToAiDelay > 0f)
                yield return new WaitForSeconds(playerToAiDelay);

            int activePredatorTurns = 0;
            autonomousTurnActive = true;
            foreach (PredatorAI predator in FindObjectsByType<PredatorAI>(FindObjectsSortMode.None))
            {
                if (predator == null || !predator.isActiveAndEnabled) continue;

                activePredatorTurns++;
                StartCoroutine(RunPredatorTurn(predator, () => activePredatorTurns--));
            }

            yield return new WaitWhile(() => activePredatorTurns > 0);
            autonomousTurnActive = false;

            yield return WaitForConsumeSequencesToFinish();

            bool playerBeingEaten = false;
            foreach (PredatorAI predator in FindObjectsByType<PredatorAI>(FindObjectsSortMode.None))
            {
                if (!predator.IsEatingTarget(player)) continue;

                playerBeingEaten = true;
                break;
            }

            if (playerMover != null && !player.IsDead && !playerBeingEaten)
                playerMover.enabled = true;

            turnCoroutine = null;
        }

        private static IEnumerator WaitForConsumeSequencesToFinish()
        {
            while (true)
            {
                bool isResolving = false;
                foreach (MonoBehaviour mb in FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None))
                {
                    IConsumptionRule rule = mb as IConsumptionRule;
                    if (rule == null || !rule.IsResolving) continue;

                    isResolving = true;
                    break;
                }

                if (!isResolving)
                    yield break;

                yield return null;
            }
        }

        private IEnumerator RunPredatorTurn(PredatorAI predator, Action onFinished)
        {
            yield return predator.TakeTurn();
            onFinished?.Invoke();
        }
    }
}
