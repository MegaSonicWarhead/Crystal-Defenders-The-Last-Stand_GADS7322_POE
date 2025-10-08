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
        [SerializeField] private float spawnIntervalAccelerationPerWave = -0.1f; // negative to speed up

        [SerializeField] private TMP_Text waveText; // <-- UI Text to show wave number

        // Manual wave rules removed; spawning is fully adaptive

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
            currentWave++;

            // Update the wave UI
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
                // Adaptive difficulty adjustment
                int playerResources = ResourceManager.Instance != null ? ResourceManager.Instance.CurrentResources : 0;
                int towerHealth = GameManager.Instance != null && GameManager.Instance.Tower != null
                    ? (GameManager.Instance.Tower.GetComponent<Health>()?.CurrentHealth ?? 1000)
                    : 1000;
                float diff = WaveAdaptiveExtensions.ComputeDifficultyMultiplier(currentWave, playerResources, towerHealth);
                count = Mathf.RoundToInt(count * Mathf.Clamp(diff, 0.6f, 1.8f));
                aliveEnemies += count;
                float interval = Mathf.Max(0.1f, baseSpawnInterval + spawnIntervalAccelerationPerWave * (currentWave - 1));
                // interval can be tuned adaptively later if needed

                // Auto variant selection based on capability
                var variants = SelectVariantsForPlayerCapability(diff);
                var weights = variants.weights;
                var enemies = variants.prefabs;
                if (enemies != null && enemies.Length > 0)
                {
                    spawner.BeginSpawning(count, enemies, weights, interval, enemy =>
                    {
                        var health = enemy.GetComponent<Health>();
                        if (health != null)
                            health.SetMaxHealth(Mathf.RoundToInt((10 + (currentWave - 1) * 10) * diff), true);
                    });
                }
                else
                {
                    spawner.BeginSpawning(count, null, null, interval, enemy =>
                    {
                        var health = enemy.GetComponent<Health>();
                        if (health != null)
                            health.SetMaxHealth(Mathf.RoundToInt((10 + (currentWave - 1) * 10) * diff), true);
                    });
                }
            }

            // Wait until all enemies are dead
            while (aliveEnemies > 0)
            {
                yield return new WaitForSeconds(0.5f);
            }

            ResourceManager.Instance?.AddWaveBonus(waveBonusResources);
        }

        // Removed GetRuleForCurrentWave and GetExtraEnemiesForCurrentWave

        // Determine enemy variants based on capability proxy (diff) and presence of counters
        private (Units.Enemy[] prefabs, float[] weights) SelectVariantsForPlayerCapability(float diff)
        {
            // Locate prefabs via GameManager references if available
            var gm = GameManager.Instance;
            var baseEnemy = gm != null ? gm.enemyPrefab : null;
            var fast = gm != null ? gm.enemyFastPrefab : null;
            var ranged = gm != null ? gm.enemyRangedPrefab : null;

            bool hasPoisonArcher = FindAnyObjectByType<Units.PoisonArcherConfig>() != null;
            bool hasFireMage = FindAnyObjectByType<Units.FireMageConfig>() != null;

            bool playerStruggling = diff <= 1.0f && (!hasPoisonArcher || !hasFireMage);

            if (playerStruggling)
            {
                return (baseEnemy != null ? new Units.Enemy[] { baseEnemy } : null, baseEnemy != null ? new float[] { 1f } : null);
            }

            // Player performing well: include counters only if player has matching defender types
            var list = new System.Collections.Generic.List<Units.Enemy>();
            var w = new System.Collections.Generic.List<float>();
            if (baseEnemy != null) { list.Add(baseEnemy); w.Add(0.5f); }
            if (hasPoisonArcher && fast != null) { list.Add(fast); w.Add(0.25f); }
            if (hasFireMage && ranged != null) { list.Add(ranged); w.Add(0.25f); }

            if (list.Count == 0) return (null, null);
            return (list.ToArray(), w.ToArray());
        }

        // Called externally when an enemy dies
        public void OnEnemyDied()
        {
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


