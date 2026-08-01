using System.Collections;
using System.Collections.Generic;
using GameJamRAC.Grid;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GameJamRAC.Gameplay
{
    /// <summary>One-step predator AI. Assign a prey target in the Inspector.</summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CharacterUnit), typeof(GridUnitMover))]
    public class PredatorAI : MonoBehaviour
    {
        [Header("捕食目标")]
        [SerializeField] private CharacterUnit target;

        [Header("动画")]
        [SerializeField] private Animator animator;
        [SerializeField] private string idleStateName = "idlewalk D";
        [SerializeField] private string eatStateName = "eatD";
        [SerializeField] private string deadStateName = "die D";
        [SerializeField, Min(0.1f)] private float eatDuration = 1.5f;

        private CharacterUnit predator;
        private GridUnitMover mover;
        private bool resolving;

        private void Awake()
        {
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

            if (!CanEatTarget())
            {
                PlayIdle();
                yield break;
            }

            Vector3Int predatorCell = mover.CurrentCell;
            Vector3Int targetCell = mover.Board.WorldToCell(target.transform.position);

            if (!IsCardinalAdjacent(predatorCell, targetCell) && predatorCell != targetCell)
            {
                // 先按直追规则行动；若道路被障碍截断，则从相邻的水平/垂直格绕开。
                // 每个候选都交给 GridUnitMover 复核，因此不会走出自己的道路层。
                foreach (MoveOption option in GetMoveOptions(predatorCell, targetCell))
                {
                    if (!mover.TryMoveToCellAsAi(option.cell, option.lifeCost)) continue;

                    yield return new WaitWhile(() => mover.IsMoving);
                    break;
                }
            }

            if (predator.IsDead)
                yield break;

            if (!CanEatTarget())
            {
                PlayIdle();
                yield break;
            }

            predatorCell = mover.CurrentCell;
            targetCell = mover.Board.WorldToCell(target.transform.position);
            if (targetCell == predatorCell || IsCardinalAdjacent(predatorCell, targetCell))
            {
                // 吃动画只作为表现，不阻塞玩家操作
                StartCoroutine(EatTarget());
            }
        }

        /// <summary>指定角色是否正被本捕食者吃（动画播放中）。</summary>
        public bool IsEatingTarget(CharacterUnit character) => resolving && target == character;

        public void ResetState()
        {
            resolving = false;
            if (mover != null) mover.enabled = true;
            PlayIdle();
        }

        private void OnPredatorDied(CharacterUnit deadPredator)
        {
            if (deadPredator != predator) return;

            // D 的生命归零后只保留死亡画面；停止该回合的追击和后续自动移动。
            resolving = false;
            if (mover != null)
            {
                mover.StopAllCoroutines();
                mover.enabled = false;
            }
            if (animator != null) animator.Play(deadStateName, 0, 0f);
        }

        private IEnumerator EatTarget()
        {
            if (resolving || !CanEatTarget())
            {
                PlayIdle();
                yield break;
            }

            resolving = true;

            // 吃动画期间禁止目标移动
            GridUnitMover targetMover = target != null ? target.GetComponent<GridUnitMover>() : null;
            if (targetMover != null) targetMover.enabled = false;

            HorizontalMoveFlip predatorFlip = predator != null ? predator.GetComponent<HorizontalMoveFlip>() : null;
            if (predatorFlip != null && target != null)
                yield return predatorFlip.FaceWorldPositionTemporarily(target.transform.position);

            if (animator != null) animator.Play(eatStateName, 0, 0f);
            yield return new WaitForSeconds(eatDuration);

            // 多个 D 同步回合时，动画期间目标可能已被另一只 D 处理；结算前再次确认。
            if (!CanEatTarget())
            {
                PlayIdle();
                if (predatorFlip != null)
                    yield return predatorFlip.RestoreTemporaryFacing();
                resolving = false;
                yield break;
            }

            // 捕食主控角色时，不能抑制它的全局复活流程。
            target.GiveRemainingLifeTo(predator, false);
            target.SetPresentationVisible(false);
            PlayIdle();
            if (predatorFlip != null)
                yield return predatorFlip.RestoreTemporaryFacing();
            resolving = false;
        }

        private void PlayIdle()
        {
            if (animator != null) animator.Play(idleStateName, 0, 0f);
        }

        private bool CanEatTarget()
        {
            // 捕食判定只看视觉死亡；B 进入激活格后即使生命尚在，也不可再被捕食。
            if (target == null || target.IsVisuallyDead)
                return false;

            SceneThreeBConsumeSequence sceneThreeB = FindFirstObjectByType<SceneThreeBConsumeSequence>();
            return sceneThreeB == null || !sceneThreeB.IsUnavailableAsPrey(target);
        }

        private static bool IsCardinalAdjacent(Vector3Int from, Vector3Int to)
        {
            return Mathf.Abs(from.x - to.x) + Mathf.Abs(from.y - to.y) == 1;
        }

        private static IEnumerable<MoveOption> GetMoveOptions(Vector3Int from, Vector3Int to)
        {
            int dx = to.x - from.x;
            int dy = to.y - from.y;
            int absX = Mathf.Abs(dx);
            int absY = Mathf.Abs(dy);
            int stepX = dx == 0 ? 0 : (dx > 0 ? 1 : -1);
            int stepY = dy == 0 ? 0 : (dy > 0 ? 1 : -1);

            // 原始追击优先级：非相邻的斜向目标优先走斜向，直线目标优先直走。
            if (dx != 0 && dy != 0 && !(absX == 1 && absY == 1))
            {
                yield return new MoveOption(from + new Vector3Int(stepX, stepY, 0), 2);

                // 斜角格被堵时，改从两个相邻的正交格择一绕行。
                yield return new MoveOption(from + new Vector3Int(stepX, 0, 0), 1);
                yield return new MoveOption(from + new Vector3Int(0, stepY, 0), 1);
                yield break;
            }

            if (absX >= absY && dx != 0)
            {
                yield return new MoveOption(from + new Vector3Int(stepX, 0, 0), 1);
                // 直线被堵时，先向两侧横移一格，再由下一回合重新朝目标前进。
                yield return new MoveOption(from + new Vector3Int(0, 1, 0), 1);
                yield return new MoveOption(from + new Vector3Int(0, -1, 0), 1);
                yield break;
            }

            if (dy != 0)
            {
                yield return new MoveOption(from + new Vector3Int(0, stepY, 0), 1);
                yield return new MoveOption(from + new Vector3Int(1, 0, 0), 1);
                yield return new MoveOption(from + new Vector3Int(-1, 0, 0), 1);
            }
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
