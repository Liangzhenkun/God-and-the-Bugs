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
        [SerializeField] private BridgeConsumptionRule bridgeConsumptionRule;
        [SerializeField, Min(1f)] private float respawnDelay = 1f;

        private readonly HashSet<CharacterUnit> respawning = new HashSet<CharacterUnit>();

        private void Awake()
        {
            if (characters == null || characters.Length == 0)
                characters = FindObjectsByType<CharacterUnit>(FindObjectsSortMode.None);
            if (soulSwapManager == null) soulSwapManager = FindFirstObjectByType<SoulSwapManager>();
            if (gameplaySceneManager == null) gameplaySceneManager = FindFirstObjectByType<GameplaySceneManager>();
            if (bridgeConsumptionRule == null) bridgeConsumptionRule = FindFirstObjectByType<BridgeConsumptionRule>();
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
            List<IConsumptionRule> consumedRules = new List<IConsumptionRule>();

            foreach (MonoBehaviour mb in FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None))
            {
                IConsumptionRule rule = mb as IConsumptionRule;
                if (rule == null) continue;

                bool wasBConsumed = rule.WasBConsumed;
                if (wasBConsumed)
                    consumedRules.Add(rule);

                rule.ResetSequence(!wasBConsumed);
            }

            foreach (PredatorAI predator in FindObjectsByType<PredatorAI>(FindObjectsSortMode.None))
                predator.ResetState();
            soulSwapManager?.ResetProgressForRespawn();

            yield return new WaitForSeconds(Mathf.Max(1f, respawnDelay));
            ResetAllCharactersToInitialState();
            RehideConsumedCharacters(consumedRules);
            ClearConsumedFlags(consumedRules);
            HideCLabels();

            bridgeConsumptionRule?.EvaluateInitialAInteraction();
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

        private static void ClearConsumedFlags(List<IConsumptionRule> consumedRules)
        {
            foreach (IConsumptionRule rule in consumedRules)
                rule.ClearConsumedFlag();
        }

        private static void RehideConsumedCharacters(List<IConsumptionRule> consumedRules)
        {
            foreach (IConsumptionRule rule in consumedRules)
                rule.RehideBAfterRespawn();
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
