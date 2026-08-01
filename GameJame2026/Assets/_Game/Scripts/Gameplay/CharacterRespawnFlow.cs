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

            if (soulSwapManager != null && soulSwapManager.RecoverToAAfterBDefeated(character))
                return;

            bool primaryCharacterDied = soulSwapManager != null && soulSwapManager.IsPrimaryCharacter(character);
            if (!character.WasPlayerControlledAtDeath && !primaryCharacterDied) return;
            StartCoroutine(RespawnRoutine(character));
        }

        private IEnumerator RespawnRoutine(CharacterUnit character)
        {
            respawning.Add(character);
            List<IConsumeSequence> consumedSequences = new List<IConsumeSequence>();

            foreach (MonoBehaviour mb in FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None))
            {
                IConsumeSequence sequence = mb as IConsumeSequence;
                if (sequence == null) continue;

                if (sequence.WasBConsumed)
                    consumedSequences.Add(sequence);
                sequence.ResetSequence(false);
            }

            foreach (PredatorAI predator in FindObjectsByType<PredatorAI>(FindObjectsSortMode.None))
                predator.ResetState();
            soulSwapManager?.ResetProgressForRespawn();

            yield return new WaitForSeconds(Mathf.Max(1f, respawnDelay));
            ResetAllCharactersToInitialState();
            ClearConsumedFlags(consumedSequences);
            HideCLabels();

            soulBridgeSequence?.EvaluateInitialAInteraction();
            if (soulSwapManager != null)
            {
                soulSwapManager.RestoreInitialControlAfterRespawn();
                soulSwapManager.PlayRespawnCameraSequence(character);
            }
            else
            {
                character.TakeControl();
            }

            respawning.Remove(character);
        }

        private void ResetAllCharactersToInitialState()
        {
            CharacterUnit[] sceneCharacters = FindObjectsByType<CharacterUnit>(FindObjectsSortMode.None);
            foreach (CharacterUnit sceneCharacter in sceneCharacters)
            {
                if (sceneCharacter != null)
                    sceneCharacter.RespawnAtInitialPosition();
            }
        }

        private static void ClearConsumedFlags(List<IConsumeSequence> consumedSequences)
        {
            foreach (IConsumeSequence sequence in consumedSequences)
                sequence.ClearConsumedFlag();
        }

        private static void HideCLabels()
        {
            foreach (CharacterUnit c in FindObjectsByType<CharacterUnit>(FindObjectsSortMode.None))
            {
                if (c == null || string.IsNullOrEmpty(c.DisplayName) || !c.DisplayName.Contains("C")) continue;

                foreach (ScoreLabelUI label in c.GetComponentsInChildren<ScoreLabelUI>(true))
                    label.gameObject.SetActive(false);
            }
        }
    }
}
