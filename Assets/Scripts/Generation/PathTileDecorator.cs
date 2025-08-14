using System.Collections.Generic;
using UnityEngine;

namespace CrystalDefenders.Generation
{
	[DisallowMultipleComponent]
	public class PathTileDecorator : MonoBehaviour
	{
		[Header("References")] public ProceduralTerrainGenerator generator; public Transform container; public Transform propsContainer;
		[Header("Path Prefabs")] public GameObject straightTilePrefab; public GameObject straightSlopePrefab; public GameObject cornerTilePrefab; public GameObject endTilePrefab; public GameObject transitionTilePrefab; public GameObject spawnTilePrefab; public GameObject hubTilePrefab;
		[Header("Options")] public float yOffset = 0.02f; public bool decorateOnStart = true; public bool clearOld = true; public bool useSpawnerTransforms = true;
		[Header("Rotation Offsets (degrees)")] public float straightYawOffset = 0f; public float cornerYawOffset = 270f; public float endYawOffset = 0f; public float slopeYawOffset = 0f;
		[Header("Slope Settings")] public bool useStraightSlopeTiles = true; public float slopeHeightThreshold = 0.15f; public bool pitchSlopeTiles = false; public float maxSlopePitchDegrees = 20f; public bool invertSlopePitch = false;
		[Tooltip("If true, slope tiles rotate 180° when the elevation decreases along the tile's forward axis (so the visual ramp faces uphill).")]
		public bool flipSlopeYawWhenDescending = true;
		[Header("Prop Decoration")] public bool decorateProps = false; public GameObject[] treePrefabs; public GameObject[] rockPrefabs; public GameObject[] detailPrefabs; public int treeCount = 24; public int rockCount = 12; public int detailCount = 10; public float propYOffset = 0.0f; public Vector2 propUniformScaleRange = new Vector2(0.8f, 1.2f);

		private bool hasDecorated = false;
		public bool HasDecorated => hasDecorated;

		[Header("Transition Tiles")] public bool placeTransitions = false; public Transform transitionsContainer; public float transitionYawOffset = 0f; public float transitionYOffset = 0.02f;

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
			if (decorateOnStart)
			{
				DecoratePaths();
			}
		}

		public void DecoratePaths()
		{
			if (generator == null) generator = FindObjectOfType<ProceduralTerrainGenerator>();
			if (generator == null) return;
			if (hasDecorated && !clearOld) return;

			// Ensure terrain and paths are generated before decorating
			var paths = generator.PathWaypoints;
			if (paths == null || paths.Count == 0)
			{
				generator.GenerateTerrainAndPaths();
				paths = generator.PathWaypoints;
			}

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
				for (int pIndex = 0; pIndex < paths.Count; pIndex++)
				{
					var path = paths[pIndex];
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
			}

			// Place path tiles with rotation and corner detection
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
				}
			}

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
					foreach (var spawner in spawners)
					{
						var p = spawner.transform.position; p.y += yOffset;
						Instantiate(spawnTilePrefab, p, Quaternion.identity, parent);
					}
				}
				else
				{
					foreach (var sp in generator.SpawnPositions)
					{
						var pos = new Vector3(sp.x, sp.y + yOffset, sp.z);
						Instantiate(spawnTilePrefab, pos, Quaternion.identity, parent);
					}
				}
			}

			// Hub marker
			if (hubTilePrefab != null)
			{
				var h = generator.HubWorldPosition;
				var pos = new Vector3(h.x, h.y + yOffset, h.z);
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

			PlaceRandomFromSet(generator.GetCandidatePlacementNodes(Mathf.Max(treeCount, 0)), treePrefabs, treeCount, parent);
			PlaceRandomFromSet(generator.GetCandidatePlacementNodes(Mathf.Max(rockCount, 0)), rockPrefabs, rockCount, parent);
			PlaceRandomFromSet(generator.GetCandidatePlacementNodes(Mathf.Max(detailCount, 0)), detailPrefabs, detailCount, parent);
		}

		private void PlaceRandomFromSet(List<Vector3> positions, GameObject[] prefabSet, int count, Transform parent)
		{
			if (prefabSet == null || prefabSet.Length == 0 || count <= 0 || positions == null || positions.Count == 0) return;
			int placed = 0;
			int idx = 0;
			while (placed < count && positions.Count > 0)
			{
				var pos = positions[idx % positions.Count];
				idx++;
				var prefab = prefabSet[Random.Range(0, prefabSet.Length)];
				if (prefab == null) continue;
				var p = new Vector3(pos.x, pos.y + propYOffset, pos.z);
				var rot = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
				var go = Instantiate(prefab, p, rot, parent);
				float s = Random.Range(propUniformScaleRange.x, propUniformScaleRange.y);
				go.transform.localScale = new Vector3(s, s, s);
				placed++;
			}
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


