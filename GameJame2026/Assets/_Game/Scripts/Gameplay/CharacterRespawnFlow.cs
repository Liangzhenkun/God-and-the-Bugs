using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GameJamRAC.Gameplay
{
    [DisallowMultipleComponent]
    public class CharacterRespawnFlow : MonoBehaviour
    {
        [SerializeField] private CharacterUnit[] characters;
        [SerializeField] private SoulSwapManager soulSwapManager;
        [SerializeField] private GameplaySceneManager gameplaySceneManager;
        [SerializeField] private SoulBridgeSequence soulBridgeSequence;
        [SerializeField, Min(1f)] private float respawnDelay = 1f;

        private readonly HashSet<CharacterUnit> respawning = new HashSet<CharacterUnit>();

        private void Awake()
        {
            if (characters == null || characters.Length == 0)
                characters = FindObjectsByType<CharacterUnit>(FindObjectsSortMode.None);
            if (soulSwapManager == null) soulSwapManager = FindFirstObjectByType<SoulSwapManager>();
            if (gameplaySceneManager == null) gameplaySceneManager = FindFirstObjectByType<GameplaySceneManager>();
            if (soulBridgeSequence == null) soulBridgeSequence = FindFirstObjectByType<SoulBridgeSequence>();
        }

        private void OnEnable()
        {
            foreach (CharacterUnit character in characters)
                if (character != null) character.onDied += OnCharacterDied;
        }

        private void OnDisable()
        {
            foreach (CharacterUnit character in characters)
                if (character != null) character.onDied -= OnCharacterDied;
        }

        private void OnCharacterDied(CharacterUnit character)
        {
            if (character == null || respawning.Contains(character)) return;
            if (character.SuppressAutomaticRespawnOnDeath) return;
            if (gameplaySceneManager != null && gameplaySceneManager.HasWon) return;

            // B 被吃掉并不代表本局失败；若当时操控 B，交还给 A 后继续。
            if (soulSwapManager != null && soulSwapManager.RecoverToAAfterBDefeated(character))
                return;

            // A 是主角，不论当时是否被直接控制，死亡都结束本局。
            bool primaryCharacterDied = soulSwapManager != null && soulSwapManager.IsPrimaryCharacter(character);
            if (!character.WasPlayerControlledAtDeath && !primaryCharacterDied) return;
            StartCoroutine(RespawnRoutine(character));
        }

        private IEnumerator RespawnRoutine(CharacterUnit character)
        {
            respawning.Add(character);
            // 统一通过接口重置所有消耗序列
            foreach (var mb in FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None))
            {
                if (mb is IConsumeSequence cs)
                    cs.ResetSequence(false);
            }
            foreach (PredatorAI predator in FindObjectsByType<PredatorAI>(FindObjectsSortMode.None))
                predator.ResetState();
            soulSwapManager?.ResetProgressForRespawn();
            yield return new WaitForSeconds(Mathf.Max(1f, respawnDelay));
            ResetAllCharactersToInitialState();

            // C 角色的头顶标签始终隐藏（匹配 "C1"、"角色 C2" 等）
            foreach (CharacterUnit c in FindObjectsByType<CharacterUnit>(FindObjectsSortMode.None))
            {
                if (c != null && !string.IsNullOrEmpty(c.DisplayName) && c.DisplayName.Contains("C"))
                {
                    foreach (ScoreLabelUI label in c.GetComponentsInChildren<ScoreLabelUI>(true))
                        label.gameObject.SetActive(false);
                }
            }

            // 所有消耗序列中 B 被吃掉后，复位时重新隐藏避免闪烁
            foreach (var mb in FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None))
            {
                if (mb is IConsumeSequence cs && cs.WasBConsumed)
                {
                    cs.RehideBAfterRespawn();
                    cs.ClearConsumedFlag();
                }
            }

            soulBridgeSequence?.EvaluateInitialAInteraction();
            if (soulSwapManager != null)
            {
                soulSwapManager.RestoreInitialControlAfterRespawn();
                soulSwapManager.PlayRespawnCameraSequence(character);
            }
            else character.TakeControl();
            respawning.Remove(character);
        }

        /// <summary>死亡代表本轮失败，因此所有场内角色都回到各自的出生点。</summary>
        private void ResetAllCharactersToInitialState()
        {
            CharacterUnit[] sceneCharacters = FindObjectsByType<CharacterUnit>(FindObjectsSortMode.None);
            foreach (CharacterUnit sceneCharacter in sceneCharacters)
            {
                if (sceneCharacter != null)
                    sceneCharacter.RespawnAtInitialPosition();
            }
        }
    }
}
