using CrystalDefenders.Combat;
using CrystalDefenders.Units;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace CrystalDefenders.Gameplay
{
    /// <summary>
    /// Manages wave-based enemy spawning and adaptive difficulty.
    /// Handles enemy selection, spawn timing, defender pressure, and wave progression.
    /// </summary>
    public class WaveManager : MonoBehaviour
    {
        public static WaveManager Instance { get; private set; }

        [Header("Base Wave Settings")]
        [SerializeField] private int baseEnemiesPerSpawner = 3;        // Initial enemies per spawner
        [SerializeField] private int enemiesIncrementPerWave = 2;      // Additional enemies per wave
        [SerializeField] private int waveBonusResources = 100;         // Player reward after clearing wave

        [Header("Spawn Timing Settings")]
        [SerializeField] private float baseSpawnInterval = 2.0f;       // Base time between spawns
        [SerializeField] private float spawnIntervalAccelerationPerWave = -0.08f; // Spawn interval decreases per wave
        [SerializeField] private float earlyWaveSlowMultiplier = 1.5f; // Early waves spawn slower for ease

        [Header("Wave Rules")]
        [SerializeField] private int guaranteedCounterWave = 5;       // Wave after which all enemy types always appear

        [Header("UI")]
        [SerializeField] private TMP_Text waveText;                    // UI text for current wave

        private readonly List<EnemySpawner> spawners = new List<EnemySpawner>();
        private int currentWave = 0;                                   // Tracks the current wave
        private int aliveEnemies = 0;                                   // Count of enemies currently alive

        private void Awake()
        {
            // Ensure singleton instance
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        /// <summary>
        /// Registers an EnemySpawner with this WaveManager.
        /// </summary>
        /// <param name="spawner">Spawner to register</param>
        public void RegisterSpawner(EnemySpawner spawner)
        {
            if (spawner != null && !spawners.Contains(spawner))
                spawners.Add(spawner);
        }

        /// <summary>
        /// Initiates the next wave of enemies.
        /// </summary>
        public void StartNextWave()
        {
            currentWave++;
            Debug.Log($"[WaveManager] Starting Wave {currentWave}");

            if (waveText != null)
                waveText.text = $"Wave {currentWave}";

            StartCoroutine(SpawnWaveRoutine());
        }

        /// <summary>
        /// Coroutine handling enemy spawning, adaptive difficulty, and wave pacing.
        /// </summary>
        private IEnumerator SpawnWaveRoutine()
        {
            aliveEnemies = 0;

            // Gather player stats
            int playerResources = ResourceManager.Instance != null ? ResourceManager.Instance.CurrentResources : 0;
            int towerHealth = GameManager.Instance?.Tower?.GetComponent<Health>()?.CurrentHealth ?? 1000;

            // Compute difficulty and spawn multipliers
            float diff = WaveAdaptiveExtensions.ComputeDifficultyMultiplier(currentWave, playerResources, towerHealth);
            float baseSpawnMult = WaveAdaptiveExtensions.ComputeSpawnMultiplier(currentWave, playerResources, towerHealth);
            float defenderHealthFactor = ComputeDefenderHealthFactor();
            float defenderPressureAdj = WaveAdaptiveExtensions.ComputeDefenderPressureAdjustment(defenderHealthFactor);

            float spawnMult = baseSpawnMult * defenderPressureAdj;

            // Calculate number of enemies per spawner for this wave
            int baseCount = Mathf.Max(1, baseEnemiesPerSpawner + enemiesIncrementPerWave * (currentWave - 1));
            if (currentWave <= 2)
                baseCount = Mathf.Max(1, baseCount - 3); // Reduce enemies early for player ease

            int toSpawnPerSpawner = Mathf.RoundToInt(baseCount * Mathf.Clamp(spawnMult, 0.5f, 2.5f));

            // Determine spawn interval with early wave adjustment
            float interval = Mathf.Max(0.5f, baseSpawnInterval + spawnIntervalAccelerationPerWave * (currentWave - 1));
            if (currentWave <= 2)
                interval *= earlyWaveSlowMultiplier;

            // Select enemy prefabs for this wave
            var variants = SelectVariantsForPlayerCapability(diff);
            List<Units.Enemy> enemiesToSpawn = new List<Units.Enemy>();
            List<float> weights = new List<float>();

            if (variants.prefabs != null)
            {
                enemiesToSpawn.AddRange(variants.prefabs);
                weights.AddRange(variants.weights);
            }

            // Force spawn of all enemy types if wave >= guaranteedCounterWave
            bool forceAllEnemies = WaveAdaptiveExtensions.ShouldSpawnAllEnemyTypes(currentWave, playerResources, towerHealth, guaranteedCounterWave);
            if (forceAllEnemies)
            {
                var gm = GameManager.Instance;
                var fastEnemy = gm?.enemyFastPrefab;
                var rangedEnemy = gm?.enemyRangedPrefab;

                if (fastEnemy != null && !enemiesToSpawn.Contains(fastEnemy)) { enemiesToSpawn.Add(fastEnemy); weights.Add(0.25f); }
                if (rangedEnemy != null && !enemiesToSpawn.Contains(rangedEnemy)) { enemiesToSpawn.Add(rangedEnemy); weights.Add(0.25f); }
            }

            Debug.Log($"[WaveManager] Wave {currentWave} | Diff={diff:F2} | Count={toSpawnPerSpawner} | Interval={interval:F2}s | PressureAdj={defenderPressureAdj:F2}");

            // Spawn enemies from each registered spawner
            foreach (var spawner in spawners)
            {
                if (spawner == null) continue;

                aliveEnemies += toSpawnPerSpawner;

                spawner.BeginSpawning(
                    toSpawnPerSpawner,
                    enemiesToSpawn,
                    weights,
                    interval,
                    enemy =>
                    {
                        // Dynamically scale enemy HP based on difficulty
                        var health = enemy.GetComponent<Health>();
                        if (health != null)
                        {
                            int hp = Mathf.RoundToInt((10 + (currentWave - 1) * 10) * diff);
                            health.SetMaxHealth(hp, true);
                        }
                    });
            }

            // Wait until all enemies are defeated
            while (aliveEnemies > 0)
                yield return new WaitForSeconds(0.5f);

            // Reward player for completing wave
            ResourceManager.Instance?.AddWaveBonus(waveBonusResources);

            // Delay before starting next wave for pacing
            yield return new WaitForSeconds(currentWave <= 2 ? 5f : 3f);
            StartNextWave();
        }

        /// <summary>
        /// Selects enemy variants based on player capability and current difficulty.
        /// </summary>
        private (Units.Enemy[] prefabs, float[] weights) SelectVariantsForPlayerCapability(float diff)
        {
            var gm = GameManager.Instance;
            var baseEnemy = gm?.enemyPrefab;
            var fast = gm?.enemyFastPrefab;
            var ranged = gm?.enemyRangedPrefab;

            // Check if player has unlocked advanced defenders
            bool hasPoisonArcher = FindAnyObjectByType<Units.PoisonArcherConfig>() != null;
            bool hasFireMage = FindAnyObjectByType<Units.FireMageConfig>() != null;

            // Simplify enemies if player is struggling
            bool playerStruggling = diff <= 1.0f && (!hasPoisonArcher || !hasFireMage);

            if (playerStruggling)
            {
                return (baseEnemy != null ? new Units.Enemy[] { baseEnemy } : null,
                        baseEnemy != null ? new float[] { 1f } : null);
            }

            // Otherwise, include a mix of enemy types with weights
            var list = new List<Units.Enemy>();
            var w = new List<float>();
            if (baseEnemy != null) { list.Add(baseEnemy); w.Add(0.5f); }
            if (hasPoisonArcher && fast != null) { list.Add(fast); w.Add(0.25f); }
            if (hasFireMage && ranged != null) { list.Add(ranged); w.Add(0.25f); }

            if (list.Count == 0) return (null, null);
            return (list.ToArray(), w.ToArray());
        }

        /// <summary>
        /// Computes average health ratio of all key defender types.
        /// Used to adjust wave difficulty dynamically.
        /// </summary>
        private float ComputeDefenderHealthFactor()
        {
            var defenders = FindObjectsOfType<Defender>();
            if (defenders.Length == 0)
                return 1f;

            float total = 0f;
            int count = 0;

            foreach (var def in defenders)
            {
                if (def.name.Contains("Defender_Turret") ||
                    def.name.Contains("DefenderFireCannon") ||
                    def.name.Contains("DefenderPoisonBalista"))
                {
                    var health = def.GetComponent<Health>();
                    if (health != null)
                    {
                        total += (float)health.CurrentHealth / health.MaxHealth;
                        count++;
                    }
                }
            }

            return count == 0 ? 1f : total / count;
        }

        /// <summary>
        /// Callback for when an enemy dies. Decrements alive count and triggers next wave if cleared.
        /// </summary>
        public void OnEnemyDied()
        {
            if (aliveEnemies > 0) aliveEnemies--;
            if (aliveEnemies == 0)
                Debug.Log("[WaveManager] All enemies cleared!");
        }
    }
}