using System.Collections;
using GameJamRAC.Grid;
using UnityEngine;

namespace GameJamRAC.Gameplay
{
    /// <summary>Scene 3: B dies on its tile; A eats B when it reaches that tile.</summary>
    [DisallowMultipleComponent]
    public class SceneThreeBConsumeSequence : MonoBehaviour
    {
        [SerializeField] private CharacterUnit characterA;
        [SerializeField] private CharacterUnit characterB;
        [SerializeField] private GridUnitMover moverA;
        [SerializeField] private GridUnitMover moverB;
        [SerializeField] private GridBoard boardA;
        [SerializeField] private GridBoard boardB;
        [SerializeField] private CharacterSpriteState aVisualState;
        [SerializeField] private BridgeCharacterVisualState bVisualState;
        [SerializeField, Min(0.1f)] private float eatDuration = 1.5f;

        private bool bAwaitingConsumption;
        private bool resolving;
        private Coroutine consumeCoroutine;

        public bool IsResolving => resolving;
        public bool IsBUnavailable => bAwaitingConsumption || resolving || (characterB != null && characterB.IsDead);

        /// <summary>B 在激活格进入视觉死亡后，不再是捕食者可结算的目标。</summary>
        public bool IsUnavailableAsPrey(CharacterUnit candidate)
        {
            return candidate != null && candidate == characterB && characterB.IsVisuallyDead;
        }

        private void Awake() { ResolveReferences(); }

        private void OnEnable()
        {
            ResolveReferences();
            if (boardB != null) boardB.InteractiveTileEntered += OnBInteraction;
            if (moverA != null) moverA.onCellReached += OnACellReached;
        }

        private void OnDisable()
        {
            if (boardB != null) boardB.InteractiveTileEntered -= OnBInteraction;
            if (moverA != null) moverA.onCellReached -= OnACellReached;
        }

        private void OnBInteraction(string _)
        {
            if (bAwaitingConsumption || characterB == null || characterB.IsDead) return;

            bAwaitingConsumption = true;
            characterB.SetVisualDeath(true);
            bVisualState?.SetDead();
            characterB.ReleaseControl();
            if (moverB != null)
            {
                moverB.SetMoveTargetsVisible(false);
                moverB.enabled = false;
            }

            // 关卡 3 不生成临时桥格：B 所在格不会开放给 A，也不会扩展 A 的能力范围。
        }

        private void OnACellReached(Vector3Int cell)
        {
            if (!bAwaitingConsumption || resolving || boardA == null || boardB == null) return;

            // A 只要正常走进 B 的任意交互区域便触发吃掉；不要求进入 B 的死亡格。
            Vector3 worldPosition = boardA.Grid.GetCellCenterWorld(cell);
            Vector3Int cellOnBBoard = boardB.WorldToCell(worldPosition);
            if (!boardB.HasInteraction(cellOnBBoard)) return;
            consumeCoroutine = StartCoroutine(ConsumeB());
        }

        private IEnumerator ConsumeB()
        {
            resolving = true;
            if (moverA != null) moverA.enabled = false;
            aVisualState?.PlayEatAnimation();
            yield return new WaitForSeconds(eatDuration);

            characterB.TransferRemainingLifeTo(characterA);
            characterB.SetPresentationVisible(false);
            aVisualState?.FinishEatAnimation();

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
            // A 死亡后的复活等待期间，不允许已经被吃掉的 B 因通用重置而闪现。
            // 等 CharacterRespawnFlow 在等待结束后统一恢复所有角色时，B 才回到初始位置显示。
            if (revealCharacterB)
                characterB?.SetPresentationVisible(true);
            else
                characterB?.SetPresentationVisible(false);
            characterB?.SetVisualDeath(false);
            bVisualState?.SetIdle();
            aVisualState?.RefreshLifeState();
        }

        private void ResolveReferences()
        {
            if (characterA == null) characterA = GameObject.Find("A")?.GetComponent<CharacterUnit>();
            if (characterB == null) characterB = GameObject.Find("B")?.GetComponent<CharacterUnit>();
            if (moverA == null && characterA != null) moverA = characterA.GetComponent<GridUnitMover>();
            if (moverB == null && characterB != null) moverB = characterB.GetComponent<GridUnitMover>();
            if (boardA == null && moverA != null) boardA = moverA.Board;
            if (boardB == null && moverB != null) boardB = moverB.Board;
            if (aVisualState == null && characterA != null) aVisualState = characterA.GetComponentInChildren<CharacterSpriteState>(true);
            if (bVisualState == null && characterB != null) bVisualState = characterB.GetComponentInChildren<BridgeCharacterVisualState>(true);
        }
    }
}
