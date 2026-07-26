using System.Collections;
using System.Collections.Generic;
using System;
using GameJamRAC.Grid;
using UnityEngine;

namespace GameJamRAC.Gameplay
{
    /// <summary>Runs one autonomous turn after each completed player movement.</summary>
    [DisallowMultipleComponent]
    public class TurnActionManager : MonoBehaviour
    {
        [SerializeField, Min(0f)] private float playerToAiDelay = 0.08f;
        private readonly Dictionary<GridUnitMover, Action<Vector3Int>> subscribedMovers = new Dictionary<GridUnitMover, Action<Vector3Int>>();
        private Coroutine turnCoroutine;
        private bool autonomousTurnActive;

        public bool IsAutonomousTurnActive => autonomousTurnActive;

        private void OnEnable()
        {
            if (!Application.isPlaying) return;
            foreach (CharacterUnit unit in FindObjectsByType<CharacterUnit>(FindObjectsSortMode.None))
            {
                GridUnitMover mover = unit.GetComponent<GridUnitMover>();
                if (mover == null) continue;
                Action<Vector3Int> handler = cell => OnUnitCellReached(unit);
                mover.onCellReached += handler;
                subscribedMovers[mover] = handler;
            }
        }

        private void OnDisable()
        {
            if (!Application.isPlaying) return;
            foreach (KeyValuePair<GridUnitMover, Action<Vector3Int>> subscription in subscribedMovers)
                if (subscription.Key != null)
                    subscription.Key.onCellReached -= subscription.Value;
            subscribedMovers.Clear();
            if (turnCoroutine != null) StopCoroutine(turnCoroutine);
            turnCoroutine = null;
            autonomousTurnActive = false;
        }

        private void OnUnitCellReached(CharacterUnit unit)
        {
            if (unit == null || !unit.IsPlayerControlled || unit.IsDead || turnCoroutine != null) return;
            turnCoroutine = StartCoroutine(RunAutonomousTurns(unit));
        }

        private IEnumerator RunAutonomousTurns(CharacterUnit player)
        {
            SceneThreeBConsumeSequence consume = FindFirstObjectByType<SceneThreeBConsumeSequence>();
            if (consume != null)
                yield return new WaitWhile(() => consume.IsResolving);

            GridUnitMover playerMover = player.GetComponent<GridUnitMover>();
            if (playerMover != null) playerMover.enabled = false;

            // 只保留玩家和 AI 回合交接时的短暂停顿；所有 AI 同时起步。
            if (playerToAiDelay > 0f)
                yield return new WaitForSeconds(playerToAiDelay);

            int activePredatorTurns = 0;
            autonomousTurnActive = true;
            foreach (PredatorAI predator in FindObjectsByType<PredatorAI>(FindObjectsSortMode.None))
            {
                if (predator != null && predator.isActiveAndEnabled)
                {
                    activePredatorTurns++;
                    StartCoroutine(RunPredatorTurn(predator, () => activePredatorTurns--));
                }
            }

            yield return new WaitWhile(() => activePredatorTurns > 0);
            autonomousTurnActive = false;

            if (playerMover != null && !player.IsDead)
                playerMover.enabled = true;
            turnCoroutine = null;
        }

        private IEnumerator RunPredatorTurn(PredatorAI predator, Action onFinished)
        {
            yield return predator.TakeTurn();
            onFinished?.Invoke();
        }
    }
}
