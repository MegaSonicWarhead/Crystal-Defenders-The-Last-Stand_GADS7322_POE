using CrystalDefenders.Combat;
using CrystalDefenders.Generation;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CrystalDefenders.Units
{
    public class EnemySpawner : MonoBehaviour
    {
        [Header("Base Spawning Settings")]
        public Enemy enemyPrefab;
        public int pathIndex = 0;

        [Header("Wave Scaling Settings")]
        [SerializeField] private int baseEnemiesPerWave = 3;           // fewer for early waves
        [SerializeField] private int enemiesPerWaveIncrement = 2;      // +2 per wave
        [SerializeField] private float baseSpawnInterval = 2.0f;       // slower early pacing
        [SerializeField] private float spawnAccelerationPerWave = -0.08f; // small speed-up per wave
        [SerializeField] private float randomSpawnVariance = 0.15f;    // ±15% randomness
        [SerializeField] private float earlyWaveSlowMultiplier = 1.5f; // Wave 1–2 slower by 50%

        private IList<Vector3> assignedPath;
        private Coroutine spawningRoutine;

        // Variant spawning
        private IList<Enemy> variantPrefabs;
        private IList<float> variantWeights;
        private float intervalOverride = -1f;
        private Action<Enemy> onEnemySpawned;

        // Wave info
        private int currentWave = 1;
        private int baseHealth = 10;
        private int healthIncreasePerWave = 10;

        public void Initialize(IList<Vector3> pathWaypoints)
        {
            assignedPath = pathWaypoints;
        }

        public void BeginSpawning(int totalToSpawn, int waveNumber = 1)
        {
            currentWave = waveNumber;
            BeginSpawning(totalToSpawn, null, null, null, null);
        }

        public void BeginSpawning(int? overrideCount = null,
            IList<Enemy> variants = null, IList<float> weights = null,
            float? intervalSeconds = null, Action<Enemy> onSpawned = null)
        {
            if (spawningRoutine != null)
                StopCoroutine(spawningRoutine);

            variantPrefabs = variants;
            variantWeights = weights;
            intervalOverride = intervalSeconds.HasValue ? intervalSeconds.Value : -1f;
            onEnemySpawned = onSpawned;

            // 👇 Auto-calculate count & pacing if not provided
            int totalToSpawn = overrideCount ?? ComputeEnemyCount();
            float interval = ComputeSpawnInterval();

            spawningRoutine = StartCoroutine(SpawnRoutine(totalToSpawn, interval));
        }

        // === NEW adaptive logic ===
        private int ComputeEnemyCount()
        {
            int count = baseEnemiesPerWave + enemiesPerWaveIncrement * (currentWave - 1);

            // Make first waves easier
            if (currentWave <= 2)
                count = Mathf.Max(1, count - 3);

            return Mathf.Max(1, count);
        }

        private float ComputeSpawnInterval()
        {
            float interval = baseSpawnInterval + spawnAccelerationPerWave * (currentWave - 1);
            interval = Mathf.Max(0.5f, interval); // clamp so it never gets too fast

            // Slow down early waves
            if (currentWave <= 2)
                interval *= earlyWaveSlowMultiplier;

            return interval;
        }

        // === Spawn loop ===
        private IEnumerator SpawnRoutine(int total, float interval)
        {
            int spawned = 0;
            while (spawned < total)
            {
                SpawnOne();
                spawned++;

                // Random variation per enemy
                float randomFactor = UnityEngine.Random.Range(1f - randomSpawnVariance, 1f + randomSpawnVariance);
                float wait = interval * randomFactor;

                // Slightly longer gap for first two enemies
                if (spawned <= 2)
                    wait *= 1.3f;

                yield return new WaitForSeconds(wait);
            }
        }

        private void SpawnOne()
        {
            if ((enemyPrefab == null && (variantPrefabs == null || variantPrefabs.Count == 0))
                || assignedPath == null || assignedPath.Count == 0)
                return;

            Vector3 spawnPos = transform.position;

            if (assignedPath.Count > 1)
            {
                Vector3 pathDir = (assignedPath[1] - assignedPath[0]).normalized;
                Vector3 perp = Vector3.Cross(pathDir, Vector3.up).normalized;
                float offsetAmount = 0.5f;
                spawnPos = assignedPath[0] + perp * offsetAmount;
            }

            var prefab = SelectVariant() ?? enemyPrefab;
            var enemy = Instantiate(prefab, spawnPos, Quaternion.identity);
            enemy.SetPath(assignedPath);

            var health = enemy.GetComponent<Health>();
            if (health != null)
            {
                int newMaxHealth = baseHealth + (healthIncreasePerWave * (currentWave - 1));
                health.SetMaxHealth(newMaxHealth);
                health.CurrentHealth = newMaxHealth;

                if (UIManager.Instance != null)
                    UIManager.Instance.AttachHealthBar(health);
            }

            onEnemySpawned?.Invoke(enemy);
        }

        private Enemy SelectVariant()
        {
            if (variantPrefabs == null || variantPrefabs.Count == 0)
                return null;

            if (variantWeights == null || variantWeights.Count != variantPrefabs.Count)
                return variantPrefabs[UnityEngine.Random.Range(0, variantPrefabs.Count)];

            float total = 0f;
            for (int i = 0; i < variantWeights.Count; i++)
                total += Mathf.Max(0f, variantWeights[i]);

            if (total <= 0f)
                return variantPrefabs[UnityEngine.Random.Range(0, variantPrefabs.Count)];

            float r = UnityEngine.Random.Range(0f, total);
            float accum = 0f;
            for (int i = 0; i < variantPrefabs.Count; i++)
            {
                accum += Mathf.Max(0f, variantWeights[i]);
                if (r <= accum) return variantPrefabs[i];
            }

            return variantPrefabs[variantPrefabs.Count - 1];
        }
    }
}