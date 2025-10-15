using System.Collections.Generic;
using UnityEngine;
using CrystalDefenders.Generation;
using CrystalDefenders.Units;

namespace CrystalDefenders.Gameplay
{
    /// <summary>
    /// Manages procedural placement nodes for defenders in the game.
    /// Can generate nodes near paths or anywhere on the terrain.
    /// Handles instantiation, resetting, and spacing logic.
    /// </summary>
    public class DefenderPlacementManager : MonoBehaviour
    {
        [Header("Placement Node Settings")]
        public GameObject placementNodePrefab;   // Prefab for a placement node
        public int desiredNodeCount = 16;        // Target number of nodes to generate

        private readonly List<PlacementNode> nodes = new List<PlacementNode>(); // Tracks all active nodes

        private void Start()
        {
            // Attempt to locate the procedural terrain generator in the scene
            var generator = FindObjectOfType<ProceduralTerrainGenerator>();
            if (generator != null)
            {
                // Generate placement nodes near paths initially
                CreateNodesNearPaths(generator, 3f, 2);
            }
            else
            {
                Debug.LogWarning("No ProceduralTerrainGenerator found in scene!");
            }
        }

        /// <summary>
        /// Generates nodes anywhere on the terrain.
        /// Resets existing nodes if already present.
        /// </summary>
        public void CreateNodes(ProceduralTerrainGenerator generator)
        {
            if (nodes.Count == 0)
            {
                GenerateNodes(generator);
            }
            else
            {
                ResetNodes();
            }
        }

        /// <summary>
        /// Generates nodes preferentially near paths to encourage strategic defender placement.
        /// Resets existing nodes if already present.
        /// </summary>
        /// <param name="distanceFromPath">Distance from path center to place nodes</param>
        /// <param name="minNodesPerPath">Minimum nodes per path segment</param>
        public void CreateNodesNearPaths(ProceduralTerrainGenerator generator, float distanceFromPath = 3f, int minNodesPerPath = 2)
        {
            if (nodes.Count == 0)
            {
                GenerateNodesNearPaths(generator, distanceFromPath, minNodesPerPath);
            }
            else
            {
                ResetNodes();
            }
        }

        /// <summary>
        /// Generates a set of placement nodes anywhere on the terrain.
        /// Ensures enough nodes to meet the desired count.
        /// </summary>
        private void GenerateNodes(ProceduralTerrainGenerator generator)
        {
            ClearNodes(true); // Remove any existing nodes

            // Attempt to generate more candidate positions than needed for flexibility
            var positions = generator.GetCandidatePlacementNodes(desiredNodeCount * 3);
            InstantiateNodes(positions);

            // Top-up if not enough nodes were successfully instantiated
            if (nodes.Count < desiredNodeCount)
            {
                var more = generator.GetCandidatePlacementNodes(desiredNodeCount * 2);
                InstantiateNodes(more);
            }
        }

        /// <summary>
        /// Generates placement nodes preferentially near paths with additional nodes for general areas.
        /// </summary>
        private void GenerateNodesNearPaths(ProceduralTerrainGenerator generator, float distanceFromPath, int minNodesPerPath)
        {
            ClearNodes(true); // Remove existing nodes

            var positions = generator.GetCandidateNodesNearPaths(distanceFromPath, minNodesPerPath);
            InstantiateNodes(positions);

            // Top-up with general positions to fill open spaces
            var extras = generator.GetCandidatePlacementNodes(desiredNodeCount * 3);
            InstantiateNodes(extras);
        }

        /// <summary>
        /// Instantiates placement nodes at given positions.
        /// Ensures minimum spacing and snaps nodes to terrain height.
        /// </summary>
        private void InstantiateNodes(List<Vector3> positions)
        {
            const float minSpacing = 2.0f; // Minimum distance between nodes

            for (int i = 0; i < positions.Count; i++)
            {
                Vector3 pos = positions[i];
                bool tooClose = false;

                // Check spacing against existing nodes
                for (int j = 0; j < nodes.Count; j++)
                {
                    if (nodes[j] == null) continue;
                    if (Vector3.Distance(nodes[j].transform.position, pos) < minSpacing) { tooClose = true; break; }
                }
                if (tooClose) continue;

                // Snap node to terrain using raycast
                Vector3 spawn = pos + Vector3.up * 5f;
                if (Physics.Raycast(spawn, Vector3.down, out RaycastHit hit, 50f))
                {
                    pos = hit.point;
                }

                // Instantiate node prefab and initialize
                var go = Instantiate(placementNodePrefab, pos, Quaternion.identity, transform);
                var node = go.GetComponent<PlacementNode>();
                if (node == null) node = go.AddComponent<PlacementNode>();
                node.Initialize();
                nodes.Add(node);
            }
        }

        /// <summary>
        /// Resets all existing nodes for reuse without destroying them.
        /// </summary>
        private void ResetNodes()
        {
            foreach (var node in nodes)
            {
                if (node != null)
                {
                    node.Initialize();
                }
            }
        }

        /// <summary>
        /// Clears existing nodes from the scene.
        /// Can either destroy GameObjects or reset them for reuse.
        /// </summary>
        /// <param name="destroy">If true, destroys node GameObjects; otherwise resets them</param>
        private void ClearNodes(bool destroy = false)
        {
            if (destroy)
            {
                foreach (var n in nodes)
                {
                    if (n != null) Destroy(n.gameObject);
                }
                nodes.Clear();
            }
            else
            {
                foreach (var n in nodes)
                {
                    if (n != null)
                    {
                        n.Initialize();
                        n.gameObject.SetActive(true);
                    }
                }
            }
        }
    }
}