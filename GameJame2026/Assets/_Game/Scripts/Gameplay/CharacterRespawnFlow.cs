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
        [SerializeField] private VictoryFlowManager victoryFlowManager;
        [SerializeField] private SoulBridgeSequence soulBridgeSequence;
        [SerializeField, Min(0f)] private float respawnDelay = 0.45f;

        private readonly HashSet<CharacterUnit> respawning = new HashSet<CharacterUnit>();

        private void Awake()
        {
            if (characters == null || characters.Length == 0)
                characters = FindObjectsByType<CharacterUnit>(FindObjectsSortMode.None);
            if (soulSwapManager == null) soulSwapManager = FindFirstObjectByType<SoulSwapManager>();
            if (victoryFlowManager == null) victoryFlowManager = FindFirstObjectByType<VictoryFlowManager>();
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
            if (victoryFlowManager != null && victoryFlowManager.HasWon) return;
            StartCoroutine(RespawnRoutine(character));
        }

        private IEnumerator RespawnRoutine(CharacterUnit character)
        {
            respawning.Add(character);
            soulBridgeSequence?.ResetSequence();
            soulSwapManager?.ResetProgressForRespawn();
            yield return new WaitForSeconds(respawnDelay);
            ResetAllCharactersToInitialState();
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
