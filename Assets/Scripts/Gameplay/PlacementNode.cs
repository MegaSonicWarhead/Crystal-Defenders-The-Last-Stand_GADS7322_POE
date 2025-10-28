using UnityEngine;
using CrystalDefenders.Units;

namespace CrystalDefenders.Gameplay
{
    /// <summary>
    /// Represents a single placement node on the map where a defender can be instantiated.
    /// Handles placement logic, occupancy, and snapping to terrain.
    /// </summary>
    [DisallowMultipleComponent]
    public class PlacementNode : MonoBehaviour
    {
        [Header("Default Defender Prefab")]
        public Defender defenderPrefab;  // Optional default prefab for this node

        private bool occupied = false;   // Tracks if the node currently has a defender

        /// <summary>
        /// Initializes the node for placement.
        /// Resets occupancy and ensures the node is active.
        /// </summary>
        public void Initialize()
        {
            occupied = false;
            gameObject.SetActive(true);
        }

        /// <summary>
        /// Returns true if the node is currently occupied by a defender.
        /// </summary>
        public bool IsOccupied => occupied;

        /// <summary>
        /// Returns true if the node is free and active in the scene, meaning a defender can be placed here.
        /// </summary>
        public bool IsAvailable => !occupied && gameObject.activeInHierarchy;

        /// <summary>
        /// Handles player click input on this node.
        /// Attempts to place a defender if conditions are met.
        /// </summary>
        private void OnMouseDown()
        {
            TryPlaceDefender();
        }

        /// <summary>
        /// Attempts to place a defender prefab on this node.
        /// Performs checks for occupancy, shop selection, and snaps the defender to terrain.
        /// </summary>
        /// <returns>True if placement succeeded, false otherwise.</returns>
        public bool TryPlaceDefender()
        {
            if (occupied) return false;                                    // Node already occupied
            if (!WeaponShop.Instance.HasDefenderToPlace) return false;     // No defender selected

            // Determine prefab to place
            var prefab = WeaponShop.Instance.SelectedDefenderPrefab != null
                ? WeaponShop.Instance.SelectedDefenderPrefab
                : defenderPrefab;

            if (prefab == null) return false;                              // No valid prefab

            // Instantiate above the node (arbitrary height to avoid immediate collisions)
            var instance = Instantiate(prefab, transform.position + Vector3.up * 6f, Quaternion.identity);

            // Disable all colliders temporarily
            Collider[] cols = instance.GetComponentsInChildren<Collider>();
            foreach (var c in cols) c.enabled = false;

            // Cast a ray downward to find terrain
            Vector3 probeStart = transform.position + Vector3.up * 10f;
            if (Physics.Raycast(probeStart, Vector3.down, out RaycastHit hit, 50f))
            {
                Vector3 finalPosition = hit.point;

                // Compute lowest point of all colliders relative to pivot
                float lowestLocal = float.MaxValue;
                foreach (var c in cols)
                {
                    float localBottom = c.bounds.min.y - instance.transform.position.y;
                    if (localBottom < lowestLocal) lowestLocal = localBottom;
                }

                // Move defender down so bottom sits on terrain
                finalPosition.y -= lowestLocal;
                instance.transform.position = finalPosition;
            }

            // Re-enable colliders
            foreach (var c in cols) c.enabled = true;

            // Assign node reference to defender
            if (instance.TryGetComponent(out Defender defender))
                defender.OriginNode = this;

            // Mark node as occupied and notify shop
            occupied = true;
            WeaponShop.Instance.OnDefenderPlaced();

            // Optionally hide node to prevent re-placement
            gameObject.SetActive(false);

            return true;
        }

        /// <summary>
        /// Frees this node when the defender dies or is removed.
        /// Makes the node available for future placements.
        /// </summary>
        public void Free()
        {
            occupied = false;
            gameObject.SetActive(true); // Show node again
        }
    }
}