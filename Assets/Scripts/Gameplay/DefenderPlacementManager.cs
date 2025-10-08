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
                //generator.GenerateTerrainAndPaths(); // Ensure terrain and paths exist
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
            ClearNodes();
			// Request more candidates than desired and let spacing filter accept as many as fit
			var positions = generator.GetCandidatePlacementNodes(desiredNodeCount * 3);
            InstantiateNodes(positions);

			// If still under target, attempt another broad pass
			if (nodes.Count < desiredNodeCount)
			{
				var more = generator.GetCandidatePlacementNodes(desiredNodeCount * 2);
				InstantiateNodes(more);
			}
        }

        // For placements near paths
        public void CreateNodesNearPaths(ProceduralTerrainGenerator generator, float distanceFromPath = 3f, int minNodesPerPath = 2)
        {
            Debug.Log("MEOW MEOW MEOW");
            ClearNodes();
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

                // Snap to terrain using raycast in case of slight height mismatch
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


