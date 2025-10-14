using CrystalDefenders.Combat;
using CrystalDefenders.Units;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace CrystalDefenders.Gameplay
{
    public class WaveManager : MonoBehaviour
    {
        public static WaveManager Instance { get; private set; }

        [Header("Base Wave Settings")]
        [SerializeField] private int baseEnemiesPerSpawner = 3;        // Starting point
        [SerializeField] private int enemiesIncrementPerWave = 2;      // Growth per wave
        [SerializeField] private int waveBonusResources = 100;

        [Header("Spawn Timing Settings")]
        [SerializeField] private float baseSpawnInterval = 2.0f;       // Longer gaps early on
        [SerializeField] private float spawnIntervalAccelerationPerWave = -0.08f; // Faster over time
        [SerializeField] private float earlyWaveSlowMultiplier = 1.5f; // Waves 1–2 slower by 50%

        [Header("Wave Rules")]
        [SerializeField] private int guaranteedCounterWave = 5; // Always spawn all types past this wave

        [Header("UI")]
        [SerializeField] private TMP_Text waveText;

        private readonly List<EnemySpawner> spawners = new List<EnemySpawner>();
        private int currentWave = 0;
        private int aliveEnemies = 0;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        public void RegisterSpawner(EnemySpawner spawner)
        {
            if (spawner != null && !spawners.Contains(spawner))
                spawners.Add(spawner);
        }

        public void StartNextWave()
        {
            currentWave++;
            Debug.Log($"[WaveManager] Starting Wave {currentWave}");

            if (waveText != null)
                waveText.text = $"Wave {currentWave}";

            StartCoroutine(SpawnWaveRoutine());
        }

        private IEnumerator SpawnWaveRoutine()
        {
            aliveEnemies = 0;

            int playerResources = ResourceManager.Instance != null ? ResourceManager.Instance.CurrentResources : 0;
            int towerHealth = GameManager.Instance != null && GameManager.Instance.Tower != null
                ? (GameManager.Instance.Tower.GetComponent<Health>()?.CurrentHealth ?? 1000)
                : 1000;

            // Core difficulty metrics
            float diff = WaveAdaptiveExtensions.ComputeDifficultyMultiplier(currentWave, playerResources, towerHealth);
            float baseSpawnMult = WaveAdaptiveExtensions.ComputeSpawnMultiplier(currentWave, playerResources, towerHealth);
            float defenderHealthFactor = ComputeDefenderHealthFactor();
            float defenderPressureAdj = WaveAdaptiveExtensions.ComputeDefenderPressureAdjustment(defenderHealthFactor);

            float spawnMult = baseSpawnMult * defenderPressureAdj;

            // Adjusted enemy count and spawn timing per wave
            int baseCount = Mathf.Max(1, baseEnemiesPerSpawner + enemiesIncrementPerWave * (currentWave - 1));
            if (currentWave <= 2)
                baseCount = Mathf.Max(1, baseCount - 3); // Early wave ease

            int toSpawnPerSpawner = Mathf.RoundToInt(baseCount * Mathf.Clamp(spawnMult, 0.5f, 2.5f));

            float interval = Mathf.Max(0.5f, baseSpawnInterval + spawnIntervalAccelerationPerWave * (currentWave - 1));
            if (currentWave <= 2)
                interval *= earlyWaveSlowMultiplier;

            // Pick which enemy types to use
            var gm = GameManager.Instance;
            var baseEnemy = gm != null ? gm.enemyPrefab : null;
            var fastEnemy = gm != null ? gm.enemyFastPrefab : null;
            var rangedEnemy = gm != null ? gm.enemyRangedPrefab : null;

            List<Units.Enemy> enemiesToSpawn = new List<Units.Enemy>();
            List<float> weights = new List<float>();

            var variants = SelectVariantsForPlayerCapability(diff);
            if (variants.prefabs != null)
            {
                enemiesToSpawn.AddRange(variants.prefabs);
                weights.AddRange(variants.weights);
            }

            bool forceAllEnemies = WaveAdaptiveExtensions.ShouldSpawnAllEnemyTypes(currentWave, playerResources, towerHealth, guaranteedCounterWave);
            if (forceAllEnemies)
            {
                if (fastEnemy != null && !enemiesToSpawn.Contains(fastEnemy)) { enemiesToSpawn.Add(fastEnemy); weights.Add(0.25f); }
                if (rangedEnemy != null && !enemiesToSpawn.Contains(rangedEnemy)) { enemiesToSpawn.Add(rangedEnemy); weights.Add(0.25f); }
            }

            Debug.Log($"[WaveManager] Wave {currentWave} | Diff={diff:F2} | Count={toSpawnPerSpawner} | Interval={interval:F2}s | PressureAdj={defenderPressureAdj:F2}");

            // Spawn from each spawner
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
                        var health = enemy.GetComponent<Health>();
                        if (health != null)
                        {
                            int hp = Mathf.RoundToInt((10 + (currentWave - 1) * 10) * diff);
                            health.SetMaxHealth(hp, true);
                        }
                    });
            }

            // Wait until all enemies are dead
            while (aliveEnemies > 0)
                yield return new WaitForSeconds(0.5f);

            ResourceManager.Instance?.AddWaveBonus(waveBonusResources);

            // Delay next wave for pacing
            yield return new WaitForSeconds(currentWave <= 2 ? 5f : 3f);
            StartNextWave();
        }

        private (Units.Enemy[] prefabs, float[] weights) SelectVariantsForPlayerCapability(float diff)
        {
            var gm = GameManager.Instance;
            var baseEnemy = gm != null ? gm.enemyPrefab : null;
            var fast = gm != null ? gm.enemyFastPrefab : null;
            var ranged = gm != null ? gm.enemyRangedPrefab : null;

            bool hasPoisonArcher = FindAnyObjectByType<Units.PoisonArcherConfig>() != null;
            bool hasFireMage = FindAnyObjectByType<Units.FireMageConfig>() != null;

            bool playerStruggling = diff <= 1.0f && (!hasPoisonArcher || !hasFireMage);

            if (playerStruggling)
            {
                return (baseEnemy != null ? new Units.Enemy[] { baseEnemy } : null,
                        baseEnemy != null ? new float[] { 1f } : null);
            }

            var list = new List<Units.Enemy>();
            var w = new List<float>();
            if (baseEnemy != null) { list.Add(baseEnemy); w.Add(0.5f); }
            if (hasPoisonArcher && fast != null) { list.Add(fast); w.Add(0.25f); }
            if (hasFireMage && ranged != null) { list.Add(ranged); w.Add(0.25f); }

            if (list.Count == 0) return (null, null);
            return (list.ToArray(), w.ToArray());
        }

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

            if (count == 0) return 1f;
            return total / count;
        }

        public void OnEnemyDied()
        {
            if (aliveEnemies > 0) aliveEnemies--;
            if (aliveEnemies == 0)
                Debug.Log("[WaveManager] All enemies cleared!");
        }
    }
}