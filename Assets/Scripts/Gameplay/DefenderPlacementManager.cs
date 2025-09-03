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

        // For general placement anywhere
        public void CreateNodes(ProceduralTerrainGenerator generator)
        {
            ClearNodes();
            var positions = generator.GetCandidatePlacementNodes(desiredNodeCount);
            InstantiateNodes(positions);
        }

        // For placements near paths
        public void CreateNodesNearPaths(ProceduralTerrainGenerator generator, float distanceFromPath = 2f, int minNodesPerPath = 2)
        {
            ClearNodes();
            var positions = generator.GetCandidateNodesNearPaths(distanceFromPath, minNodesPerPath);
            InstantiateNodes(positions);
        }

        // Shared instantiation logic
        private void InstantiateNodes(List<Vector3> positions)
        {
            foreach (var p in positions)
            {
                var go = Instantiate(placementNodePrefab, p, Quaternion.identity, transform);
                var node = go.GetComponent<PlacementNode>();
                if (node == null) node = go.AddComponent<PlacementNode>();
                node.Initialize();
                nodes.Add(node);
            }
        }

        private void ClearNodes()
        {
            foreach (var n in nodes)
            {
                if (n != null) Destroy(n.gameObject);
            }
            nodes.Clear();
        }
    }
}


