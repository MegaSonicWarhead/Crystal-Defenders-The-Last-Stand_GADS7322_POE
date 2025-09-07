using System;
using System.Collections.Generic;
using UnityEngine;



    namespace CrystalDefenders.Generation
    {
        [RequireComponent(typeof(MeshFilter))]
        [RequireComponent(typeof(MeshRenderer))]
        [DisallowMultipleComponent]
        public class ProceduralTerrainGenerator : MonoBehaviour
        {
            [Header("Grid Settings")]
            public int gridWidth = 20;
            public int gridHeight = 20;
            public float tileSize = 1f;

            [Header("Heightmap Noise")]
            public float noiseScale = 4f;
            public float elevationScale = 3f;
            public int seed = 0;
            public bool randomizeSeedOnAwake = true;

            [Header("Paths")]
            public int minPathCount = 3;
            public int pathWideningRadiusTiles = 2;
            public float pathFlattenHeight = 0.05f;
            public float minPathSeparation = 1f;

            [Header("Path Smoothing")]
            public bool smoothPaths = true;
            public int pathSmoothingIterations = 5;
            public float pathSmoothingStrength = 0.95f;
            public float pathSmoothingRadius = 3f;

            [Header("Aggressive Flattening")]
            public bool useAggressiveFlattening = true;
            public float flatteningRadius = 2.5f;
            public float flatteningStrength = 1.0f;
            public int flatteningIterations = 3;

            private float[,] heightMap; // size [gridWidth+1, gridHeight+1] for vertex heights
            private bool[,] isPathTile; // size [gridWidth, gridHeight]
            private Vector2Int hubGrid; // central tile coordinate
            private readonly List<List<Vector2Int>> pathsGrid = new List<List<Vector2Int>>();
            private readonly List<Vector3> spawnWorldPositions = new List<Vector3>();
            private readonly List<List<Vector3>> pathsWorld = new List<List<Vector3>>();

            public Vector3 HubWorldPosition => GridToWorldCenter(hubGrid);
            public IReadOnlyList<Vector3> SpawnPositions => spawnWorldPositions;
            public IReadOnlyList<IReadOnlyList<Vector3>> PathWaypoints => pathsWorld;

            private System.Random pseudoRandom;

            private void Awake()
            {
                if (randomizeSeedOnAwake || seed == 0)
                {
                    seed = UnityEngine.Random.Range(int.MinValue, int.MaxValue);
                }
                pseudoRandom = new System.Random(seed);
            }

            private void Start()
            {
                // GenerateTerrainAndPaths();
            }

            public void GenerateTerrainAndPaths()
            {
                ClearState();
                GenerateHeightmap();
                ComputeHub();
                GeneratePaths();
                ApplyPathFlatteningAndWidening();
                BuildMesh();
                CacheWorldWaypointsAndSpawns();
            }

            private void ClearState()
            {
                heightMap = new float[gridWidth + 1, gridHeight + 1];
                isPathTile = new bool[gridWidth, gridHeight];
                pathsGrid.Clear();
                pathsWorld.Clear();
                spawnWorldPositions.Clear();
            }

            private void GenerateHeightmap()
            {
                // Generate a Perlin-based heightmap. Vertices are (gridWidth+1)*(gridHeight+1)
                float offsetX = pseudoRandom.Next(-100000, 100000);
                float offsetY = pseudoRandom.Next(-100000, 100000);

                for (int y = 0; y <= gridHeight; y++)
                {
                    for (int x = 0; x <= gridWidth; x++)
                    {
                        float nx = (x / (float)gridWidth) * noiseScale + offsetX;
                        float ny = (y / (float)gridHeight) * noiseScale + offsetY;
                        float h = Mathf.PerlinNoise(nx, ny);
                        // Optional falloff toward edges to create a basin-like arena
                        float edgeFalloff = Mathf.SmoothStep(0f, 1f, 1f - Mathf.Max(
                            Mathf.Abs((x - gridWidth * 0.5f) / (gridWidth * 0.5f)),
                            Mathf.Abs((y - gridHeight * 0.5f) / (gridHeight * 0.5f))
                        ));
                        heightMap[x, y] = h * elevationScale * edgeFalloff;
                    }
                }
            }

            private void ComputeHub()
            {
                hubGrid = new Vector2Int(gridWidth / 2, gridHeight / 2);
            }

            private void GeneratePaths()
            {
                // Select distinct edges and start points
                var edges = new List<Func<Vector2Int>>
    {
        () => new Vector2Int(0, pseudoRandom.Next(0, gridHeight)),
        () => new Vector2Int(gridWidth - 1, pseudoRandom.Next(0, gridHeight)),
        () => new Vector2Int(pseudoRandom.Next(0, gridWidth), 0),
        () => new Vector2Int(pseudoRandom.Next(0, gridWidth), gridHeight - 1)
    };

                var usedStarts = new HashSet<Vector2Int>();
                var existingPaths = new List<List<Vector2Int>>();
                int pathCount = Mathf.Max(minPathCount, 3);
                int maxAttempts = 100; // Prevent infinite loops

               // Debug.Log($"ProceduralTerrainGenerator: Attempting to generate {pathCount} paths with minimum separation {minPathSeparation}");

                for (int i = 0; i < pathCount; i++)
                {
                    Vector2Int start;
                    List<Vector2Int> path;
                    int attempts = 0;
                    bool pathAccepted = false;

                    // Try to generate a path that doesn't overlap with existing ones
                    do
                    {
                        // Try to pick a unique edge start
                        int guard = 0;
                        do
                        {
                            start = edges[pseudoRandom.Next(edges.Count)]();
                            guard++;
                        } while (usedStarts.Contains(start) && guard < 50);
                        usedStarts.Add(start);

                        path = CarvePathGreedy(start, hubGrid);

                        // Check if this path overlaps too much with existing paths
                        if (!DoesPathOverlap(path, existingPaths))
                        {
                            pathAccepted = true;
                           // Debug.Log($"ProceduralTerrainGenerator: Path {i + 1} accepted after {attempts + 1} attempts");
                        }
                        else
                        {
                            //Debug.Log("What");
                           // Debug.Log("GridofTheHub: " + hubGrid);
                            for (int t = 0; t < existingPaths[0].Count; t++)
                            {
                                //Debug.Log("Existing Tile: " + existingPaths[0][t]);
                            }
                            attempts++;
                           // Debug.Log($"ProceduralTerrainGenerator: Path {i + 1} rejected due to overlap, attempt {attempts}");
                        }
                    } while (!pathAccepted && attempts < maxAttempts);

                    if (pathAccepted)
                    {
                        pathsGrid.Add(path);
                        existingPaths.Add(path);
                        // Mark spawn world position for this path
                        spawnWorldPositions.Add(GridToWorldCenter(start));
                    }
                    else
                    {
                       // Debug.LogWarning($"ProceduralTerrainGenerator: Failed to generate non-overlapping path {i + 1} after {maxAttempts} attempts");
                        // Add the path anyway to maintain minimum count
                        pathsGrid.Add(path);
                        existingPaths.Add(path);
                        spawnWorldPositions.Add(GridToWorldCenter(start));
                    }
                }

                // Mark path tiles
                int totalPathTiles = 0;
                foreach (var path in pathsGrid)
                {
                    // Ensure path reaches the hub if it's close
                    if (path.Count > 0)
                    {
                        var lastTile = path[path.Count - 1];
                        if (Vector2Int.Distance(lastTile, hubGrid) > 1)
                        {
                            // Add hub tile if path doesn't reach it
                            path.Add(hubGrid);
                        }
                    }

                    foreach (var c in path)
                    {
                        if (IsInGrid(c))
                        {
                            isPathTile[c.x, c.y] = true;
                            totalPathTiles++;
                        }
                    }
                }
                //Debug.Log($"ProceduralTerrainGenerator: Generated {pathsGrid.Count} paths with {totalPathTiles} total path tiles");
            }

            private List<Vector2Int> CarvePathGreedy(Vector2Int start, Vector2Int goal)
            {
                // Improved greedy algorithm with better pathfinding and shorter routes
                var current = start;
                var visited = new HashSet<Vector2Int>();
                var path = new List<Vector2Int> { current };
                visited.Add(current);

                int maxPathLength = Mathf.Max(gridWidth, gridHeight) * 2; // Limit path length
                int safety = maxPathLength * 2;

                while (current != goal && safety-- > 0 && path.Count < maxPathLength)
                {
                    Vector2Int bestNext = current;
                    float bestScore = float.MaxValue;

                    foreach (var dir in Neighbors4())
                    {
                        var next = current + dir;
                        if (!IsInGrid(next) || visited.Contains(next)) continue;

                        // Prioritize direct movement toward goal
                        float manhattan = Mathf.Abs(next.x - goal.x) + Mathf.Abs(next.y - goal.y);
                        float slopePenalty = GetSlopePenalty(current, next);

                        // Reduce random jitter for more direct paths
                        float randomJitter = (float)pseudoRandom.NextDouble() * 0.1f;

                        // Reduce path separation penalty to allow shorter routes
                        float pathSeparationPenalty = GetPathSeparationPenalty(next) * 0.5f;

                        // Weight manhattan distance more heavily for shorter paths
                        float score = manhattan * 2f + slopePenalty + randomJitter + pathSeparationPenalty;

                        if (score < bestScore)
                        {
                            bestScore = score;
                            bestNext = next;
                        }
                    }

                    if (bestNext == current)
                    {
                        // If stuck, try to find any valid neighbor
                        var candidates = new List<Vector2Int>();
                        foreach (var dir in Neighbors4())
                        {
                            var next = current + dir;
                            if (IsInGrid(next) && !visited.Contains(next))
                            {
                                candidates.Add(next);
                            }
                        }

                        if (candidates.Count > 0)
                        {
                            // Prefer neighbors closer to the goal
                            candidates.Sort((a, b) =>
                            {
                                float distA = Vector2Int.Distance(a, goal);
                                float distB = Vector2Int.Distance(b, goal);
                                return distA.CompareTo(distB);
                            });
                            bestNext = candidates[0];
                        }
                        else
                        {
                            break; // No valid neighbors, stop here
                        }
                    }

                    current = bestNext;
                    path.Add(current);
                    visited.Add(current);

                    // Early exit if we're very close to the goal
                    if (Vector2Int.Distance(current, goal) <= 1)
                    {
                        break;
                    }
                }

                // Ensure the path reaches the goal if possible
                if (path.Count > 0 && Vector2Int.Distance(path[path.Count - 1], goal) > 1)
                {
                    path.Add(goal);
                }

                return path;
            }

            private float GetPathSeparationPenalty(Vector2Int tile)
            {
                // Check if this tile is too close to existing paths
                float minDistance = float.MaxValue;

                foreach (var existingPath in pathsGrid)
                {
                    foreach (var existingTile in existingPath)
                    {
                        float distance = Vector2Int.Distance(tile, existingTile);
                        if (distance < minDistance)
                        {
                            minDistance = distance;
                        }
                    }
                }

                // If too close to existing paths, add a penalty
                if (minDistance < minPathSeparation)
                {
                    return (minPathSeparation - minDistance) * 3f; // Reduced penalty for shorter paths
                }

                return 0f; // No penalty if far enough from existing paths
            }

            private float GetSlopePenalty(Vector2Int a, Vector2Int b)
            {
                // Approximate slope cost using vertex heights at tile corners
                Vector3 wa = GridToWorldCenter(a);
                Vector3 wb = GridToWorldCenter(b);
                float dy = Mathf.Abs(wb.y - wa.y);
                return dy * 4f; // heavier cost for steep ascents/descents
            }

            private void ApplyPathFlatteningAndWidening()
            {
               // Debug.Log($"ProceduralTerrainGenerator: Starting path processing - {pathsGrid.Count} paths, {pathWideningRadiusTiles} widening radius");

                // First, widen the path flags
                if (pathWideningRadiusTiles > 0)
                {
                    var widened = new bool[gridWidth, gridHeight];
                    int widenedCount = 0;
                    for (int y = 0; y < gridHeight; y++)
                    {
                        for (int x = 0; x < gridWidth; x++)
                        {
                            if (isPathTile[x, y] || IsNearPath(new Vector2Int(x, y), pathWideningRadiusTiles))
                            {
                                widened[x, y] = true;
                                widenedCount++;
                            }
                        }
                    }
                    isPathTile = widened;
                    //Debug.Log($"ProceduralTerrainGenerator: Widened paths to {widenedCount} tiles");
                }

                // Apply progressive path flattening with smooth transitions
                ApplyProgressivePathFlattening();

                // Apply path smoothing if enabled
                if (smoothPaths)
                {
                    ApplyPathSmoothing();
                }

                // Final terrain leveling pass for completely flat paths
                if (useAggressiveFlattening)
                {
                    ApplyFinalTerrainLeveling();
                }

               // Debug.Log($"ProceduralTerrainGenerator: Path processing complete");
            }

            private void ApplyProgressivePathFlattening()
            {
                //Debug.Log($"ProceduralTerrainGenerator: Starting aggressive landscape painting flattening");

                if (useAggressiveFlattening)
                {
                    ApplyAggressiveLandscapeFlattening();
                }
                else
                {
                    ApplyStandardPathFlattening();
                }
            }

            private void ApplyAggressiveLandscapeFlattening()
            {
                // Create a completely flat surface for paths using multiple iterations
                for (int iteration = 0; iteration < flatteningIterations; iteration++)
                {
                    var tempHeightMap = new float[gridWidth + 1, gridHeight + 1];
                    Array.Copy(heightMap, tempHeightMap, heightMap.Length);

                    int flattenedVertices = 0;

                    // Apply aggressive flattening to all vertices near paths
                    for (int y = 0; y <= gridHeight; y++)
                    {
                        for (int x = 0; x <= gridWidth; x++)
                        {
                            int tx = Mathf.Clamp(x, 0, gridWidth - 1);
                            int ty = Mathf.Clamp(y, 0, gridHeight - 1);

                            float distanceToPath = GetDistanceToPathCenter(new Vector2Int(tx, ty));

                            if (distanceToPath <= flatteningRadius)
                            {
                                // Use a more aggressive falloff curve for complete flattening
                                float normalizedDistance = distanceToPath / flatteningRadius;
                                float flattenStrength = Mathf.Pow(1f - normalizedDistance, 2f); // Quadratic falloff

                                // Completely flatten the path center, gradual transition to terrain
                                float targetHeight = Mathf.Lerp(pathFlattenHeight, heightMap[x, y], 1f - flattenStrength);

                                // Apply the flattening with full strength
                                tempHeightMap[x, y] = Mathf.Lerp(heightMap[x, y], targetHeight, flattenStrength * flatteningStrength);
                                flattenedVertices++;
                            }
                        }
                    }

                    heightMap = tempHeightMap;
                    //Debug.Log($"ProceduralTerrainGenerator: Aggressive flattening iteration {iteration + 1}: flattened {flattenedVertices} vertices");
                }
            }

            private void ApplyStandardPathFlattening()
            {
                //Debug.Log($"ProceduralTerrainGenerator: Starting standard path flattening");

                // Create a temporary heightmap for smooth blending
                var tempHeightMap = new float[gridWidth + 1, gridHeight + 1];
                Array.Copy(heightMap, tempHeightMap, heightMap.Length);

                int flattenedVertices = 0;
                // Apply progressive flattening with smooth falloff
                for (int y = 0; y <= gridHeight; y++)
                {
                    for (int x = 0; x <= gridWidth; x++)
                    {
                        // Determine the nearest tile index to this vertex
                        int tx = Mathf.Clamp(x, 0, gridWidth - 1);
                        int ty = Mathf.Clamp(y, 0, gridHeight - 1);

                        float distanceToPath = GetDistanceToPathCenter(new Vector2Int(tx, ty));
                        if (distanceToPath <= pathWideningRadiusTiles + 1)
                        {
                            // Calculate flattening strength based on distance
                            float flattenStrength = Mathf.SmoothStep(1f, 0f, distanceToPath / (pathWideningRadiusTiles + 1f));

                            // Target height: blend between current height and flat path height
                            float targetHeight = Mathf.Lerp(pathFlattenHeight, heightMap[x, y], 1f - flattenStrength);

                            // Apply smooth transition
                            tempHeightMap[x, y] = Mathf.Lerp(heightMap[x, y], targetHeight, flattenStrength * 0.8f);
                            flattenedVertices++;
                        }
                    }
                }

                // Apply the smoothed heightmap
                heightMap = tempHeightMap;
                //Debug.Log($"ProceduralTerrainGenerator: Standard flattening: flattened {flattenedVertices} vertices");
            }

            private void ApplyPathSmoothing()
            {
                //Debug.Log($"ProceduralTerrainGenerator: Starting aggressive path smoothing with {pathSmoothingIterations} iterations");

                // Apply multiple iterations of height smoothing around paths
                for (int iteration = 0; iteration < pathSmoothingIterations; iteration++)
                {
                    var smoothedHeightMap = new float[gridWidth + 1, gridHeight + 1];
                    Array.Copy(heightMap, smoothedHeightMap, heightMap.Length);

                    int smoothedVertices = 0;

                    for (int y = 0; y <= gridHeight; y++)
                    {
                        for (int x = 0; x <= gridWidth; x++)
                        {
                            int tx = Mathf.Clamp(x, 0, gridWidth - 1);
                            int ty = Mathf.Clamp(y, 0, gridHeight - 1);

                            if (IsNearPath(new Vector2Int(tx, ty), Mathf.RoundToInt(pathSmoothingRadius)))
                            {
                                // Use a larger smoothing kernel for more aggressive smoothing
                                float totalWeight = 0f;
                                float weightedSum = 0f;

                                // Extended smoothing kernel (3x3 instead of just immediate neighbors)
                                for (int dy = -2; dy <= 2; dy++)
                                {
                                    for (int dx = -2; dx <= 2; dx++)
                                    {
                                        int nx = x + dx;
                                        int ny = y + dy;

                                        if (nx >= 0 && ny >= 0 && nx <= gridWidth && ny <= gridHeight)
                                        {
                                            // Use distance-based weights for smoother transitions
                                            float distance = Mathf.Sqrt(dx * dx + dy * dy);
                                            float weight = Mathf.Exp(-distance * 0.5f); // Exponential falloff

                                            weightedSum += heightMap[nx, ny] * weight;
                                            totalWeight += weight;
                                        }
                                    }
                                }

                                if (totalWeight > 0)
                                {
                                    float smoothedHeight = weightedSum / totalWeight;

                                    // More aggressive smoothing in early iterations
                                    float iterationStrength = 1f - (iteration / (float)pathSmoothingIterations);
                                    float blendFactor = pathSmoothingStrength * iterationStrength;

                                    // Apply stronger smoothing near path centers
                                    float distanceToPath = GetDistanceToPathCenter(new Vector2Int(tx, ty));
                                    float pathProximity = Mathf.Max(0f, 1f - distanceToPath / pathSmoothingRadius);
                                    blendFactor *= (0.5f + 0.5f * pathProximity);

                                    smoothedHeightMap[x, y] = Mathf.Lerp(heightMap[x, y], smoothedHeight, blendFactor);
                                    smoothedVertices++;
                                }
                            }
                        }
                    }

                    heightMap = smoothedHeightMap;
                   // Debug.Log($"ProceduralTerrainGenerator: Smoothing iteration {iteration + 1}: smoothed {smoothedVertices} vertices");
                }
            }

            private void ApplyFinalTerrainLeveling()
            {
                //Debug.Log("ProceduralTerrainGenerator: Applying final terrain leveling for completely flat paths");

                var leveledHeightMap = new float[gridWidth + 1, gridHeight + 1];
                Array.Copy(heightMap, leveledHeightMap, heightMap.Length);

                int leveledVertices = 0;

                // Create a completely flat surface for path centers
                for (int y = 0; y <= gridHeight; y++)
                {
                    for (int x = 0; x <= gridWidth; x++)
                    {
                        int tx = Mathf.Clamp(x, 0, gridWidth - 1);
                        int ty = Mathf.Clamp(y, 0, gridHeight - 1);

                        float distanceToPath = GetDistanceToPathCenter(new Vector2Int(tx, ty));

                        if (distanceToPath <= 1.0f) // Directly on or adjacent to paths
                        {
                            // Force completely flat surface for path centers
                            leveledHeightMap[x, y] = pathFlattenHeight;
                            leveledVertices++;
                        }
                        else if (distanceToPath <= flatteningRadius)
                        {
                            // Smooth transition from flat to terrain
                            float transition = (distanceToPath - 1.0f) / (flatteningRadius - 1.0f);
                            float targetHeight = Mathf.Lerp(pathFlattenHeight, heightMap[x, y], transition);

                            // Use a smooth curve for the transition
                            float smoothTransition = Mathf.SmoothStep(0f, 1f, transition);
                            leveledHeightMap[x, y] = Mathf.Lerp(pathFlattenHeight, targetHeight, smoothTransition);
                            leveledVertices++;
                        }
                    }
                }

                heightMap = leveledHeightMap;
                //Debug.Log($"ProceduralTerrainGenerator: Final leveling: processed {leveledVertices} vertices");
            }

            private float GetDistanceToPathCenter(Vector2Int tile)
            {
                if (isPathTile[tile.x, tile.y]) return 0f;

                // Use a more efficient distance calculation with early exit
                float minDistance = float.MaxValue;
                int searchRadius = Mathf.Max(pathWideningRadiusTiles + 2, 5); // Limit search area

                int startX = Mathf.Max(0, tile.x - searchRadius);
                int endX = Mathf.Min(gridWidth - 1, tile.x + searchRadius);
                int startY = Mathf.Max(0, tile.y - searchRadius);
                int endY = Mathf.Min(gridHeight - 1, tile.y + searchRadius);

                for (int y = startY; y <= endY; y++)
                {
                    for (int x = startX; x <= endX; x++)
                    {
                        if (isPathTile[x, y])
                        {
                            float distance = Vector2Int.Distance(tile, new Vector2Int(x, y));
                            if (distance < minDistance)
                            {
                                minDistance = distance;
                                // Early exit if we're very close to a path
                                if (distance <= 1f) return distance;
                            }
                        }
                    }
                }

                return minDistance;
            }

            private bool IsNearPath(Vector2Int tile, int radius)
            {
                if (isPathTile[tile.x, tile.y]) return true;
                for (int dy = -radius; dy <= radius; dy++)
                {
                    for (int dx = -radius; dx <= radius; dx++)
                    {
                        int nx = tile.x + dx;
                        int ny = tile.y + dy;
                        if (nx < 0 || ny < 0 || nx >= gridWidth || ny >= gridHeight) continue;
                        if (isPathTile[nx, ny]) return true;
                    }
                }
                return false;
            }

            private void BuildMesh()
            {
                var mesh = new Mesh { name = "ProceduralTerrain" };

                int vxCountX = gridWidth + 1;
                int vxCountY = gridHeight + 1;
                int vertexCount = vxCountX * vxCountY;
                var vertices = new Vector3[vertexCount];
                var uvs = new Vector2[vertexCount];

                // Generate vertices and UVs
                for (int y = 0; y < vxCountY; y++)
                {
                    for (int x = 0; x < vxCountX; x++)
                    {
                        int i = y * vxCountX + x;
                        float wx = x * tileSize;
                        float wz = y * tileSize;
                        float wy = heightMap[x, y];
                        vertices[i] = new Vector3(wx, wy, wz);
                        uvs[i] = new Vector2(x / (float)gridWidth, y / (float)gridHeight);
                    }
                }

                // Generate triangles
                int quadCount = gridWidth * gridHeight;
                var triangles = new int[quadCount * 6];
                int t = 0;
                for (int y = 0; y < gridHeight; y++)
                {
                    for (int x = 0; x < gridWidth; x++)
                    {
                        int i = y * vxCountX + x;
                        // two triangles per quad
                        triangles[t++] = i;
                        triangles[t++] = i + vxCountX;
                        triangles[t++] = i + 1;

                        triangles[t++] = i + 1;
                        triangles[t++] = i + vxCountX;
                        triangles[t++] = i + vxCountX + 1;
                    }
                }

                mesh.vertices = vertices;
                mesh.uv = uvs;
                mesh.triangles = triangles;

                // Calculate proper normals for smooth shading
                mesh.RecalculateNormals();
                mesh.RecalculateBounds();
                mesh.RecalculateTangents();

                var filter = GetComponent<MeshFilter>();
                filter.sharedMesh = mesh;
            }

            private void CacheWorldWaypointsAndSpawns()
            {
                pathsWorld.Clear();
                foreach (var path in pathsGrid)
                {
                    var wps = new List<Vector3>(path.Count);
                    foreach (var c in path) wps.Add(GridToWorldCenter(c));
                    pathsWorld.Add(wps);
                }
            }

        public List<Vector3> GetCandidateNodesNearPaths(float distanceFromPath = 2f, int minPerPath = 2)
        {
            var result = new List<Vector3>();
            var usedGridPositions = new HashSet<Vector2Int>(); // track global used tiles

            if (PathWaypoints == null || PathWaypoints.Count == 0)
                return result;

            foreach (var path in PathWaypoints)
            {
                var pathNodes = new List<Vector3>();

                foreach (var waypoint in path)
                {
                    int radius = Mathf.CeilToInt(distanceFromPath / tileSize);

                    for (int dx = -radius; dx <= radius; dx++)
                    {
                        for (int dy = -radius; dy <= radius; dy++)
                        {
                            Vector2Int grid = WorldToGridKey(waypoint) + new Vector2Int(dx, dy);
                            if (!IsInGrid(grid)) continue;
                            if (isPathTile[grid.x, grid.y]) continue;        // skip path
                            if (usedGridPositions.Contains(grid)) continue;   // skip already used

                            Vector3 worldPos = GridToWorldCenter(grid);

                            // Must be within distanceFromPath
                            if (Vector3.Distance(worldPos, waypoint) <= distanceFromPath)
                            {
                                pathNodes.Add(worldPos);
                            }
                        }
                    }
                }

                // Shuffle and pick at least minPerPath nodes
                Shuffle(pathNodes);

                int nodesToAdd = Mathf.Min(minPerPath, pathNodes.Count);
                for (int i = 0; i < nodesToAdd; i++)
                {
                    Vector3 nodePos = pathNodes[i];
                    result.Add(nodePos);

                    // mark the grid as used
                    Vector2Int gridPos = WorldToGridKey(nodePos);
                    usedGridPositions.Add(gridPos);
                }
            }

            return result;
        }

        // Helper: convert world pos to grid coordinates
        private Vector2Int WorldToGridKey(Vector3 worldPos)
        {
            int x = Mathf.FloorToInt(worldPos.x / tileSize);
            int y = Mathf.FloorToInt(worldPos.z / tileSize);
            return new Vector2Int(x, y);
        }

        public List<Vector3> GetCandidatePlacementNodes(int desiredCount = 16, float minHeight = 0.8f, float minDistanceToPath = 1.5f)
            {
                var nodes = new List<Vector3>();
                var candidates = new List<Vector2Int>();
                for (int y = 0; y < gridHeight; y++)
                {
                    for (int x = 0; x < gridWidth; x++)
                    {
                        if (isPathTile[x, y]) continue;
                        Vector3 wc = GridToWorldCenter(new Vector2Int(x, y));
                        if (wc.y < minHeight) continue;
                        float d = DistanceToNearestPath(new Vector2Int(x, y));
                        if (d < minDistanceToPath) continue;
                        candidates.Add(new Vector2Int(x, y));
                    }
                }

                // Shuffle candidates and pick up to desiredCount with spacing
                Shuffle(candidates);
                float minSpacing = 2.0f * tileSize;
                foreach (var c in candidates)
                {
                    var pos = GridToWorldCenter(c);
                    bool tooClose = false;
                    foreach (var chosen in nodes)
                    {
                        if (Vector3.Distance(pos, chosen) < minSpacing) { tooClose = true; break; }
                    }
                    if (!tooClose)
                    {
                        nodes.Add(pos);
                        if (nodes.Count >= desiredCount) break;
                    }
                }
                return nodes;
            }

            private float DistanceToNearestPath(Vector2Int tile)
            {
                float best = float.MaxValue;
                for (int y = 0; y < gridHeight; y++)
                {
                    for (int x = 0; x < gridWidth; x++)
                    {
                        if (!isPathTile[x, y]) continue;
                        float d = Vector2Int.Distance(tile, new Vector2Int(x, y));
                        if (d < best) best = d;
                    }
                }
                return best;
            }

            public Vector3 GridToWorldCenter(Vector2Int c)
            {
                float wx = (c.x + 0.5f) * tileSize;
                float wz = (c.y + 0.5f) * tileSize;
                // approximate center height using surrounding vertices
                float hx0 = heightMap[c.x, c.y];
                float hx1 = heightMap[c.x + 1, c.y];
                float hx2 = heightMap[c.x, c.y + 1];
                float hx3 = heightMap[c.x + 1, c.y + 1];
                float wy = (hx0 + hx1 + hx2 + hx3) * 0.25f;
                return new Vector3(wx, wy, wz);
            }

            private static IEnumerable<Vector2Int> Neighbors4()
            {
                yield return Vector2Int.right;
                yield return Vector2Int.left;
                yield return Vector2Int.up;
                yield return Vector2Int.down;
            }

            private bool IsInGrid(Vector2Int c) => c.x >= 0 && c.y >= 0 && c.x < gridWidth && c.y < gridHeight;

            private bool DoesPathOverlap(List<Vector2Int> newPath, List<List<Vector2Int>> existingPaths)
            {
                if (existingPaths.Count == 0) return false;

                // Check each tile in the new path against existing paths
                foreach (var tile in newPath)
                {
                    foreach (var existingPath in existingPaths)
                    {
                        foreach (var existingTile in existingPath)
                        {
                            float distance = Vector2Int.Distance(tile, existingTile);
                            if (distance < minPathSeparation)
                            {
                                return true; // Paths overlap
                            }
                        }
                    }
                }
                return false;
            }

            private void Shuffle<T>(IList<T> list)
            {
                for (int i = list.Count - 1; i > 0; i--)
                {
                    int j = pseudoRandom.Next(i + 1);
                    (list[i], list[j]) = (list[j], list[i]);
                }
            }

        }
    }






