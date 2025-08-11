using System.Collections.Generic;
using UnityEngine;
using CrystalDefenders.Generation;
using CrystalDefenders.Units;

namespace CrystalDefenders.Gameplay
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("Scene References")] public ProceduralTerrainGenerator terrainGenerator;
        public EnemySpawner spawnerPrefab; public Enemy enemyPrefab; public Defender defenderPrefab; public Tower towerPrefab;
        public DefenderPlacementManager placementManager;

        public Transform Tower { get; private set; }

        private readonly List<EnemySpawner> spawners = new List<EnemySpawner>();

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void Start()
        {
            InitializeGame();
        }

        public void InitializeGame()
        {
            if (terrainGenerator == null) terrainGenerator = FindObjectOfType<ProceduralTerrainGenerator>();
            terrainGenerator.GenerateTerrainAndPaths();

            SpawnTowerAtHub();
            CreateSpawners();
            CreatePlacementNodes();

            // Start wave system
            WaveManager.Instance.StartNextWave();
        }

        private void SpawnTowerAtHub()
        {
            if (towerPrefab == null)
            {
                Debug.LogError("Tower prefab not assigned on GameManager");
                return;
            }
            var tower = Instantiate(towerPrefab, terrainGenerator.HubWorldPosition, Quaternion.identity);
            Tower = tower.transform;
        }

        private void CreateSpawners()
        {
            // Clear existing
            foreach (var s in spawners) if (s != null) Destroy(s.gameObject);
            spawners.Clear();

            var paths = terrainGenerator.PathWaypoints;
            for (int i = 0; i < paths.Count; i++)
            {
                Vector3 spawnPos = terrainGenerator.SpawnPositions[i];
                var spawner = Instantiate(spawnerPrefab, spawnPos, Quaternion.identity);
                spawner.enemyPrefab = enemyPrefab;
                spawner.pathIndex = i;
                spawner.Initialize(paths[i] as IList<Vector3>);
                spawners.Add(spawner);
                WaveManager.Instance.RegisterSpawner(spawner);
            }
        }

        private void CreatePlacementNodes()
        {
            if (placementManager == null) placementManager = FindObjectOfType<DefenderPlacementManager>();
            placementManager.CreateNodes(terrainGenerator);

            // Ensure node prefab has defender assigned
            foreach (var node in FindObjectsOfType<PlacementNode>())
            {
                if (node.defenderPrefab == null) node.defenderPrefab = defenderPrefab;
            }
        }

        public void OnEnemyDied()
        {
            WaveManager.Instance?.OnEnemyDied();
        }

        public void OnTowerDestroyed()
        {
            // Show Game Over UI here; for now log and stop gameplay
            Debug.Log("Game Over — Tower destroyed");
            Time.timeScale = 0f;
        }
    }
}


