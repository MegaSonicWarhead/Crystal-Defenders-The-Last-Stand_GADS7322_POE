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

        [Header("Spawning Settings")]
        public Enemy enemyPrefab;
        public int pathIndex = 0;
        public float spawnIntervalSeconds = 1.5f;

        private IList<Vector3> assignedPath;
        private Coroutine spawningRoutine;

        // Variant spawning (optional)
        private IList<Enemy> variantPrefabs;
        private IList<float> variantWeights;
        private float intervalOverride = -1f;

        // Callback for newly spawned enemies
        private Action<Enemy> onEnemySpawned;

        // Wave scaling
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

        public void BeginSpawning(int totalToSpawn, IList<Enemy> variants, IList<float> weights = null,
            float? intervalSeconds = null, Action<Enemy> onSpawned = null)
        {
            if (spawningRoutine != null) StopCoroutine(spawningRoutine);

            variantPrefabs = variants;
            variantWeights = weights;
            intervalOverride = intervalSeconds.HasValue ? intervalSeconds.Value : -1f;
            onEnemySpawned = onSpawned;

            spawningRoutine = StartCoroutine(SpawnRoutine(totalToSpawn));
        }

        private IEnumerator SpawnRoutine(int total)
        {
            int spawned = 0;
            while (spawned < total)
            {
                SpawnOne();
                spawned++;
                float wait = intervalOverride > 0f ? intervalOverride : spawnIntervalSeconds;
                yield return new WaitForSeconds(wait);
            }
        }

        private void SpawnOne()
        {
            if ((enemyPrefab == null && (variantPrefabs == null || variantPrefabs.Count == 0))
                || assignedPath == null || assignedPath.Count == 0) return;

            // Offset spawn position slightly to avoid overlapping spawners
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

            // === NEW CODE: Scale enemy health by wave ===
            var health = enemy.GetComponent<Health>();
            if (health != null)
            {
                int newMaxHealth = baseHealth + (healthIncreasePerWave * (currentWave - 1));
                health.SetMaxHealth(newMaxHealth); // you must implement SetMaxHealth in Health.cs
                health.CurrentHealth = newMaxHealth;

                // Attach health bar automatically
                if (UIManager.Instance != null)
                {
                    UIManager.Instance.AttachHealthBar(health);
                }
            }

            // Call the external callback if provided
            onEnemySpawned?.Invoke(enemy);
        }

        private Enemy SelectVariant()
        {
            if (variantPrefabs == null || variantPrefabs.Count == 0) return null;

            if (variantWeights == null || variantWeights.Count != variantPrefabs.Count)
            {
                int i = UnityEngine.Random.Range(0, variantPrefabs.Count);
                return variantPrefabs[i];
            }

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



