using System.Collections;
using GameJamRAC.Grid;
using UnityEngine;

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
                Vector3Int nextCell = GetNextCell(predatorCell, targetCell, out int lifeCost);
                if (mover.TryMoveToCellAsAi(nextCell, lifeCost))
                    yield return new WaitWhile(() => mover.IsMoving);
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
                yield return EatTarget();
        }

        public void ResetState()
        {
            resolving = false;
            PlayIdle();
        }

        private IEnumerator EatTarget()
        {
            if (resolving || !CanEatTarget())
            {
                PlayIdle();
                yield break;
            }

            resolving = true;
            if (animator != null) animator.Play(eatStateName, 0, 0f);
            yield return new WaitForSeconds(eatDuration);

            // 多个 D 同步回合时，动画期间目标可能已被另一只 D 处理；结算前再次确认。
            if (!CanEatTarget())
            {
                PlayIdle();
                resolving = false;
                yield break;
            }

            // 捕食主控角色时，不能抑制它的全局复活流程。
            target.GiveRemainingLifeTo(predator, false);
            target.SetPresentationVisible(false);
            PlayIdle();
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

        private static Vector3Int GetNextCell(Vector3Int from, Vector3Int to, out int lifeCost)
        {
            int dx = to.x - from.x;
            int dy = to.y - from.y;
            int absX = Mathf.Abs(dx);
            int absY = Mathf.Abs(dy);

            // Diagonal movement is only used while the prey is not a neighbouring diagonal cell.
            if (dx != 0 && dy != 0 && !(absX == 1 && absY == 1))
            {
                lifeCost = 2;
                return from + new Vector3Int(dx > 0 ? 1 : -1, dy > 0 ? 1 : -1, 0);
            }

            lifeCost = 1;
            if (absX >= absY && dx != 0)
                return from + new Vector3Int(dx > 0 ? 1 : -1, 0, 0);
            return from + new Vector3Int(0, dy > 0 ? 1 : -1, 0);
        }
    }
}
