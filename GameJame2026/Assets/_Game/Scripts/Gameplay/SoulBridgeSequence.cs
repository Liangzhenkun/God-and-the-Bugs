using System.Collections;
using GameJamRAC.Grid;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GameJamRAC.Gameplay
{
    /// <summary>
    /// Drives the two-step A/B interaction sequence.
    /// A's own interaction tiles unlock B and soul swapping.
    /// B's own interaction tiles turn B into a temporary bridge for A.
    /// </summary>
    [DisallowMultipleComponent]
    public class SoulBridgeSequence : MonoBehaviour, IConsumeSequence
    {
        [Header("Characters")]
        [SerializeField] private CharacterUnit characterA;
        [SerializeField] private CharacterUnit characterB;
        [SerializeField] private GridUnitMover moverA;
        [SerializeField] private GridUnitMover moverB;

        [Header("Separate route boards")]
        [SerializeField] private GridBoard boardA;
        [SerializeField] private GridBoard boardB;
        [SerializeField] private SoulSwapManager soulSwapManager;
        [SerializeField] private BridgeCharacterVisualState bVisualState;
        [SerializeField] private CharacterSpriteState aVisualState;

        [Header("Bridge")]
        [SerializeField, Min(0f)] private float bridgeHeightAboveB = 2.6f;
        [SerializeField] private Color activatedGlowColor = new Color(0.15f, 0.6f, 1f, 1f);
        [SerializeField] private Color bridgeGlowColor = new Color(1f, 0.7f, 0.1f, 1f);
        [SerializeField, Min(0f)] private float glowRange = 5f;
        [SerializeField, Min(0f)] private float glowIntensity = 6f;
        [SerializeField, Min(0.1f)] private float absorbEatDuration = 1.5f;
        [SerializeField] private bool enableBridgeStep = true;

        private bool bActivated;
        private bool bridgeActive;
        private bool aStandingOnBridge;
        private Vector3Int bridgeCellForA;
        private Light glowLight;
        private Coroutine absorptionCoroutine;

        private void Reset()
        {
            characterA = GameObject.Find("A")?.GetComponent<CharacterUnit>();
            characterB = GameObject.Find("B")?.GetComponent<CharacterUnit>();
            moverA = characterA != null ? characterA.GetComponent<GridUnitMover>() : null;
            moverB = characterB != null ? characterB.GetComponent<GridUnitMover>() : null;
            boardA = moverA != null ? moverA.Board : null;
            boardB = moverB != null ? moverB.Board : null;
            soulSwapManager = FindFirstObjectByType<SoulSwapManager>();
        }

        private void Awake()
        {
            ResolveReferences();
            SetGlow(false, activatedGlowColor);
        }

        private void OnEnable()
        {
            ResolveReferences();
            if (boardA != null) boardA.InteractiveTileEntered += OnAInteraction;
            if (boardB != null) boardB.InteractiveTileEntered += OnBInteraction;
            if (moverA != null) moverA.onCellReached += OnACellReached;
            if (soulSwapManager != null) soulSwapManager.onStateChanged += OnSoulSwapStateChanged;

            // 主场景中，B 的体力耗尽也要进入与桥接死亡一致的视觉和不可操作状态。
            if (IsMainScene && characterB != null) characterB.onDied += OnMainSceneBDepleted;
        }

        private IEnumerator Start()
        {
            if (soulSwapManager != null)
                soulSwapManager.SetSoulSwapUnlocked(false);

            // GridUnitMover 也会在 Start 对齐初始位置；等待一帧后，
            // 用与正常移动相同的规则检测出生格。
            yield return null;
            EvaluateInitialAInteraction();
        }

        private void OnDisable()
        {
            if (boardA != null) boardA.InteractiveTileEntered -= OnAInteraction;
            if (boardB != null) boardB.InteractiveTileEntered -= OnBInteraction;
            if (moverA != null) moverA.onCellReached -= OnACellReached;
            if (soulSwapManager != null) soulSwapManager.onStateChanged -= OnSoulSwapStateChanged;
            if (IsMainScene && characterB != null) characterB.onDied -= OnMainSceneBDepleted;
        }

        private bool IsMainScene => SceneManager.GetActiveScene().name == "MainScene";

        /// <summary>
        /// 主场景补充规则：B 的生命归零时，播放 DieB（BridgeState = 2）并停止移动。
        /// CharacterUnit 已负责把 B 标记为死亡；这里仅补齐该场景的动画与交互表现。
        /// </summary>
        private void OnMainSceneBDepleted(CharacterUnit defeatedCharacter)
        {
            if (!IsMainScene || defeatedCharacter != characterB)
                return;

            bActivated = false;
            bridgeActive = false;
            aStandingOnBridge = false;
            boardA?.ClearTemporaryWalkableCells();

            if (moverB != null)
            {
                moverB.SetMoveTargetsVisible(false);
                moverB.enabled = false;
            }

            bVisualState?.SetDead();
            SetGlow(false, activatedGlowColor);
            soulSwapManager?.SetSoulSwapUnlocked(false);
        }

        private void OnAInteraction(string interactionId)
        {
            if (bActivated) return;

            bActivated = true;
            bVisualState?.SetActive();
            SetGlow(true, activatedGlowColor);
            if (soulSwapManager != null)
                soulSwapManager.SetSoulSwapUnlocked(true);
        }

        /// <summary>检测 A 当前的出生格或重置格是否为激活交互格。</summary>
        public void EvaluateInitialAInteraction()
        {
            ResolveReferences();
            if (bActivated || boardA == null || moverA == null) return;
            if (boardA.HasInteraction(moverA.CurrentCell))
                OnAInteraction(string.Empty);
        }

        private void OnBInteraction(string interactionId)
        {
            if (!enableBridgeStep || !bActivated || bridgeActive || boardA == null || boardB == null || moverB == null || characterB == null)
                return;

            bridgeActive = true;
            bVisualState?.SetDead();
            SetBVisualVisible(true);
            if (moverB != null) moverB.enabled = false;
            Vector3 bridgeWorldPosition = boardB.Grid.GetCellCenterWorld(moverB.CurrentCell);
            bridgeCellForA = boardA.WorldToCell(bridgeWorldPosition);
            float bridgeHeight = characterB.transform.position.y + bridgeHeightAboveB;
            boardA.SetTemporaryWalkableCell(bridgeCellForA, true, bridgeHeight);
            moverA?.RefreshMoveTargets();
            SetGlow(true, bridgeGlowColor);
        }

        private void OnACellReached(Vector3Int cell)
        {
            if (!bridgeActive || characterA == null || characterB == null)
                return;

            if (cell == bridgeCellForA)
            {
                aStandingOnBridge = true;
                return;
            }

            if (!aStandingOnBridge || absorptionCoroutine != null)
                return;

            aStandingOnBridge = false;
            absorptionCoroutine = StartCoroutine(AbsorbBridgeLifeAfterLeaving());
        }

        private IEnumerator AbsorbBridgeLifeAfterLeaving()
        {
            if (moverA != null) moverA.enabled = false;
            aVisualState?.PlayEatAnimation();
            yield return new WaitForSeconds(absorbEatDuration);

            characterB.TransferRemainingLifeTo(characterA);

            bridgeActive = false;
            boardA?.ClearTemporaryWalkableCells();
            SetGlow(false, bridgeGlowColor);
            soulSwapManager?.SetSoulSwapUnlocked(false);
            bVisualState?.SetDead();
            SetBVisualVisible(false);
            aVisualState?.FinishEatAnimation();

            if (moverA != null)
            {
                moverA.enabled = true;
                moverA.RefreshMoveTargets();
            }

            absorptionCoroutine = null;
        }

        public bool IsResolving => absorptionCoroutine != null;
        public bool WasBConsumed => false;
        public void RehideBAfterRespawn() { }
        public void ClearConsumedFlag() { }

        /// <summary>恢复本关开局的交互状态，供死亡复活流程调用。</summary>
        public void ResetSequence(bool revealB = true)
        {
            if (absorptionCoroutine != null)
            {
                StopCoroutine(absorptionCoroutine);
                absorptionCoroutine = null;
            }

            bActivated = false;
            bridgeActive = false;
            aStandingOnBridge = false;
            bridgeCellForA = default;

            if (boardA != null)
                boardA.ClearTemporaryWalkableCells();

            if (moverA != null) moverA.enabled = true;
            if (moverB != null) moverB.enabled = true;
            moverA?.RefreshMoveTargets();
            SetGlow(false, activatedGlowColor);
            SetBVisualVisible(true);
            bVisualState?.SetIdle();
            aVisualState?.RefreshLifeState();

            if (soulSwapManager != null)
                soulSwapManager.SetSoulSwapUnlocked(false);
        }

        private void ResolveReferences()
        {
            if (characterA == null) characterA = GameObject.Find("A")?.GetComponent<CharacterUnit>();
            if (characterB == null) characterB = GameObject.Find("B")?.GetComponent<CharacterUnit>();
            if (moverA == null && characterA != null) moverA = characterA.GetComponent<GridUnitMover>();
            if (moverB == null && characterB != null) moverB = characterB.GetComponent<GridUnitMover>();
            if (boardA == null && moverA != null) boardA = moverA.Board;
            if (boardB == null && moverB != null) boardB = moverB.Board;
            if (soulSwapManager == null) soulSwapManager = FindFirstObjectByType<SoulSwapManager>();
            if (bVisualState == null && characterB != null) bVisualState = characterB.GetComponentInChildren<BridgeCharacterVisualState>(true);
            if (aVisualState == null && characterA != null) aVisualState = characterA.GetComponentInChildren<CharacterSpriteState>(true);
        }

        private void OnSoulSwapStateChanged(SoulSwapManager.SwapState state)
        {
            if (state == SoulSwapManager.SwapState.PossessingB && bActivated && !bridgeActive)
                bVisualState?.SetActive();
        }

        private void SetGlow(bool active, Color color)
        {
            if (characterB == null) return;
            if (glowLight == null)
            {
                Transform existing = characterB.transform.Find("SoulBridgeGlow");
                if (existing != null) glowLight = existing.GetComponent<Light>();
                if (glowLight == null)
                {
                    GameObject glow = new GameObject("SoulBridgeGlow");
                    glow.transform.SetParent(characterB.transform, false);
                    glow.transform.localPosition = Vector3.up * 1.2f;
                    glowLight = glow.AddComponent<Light>();
                    glowLight.type = LightType.Point;
                    glowLight.shadows = LightShadows.None;
                }
            }

            glowLight.color = color;
            glowLight.range = glowRange;
            glowLight.intensity = active ? glowIntensity : 0f;
        }

        private void SetBVisualVisible(bool visible)
        {
            if (characterB == null) return;

            foreach (SpriteRenderer sprite in characterB.GetComponentsInChildren<SpriteRenderer>(true))
                sprite.enabled = visible;

            foreach (ScoreLabelUI label in characterB.GetComponentsInChildren<ScoreLabelUI>(true))
                label.gameObject.SetActive(visible);
        }
    }
}
