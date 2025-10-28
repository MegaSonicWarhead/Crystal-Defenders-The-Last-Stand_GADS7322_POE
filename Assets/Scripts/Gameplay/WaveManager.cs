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
    /// Handles enemy selection, spawn timing, defender pressure, boss spawning, and wave progression.
    /// </summary>
    public class WaveManager : MonoBehaviour
    {
        public static WaveManager Instance { get; private set; }

        [Header("Base Wave Settings")]
        [SerializeField] private int baseEnemiesPerSpawner = 3;
        [SerializeField] private int enemiesIncrementPerWave = 2;
        [SerializeField] private int waveBonusResources = 100;

        [Header("Spawn Timing Settings")]
        [SerializeField] private float baseSpawnInterval = 2.0f;
        [SerializeField] private float spawnIntervalAccelerationPerWave = -0.08f;
        [SerializeField] private float earlyWaveSlowMultiplier = 1.5f;

        [Header("Wave Rules")]
        [SerializeField] private int guaranteedCounterWave = 5;
        [SerializeField] private int bossWaveInterval = 5; // Boss every 5 waves

        [Header("Prefabs")]
        [SerializeField] private BossEnemy bossEnemyPrefab; // Procedural adaptive boss

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

            // Boss wave check
            if (currentWave % bossWaveInterval == 0 && bossEnemyPrefab != null)
                StartCoroutine(SpawnBossWaveRoutine());
            else
                StartCoroutine(SpawnWaveRoutine());
        }

        /// <summary>
        /// Normal wave routine — adaptive difficulty & scaling.
        /// </summary>
        private IEnumerator SpawnWaveRoutine()
        {
            aliveEnemies = 0;

            int playerResources = ResourceManager.Instance != null ? ResourceManager.Instance.CurrentResources : 0;
            int towerHealth = GameManager.Instance?.Tower?.GetComponent<Health>()?.CurrentHealth ?? 1000;

            float diff = WaveAdaptiveExtensions.ComputeDifficultyMultiplier(currentWave, playerResources, towerHealth);
            float baseSpawnMult = WaveAdaptiveExtensions.ComputeSpawnMultiplier(currentWave, playerResources, towerHealth);
            float defenderHealthFactor = ComputeDefenderHealthFactor();
            float defenderPressureAdj = WaveAdaptiveExtensions.ComputeDefenderPressureAdjustment(defenderHealthFactor);

            float spawnMult = baseSpawnMult * defenderPressureAdj;

            int baseCount = Mathf.Max(1, baseEnemiesPerSpawner + enemiesIncrementPerWave * (currentWave - 1));
            if (currentWave <= 2)
                baseCount = Mathf.Max(1, baseCount - 3);

            int toSpawnPerSpawner = Mathf.RoundToInt(baseCount * Mathf.Clamp(spawnMult, 0.5f, 2.5f));

            float interval = Mathf.Max(0.5f, baseSpawnInterval + spawnIntervalAccelerationPerWave * (currentWave - 1));
            if (currentWave <= 2)
                interval *= earlyWaveSlowMultiplier;

            var variants = SelectVariantsForPlayerCapability(diff);
            List<Enemy> enemiesToSpawn = new List<Enemy>();
            List<float> weights = new List<float>();

            if (variants.prefabs != null)
            {
                enemiesToSpawn.AddRange(variants.prefabs);
                weights.AddRange(variants.weights);
            }

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

            while (aliveEnemies > 0)
                yield return new WaitForSeconds(0.5f);

            ResourceManager.Instance?.AddWaveBonus(waveBonusResources);

            yield return new WaitForSeconds(currentWave <= 2 ? 5f : 3f);
            StartNextWave();
        }

        /// <summary>
        /// Special boss wave routine with adaptive scaling.
        /// </summary>
        private IEnumerator SpawnBossWaveRoutine()
        {
            Debug.Log($"[WaveManager] ⚔ Boss Wave {currentWave} incoming!");
            aliveEnemies = 1;

            int playerResources = ResourceManager.Instance != null ? ResourceManager.Instance.CurrentResources : 0;
            int towerHealth = GameManager.Instance?.Tower?.GetComponent<Health>()?.CurrentHealth ?? 1000;
            float diff = WaveAdaptiveExtensions.ComputeDifficultyMultiplier(currentWave, playerResources, towerHealth);
            float defenderHealthFactor = ComputeDefenderHealthFactor();

            if (spawners.Count == 0 || bossEnemyPrefab == null)
            {
                Debug.LogWarning("[WaveManager] No spawners or boss prefab available!");
                yield break;
            }

            var spawner = spawners[Random.Range(0, spawners.Count)];
            var bossInstance = Instantiate(bossEnemyPrefab, spawner.transform.position, Quaternion.identity);
            bossInstance.ConfigureProceduralStats(diff, defenderHealthFactor, currentWave);

            var h = bossInstance.GetComponent<Health>();
            if (h != null)
                h.onDeath.AddListener(OnEnemyDied);

            while (aliveEnemies > 0)
                yield return new WaitForSeconds(1f);

            Debug.Log($"[WaveManager] 🏆 Boss of Wave {currentWave} defeated!");
            ResourceManager.Instance?.AddWaveBonus(waveBonusResources * 3);

            yield return new WaitForSeconds(5f);
            StartNextWave();
        }

        private (Enemy[] prefabs, float[] weights) SelectVariantsForPlayerCapability(float diff)
        {
            var gm = GameManager.Instance;
            var baseEnemy = gm?.enemyPrefab;
            var fast = gm?.enemyFastPrefab;
            var ranged = gm?.enemyRangedPrefab;

            bool hasPoisonArcher = FindAnyObjectByType<PoisonArcherConfig>() != null;
            bool hasFireMage = FindAnyObjectByType<FireMageConfig>() != null;

            bool playerStruggling = diff <= 1.0f && (!hasPoisonArcher || !hasFireMage);

            if (playerStruggling)
            {
                return (baseEnemy != null ? new Enemy[] { baseEnemy } : null,
                        baseEnemy != null ? new float[] { 1f } : null);
            }

            var list = new List<Enemy>();
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

            return count == 0 ? 1f : total / count;
        }

        public void OnEnemyDied()
        {
            if (aliveEnemies > 0) aliveEnemies--;
            if (aliveEnemies == 0)
                Debug.Log("[WaveManager] All enemies cleared!");
        }
    }
}