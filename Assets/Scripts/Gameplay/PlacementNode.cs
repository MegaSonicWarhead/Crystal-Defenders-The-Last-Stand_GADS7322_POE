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
            if (occupied) return false;                       // Node already occupied
            if (!WeaponShop.Instance.HasDefenderToPlace) return false; // No defender selected in shop

            // Determine which prefab to place (shop selection overrides default)
            var prefab = WeaponShop.Instance.SelectedDefenderPrefab != null
                ? WeaponShop.Instance.SelectedDefenderPrefab
                : defenderPrefab;

            if (prefab == null) return false;                // No valid prefab available

            // Instantiate the defender at this node
            var instance = Instantiate(prefab, transform.position, Quaternion.identity);

            // Snap defender to terrain height to prevent sinking/floating
            Vector3 probeStart = instance.transform.position + Vector3.up * 5f;
            if (Physics.Raycast(probeStart, Vector3.down, out RaycastHit hit, 50f))
            {
                instance.transform.position = hit.point;
            }

            // Link the defender to this node for future reference
            if (instance.TryGetComponent(out Defender defender))
            {
                defender.OriginNode = this;
            }

            // Mark node as occupied and notify WeaponShop
            occupied = true;
            WeaponShop.Instance.OnDefenderPlaced();

            // Hide the node while occupied to prevent re-placement
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