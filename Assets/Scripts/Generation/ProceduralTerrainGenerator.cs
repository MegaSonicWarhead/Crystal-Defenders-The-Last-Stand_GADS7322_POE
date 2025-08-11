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
        [Header("Grid Settings")] public int gridWidth = 20; public int gridHeight = 20; public float tileSize = 1f;
        [Header("Heightmap Noise")] public float noiseScale = 4f; public float elevationScale = 3f; public int seed = 0; public bool randomizeSeedOnAwake = true;
        [Header("Paths")] public int minPathCount = 3; public int pathWideningRadiusTiles = 0; public float pathFlattenHeight = 0.2f;

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
            GenerateTerrainAndPaths();
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
            int pathCount = Mathf.Max(minPathCount, 3);

            for (int i = 0; i < pathCount; i++)
            {
                Vector2Int start;
                // Try to pick a unique edge start
                int guard = 0;
                do
                {
                    start = edges[pseudoRandom.Next(edges.Count)]();
                    guard++;
                } while (usedStarts.Contains(start) && guard < 50);
                usedStarts.Add(start);

                List<Vector2Int> path = CarvePathGreedy(start, hubGrid);
                pathsGrid.Add(path);

                // Mark spawn world position for this path
                spawnWorldPositions.Add(GridToWorldCenter(start));
            }

            // Mark path tiles
            foreach (var path in pathsGrid)
            {
                foreach (var c in path)
                {
                    if (IsInGrid(c)) isPathTile[c.x, c.y] = true;
                }
            }
        }

        private List<Vector2Int> CarvePathGreedy(Vector2Int start, Vector2Int goal)
        {
            // Simple greedy descent toward the hub with small random turns
            var current = start;
            var visited = new HashSet<Vector2Int>();
            var path = new List<Vector2Int> { current };
            visited.Add(current);

            int safety = gridWidth * gridHeight * 4;
            while (current != goal && safety-- > 0)
            {
                Vector2Int bestNext = current;
                float bestScore = float.MaxValue;

                foreach (var dir in Neighbors4())
                {
                    var next = current + dir;
                    if (!IsInGrid(next) || visited.Contains(next)) continue;

                    float manhattan = Mathf.Abs(next.x - goal.x) + Mathf.Abs(next.y - goal.y);
                    float slopePenalty = GetSlopePenalty(current, next);
                    float randomJitter = (float)pseudoRandom.NextDouble() * 0.25f; // small variety
                    float score = manhattan + slopePenalty + randomJitter;
                    if (score < bestScore)
                    {
                        bestScore = score;
                        bestNext = next;
                    }
                }

                if (bestNext == current)
                {
                    // Dead-end: try a random neighbor to break ties
                    var candidates = new List<Vector2Int>();
                    foreach (var dir in Neighbors4())
                    {
                        var next = current + dir;
                        if (IsInGrid(next) && !visited.Contains(next)) candidates.Add(next);
                    }
                    if (candidates.Count == 0) break;
                    bestNext = candidates[pseudoRandom.Next(candidates.Count)];
                }

                current = bestNext;
                path.Add(current);
                visited.Add(current);
            }

            return path;
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
            // Lower the vertex heights near path tiles for visual clarity
            for (int y = 0; y <= gridHeight; y++)
            {
                for (int x = 0; x <= gridWidth; x++)
                {
                    // Determine the nearest tile index to this vertex
                    int tx = Mathf.Clamp(x, 0, gridWidth - 1);
                    int ty = Mathf.Clamp(y, 0, gridHeight - 1);
                    if (IsNearPath(new Vector2Int(tx, ty), pathWideningRadiusTiles))
                    {
                        heightMap[x, y] = Mathf.Min(heightMap[x, y], pathFlattenHeight);
                    }
                }
            }

            // Also widen the path flags
            if (pathWideningRadiusTiles > 0)
            {
                var widened = new bool[gridWidth, gridHeight];
                for (int y = 0; y < gridHeight; y++)
                {
                    for (int x = 0; x < gridWidth; x++)
                    {
                        if (isPathTile[x, y] || IsNearPath(new Vector2Int(x, y), pathWideningRadiusTiles))
                        {
                            widened[x, y] = true;
                        }
                    }
                }
                isPathTile = widened;
            }
        }

        private bool IsNearPath(Vector2Int tile, int radius)
        {
            if (isPathTile[tile.x, tile.y]) return true;
            for (int dy = -radius; dy <= radius; dy++)
            {
                for (int dx = -radius; dx <= radius; dx++)
                {
                    int nx = tile.x + dx; int ny = tile.y + dy;
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
            var normals = new Vector3[vertexCount];

            int ti = 0;
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
                    normals[i] = Vector3.up;
                }
            }

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
            mesh.normals = normals;
            mesh.RecalculateBounds();

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

        private Vector3 GridToWorldCenter(Vector2Int c)
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
            yield return Vector2Int.right; yield return Vector2Int.left; yield return Vector2Int.up; yield return Vector2Int.down;
        }

        private bool IsInGrid(Vector2Int c) => c.x >= 0 && c.y >= 0 && c.x < gridWidth && c.y < gridHeight;

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


