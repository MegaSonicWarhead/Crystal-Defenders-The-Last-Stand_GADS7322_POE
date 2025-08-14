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
		[SerializeField] private float baseSpawnInterval = 1.5f;
		[SerializeField] private float spawnIntervalAccelerationPerWave = -0.1f; // negative to speed up

		[System.Serializable]
		public class WaveRule
		{
			public int startingWave = 1;
			public int endingWave = 9999;
			public Units.Enemy[] enemyVariants;
			public float[] variantWeights;
			public int extraEnemiesPerSpawner = 0;
			public float spawnInterval = -1f; // -1 = use computed
		}

		[SerializeField] private List<WaveRule> waveRules = new List<WaveRule>();

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
				int count = toSpawnPerSpawner + GetExtraEnemiesForCurrentWave();
				aliveEnemies += count;
				var rule = GetRuleForCurrentWave();
				float interval = Mathf.Max(0.1f, baseSpawnInterval + spawnIntervalAccelerationPerWave * (currentWave - 1));
				if (rule != null && rule.spawnInterval > 0f) interval = rule.spawnInterval;
				if (rule != null && rule.enemyVariants != null && rule.enemyVariants.Length > 0)
				{
					spawner.BeginSpawning(count, rule.enemyVariants, rule.variantWeights, interval);
				}
				else
				{
					spawner.BeginSpawning(count);
				}
            }

            // Monitor enemy deaths
            while (aliveEnemies > 0)
            {
                // Count by polling registry length vs. spawned? Simpler: listen via GameManager decrement call.
                yield return new WaitForSeconds(0.5f);
            }

            ResourceManager.Instance?.AddWaveBonus(waveBonusResources);
        }

		private WaveRule GetRuleForCurrentWave()
		{
			foreach (var r in waveRules)
			{
				if (r == null) continue;
				if (currentWave >= r.startingWave && currentWave <= r.endingWave) return r;
			}
			return null;
		}

		private int GetExtraEnemiesForCurrentWave()
		{
			var r = GetRuleForCurrentWave();
			return r != null ? r.extraEnemiesPerSpawner : 0;
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


