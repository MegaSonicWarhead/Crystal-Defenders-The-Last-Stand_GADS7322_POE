using System.Collections.Generic;
using UnityEngine;
using CrystalDefenders.Generation;
using CrystalDefenders.Units;

namespace CrystalDefenders.Gameplay
{
    /// <summary>
    /// Manages procedural placement nodes for defenders in the game.
    /// Supports procedural nodes and extra nodes bought by the player.
    /// </summary>
    public class DefenderPlacementManager : MonoBehaviour
    {
        [Header("Placement Node Settings")]
        public GameObject placementNodePrefab;   // Prefab for a placement node
        public int desiredNodeCount = 16;        // Target number of procedural nodes

        private readonly List<PlacementNode> nodes = new List<PlacementNode>();

        // Extra node placement
        private bool isPlacingExtraNode = false;
        private GameObject previewNode; // The hovering node
        private GameObject extraNodePrefab;


        private void Start()
        {
            // Generate initial procedural nodes
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

        private void Update()
        {
            HandleExtraNodePlacement();
        }

        // --- Extra Node Placement ---
        public void StartPlacingExtraNode(GameObject prefab)
        {
            if (prefab == null) return;

            extraNodePrefab = prefab;
            isPlacingExtraNode = true;

            // Create preview node
            if (previewNode != null) Destroy(previewNode);
            previewNode = Instantiate(extraNodePrefab, Vector3.zero, Quaternion.identity, transform);

            // Disable colliders so it doesn't interfere with raycasts or physics
            foreach (var col in previewNode.GetComponentsInChildren<Collider>())
                col.enabled = false;

            // Optional: make semi-transparent to indicate "preview"
            foreach (var r in previewNode.GetComponentsInChildren<Renderer>())
            {
                var mat = r.material;
                mat.color = new Color(mat.color.r, mat.color.g, mat.color.b, 0.6f);
            }

            Debug.Log("Extra node placement active. Hover over terrain and left-click to place.");
        }

        private void HandleExtraNodePlacement()
        {
            if (!isPlacingExtraNode || previewNode == null) return;

            // Raycast from mouse to terrain
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                // Snap preview node to terrain
                previewNode.transform.position = hit.point;
            }

            // Left click: attempt to place node
            if (Input.GetMouseButtonDown(0))
            {
                Vector3 placePos = previewNode.transform.position;

                // Check min spacing against existing nodes
                const float minSpacing = 2f;
                if (nodes.Exists(n => Vector3.Distance(n.transform.position, placePos) < minSpacing))
                {
                    Debug.Log("Cannot place node too close to another node.");
                    return;
                }

                // Instantiate real node
                var go = Instantiate(extraNodePrefab, placePos, Quaternion.identity, transform);
                var node = go.GetComponent<PlacementNode>() ?? go.AddComponent<PlacementNode>();
                node.Initialize();
                nodes.Add(node);

                // Cleanup preview
                Destroy(previewNode);
                previewNode = null;
                isPlacingExtraNode = false;
                extraNodePrefab = null;

                Debug.Log("Extra placement node placed!");
            }

            // Right click / Escape: cancel placement
            if (Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.Escape))
            {
                Destroy(previewNode);
                previewNode = null;
                isPlacingExtraNode = false;
                extraNodePrefab = null;
                Debug.Log("Extra placement cancelled.");
            }
        }

        // --- Procedural Node Methods ---
        public void CreateNodes(ProceduralTerrainGenerator generator)
        {
            if (nodes.Count == 0) GenerateNodes(generator);
            else ResetNodes();
        }

        public void CreateNodesNearPaths(ProceduralTerrainGenerator generator, float distanceFromPath = 3f, int minNodesPerPath = 2)
        {
            if (nodes.Count == 0) GenerateNodesNearPaths(generator, distanceFromPath, minNodesPerPath);
            else ResetNodes();
        }

        private void GenerateNodes(ProceduralTerrainGenerator generator)
        {
            ClearNodes(true);
            InstantiateNodes(generator.GetCandidatePlacementNodes(desiredNodeCount * 3));

            if (nodes.Count < desiredNodeCount)
                InstantiateNodes(generator.GetCandidatePlacementNodes(desiredNodeCount * 2));
        }

        private void GenerateNodesNearPaths(ProceduralTerrainGenerator generator, float distanceFromPath, int minNodesPerPath)
        {
            ClearNodes(true);
            InstantiateNodes(generator.GetCandidateNodesNearPaths(distanceFromPath, minNodesPerPath));
            InstantiateNodes(generator.GetCandidatePlacementNodes(desiredNodeCount * 3));
        }

        // --- Node Instantiation ---
        public void InstantiateNodes(List<Vector3> positions)
        {
            const float minSpacing = 2.0f;

            foreach (var originalPos in positions)
            {
                Vector3 pos = originalPos;
                bool tooClose = false;

                foreach (var n in nodes)
                {
                    if (n == null) continue;
                    if (Vector3.Distance(n.transform.position, pos) < minSpacing)
                    {
                        tooClose = true;
                        break;
                    }
                }
                if (tooClose) continue;

                Vector3 spawn = pos + Vector3.up * 5f;
                if (Physics.Raycast(spawn, Vector3.down, out RaycastHit hit, 50f)) pos = hit.point;

                var go = Instantiate(placementNodePrefab, pos, Quaternion.identity, transform);
                var node = go.GetComponent<PlacementNode>();
                if (node == null) node = go.AddComponent<PlacementNode>();
                node.Initialize();
                nodes.Add(node);
            }
        }

        private void ResetNodes()
        {
            foreach (var node in nodes) node?.Initialize();
        }

        private void ClearNodes(bool destroy = false)
        {
            if (destroy)
            {
                foreach (var n in nodes) if (n != null) Destroy(n.gameObject);
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

        // --- Utility ---
        public bool HasAvailableNode()
        {
            return nodes.Exists(n => n != null && n.IsAvailable);
        }

        public void OnNodePlaced()
        {
            // Called when a defender is placed on a node
            // Optional: remove node from available list or mark it as used
        }
    }
}