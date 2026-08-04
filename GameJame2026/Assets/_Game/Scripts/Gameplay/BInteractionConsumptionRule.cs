using System.Collections;
using GameJamRAC.Grid;
using UnityEngine;

namespace GameJamRAC.Gameplay
{
    /// <summary>第二关：B 进入交互格后视觉死亡，A 需要在交互格旁边吃掉 B。</summary>
    [DisallowMultipleComponent]
    public class BInteractionConsumptionRule : MonoBehaviour, IConsumptionRule, ICharacterAvailabilityRule
    {
        private enum BDeathTrigger
        {
            CharacterBInteractionBoard,
            OtherCharacterInteractionBoard
        }

        [SerializeField] private BDeathTrigger bDeathTrigger = BDeathTrigger.CharacterBInteractionBoard;
        [SerializeField] private CharacterUnit characterA;
        [SerializeField] private CharacterUnit characterB;
        [SerializeField] private CharacterUnit characterD;
        [SerializeField] private GridUnitMover moverA;
        [SerializeField] private GridUnitMover moverB;
        [SerializeField] private GridBoard boardA;
        [SerializeField] private GridBoard boardB;
        [SerializeField] private GridBoard bDeathTriggerBoard;
        [SerializeField] private CharacterSpriteState aVisualState;
        [SerializeField] private BridgeCharacterVisualState bVisualState;
        [SerializeField, Min(0.1f)] private float eatDuration = 1.5f;

        private bool bAwaitingConsumption;
        private bool resolving;
        private bool bConsumed;
        private Coroutine consumeCoroutine;
        private bool subscribed;

        public bool WasBConsumed => bConsumed;

        public bool IsResolving => resolving;
        public bool IsBUnavailable => bAwaitingConsumption || resolving
            || (characterB != null && (characterB.IsDead || characterB.IsVisuallyDead));

        public bool IsCharacterUnavailable(CharacterUnit candidate)
        {
            return candidate != null && candidate == characterB && IsBUnavailable;
        }

        /// <summary>B 视觉死亡后，不再作为捕食者可结算的猎物。</summary>
        public bool IsUnavailableAsPrey(CharacterUnit candidate)
        {
            return candidate != null && candidate == characterB && characterB.IsVisuallyDead;
        }

        private void Awake()
        {
            ResolveReferences();
        }

        private void OnEnable()
        {
            if (Application.isPlaying)
                Subscribe();
        }

        private void Start()
        {
            Subscribe();
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        private void Subscribe()
        {
            if (subscribed) return;

            ResolveReferences();
            if (moverB == null || moverA == null || characterB == null) return;

            moverB.onCellReached += OnBCellReached;
            moverA.onCellReached += OnACellReached;
            characterB.onDied += OnBDeath;
            subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!subscribed) return;

            if (moverB != null) moverB.onCellReached -= OnBCellReached;
            if (moverA != null) moverA.onCellReached -= OnACellReached;
            if (characterB != null) characterB.onDied -= OnBDeath;
            subscribed = false;
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
            if (bAwaitingConsumption || characterB == null || characterB.IsDead || bDeathTriggerBoard == null) return;

            Vector3Int cellOnTriggerBoard = bDeathTriggerBoard.WorldToCell(characterB.transform.position);
            if (!bDeathTriggerBoard.HasInteraction(cellOnTriggerBoard)) return;

            bAwaitingConsumption = true;
            characterB.SetVisualDeath(true);
            bVisualState?.SetDead();
            characterB.ReleaseControl();
            if (moverB != null)
            {
                moverB.SetMoveTargetsVisible(false);
                moverB.enabled = false;
            }

            TryStartConsumptionIfAIsOnBInteraction();
        }

        private void OnACellReached(Vector3Int _)
        {
            TryStartConsumptionIfAIsOnBInteraction();
        }

        private void TryStartConsumptionIfAIsOnBInteraction()
        {
            if (!bAwaitingConsumption || resolving || boardB == null || characterA == null || characterB == null || moverB == null)
                return;

            Vector3Int cellOnBBoard = boardB.WorldToCell(characterA.transform.position);
            if (!boardB.HasInteraction(cellOnBBoard)) return;
            if (!IsBVisuallyDeadNextTo(cellOnBBoard)) return;

            consumeCoroutine = StartCoroutine(ConsumeB());
        }

        private bool IsBVisuallyDeadNextTo(Vector3Int centerCell)
        {
            if (!characterB.IsVisuallyDead) return false;

            Vector3Int bCell = moverB.CurrentCell;
            return bCell == centerCell + Vector3Int.right
                || bCell == centerCell + Vector3Int.left
                || bCell == centerCell + Vector3Int.up
                || bCell == centerCell + Vector3Int.down;
        }

        private IEnumerator ConsumeB()
        {
            resolving = true;
            bConsumed = true;
            if (moverA != null) moverA.enabled = false;

            HorizontalMoveFlip aFlip = characterA != null ? characterA.GetComponent<HorizontalMoveFlip>() : null;
            if (aFlip != null && characterB != null)
                yield return aFlip.FaceWorldPositionTemporarily(characterB.transform.position);

            aVisualState?.PlayEatAnimation();
            yield return new WaitForSeconds(eatDuration);

            characterB.TransferRemainingLifeTo(characterA);
            characterB.SetPresentationVisible(false);
            aVisualState?.FinishEatAnimation();
            if (aFlip != null)
                yield return aFlip.RestoreTemporaryFacing();

            bAwaitingConsumption = false;
            resolving = false;
            consumeCoroutine = null;
            if (moverA != null)
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
            if (moverA != null) moverA.enabled = true;
            if (moverB != null) moverB.enabled = true;
            if (revealCharacterB)
                characterB?.SetPresentationVisible(true);
            else
                characterB?.SetPresentationVisible(false);
            characterB?.SetVisualDeath(false);
            bVisualState?.SetIdle();
            aVisualState?.RefreshLifeState();
        }

        public void RehideBAfterRespawn()
        {
            characterB?.SetPresentationVisible(false);
        }

        public void ClearConsumedFlag()
        {
            bConsumed = false;
        }

        private void ResolveReferences()
        {
            if (characterA == null) characterA = GameObject.Find("A")?.GetComponent<CharacterUnit>();
            if (characterB == null) characterB = GameObject.Find("B")?.GetComponent<CharacterUnit>();
            if (characterD == null) characterD = GameObject.Find("D")?.GetComponent<CharacterUnit>();
            if (characterD == null) characterD = GameObject.Find("D1")?.GetComponent<CharacterUnit>();
            if (moverA == null && characterA != null) moverA = characterA.GetComponent<GridUnitMover>();
            if (moverB == null && characterB != null) moverB = characterB.GetComponent<GridUnitMover>();
            if (boardA == null && moverA != null) boardA = moverA.Board;
            if (boardB == null && moverB != null) boardB = moverB.Board;
            if (bDeathTrigger == BDeathTrigger.CharacterBInteractionBoard)
                bDeathTriggerBoard = boardB;
            else if (bDeathTriggerBoard == null && characterD != null)
                bDeathTriggerBoard = characterD.GetComponent<GridUnitMover>()?.Board;
            if (aVisualState == null && characterA != null) aVisualState = characterA.GetComponentInChildren<CharacterSpriteState>(true);
            if (bVisualState == null && characterB != null) bVisualState = characterB.GetComponentInChildren<BridgeCharacterVisualState>(true);
        }
    }
}
