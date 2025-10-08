using CrystalDefenders.Generation;
using CrystalDefenders.Units;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CrystalDefenders.Gameplay
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("Scene References")] public ProceduralTerrainGenerator terrainGenerator;
        public EnemySpawner spawnerPrefab; public Enemy enemyPrefab; public Enemy enemyFastPrefab; public Enemy enemyRangedPrefab; public Defender defenderPrefab; public Tower towerPrefab;
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
            if (terrainGenerator == null)
            {
                //Debug.LogError("GameManager: No ProceduralTerrainGenerator found!");
                return;
            }
            
            //Debug.Log("GameManager: Starting terrain generation...");
            terrainGenerator.GenerateTerrainAndPaths();
            
            var paths = terrainGenerator.PathWaypoints;
            var spawns = terrainGenerator.SpawnPositions;
            //Debug.Log($"GameManager: Terrain generated - {paths.Count} paths, {spawns.Count} spawn positions");

            SpawnTowerAtHub();
            CreateSpawners();
            CreatePlacementNodes();
            
            // Ensure path decoration happens after everything is set up
            DecoratePathVisuals();

            // Start wave system
            // Optionally configure wave rules with variants present in scene/prefabs via inspector
            WaveManager.Instance.StartNextWave();
        }

        private void SpawnTowerAtHub()
        {
            if (towerPrefab == null)
            {
                //Debug.LogError("Tower prefab not assigned on GameManager");
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
            //Debug.Log($"GameManager: Creating spawners for {paths.Count} paths");
            
            for (int i = 0; i < paths.Count; i++)
            {
                Vector3 spawnPos = terrainGenerator.SpawnPositions[i];
                var spawner = Instantiate(spawnerPrefab, spawnPos, Quaternion.identity);
                spawner.name = $"EnemySpawner_{i}";
                spawner.enemyPrefab = enemyPrefab;
                spawner.pathIndex = i;
                // Convert IReadOnlyList<Vector3> to List<Vector3> for the spawner
                var pathList = new List<Vector3>(paths[i]);
                spawner.Initialize(pathList);
                spawners.Add(spawner);
                WaveManager.Instance.RegisterSpawner(spawner);
                //Debug.Log($"GameManager: Created spawner {i} at {spawnPos} with {pathList.Count} waypoints");
            }
        }

        private void DecoratePathVisuals()
        {
            // Invoke any PathTileDecorator in the scene after terrain and spawners exist
            var decorators = FindObjectsOfType<Generation.PathTileDecorator>();
           // Debug.Log($"GameManager: Found {decorators.Length} PathTileDecorators to update");
            
            foreach (var deco in decorators)
            {
                if (deco != null)
                {
                    //Debug.Log($"GameManager: Updating PathTileDecorator {deco.name}");
                    deco.DecoratePaths();
                }
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
            Debug.Log("Game Over — Tower destroyed");
            StartCoroutine(GoToGameOver());
        }

        private IEnumerator GoToGameOver()
        {
            yield return new WaitForSeconds(1f); // 1 second delay
            Time.timeScale = 1f;
            SceneManager.LoadScene("GameOver");
        }
    }
}


