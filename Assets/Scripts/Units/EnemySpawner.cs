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

        public void Initialize(IList<Vector3> pathWaypoints)
        {
            assignedPath = pathWaypoints;
        }

        public void BeginSpawning(int totalToSpawn)
        {
            if (spawningRoutine != null) StopCoroutine(spawningRoutine);
            spawningRoutine = StartCoroutine(SpawnRoutine(totalToSpawn));
        }

        private IEnumerator SpawnRoutine(int total)
        {
            int spawned = 0;
            while (spawned < total)
            {
                SpawnOne();
                spawned++;
                yield return new WaitForSeconds(spawnIntervalSeconds);
            }
        }

        private void SpawnOne()
        {
            if (enemyPrefab == null || assignedPath == null || assignedPath.Count == 0) return;
            Vector3 spawnPos = transform.position;
            var enemy = Instantiate(enemyPrefab, spawnPos, Quaternion.identity);
            enemy.SetPath(assignedPath);
        }
    }
}


