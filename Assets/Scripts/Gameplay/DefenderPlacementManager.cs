using System.Collections.Generic;
using UnityEngine;
using CrystalDefenders.Generation;
using CrystalDefenders.Units;

namespace CrystalDefenders.Gameplay
{
    public class DefenderPlacementManager : MonoBehaviour
    {
        public GameObject placementNodePrefab;
        public int desiredNodeCount = 16;

        private readonly List<PlacementNode> nodes = new List<PlacementNode>();

        private void Start()
        {
            var generator = FindObjectOfType<ProceduralTerrainGenerator>();
            if (generator != null)
            {
                CreateNodesNearPaths(generator, 3f, 2);
            }
            else
            {
                Debug.LogWarning("No ProceduralTerrainGenerator found in scene!");
            }
        }

        // For general placement anywhere
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

        // For placements near paths
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

        // Generate nodes once
        private void GenerateNodes(ProceduralTerrainGenerator generator)
        {
            ClearNodes(true);
            var positions = generator.GetCandidatePlacementNodes(desiredNodeCount * 3);
            InstantiateNodes(positions);

            if (nodes.Count < desiredNodeCount)
            {
                var more = generator.GetCandidatePlacementNodes(desiredNodeCount * 2);
                InstantiateNodes(more);
            }
        }

        private void GenerateNodesNearPaths(ProceduralTerrainGenerator generator, float distanceFromPath, int minNodesPerPath)
        {
            ClearNodes(true);
            var positions = generator.GetCandidateNodesNearPaths(distanceFromPath, minNodesPerPath);
            InstantiateNodes(positions);

            // Top-up with general candidates to fill open spaces
            var extras = generator.GetCandidatePlacementNodes(desiredNodeCount * 3);
            InstantiateNodes(extras);
        }

        // Shared instantiation logic
        private void InstantiateNodes(List<Vector3> positions)
        {
            const float minSpacing = 2.0f; // meters
            for (int i = 0; i < positions.Count; i++)
            {
                Vector3 pos = positions[i];
                bool tooClose = false;
                for (int j = 0; j < nodes.Count; j++)
                {
                    if (nodes[j] == null) continue;
                    if (Vector3.Distance(nodes[j].transform.position, pos) < minSpacing) { tooClose = true; break; }
                }
                if (tooClose) continue;

                // Snap to terrain using raycast
                Vector3 spawn = pos + Vector3.up * 5f;
                if (Physics.Raycast(spawn, Vector3.down, out RaycastHit hit, 50f))
                {
                    pos = hit.point;
                }

                var go = Instantiate(placementNodePrefab, pos, Quaternion.identity, transform);
                var node = go.GetComponent<PlacementNode>();
                if (node == null) node = go.AddComponent<PlacementNode>();
                node.Initialize();
                nodes.Add(node);
            }
        }

        // Reset existing nodes for reuse (don�t destroy or recreate)
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

        // If we ever need to rebuild from scratch (e.g., new map)
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