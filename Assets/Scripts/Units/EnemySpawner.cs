using CrystalDefenders.Generation;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CrystalDefenders.Units
{
    public class EnemySpawner : MonoBehaviour
    {
        
            public Enemy enemyPrefab;
            public int pathIndex = 0;
            public float spawnIntervalSeconds = 1.5f;

            private IList<Vector3> assignedPath;
            private Coroutine spawningRoutine;

            // Variant spawning (optional)
            private IList<Enemy> variantPrefabs;
            private IList<float> variantWeights;
            private float intervalOverride = -1f;

            public void Initialize(IList<Vector3> pathWaypoints)
            {
                assignedPath = pathWaypoints;
            }

            public void BeginSpawning(int totalToSpawn)
            {
                if (spawningRoutine != null) StopCoroutine(spawningRoutine);
                variantPrefabs = null; variantWeights = null; intervalOverride = -1f;
                spawningRoutine = StartCoroutine(SpawnRoutine(totalToSpawn));
            }

            public void BeginSpawning(int totalToSpawn, IList<Enemy> variants, IList<float> weights = null, float? intervalSeconds = null)
            {
                if (spawningRoutine != null) StopCoroutine(spawningRoutine);
                variantPrefabs = variants;
                variantWeights = weights;
                intervalOverride = intervalSeconds.HasValue ? intervalSeconds.Value : -1f;
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

                if (assignedPath.Count > 0)
                {
                    // Offset perpendicular to the path direction at the start
                    Vector3 pathDir = Vector3.zero;
                    if (assignedPath.Count > 1)
                        pathDir = (assignedPath[1] - assignedPath[0]).normalized;

                    // Get a perpendicular vector
                    Vector3 perp = Vector3.Cross(pathDir, Vector3.up).normalized;

                    // Offset based on spawner index or random small value
                    float offsetAmount = 0.5f; // half tile spacing
                    spawnPos = assignedPath[0] + perp * offsetAmount;
                }

                var prefab = SelectVariant();
                if (prefab == null) prefab = enemyPrefab;

                var enemy = Instantiate(prefab, spawnPos, Quaternion.identity);
                enemy.SetPath(assignedPath);
            }

            private Enemy SelectVariant()
            {
                if (variantPrefabs == null || variantPrefabs.Count == 0) return null;
                if (variantWeights == null || variantWeights.Count != variantPrefabs.Count)
                {
                    // Uniform random
                    int i = Random.Range(0, variantPrefabs.Count);
                    return variantPrefabs[i];
                }
                float total = 0f;
                for (int i = 0; i < variantWeights.Count; i++) total += Mathf.Max(0f, variantWeights[i]);
                if (total <= 0f)
                {
                    int i = Random.Range(0, variantPrefabs.Count);
                    return variantPrefabs[i];
                }
                float r = Random.Range(0f, total);
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



