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

        [SerializeField] private int baseEnemiesPerSpawner = 5;
        [SerializeField] private int enemiesIncrementPerWave = 2;
        [SerializeField] private int waveBonusResources = 100;
        [SerializeField] private float baseSpawnInterval = 1.5f;
        [SerializeField] private float spawnIntervalAccelerationPerWave = -0.1f;
        [SerializeField] private int guaranteedCounterWave = 5; // wave threshold to always spawn all enemy types

        [SerializeField] private TMP_Text waveText;

        private readonly List<EnemySpawner> spawners = new List<EnemySpawner>();
        private int currentWave = 0;
        private int aliveEnemies = 0;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        public void RegisterSpawner(EnemySpawner spawner)
        {
            if (spawner != null && !spawners.Contains(spawner)) spawners.Add(spawner);
        }

        public void StartNextWave()
        {
            Debug.Log($"Starting Wave {currentWave}");
            currentWave++;

            if (waveText != null)
                waveText.text = $"Wave {currentWave}";

            int toSpawnPerSpawner = baseEnemiesPerSpawner + enemiesIncrementPerWave * (currentWave - 1);
            StartCoroutine(SpawnWave(toSpawnPerSpawner));
        }

        private IEnumerator SpawnWave(int toSpawnPerSpawner)
        {
            aliveEnemies = 0;

            foreach (var spawner in spawners)
            {
                int count = toSpawnPerSpawner;

                int playerResources = ResourceManager.Instance != null ? ResourceManager.Instance.CurrentResources : 0;
                int towerHealth = GameManager.Instance != null && GameManager.Instance.Tower != null
                    ? (GameManager.Instance.Tower.GetComponent<Health>()?.CurrentHealth ?? 1000)
                    : 1000;

                float diff = WaveAdaptiveExtensions.ComputeDifficultyMultiplier(currentWave, playerResources, towerHealth);
                float spawnMult = WaveAdaptiveExtensions.ComputeSpawnMultiplier(currentWave, playerResources, towerHealth);

                count = Mathf.RoundToInt(count * Mathf.Clamp(spawnMult, 0.5f, 2.5f));
                aliveEnemies += count;

                float interval = Mathf.Max(0.1f, baseSpawnInterval + spawnIntervalAccelerationPerWave * (currentWave - 1));

                // Determine which prefabs to spawn
                var gm = GameManager.Instance;
                var baseEnemy = gm != null ? gm.enemyPrefab : null;
                var fastEnemy = gm != null ? gm.enemyFastPrefab : null;
                var rangedShooter = gm != null ? gm.enemyRangedPrefab : null;

                List<Units.Enemy> enemiesToSpawn = new List<Units.Enemy>();
                List<float> weights = new List<float>();

                // Adaptive variant selection
                var variants = SelectVariantsForPlayerCapability(diff);
                if (variants.prefabs != null)
                {
                    enemiesToSpawn.AddRange(variants.prefabs);
                    weights.AddRange(variants.weights);
                }

                // ✅ Adaptive counter logic based on performance & threshold
                bool forceAllEnemies = WaveAdaptiveExtensions.ShouldSpawnAllEnemyTypes(currentWave, playerResources, towerHealth, guaranteedCounterWave);

                if (forceAllEnemies)
                {
                    if (fastEnemy != null && !enemiesToSpawn.Contains(fastEnemy)) { enemiesToSpawn.Add(fastEnemy); weights.Add(0.25f); }
                    if (rangedShooter != null && !enemiesToSpawn.Contains(rangedShooter)) { enemiesToSpawn.Add(rangedShooter); weights.Add(0.25f); }
                }

                spawner.BeginSpawning(count, enemiesToSpawn.ToArray(), weights.ToArray(), interval, enemy =>
                {
                    var health = enemy.GetComponent<Health>();
                    if (health != null)
                        health.SetMaxHealth(Mathf.RoundToInt((10 + (currentWave - 1) * 10) * diff), true);
                });

                Debug.Log($"[WaveManager] Wave {currentWave} | TotalCount={count} | Diff={diff:F2} | SpawnMult={spawnMult:F2} | ForceAll={forceAllEnemies}");
            }

            while (aliveEnemies > 0)
            {
                yield return new WaitForSeconds(0.5f);
            }

            ResourceManager.Instance?.AddWaveBonus(waveBonusResources);
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

        public void OnEnemyDied()
        {
            Debug.Log($"Enemy died. Remaining: {aliveEnemies - 1}");
            if (aliveEnemies > 0) aliveEnemies--;
            if (aliveEnemies == 0)
            {
                StartCoroutine(StartNextWaveDelayed());
            }
        }

        private IEnumerator StartNextWaveDelayed()
        {
            yield return new WaitForSeconds(2f);
            StartNextWave();
        }
    }
}