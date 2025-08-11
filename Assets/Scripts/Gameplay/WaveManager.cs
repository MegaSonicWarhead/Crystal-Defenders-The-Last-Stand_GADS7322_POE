using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CrystalDefenders.Units;

namespace CrystalDefenders.Gameplay
{
    public class WaveManager : MonoBehaviour
    {
        public static WaveManager Instance { get; private set; }

        [SerializeField] private int baseEnemiesPerSpawner = 5;
        [SerializeField] private int enemiesIncrementPerWave = 2;
        [SerializeField] private int waveBonusResources = 100;

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
            int toSpawnPerSpawner = baseEnemiesPerSpawner + enemiesIncrementPerWave * (currentWave - 1);
            StartCoroutine(SpawnWave(toSpawnPerSpawner));
        }

        private IEnumerator SpawnWave(int toSpawnPerSpawner)
        {
            aliveEnemies = 0;

            foreach (var spawner in spawners)
            {
                aliveEnemies += toSpawnPerSpawner;
                spawner.BeginSpawning(toSpawnPerSpawner);
            }

            // Monitor enemy deaths
            while (aliveEnemies > 0)
            {
                // Count by polling registry length vs. spawned? Simpler: listen via GameManager decrement call.
                yield return new WaitForSeconds(0.5f);
            }

            ResourceManager.Instance?.AddWaveBonus(waveBonusResources);
        }

        // Called externally when an enemy dies
        public void OnEnemyDied()
        {
            if (aliveEnemies > 0) aliveEnemies--;
            if (aliveEnemies == 0)
            {
                // Auto-start next wave after brief delay
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


