using CrystalDefenders.Generation;
using CrystalDefenders.Units;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CrystalDefenders.Gameplay
{
    /// <summary>
    /// Main game manager controlling terrain, spawners, tower, defender placement, and wave progression.
    /// Implements a singleton pattern for global access.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        // --- Singleton instance ---
        public static GameManager Instance { get; private set; }

        [Header("Scene References")]
        public ProceduralTerrainGenerator terrainGenerator; // Terrain & paths generator
        public EnemySpawner spawnerPrefab;                   // Prefab for enemy spawners
        public Enemy enemyPrefab;                            // Default enemy prefab
        public Enemy enemyFastPrefab;                        // Fast enemy prefab
        public Enemy enemyRangedPrefab;                      // Ranged enemy prefab
        public Defender defenderPrefab;                      // Default defender prefab
        public Tower towerPrefab;                            // Main tower prefab
        public DefenderPlacementManager placementManager;    // Manages defender placement nodes

        /// <summary>
        /// Reference to the tower instance in the scene.
        /// </summary>
        public Transform Tower { get; private set; }

        private readonly List<EnemySpawner> spawners = new List<EnemySpawner>();

        private void Awake()
        {
            // Singleton enforcement: destroy duplicates
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            InitializeGame();
        }

        /// <summary>
        /// Sets up the entire game scene:
        /// generates terrain, spawners, tower, placement nodes, path decorations, and starts waves.
        /// </summary>
        public void InitializeGame()
        {
            // Find terrain generator if not assigned
            if (terrainGenerator == null)
                terrainGenerator = FindObjectOfType<ProceduralTerrainGenerator>();
            if (terrainGenerator == null)
                return;

            // Generate terrain and paths
            terrainGenerator.GenerateTerrainAndPaths();

            // Spawn the main tower at the hub
            SpawnTowerAtHub();

            // Instantiate enemy spawners along paths
            CreateSpawners();

            // Generate defender placement nodes
            CreatePlacementNodes();

            // Apply visual decorations to paths
            DecoratePathVisuals();

            // Start the wave system
            WaveManager.Instance.StartNextWave();
        }

        /// <summary>
        /// Instantiates the main tower at the terrain hub.
        /// </summary>
        private void SpawnTowerAtHub()
        {
            if (towerPrefab == null) return;

            var tower = Instantiate(towerPrefab, terrainGenerator.HubWorldPosition, Quaternion.identity);
            Tower = tower.transform;
        }

        /// <summary>
        /// Creates enemy spawners for each path and registers them with the wave manager.
        /// </summary>
        private void CreateSpawners()
        {
            // Clear existing spawners
            foreach (var s in spawners)
                if (s != null) Destroy(s.gameObject);
            spawners.Clear();

            var paths = terrainGenerator.PathWaypoints;

            for (int i = 0; i < paths.Count; i++)
            {
                Vector3 spawnPos = terrainGenerator.SpawnPositions[i];

                // Instantiate spawner prefab at path start
                var spawner = Instantiate(spawnerPrefab, spawnPos, Quaternion.identity);
                spawner.name = $"EnemySpawner_{i}";
                spawner.enemyPrefab = enemyPrefab;
                spawner.pathIndex = i;

                // Convert path to List<Vector3> for spawner
                var pathList = new List<Vector3>(paths[i]);
                spawner.Initialize(pathList);

                spawners.Add(spawner);
                WaveManager.Instance.RegisterSpawner(spawner);
            }
        }

        /// <summary>
        /// Invokes all PathTileDecorators in the scene to update path visuals after terrain generation.
        /// </summary>
        private void DecoratePathVisuals()
        {
            var decorators = FindObjectsOfType<Generation.PathTileDecorator>();
            foreach (var deco in decorators)
            {
                if (deco != null)
                    deco.DecoratePaths();
            }
        }

        /// <summary>
        /// Generates defender placement nodes and ensures each node has a defender prefab assigned.
        /// </summary>
        private void CreatePlacementNodes()
        {
            if (placementManager == null)
                placementManager = FindObjectOfType<DefenderPlacementManager>();

            placementManager.CreateNodes(terrainGenerator);

            // Assign default defender prefab to any unassigned nodes
            foreach (var node in FindObjectsOfType<PlacementNode>())
            {
                if (node.defenderPrefab == null)
                    node.defenderPrefab = defenderPrefab;
            }
        }

        /// <summary>
        /// Called when an enemy dies; notifies the wave manager.
        /// </summary>
        public void OnEnemyDied()
        {
            WaveManager.Instance?.OnEnemyDied();
        }

        /// <summary>
        /// Called when the tower is destroyed; triggers game over sequence.
        /// </summary>
        public void OnTowerDestroyed()
        {
            Debug.Log("Game Over — Tower destroyed");
            StartCoroutine(GoToGameOver());
        }

        /// <summary>
        /// Coroutine to transition to the GameOver scene after a short delay.
        /// </summary>
        private IEnumerator GoToGameOver()
        {
            yield return new WaitForSeconds(1f);
            Time.timeScale = 1f;
            SceneManager.LoadScene("GameOver");
        }
    }
}