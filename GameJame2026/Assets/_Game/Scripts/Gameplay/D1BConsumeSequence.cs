using System;
using System.Collections;
using GameJamRAC.Grid;
using UnityEngine;

namespace GameJamRAC.Gameplay
{
    /// <summary>
    /// 当前关卡的 D1、B、A 三段交互：B 踏入 D1 的交互格后进入视觉死亡，
    /// A 踏入 B 的交互格后吞噬 B。
    /// </summary>
    [DisallowMultipleComponent]
    public class D1BConsumeSequence : MonoBehaviour, IConsumeSequence
    {
        [Header("角色")]
        [SerializeField] private CharacterUnit characterA;
        [SerializeField] private CharacterUnit characterB;
        [SerializeField] private CharacterUnit characterD1;

        [Header("格盘")]
        [SerializeField] private GridBoard boardB;
        [SerializeField] private GridBoard d1InteractionBoard;

        [Header("动画")]
        [SerializeField] private CharacterSpriteState aVisualState;
        [SerializeField] private BridgeCharacterVisualState bVisualState;
        [SerializeField, Min(0.1f)] private float eatDuration = 1.5f;

        private GridUnitMover moverA;
        private GridUnitMover moverB;
        private bool bAwaitingConsumption;
        private bool resolving;
        private bool bConsumed;
        private Coroutine consumeCoroutine;

        /// <summary>B 已被吃掉，重生流程中先别显示。</summary>
        public bool WasBConsumed => bConsumed;

        public bool IsResolving => resolving;
        public bool IsBUnavailable => bAwaitingConsumption || resolving
            || (characterB != null && (characterB.IsDead || characterB.IsVisuallyDead));

        private void Awake()
        {
            ResolveReferences();
            SetAllCInitialLifeToTen();

            if (FindFirstObjectByType<TurnActionManager>() == null)
            {
                GameObject manager = new GameObject("TurnActionManager");
                manager.AddComponent<TurnActionManager>();
            }
        }

        private void OnEnable()
        {
            ResolveReferences();
            if (moverB != null) moverB.onCellReached += OnBCellReached;
            if (moverA != null) moverA.onCellReached += OnACellReached;
            if (characterB != null) characterB.onDied += OnBDeath;
        }

        private void OnDisable()
        {
            if (moverB != null) moverB.onCellReached -= OnBCellReached;
            if (moverA != null) moverA.onCellReached -= OnACellReached;
            if (characterB != null) characterB.onDied -= OnBDeath;
        }

        private void OnBDeath(CharacterUnit character)
        {
            if (character != characterB || bConsumed) return;
            bAwaitingConsumption = false;
            bVisualState?.SetDead();
            if (moverB != null)
            {
                moverB.SetMoveTargetsVisible(false);
                moverB.enabled = false;
            }
        }

        private void OnBCellReached(Vector3Int _)
        {
            if (IsBUnavailable || characterB == null || d1InteractionBoard == null) return;

            Vector3Int cellOnD1Board = d1InteractionBoard.WorldToCell(characterB.transform.position);
            if (!d1InteractionBoard.HasInteraction(cellOnD1Board)) return;

            bAwaitingConsumption = true;
            characterB.SetVisualDeath(true);
            characterB.ReleaseControl();
            bVisualState?.SetDead();
            if (moverB != null)
            {
                moverB.SetMoveTargetsVisible(false);
                moverB.enabled = false;
            }

            // A 可能已经提前站在 B 的交互格等待。此时不必要求 A 再移动一次，
            // 直接走与“刚踏入交互格”完全相同的吞噬流程。
            TryStartConsumptionIfAIsOnBInteraction();
        }

        private void OnACellReached(Vector3Int _)
        {
            TryStartConsumptionIfAIsOnBInteraction();
        }

        private void TryStartConsumptionIfAIsOnBInteraction()
        {
            if (!bAwaitingConsumption || resolving || boardB == null || characterA == null || moverB == null) return;

            Vector3Int cellOnBBoard = boardB.WorldToCell(characterA.transform.position);
            if (!boardB.HasInteraction(cellOnBBoard)) return;

            // 检测 A 前后左右四个邻居格，是否存在视觉死亡的 B
            Vector3Int[] neighbors = { cellOnBBoard + Vector3Int.right, cellOnBBoard + Vector3Int.left, cellOnBBoard + Vector3Int.up, cellOnBBoard + Vector3Int.down };
            bool bFound = false;
            foreach (Vector3Int neighbor in neighbors)
            {
                if (neighbor == moverB.CurrentCell && characterB != null && characterB.IsVisuallyDead)
                {
                    bFound = true;
                    break;
                }
            }

            if (!bFound) return;
            consumeCoroutine = StartCoroutine(ConsumeB());
        }

        private IEnumerator ConsumeB()
        {
            resolving = true;
            bConsumed = true;
            if (moverA != null) moverA.enabled = false;
            aVisualState?.PlayEatAnimation();
            yield return new WaitForSeconds(eatDuration);

            characterB?.TransferRemainingLifeTo(characterA);
            characterB?.SetPresentationVisible(false);
            characterB?.SetVisualDeath(false);
            aVisualState?.FinishEatAnimation();

            bAwaitingConsumption = false;
            resolving = false;
            consumeCoroutine = null;
            if (moverA != null && characterA != null && !characterA.IsDead)
            {
                moverA.enabled = true;
                moverA.RefreshMoveTargets();
            }
        }

        public void ResetSequence(bool revealCharacterB = true)
        {
            if (consumeCoroutine != null) StopCoroutine(consumeCoroutine);
            consumeCoroutine = null;
            resolving = false;
            bAwaitingConsumption = false;
            // bConsumed 不清，留给 CharacterRespawnFlow 做完隐藏后再清

            if (moverA != null) moverA.enabled = true;
            if (moverB != null) moverB.enabled = true;
            if (revealCharacterB) characterB?.SetPresentationVisible(true);
            characterB?.SetVisualDeath(false);
            bVisualState?.SetIdle();
            aVisualState?.RefreshLifeState();
        }

        /// <summary>重生复位后再隐藏 B，防止闪烁。</summary>
        public void RehideBAfterRespawn()
        {
            characterB?.SetPresentationVisible(false);
        }

        /// <summary>隐藏完成后清标记，下次死亡重新判断。</summary>
        public void ClearConsumedFlag()
        {
            bConsumed = false;
        }

        private void ResolveReferences()
        {
            if (characterA == null) characterA = GameObject.Find("A")?.GetComponent<CharacterUnit>();
            if (characterB == null) characterB = GameObject.Find("B")?.GetComponent<CharacterUnit>();
            if (characterD1 == null) characterD1 = GameObject.Find("D1")?.GetComponent<CharacterUnit>();

            if (moverA == null && characterA != null) moverA = characterA.GetComponent<GridUnitMover>();
            if (moverB == null && characterB != null) moverB = characterB.GetComponent<GridUnitMover>();
            if (boardB == null && moverB != null) boardB = moverB.Board;
            if (d1InteractionBoard == null && characterD1 != null)
                d1InteractionBoard = characterD1.GetComponent<GridUnitMover>()?.Board;
            if (aVisualState == null && characterA != null)
                aVisualState = characterA.GetComponentInChildren<CharacterSpriteState>(true);
            if (bVisualState == null && characterB != null)
                bVisualState = characterB.GetComponentInChildren<BridgeCharacterVisualState>(true);
        }

        private static void SetAllCInitialLifeToTen()
        {
            foreach (CharacterUnit unit in FindObjectsByType<CharacterUnit>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (unit != null && unit.name.StartsWith("C", StringComparison.OrdinalIgnoreCase))
                    unit.SetInitialLife(10);
            }
        }
    }
}
