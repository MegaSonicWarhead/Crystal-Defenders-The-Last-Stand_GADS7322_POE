using CrystalDefenders.Combat;
using CrystalDefenders.Generation;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CrystalDefenders.Units
{
    /// <summary>
    /// Handles spawning of enemies with adaptive pacing and optional variant weighting.
    /// Designed to scale progressively with wave number and early-wave easing to prevent player overwhelm.
    /// </summary>
    public class EnemySpawner : MonoBehaviour
    {
        [Header("Base Spawning Settings")]
        [Tooltip("Default enemy prefab used if no variants are defined.")]
        public Enemy enemyPrefab;

        [Tooltip("Linked path index (if using multiple lanes/paths).")]
        public int pathIndex = 0;

        [Header("Wave Scaling Settings")]
        [SerializeField, Tooltip("Base number of enemies spawned in wave 1.")]
        private int baseEnemiesPerWave = 3;

        [SerializeField, Tooltip("Additional enemies added each wave for progressive difficulty.")]
        private int enemiesPerWaveIncrement = 2;

        [SerializeField, Tooltip("Base time interval between spawns.")]
        private float baseSpawnInterval = 2.0f;

        [SerializeField, Tooltip("Spawn interval change per wave (negative value speeds up spawns).")]
        private float spawnAccelerationPerWave = -0.08f;

        [SerializeField, Tooltip("Random ± variance applied to each spawn interval.")]
        private float randomSpawnVariance = 0.15f;

        [SerializeField, Tooltip("Multiplier to slow down waves 1–2 to ease in new players.")]
        private float earlyWaveSlowMultiplier = 1.5f;

        // Assigned path reference
        private IList<Vector3> assignedPath;

        // Coroutine handler for clean stop/restart
        private Coroutine spawningRoutine;

        // Variant spawning lists (optional)
        private IList<Enemy> variantPrefabs;
        private IList<float> variantWeights;
        private float intervalOverride = -1f;
        private Action<Enemy> onEnemySpawned;

        // Wave dynamic values
        private int currentWave = 1;
        private int baseHealth = 10;
        private int healthIncreasePerWave = 10;

        /// <summary>
        /// Sets movement path for all enemies this spawner creates.
        /// </summary>
        public void Initialize(IList<Vector3> pathWaypoints)
        {
            assignedPath = pathWaypoints;
        }

        /// <summary>
        /// Begin spawning standard enemies based on wave number without variants.
        /// </summary>
        public void BeginSpawning(int totalToSpawn, int waveNumber = 1)
        {
            currentWave = waveNumber;
            BeginSpawning(totalToSpawn, null, null, null, null);
        }

        /// <summary>
        /// Begins spawning with optional overrides, variant prefabs, spawn rate adjustments, and a spawn callback.
        /// </summary>
        public void BeginSpawning(int? overrideCount = null,
            IList<Enemy> variants = null, IList<float> weights = null,
            float? intervalSeconds = null, Action<Enemy> onSpawned = null)
        {
            // Reset any existing spawn loop
            if (spawningRoutine != null)
                StopCoroutine(spawningRoutine);

            variantPrefabs = variants;
            variantWeights = weights;
            intervalOverride = intervalSeconds.HasValue ? intervalSeconds.Value : -1f;
            onEnemySpawned = onSpawned;

            // Auto-calculate total count & pacing
            int totalToSpawn = overrideCount ?? ComputeEnemyCount();
            float interval = intervalOverride > 0 ? intervalOverride : ComputeSpawnInterval();

            spawningRoutine = StartCoroutine(SpawnRoutine(totalToSpawn, interval));
        }

        // === WAVE ADAPTIVE CALCULATIONS ===

        /// <summary>
        /// Calculates how many enemies to spawn based on current wave with early-wave easing.
        /// </summary>
        private int ComputeEnemyCount()
        {
            int count = baseEnemiesPerWave + enemiesPerWaveIncrement * (currentWave - 1);

            // Ease in early waves to avoid overwhelming players
            if (currentWave <= 2)
                count = Mathf.Max(1, count - 3);

            return Mathf.Max(1, count);
        }

        /// <summary>
        /// Computes spawn interval with scaling per wave and early-wave slow multiplier.
        /// </summary>
        private float ComputeSpawnInterval()
        {
            float interval = baseSpawnInterval + spawnAccelerationPerWave * (currentWave - 1);
            interval = Mathf.Max(0.5f, interval); // Prevents absurdly fast spawns

            // Apply slow pacing for early waves
            if (currentWave <= 2)
                interval *= earlyWaveSlowMultiplier;

            return interval;
        }

        // === SPAWNING LOGIC ===

        /// <summary>
        /// Coroutine that continues spawning enemies until total spawn count is reached.
        /// Includes random spawn timing variance and introduces slight delay on first spawns.
        /// </summary>
        private IEnumerator SpawnRoutine(int total, float interval)
        {
            int spawned = 0;
            while (spawned < total)
            {
                SpawnOne();
                spawned++;

                // Apply ± variance to individual spawn spacing
                float randomFactor = UnityEngine.Random.Range(1f - randomSpawnVariance, 1f + randomSpawnVariance);
                float wait = interval * randomFactor;

                // Small cinematic delay for first couple of enemies
                if (spawned <= 2)
                    wait *= 1.3f;

                yield return new WaitForSeconds(wait);
            }
        }

        /// <summary>
        /// Spawns a single enemy instance and assigns its path and health scaling.
        /// </summary>
        private void SpawnOne()
        {
            // Validate spawn readiness
            if ((enemyPrefab == null && (variantPrefabs == null || variantPrefabs.Count == 0))
                || assignedPath == null || assignedPath.Count == 0)
                return;

            // Spawn position — use offset from path start for visual spacing
            Vector3 spawnPos = transform.position;
            if (assignedPath.Count > 1)
            {
                Vector3 pathDir = (assignedPath[1] - assignedPath[0]).normalized;
                Vector3 perp = Vector3.Cross(pathDir, Vector3.up).normalized;
                float offsetAmount = 0.5f;
                spawnPos = assignedPath[0] + perp * offsetAmount;
            }

            // Select variant or fallback to default prefab
            var prefab = SelectVariant() ?? enemyPrefab;
            var enemy = Instantiate(prefab, spawnPos, Quaternion.identity);
            enemy.SetPath(assignedPath);

            // Apply health scaling per wave and attach health bar
            var health = enemy.GetComponent<Health>();
            if (health != null)
            {
                int newMaxHealth = baseHealth + (healthIncreasePerWave * (currentWave - 1));
                health.SetMaxHealth(newMaxHealth);
                health.CurrentHealth = newMaxHealth;

                if (UIManager.Instance != null)
                    UIManager.Instance.AttachHealthBar(health);
            }

            // Trigger external callback if defined
            onEnemySpawned?.Invoke(enemy);
        }

        /// <summary>
        /// Selects a variant enemy based on weighted probability, or returns a random pick if weights are invalid.
        /// </summary>
        private Enemy SelectVariant()
        {
            if (variantPrefabs == null || variantPrefabs.Count == 0)
                return null;

            // No weights or invalid weight count — pick randomly
            if (variantWeights == null || variantWeights.Count != variantPrefabs.Count)
                return variantPrefabs[UnityEngine.Random.Range(0, variantPrefabs.Count)];

            // Sum valid weights
            float total = 0f;
            for (int i = 0; i < variantWeights.Count; i++)
                total += Mathf.Max(0f, variantWeights[i]);

            if (total <= 0f)
                return variantPrefabs[UnityEngine.Random.Range(0, variantPrefabs.Count)];

            // Weighted random selection
            float r = UnityEngine.Random.Range(0f, total);
            float accum = 0f;
            for (int i = 0; i < variantPrefabs.Count; i++)
            {
                accum += Mathf.Max(0f, variantWeights[i]);
                if (r <= accum) return variantPrefabs[i];
            }

            // Fallback (should not hit)
            return variantPrefabs[variantPrefabs.Count - 1];
        }
    }
}