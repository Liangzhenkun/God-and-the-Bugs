using System.Collections;
using System.Collections.Generic;
using GameJamRAC.Grid;
using UnityEngine;

namespace GameJamRAC.Gameplay
{
    /// <summary>捕食者 AI：按目标列表顺序，在自己的领地内寻路追踪猎物。</summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CharacterUnit), typeof(GridUnitMover))]
    public class PredatorAI : MonoBehaviour
    {
        [Header("捕食目标")]
        [SerializeField] private CharacterUnit[] targets;
        [SerializeField, HideInInspector] private CharacterUnit target;

        [Header("动画")]
        [SerializeField] private Animator animator;
        [SerializeField] private string idleStateName = "idlewalk D";
        [SerializeField] private string eatStateName = "eatD";
        [SerializeField] private string deadStateName = "die D";
        [SerializeField, Min(0.1f)] private float eatDuration = 1.5f;

        [Header("感知范围")]
        [SerializeField] private bool requireTargetOnOwnBoard = true;
        [SerializeField] private bool includeInteractionTilesInTerritory = true;
        [SerializeField, Min(0)] private int maxSearchSteps = 0;

        private static readonly Vector3Int[] CardinalDirections =
        {
            Vector3Int.right,
            Vector3Int.left,
            Vector3Int.up,
            Vector3Int.down
        };

        private static readonly Vector3Int[] SearchDirections =
        {
            Vector3Int.right,
            Vector3Int.left,
            Vector3Int.up,
            Vector3Int.down,
            new Vector3Int(1, 1, 0),
            new Vector3Int(1, -1, 0),
            new Vector3Int(-1, 1, 0),
            new Vector3Int(-1, -1, 0)
        };

        private CharacterUnit predator;
        private GridUnitMover mover;
        private CharacterUnit resolvingTarget;
        private bool resolving;

        private void OnValidate()
        {
            MigrateLegacyTarget();
        }

        private void Awake()
        {
            MigrateLegacyTarget();
            predator = GetComponent<CharacterUnit>();
            mover = GetComponent<GridUnitMover>();
            if (animator == null) animator = GetComponentInChildren<Animator>(true);
        }

        private void OnEnable()
        {
            if (predator == null) predator = GetComponent<CharacterUnit>();
            if (predator != null) predator.onDied += OnPredatorDied;
        }

        private void OnDisable()
        {
            if (predator != null) predator.onDied -= OnPredatorDied;
        }

        public IEnumerator TakeTurn()
        {
            TurnActionManager turns = FindFirstObjectByType<TurnActionManager>();
            if (turns != null && !turns.IsAutonomousTurnActive)
                yield break;

            if (resolving || predator == null || predator.IsDead)
                yield break;

            if (!TryGetTargetCellInTerritory(out CharacterUnit activeTarget, out Vector3Int targetCell))
            {
                PlayIdle();
                yield break;
            }

            Vector3Int predatorCell = mover.CurrentCell;
            if (!IsCardinalAdjacent(predatorCell, targetCell) && predatorCell != targetCell)
            {
                if (TryFindNextMove(predatorCell, targetCell, out MoveOption option)
                    && mover.TryMoveToCellAsAi(option.cell, option.lifeCost))
                {
                    yield return new WaitWhile(() => mover.IsMoving);
                }
            }

            if (predator.IsDead)
                yield break;

            if (!TryGetTargetCellInTerritory(out activeTarget, out targetCell))
            {
                PlayIdle();
                yield break;
            }

            predatorCell = mover.CurrentCell;
            if (targetCell == predatorCell || IsCardinalAdjacent(predatorCell, targetCell))
                StartCoroutine(EatTarget(activeTarget));
        }

        /// <summary>指定角色是否正在被本捕食者吃掉。</summary>
        public bool IsEatingTarget(CharacterUnit character) => resolving && resolvingTarget == character;

        public void ResetState()
        {
            resolving = false;
            resolvingTarget = null;
            if (mover != null) mover.enabled = true;
            PlayIdle();
        }

        private void OnPredatorDied(CharacterUnit deadPredator)
        {
            if (deadPredator != predator) return;

            resolving = false;
            resolvingTarget = null;
            if (mover != null)
            {
                mover.StopAllCoroutines();
                mover.enabled = false;
            }

            if (animator != null) animator.Play(deadStateName, 0, 0f);
        }

        private IEnumerator EatTarget(CharacterUnit activeTarget)
        {
            if (resolving || !CanEatTarget(activeTarget))
            {
                PlayIdle();
                yield break;
            }

            resolving = true;
            resolvingTarget = activeTarget;

            GridUnitMover targetMover = activeTarget != null ? activeTarget.GetComponent<GridUnitMover>() : null;
            if (targetMover != null) targetMover.enabled = false;

            HorizontalMoveFlip predatorFlip = predator != null ? predator.GetComponent<HorizontalMoveFlip>() : null;
            if (predatorFlip != null && activeTarget != null)
                yield return predatorFlip.FaceWorldPositionTemporarily(activeTarget.transform.position);

            if (animator != null) animator.Play(eatStateName, 0, 0f);
            yield return new WaitForSeconds(eatDuration);

            if (!CanEatTarget(activeTarget))
            {
                PlayIdle();
                if (predatorFlip != null)
                    yield return predatorFlip.RestoreTemporaryFacing();
                resolving = false;
                resolvingTarget = null;
                yield break;
            }

            activeTarget.GiveRemainingLifeTo(predator, false);
            activeTarget.SetPresentationVisible(false);
            PlayIdle();
            if (predatorFlip != null)
                yield return predatorFlip.RestoreTemporaryFacing();
            resolving = false;
            resolvingTarget = null;
        }

        private void PlayIdle()
        {
            if (animator != null) animator.Play(idleStateName, 0, 0f);
        }

        private void MigrateLegacyTarget()
        {
            if (target == null || (targets != null && targets.Length > 0)) return;

            targets = new[] { target };
            target = null;
        }

        private bool CanEatTarget(CharacterUnit activeTarget)
        {
            return TryGetTargetCellInTerritory(activeTarget, out _);
        }

        private bool TryGetTargetCellInTerritory(out CharacterUnit activeTarget, out Vector3Int targetCell)
        {
            activeTarget = null;
            targetCell = default;

            if (targets != null)
            {
                foreach (CharacterUnit candidate in targets)
                {
                    if (!TryGetTargetCellInTerritory(candidate, out targetCell)) continue;
                    activeTarget = candidate;
                    return true;
                }
            }

            if (TryGetTargetCellInTerritory(target, out targetCell))
            {
                activeTarget = target;
                return true;
            }

            return false;
        }

        private bool TryGetTargetCellInTerritory(CharacterUnit activeTarget, out Vector3Int targetCell)
        {
            targetCell = default;
            if (activeTarget == null || activeTarget.IsVisuallyDead)
                return false;

            SceneThreeBConsumeSequence sceneThreeB = FindFirstObjectByType<SceneThreeBConsumeSequence>();
            if (sceneThreeB != null && sceneThreeB.IsUnavailableAsPrey(activeTarget))
                return false;

            if (mover == null || mover.Board == null)
                return false;

            targetCell = mover.Board.WorldToCell(activeTarget.transform.position);
            if (!requireTargetOnOwnBoard)
                return true;

            if (mover.Board.CanEnter(targetCell))
                return true;

            return includeInteractionTilesInTerritory && mover.Board.HasInteraction(targetCell);
        }

        private bool TryFindNextMove(Vector3Int start, Vector3Int targetCell, out MoveOption option)
        {
            option = default;
            GridBoard board = mover != null ? mover.Board : null;
            if (board == null || !board.CanEnter(start)) return false;

            HashSet<Vector3Int> goals = BuildGoalCells(targetCell);
            if (goals.Count == 0) return false;

            Queue<Vector3Int> frontier = new Queue<Vector3Int>();
            Dictionary<Vector3Int, Vector3Int> cameFrom = new Dictionary<Vector3Int, Vector3Int>();
            HashSet<Vector3Int> visited = new HashSet<Vector3Int>();

            frontier.Enqueue(start);
            visited.Add(start);
            Dictionary<Vector3Int, int> distanceFromStart = new Dictionary<Vector3Int, int>
            {
                [start] = 0
            };

            while (frontier.Count > 0)
            {
                Vector3Int current = frontier.Dequeue();
                if (current != start && goals.Contains(current))
                    return TryBuildFirstStep(start, current, cameFrom, out option);

                int currentDistance = distanceFromStart[current];
                if (maxSearchSteps > 0 && currentDistance >= maxSearchSteps)
                    continue;

                foreach (Vector3Int direction in GetOrderedSearchDirections(current, targetCell))
                {
                    Vector3Int next = current + direction;
                    next.z = 0;
                    if (visited.Contains(next) || !IsSearchableCell(next)) continue;

                    visited.Add(next);
                    cameFrom[next] = current;
                    distanceFromStart[next] = currentDistance + 1;
                    frontier.Enqueue(next);
                }
            }

            return false;
        }

        private HashSet<Vector3Int> BuildGoalCells(Vector3Int targetCell)
        {
            HashSet<Vector3Int> goals = new HashSet<Vector3Int>();
            GridBoard board = mover.Board;

            if (board.CanEnter(targetCell))
                goals.Add(targetCell);

            foreach (Vector3Int direction in CardinalDirections)
            {
                Vector3Int adjacent = targetCell + direction;
                adjacent.z = 0;
                if (board.CanEnter(adjacent))
                    goals.Add(adjacent);
            }

            return goals;
        }

        private bool TryBuildFirstStep(Vector3Int start, Vector3Int goal, Dictionary<Vector3Int, Vector3Int> cameFrom, out MoveOption option)
        {
            Vector3Int step = goal;
            while (cameFrom.TryGetValue(step, out Vector3Int previous) && previous != start)
                step = previous;

            Vector3Int delta = step - start;
            int lifeCost = Mathf.Abs(delta.x) + Mathf.Abs(delta.y) == 2 ? 2 : 1;
            option = new MoveOption(step, lifeCost);
            return step != start;
        }

        private IEnumerable<Vector3Int> GetOrderedSearchDirections(Vector3Int from, Vector3Int targetCell)
        {
            bool[] used = new bool[SearchDirections.Length];
            for (int i = 0; i < SearchDirections.Length; i++)
            {
                int bestIndex = -1;
                int bestDistance = int.MaxValue;

                for (int j = 0; j < SearchDirections.Length; j++)
                {
                    if (used[j]) continue;

                    Vector3Int next = from + SearchDirections[j];
                    int distance = Mathf.Abs(targetCell.x - next.x) + Mathf.Abs(targetCell.y - next.y);
                    if (distance >= bestDistance) continue;

                    bestDistance = distance;
                    bestIndex = j;
                }

                if (bestIndex < 0) yield break;

                used[bestIndex] = true;
                yield return SearchDirections[bestIndex];
            }
        }

        private bool IsSearchableCell(Vector3Int cell)
        {
            GridBoard board = mover.Board;
            if (!board.CanEnter(cell)) return false;

            if (board.WalkableTilemap != null && board.WalkableTilemap.cellBounds.Contains(cell))
                return true;

            return board.IsTemporaryWalkableCell(cell);
        }

        private static bool IsCardinalAdjacent(Vector3Int from, Vector3Int to)
        {
            return Mathf.Abs(from.x - to.x) + Mathf.Abs(from.y - to.y) == 1;
        }

        private readonly struct MoveOption
        {
            public readonly Vector3Int cell;
            public readonly int lifeCost;

            public MoveOption(Vector3Int cell, int lifeCost)
            {
                this.cell = cell;
                this.lifeCost = lifeCost;
            }
        }
    }
}
