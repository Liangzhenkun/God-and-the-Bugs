using GameJamRAC.Grid;
using UnityEngine;

namespace GameJamRAC.Gameplay
{
    /// <summary>
    /// Drives the two-step A/B interaction sequence.
    /// A's own interaction tiles unlock B and soul swapping.
    /// B's own interaction tiles turn B into a temporary bridge for A.
    /// </summary>
    [DisallowMultipleComponent]
    public class SoulBridgeSequence : MonoBehaviour
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

        [Header("Bridge")]
        [SerializeField, Min(0f)] private float bridgeHeightAboveB = 2.6f;
        [SerializeField] private Color activatedGlowColor = new Color(0.15f, 0.6f, 1f, 1f);
        [SerializeField] private Color bridgeGlowColor = new Color(1f, 0.7f, 0.1f, 1f);
        [SerializeField, Min(0f)] private float glowRange = 5f;
        [SerializeField, Min(0f)] private float glowIntensity = 6f;

        private bool bActivated;
        private bool bridgeActive;
        private Vector3Int bridgeCellForA;
        private Light glowLight;

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
        }

        private void Start()
        {
            if (soulSwapManager != null)
                soulSwapManager.SetSoulSwapUnlocked(false);
        }

        private void OnDisable()
        {
            if (boardA != null) boardA.InteractiveTileEntered -= OnAInteraction;
            if (boardB != null) boardB.InteractiveTileEntered -= OnBInteraction;
            if (moverA != null) moverA.onCellReached -= OnACellReached;
            if (soulSwapManager != null) soulSwapManager.onStateChanged -= OnSoulSwapStateChanged;
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

        private void OnBInteraction(string interactionId)
        {
            if (!bActivated || bridgeActive || boardA == null || boardB == null || moverB == null || characterB == null)
                return;

            bridgeActive = true;
            bVisualState?.SetIdle();
            Vector3 bridgeWorldPosition = boardB.Grid.GetCellCenterWorld(moverB.CurrentCell);
            bridgeCellForA = boardA.WorldToCell(bridgeWorldPosition);
            float bridgeHeight = characterB.transform.position.y + bridgeHeightAboveB;
            boardA.SetTemporaryWalkableCell(bridgeCellForA, true, bridgeHeight);
            moverA?.RefreshMoveTargets();
            SetGlow(true, bridgeGlowColor);
        }

        private void OnACellReached(Vector3Int cell)
        {
            if (!bridgeActive || cell != bridgeCellForA || characterA == null || characterB == null)
                return;

            int transferredLife = characterB.TransferRemainingLifeTo(characterA);
            if (transferredLife <= 0) return;

            // B is now dead, so its temporary bridge is consumed with its life.
            bridgeActive = false;
            boardA?.ClearTemporaryWalkableCells();
            moverA?.RefreshMoveTargets();
            SetGlow(false, bridgeGlowColor);
            soulSwapManager?.SetSoulSwapUnlocked(false);
            bVisualState?.SetDead();
        }

        /// <summary>恢复本关开局的交互状态，供死亡复活流程调用。</summary>
        public void ResetSequence()
        {
            bActivated = false;
            bridgeActive = false;
            bridgeCellForA = default;

            if (boardA != null)
                boardA.ClearTemporaryWalkableCells();

            moverA?.RefreshMoveTargets();
            SetGlow(false, activatedGlowColor);
            bVisualState?.SetIdle();

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
    }
}
