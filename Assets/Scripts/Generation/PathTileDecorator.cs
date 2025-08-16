using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace CrystalDefenders.Generation
{
	[DisallowMultipleComponent]
	public class PathTileDecorator : MonoBehaviour
	{
		[Header("References")] public ProceduralTerrainGenerator generator; public Transform container; public Transform propsContainer;
		[Header("Path Prefabs")] public GameObject straightTilePrefab; public GameObject straightSlopePrefab; public GameObject cornerTilePrefab; public GameObject endTilePrefab; public GameObject transitionTilePrefab; public GameObject spawnTilePrefab; public GameObject hubTilePrefab;
		[Header("Options")] public float yOffset = 0.1f; public bool decorateOnStart = true; public bool clearOld = true; public bool useSpawnerTransforms = true;
		[Header("Rotation Offsets (degrees)")] public float straightYawOffset = 0f; public float cornerYawOffset = 270f; public float endYawOffset = 0f; public float slopeYawOffset = 0f;
		[Header("Slope Settings")] public bool useStraightSlopeTiles = true; public float slopeHeightThreshold = 0.15f; public bool pitchSlopeTiles = false; public float maxSlopePitchDegrees = 20f; public bool invertSlopePitch = false;
		[Tooltip("If true, slope tiles rotate 180° when the elevation decreases along the tile's forward axis (so the visual ramp faces uphill).")]
		public bool flipSlopeYawWhenDescending = true;
		        [Header("Prop Decoration")] public bool decorateProps = false; public GameObject[] treePrefabs; public GameObject[] rockPrefabs; public GameObject[] detailPrefabs; public int treeCount = 24; public int rockCount = 12; public int detailCount = 10; public float propYOffset = 0.0f; public Vector2 propUniformScaleRange = new Vector2(0.8f, 1.2f);
        [Header("Prop Placement")] public float minDistanceFromPaths = 2.0f; public float minDistanceBetweenProps = 2.0f; public float minDistanceFromPlacementNodes = 1.5f; public bool randomizePropRotation = true; public bool useTerrainHeight = true;

		private bool hasDecorated = false;
		public bool HasDecorated => hasDecorated;

		[Header("Transition Tiles")] public bool placeTransitions = false; public Transform transitionsContainer; public float transitionYawOffset = 0f; public float transitionYOffset = 0.15f;

		[System.Serializable]
		public enum DecorTileType { Any, Straight, Corner, End, StraightSlope }

		[System.Serializable]
		public class PathTileRule
		{
			public bool enabled = true;
			public string ruleName = "Rule";
			public DecorTileType matchTileType = DecorTileType.Any;
			public int matchPathIndex = -1; // -1 = any
			public int everyNthOnPath = 1; // 1 = every tile
			[Range(0f,1f)] public float randomChance = 1f; // 1 = always
			public GameObject prefabOverride;
			public float addYawOffset = 0f;
		}

		[Header("Tile Rules")] public List<PathTileRule> tileRules = new List<PathTileRule>();

		        private void Start()
        {
            // Don't auto-decorate on start - wait for GameManager to call it
            // This ensures spawners and paths are properly set up first
        }

		        public void DecoratePaths()
        {
            if (generator == null) generator = FindObjectOfType<ProceduralTerrainGenerator>();
            if (generator == null) return;
            if (hasDecorated && !clearOld) return;

            Debug.Log("PathTileDecorator: Starting decoration process...");
            
            // Ensure terrain and paths are generated before decorating
            var paths = generator.PathWaypoints;
            if (paths == null || paths.Count == 0)
            {
                Debug.LogWarning("PathTileDecorator: No paths found, generating terrain...");
                generator.GenerateTerrainAndPaths();
                paths = generator.PathWaypoints;
            }
            
            if (paths == null || paths.Count == 0)
            {
                Debug.LogError("PathTileDecorator: Still no paths after generation!");
                return;
            }
            
            Debug.Log($"PathTileDecorator: Found {paths.Count} paths with {paths.Sum(p => p.Count)} total waypoints");
            
            // Debug: Check if we have the required prefabs
            if (straightTilePrefab == null)
            {
                Debug.LogError("PathTileDecorator: straightTilePrefab is null!");
                return;
            }
            Debug.Log($"PathTileDecorator: straightTilePrefab is assigned: {straightTilePrefab.name}");

			Transform parent = container != null ? container : transform;
			if (clearOld)
			{
				for (int i = parent.childCount - 1; i >= 0; i--)
				{
					var child = parent.GetChild(i).gameObject;
					if (Application.isPlaying) Destroy(child); else DestroyImmediate(child);
				}
				if (propsContainer != null)
				{
					for (int i = propsContainer.childCount - 1; i >= 0; i--)
					{
						var child = propsContainer.GetChild(i).gameObject;
						if (Application.isPlaying) Destroy(child); else DestroyImmediate(child);
					}
				}
				if (transitionsContainer != null)
				{
					for (int i = transitionsContainer.childCount - 1; i >= 0; i--)
					{
						var child = transitionsContainer.GetChild(i).gameObject;
						if (Application.isPlaying) Destroy(child); else DestroyImmediate(child);
					}
				}
			}

			// Build a grid set and representative world positions
			var gridKeys = new HashSet<Vector2Int>();
			var keyToWorld = new Dictionary<Vector2Int, Vector3>();
			var keyToPathIndex = new Dictionary<Vector2Int, int>();
			var keyToStepIndex = new Dictionary<Vector2Int, int>();
			if (paths != null)
			{
				float size = Mathf.Max(0.0001f, generator.tileSize);
				Debug.Log($"PathTileDecorator: Processing paths with tile size {size}");
				
				for (int pIndex = 0; pIndex < paths.Count; pIndex++)
				{
					var path = paths[pIndex];
					Debug.Log($"PathTileDecorator: Processing path {pIndex} with {path.Count} waypoints");
					
					for (int step = 0; step < path.Count; step++)
					{
						var wp = path[step];
						var key = WorldToGridKey(wp, size);
						gridKeys.Add(key);
						keyToWorld[key] = wp; // last seen position is fine for height
						if (!keyToPathIndex.ContainsKey(key)) keyToPathIndex[key] = pIndex;
						if (!keyToStepIndex.ContainsKey(key)) keyToStepIndex[key] = step;
					}
				}
				
				Debug.Log($"PathTileDecorator: Generated {gridKeys.Count} unique grid keys from {paths.Sum(p => p.Count)} waypoints");
			}

			            Debug.Log($"PathTileDecorator: Placing {gridKeys.Count} path tiles");
            
            // Place path tiles with rotation and corner detection
            int tilesPlaced = 0;
            foreach (var key in gridKeys)
            {
                var world = keyToWorld[key];
                var pos = new Vector3(world.x, world.y + yOffset, world.z);
                bool n = gridKeys.Contains(new Vector2Int(key.x, key.y + 1));
                bool s = gridKeys.Contains(new Vector2Int(key.x, key.y - 1));
                bool e = gridKeys.Contains(new Vector2Int(key.x + 1, key.y));
                bool w = gridKeys.Contains(new Vector2Int(key.x - 1, key.y));

				int neighborCount = (n?1:0) + (s?1:0) + (e?1:0) + (w?1:0);
				GameObject prefab = straightTilePrefab;
				float yRot = 0f;
				float pitchX = 0f; float pitchZ = 0f;
				TileType tileType = TileType.Straight;
				DecorTileType decorType = DecorTileType.Straight;

				if (neighborCount == 1)
				{
					prefab = endTilePrefab != null ? endTilePrefab : straightTilePrefab;
					tileType = endTilePrefab != null ? TileType.End : TileType.Straight;
					decorType = endTilePrefab != null ? DecorTileType.End : DecorTileType.Straight;
					if (n) yRot = 0f; else if (e) yRot = 90f; else if (s) yRot = 180f; else if (w) yRot = 270f;
				}
				else if (neighborCount == 2)
				{
					// Straight vs Corner
					bool straightNS = n && s;
					bool straightEW = e && w;
					if (straightNS || straightEW)
					{
						prefab = straightTilePrefab;
						tileType = TileType.Straight;
						decorType = DecorTileType.Straight;
						yRot = straightEW ? 90f : 0f;
						// Optional slope logic for straight segments
						if (useStraightSlopeTiles && straightSlopePrefab != null)
						{
							if (straightNS)
							{
								float hN = keyToWorld[new Vector2Int(key.x, key.y + 1)].y;
								float hS = keyToWorld[new Vector2Int(key.x, key.y - 1)].y;
								float dh = hN - hS;
								if (Mathf.Abs(dh) > slopeHeightThreshold)
								{
									prefab = straightSlopePrefab;
									decorType = DecorTileType.StraightSlope;
									// NS straight: base yaw already 0. Flip if descending north->south
									if (flipSlopeYawWhenDescending && dh < 0f) { yRot += 180f; }
									yRot += slopeYawOffset;
									if (pitchSlopeTiles)
									{
										float angle = Mathf.Atan2(dh, 2f * Mathf.Max(0.0001f, generator.tileSize)) * Mathf.Rad2Deg;
										angle = Mathf.Clamp(angle, -maxSlopePitchDegrees, maxSlopePitchDegrees);
										pitchX = (invertSlopePitch ? angle : -angle);
									}
								}
							}
							else if (straightEW)
							{
								float hE = keyToWorld[new Vector2Int(key.x + 1, key.y)].y;
								float hW = keyToWorld[new Vector2Int(key.x - 1, key.y)].y;
								float dh = hE - hW;
								if (Mathf.Abs(dh) > slopeHeightThreshold)
								{
									prefab = straightSlopePrefab;
									decorType = DecorTileType.StraightSlope;
									// EW straight: base yaw already 90. Flip if descending east->west
									if (flipSlopeYawWhenDescending && dh < 0f) { yRot += 180f; }
									yRot += slopeYawOffset;
									if (pitchSlopeTiles)
									{
										float angle = Mathf.Atan2(dh, 2f * Mathf.Max(0.0001f, generator.tileSize)) * Mathf.Rad2Deg;
										angle = Mathf.Clamp(angle, -maxSlopePitchDegrees, maxSlopePitchDegrees);
										pitchZ = (invertSlopePitch ? angle : -angle);
									}
								}
							}
						}
					}
					else
					{
						// Corner cases
						if (cornerTilePrefab != null) prefab = cornerTilePrefab;
						tileType = cornerTilePrefab != null ? TileType.Corner : TileType.Straight;
						decorType = cornerTilePrefab != null ? DecorTileType.Corner : DecorTileType.Straight;
						// Map:
						// N+E = 0, E+S = 90, S+W = 180, W+N = 270
						if (n && e) yRot = 0f;
						else if (e && s) yRot = 90f;
						else if (s && w) yRot = 180f;
						else if (w && n) yRot = 270f;
					}
				}
				else
				{
					// 3-way or cross: fallback to straight aligned with any NS if available, else EW
					prefab = straightTilePrefab;
					yRot = (n || s) ? 0f : 90f;
				}

				// Apply per-type rotation offset
				switch (tileType)
				{
					case TileType.End:
						yRot += endYawOffset;
						break;
					case TileType.Corner:
						yRot += cornerYawOffset;
						break;
					default:
						yRot += straightYawOffset;
						break;
				}

				// Apply rules (first match wins)
				int pIndex = keyToPathIndex.ContainsKey(key) ? keyToPathIndex[key] : -1;
				int stepIndex = keyToStepIndex.ContainsKey(key) ? keyToStepIndex[key] : -1;
				ApplyTileRules(ref prefab, ref yRot, decorType, pIndex, stepIndex);

				                if (prefab != null)
                {
                    Instantiate(prefab, pos, Quaternion.Euler(pitchX, yRot, pitchZ), parent);
                    tilesPlaced++;
                }
            }
            
            Debug.Log($"PathTileDecorator: Successfully placed {tilesPlaced} path tiles");

			// Transition edge tiles
			if (placeTransitions && transitionTilePrefab != null)
			{
				Transform tParent = transitionsContainer != null ? transitionsContainer : parent;
				foreach (var key in gridKeys)
				{
					var world = keyToWorld[key];
					var basePos = new Vector3(world.x, world.y + transitionYOffset, world.z);
					bool n = gridKeys.Contains(new Vector2Int(key.x, key.y + 1));
					bool s = gridKeys.Contains(new Vector2Int(key.x, key.y - 1));
					bool e = gridKeys.Contains(new Vector2Int(key.x + 1, key.y));
					bool w = gridKeys.Contains(new Vector2Int(key.x - 1, key.y));
					if (!n) Instantiate(transitionTilePrefab, basePos, Quaternion.Euler(0f, 0f + transitionYawOffset, 0f), tParent);
					if (!e) Instantiate(transitionTilePrefab, basePos, Quaternion.Euler(0f, 90f + transitionYawOffset, 0f), tParent);
					if (!s) Instantiate(transitionTilePrefab, basePos, Quaternion.Euler(0f, 180f + transitionYawOffset, 0f), tParent);
					if (!w) Instantiate(transitionTilePrefab, basePos, Quaternion.Euler(0f, 270f + transitionYawOffset, 0f), tParent);
				}
			}

			            // Spawn markers
            if (spawnTilePrefab != null)
            {
                if (useSpawnerTransforms)
                {
                    var spawners = FindObjectsOfType<Units.EnemySpawner>();
                    Debug.Log($"PathTileDecorator: Found {spawners.Length} spawners for spawn markers");
                    foreach (var spawner in spawners)
                    {
                        var p = spawner.transform.position; p.y += yOffset + 0.05f; // Above tiles
                        Instantiate(spawnTilePrefab, p, Quaternion.identity, parent);
                        Debug.Log($"PathTileDecorator: Placed spawn marker at {p} for spawner {spawner.name}");
                    }
                }
                else
                {
                    var spawnPositions = generator.SpawnPositions;
                    Debug.Log($"PathTileDecorator: Using {spawnPositions.Count} generator spawn positions");
                    foreach (var sp in spawnPositions)
                    {
                        var pos = new Vector3(sp.x, sp.y + yOffset + 0.05f, sp.z); // Above tiles
                        Instantiate(spawnTilePrefab, pos, Quaternion.identity, parent);
                        Debug.Log($"PathTileDecorator: Placed spawn marker at {pos}");
                    }
                }
            }

			// Hub marker
			if (hubTilePrefab != null)
			{
				var h = generator.HubWorldPosition;
				var pos = new Vector3(h.x, h.y + yOffset + 0.05f, h.z); // Above tiles
				Instantiate(hubTilePrefab, pos, Quaternion.identity, parent);
			}

			// Optional prop decoration
			if (decorateProps)
			{
				DecorateProps();
			}

			hasDecorated = true;
		}

		private static Vector2Int WorldToGridKey(Vector3 worldCenter, float tileSize)
		{
			// Inverse of GridToWorldCenter: approximate tile indices from world center
			int gx = Mathf.RoundToInt(worldCenter.x / tileSize - 0.5f);
			int gy = Mathf.RoundToInt(worldCenter.z / tileSize - 0.5f);
			return new Vector2Int(gx, gy);
		}

		private enum TileType { Straight, Corner, End }

		        private void DecorateProps()
        {
            if (generator == null) return;
            Transform parent = propsContainer != null ? propsContainer : transform;

            Debug.Log($"PathTileDecorator: Starting prop decoration - Trees: {treeCount}, Rocks: {rockCount}, Details: {detailCount}");

            // Get all available terrain positions, excluding paths and placement nodes
            var availablePositions = GetAvailablePropPositions();
            Debug.Log($"PathTileDecorator: Found {availablePositions.Count} available positions for props");

            if (availablePositions.Count == 0)
            {
                Debug.LogWarning("PathTileDecorator: No available positions for props!");
                return;
            }

            // Place props with collision avoidance
            PlacePropsWithCollisionAvoidance(availablePositions, treePrefabs, treeCount, parent, "Tree");
            PlacePropsWithCollisionAvoidance(availablePositions, rockPrefabs, rockCount, parent, "Rock");
            PlacePropsWithCollisionAvoidance(availablePositions, detailPrefabs, detailCount, parent, "Detail");
        }

        private List<Vector3> GetAvailablePropPositions()
        {
            var positions = new List<Vector3>();
            
            // Generate positions across the entire terrain grid
            for (int y = 0; y < generator.gridHeight; y++)
            {
                for (int x = 0; x < generator.gridWidth; x++)
                {
                    // Skip path tiles
                    if (IsPathTile(x, y)) continue;
                    
                    // Skip placement nodes (areas where defenders can be placed)
                    if (IsNearPlacementNode(x, y)) continue;
                    
                    // Skip areas too close to paths
                    if (IsTooCloseToPath(x, y)) continue;
                    
                    var worldPos = generator.GridToWorldCenter(new Vector2Int(x, y));
                    positions.Add(worldPos);
                }
            }
            
            return positions;
        }

        private bool IsPathTile(int x, int y)
        {
            // Check if this tile is part of a path
            var worldPos = generator.GridToWorldCenter(new Vector2Int(x, y));
            var paths = generator.PathWaypoints;
            
            foreach (var path in paths)
            {
                foreach (var waypoint in path)
                {
                    float distance = Vector3.Distance(worldPos, waypoint);
                    if (distance < generator.tileSize * 0.8f) // Within tile size
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        private bool IsNearPlacementNode(int x, int y)
        {
            // Check if this area is near where placement nodes would be
            var worldPos = generator.GridToWorldCenter(new Vector2Int(x, y));
            
            // If too close to paths, it's near placement nodes
            var paths = generator.PathWaypoints;
            foreach (var path in paths)
            {
                foreach (var waypoint in path)
                {
                    float distance = Vector3.Distance(worldPos, waypoint);
                    if (distance < minDistanceFromPlacementNodes)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        private bool IsTooCloseToPath(int x, int y)
        {
            // Don't place props too close to paths (leave some breathing room)
            var worldPos = generator.GridToWorldCenter(new Vector2Int(x, y));
            
            var paths = generator.PathWaypoints;
            foreach (var path in paths)
            {
                foreach (var waypoint in path)
                {
                    float distance = Vector3.Distance(worldPos, waypoint);
                    if (distance < minDistanceFromPaths)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        private void PlacePropsWithCollisionAvoidance(List<Vector3> availablePositions, GameObject[] prefabSet, int count, Transform parent, string propType)
        {
            if (prefabSet == null || prefabSet.Length == 0 || count <= 0 || availablePositions.Count == 0) return;

            var positions = new List<Vector3>(availablePositions); // Copy to avoid modifying original
            var placedPositions = new List<Vector3>(); // Track where we've placed props
            int placed = 0;
            int maxAttempts = count * 10; // Prevent infinite loops
            int attempts = 0;

            Debug.Log($"PathTileDecorator: Attempting to place {count} {propType}s from {positions.Count} available positions");

            while (placed < count && positions.Count > 0 && attempts < maxAttempts)
            {
                attempts++;
                
                // Pick a random position from available ones
                int randomIndex = Random.Range(0, positions.Count);
                var pos = positions[randomIndex];
                
                // Check if this position is far enough from already placed props
                if (IsPositionValidForProp(pos, placedPositions))
                {
                    var prefab = prefabSet[Random.Range(0, prefabSet.Length)];
                    if (prefab == null) continue;
                    
                    // Calculate final position with terrain height consideration
                    Vector3 finalPos;
                    if (useTerrainHeight)
                    {
                        // Use the terrain height at this position
                        finalPos = new Vector3(pos.x, pos.y + propYOffset, pos.z);
                    }
                    else
                    {
                        // Use a fixed height offset
                        finalPos = new Vector3(pos.x, propYOffset, pos.z);
                    }
                    
                    // Apply rotation
                    Quaternion rot;
                    if (randomizePropRotation)
                    {
                        rot = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
                    }
                    else
                    {
                        rot = Quaternion.identity;
                    }
                    
                    var go = Instantiate(prefab, finalPos, rot, parent);
                    
                    // Random scale
                    float s = Random.Range(propUniformScaleRange.x, propUniformScaleRange.y);
                    go.transform.localScale = new Vector3(s, s, s);
                    
                    // Name the prop for easy identification
                    go.name = $"{propType}_{placed + 1}";
                    
                    placedPositions.Add(pos);
                    placed++;
                    
                    Debug.Log($"PathTileDecorator: Placed {propType} at {finalPos}");
                }
                
                // Remove this position from available ones to avoid infinite loops
                positions.RemoveAt(randomIndex);
            }
            
            Debug.Log($"PathTileDecorator: Successfully placed {placed} {propType}s after {attempts} attempts");
        }

        private bool IsPositionValidForProp(Vector3 position, List<Vector3> existingProps)
        {
            foreach (var existingPos in existingProps)
            {
                float distance = Vector3.Distance(position, existingPos);
                if (distance < minDistanceBetweenProps)
                {
                    return false; // Too close to existing prop
                }
            }
            return true;
        }

		private void OnValidate()
		{
			if (propUniformScaleRange.x <= 0f) propUniformScaleRange.x = 0.1f;
			if (propUniformScaleRange.y < propUniformScaleRange.x) propUniformScaleRange.y = propUniformScaleRange.x;
		}

		private void ApplyTileRules(ref GameObject prefab, ref float yRot, DecorTileType currentType, int pathIndex, int stepIndex)
		{
			if (tileRules == null || tileRules.Count == 0) return;
			foreach (var rule in tileRules)
			{
				if (rule == null || !rule.enabled) continue;
				if (rule.matchTileType != DecorTileType.Any && rule.matchTileType != currentType) continue;
				if (rule.matchPathIndex >= 0 && rule.matchPathIndex != pathIndex) continue;
				int nth = Mathf.Max(1, rule.everyNthOnPath);
				if (stepIndex >= 0 && (stepIndex % nth != 0)) continue;
				if (rule.randomChance < 1f && Random.value > rule.randomChance) continue;
				if (rule.prefabOverride != null) prefab = rule.prefabOverride;
				yRot += rule.addYawOffset;
				break; // first match wins
			}
		}
	}
}


