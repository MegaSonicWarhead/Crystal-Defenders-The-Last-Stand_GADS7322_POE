using UnityEngine;
using CrystalDefenders.Units;

namespace CrystalDefenders.Gameplay
{
    [DisallowMultipleComponent]
    public class PlacementNode : MonoBehaviour
    {
        public Defender defenderPrefab;
        private bool occupied = false;

        public void Initialize()
        {
            occupied = false;
            gameObject.SetActive(true);
        }

        public bool IsOccupied => occupied;
        public bool IsAvailable => !occupied && gameObject.activeInHierarchy;

        private void OnMouseDown()
        {
            TryPlaceDefender();
        }

        public bool TryPlaceDefender()
        {
            if (occupied) return false;
            if (!WeaponShop.Instance.HasDefenderToPlace) return false;

            var prefab = WeaponShop.Instance.SelectedDefenderPrefab != null
                ? WeaponShop.Instance.SelectedDefenderPrefab
                : defenderPrefab;
            if (prefab == null) return false;

            var instance = Instantiate(prefab, transform.position, Quaternion.identity);

            //// Force immediate ground alignment if it has a GroundAnchor
            //if (instance.TryGetComponent(out GroundAnchor anchor))
            //{
            //    anchor.SnapToGround();
            //}

            // Snap to terrain height to avoid sinking/falling due to slight offsets
            Vector3 probeStart = instance.transform.position + Vector3.up * 5f;
            if (Physics.Raycast(probeStart, Vector3.down, out RaycastHit hit, 50f))
            {
                instance.transform.position = hit.point;
            }

            // Connect the defender to this node
            if (instance.TryGetComponent(out Defender defender))
            {
                defender.OriginNode = this;
            }

            occupied = true;
            WeaponShop.Instance.OnDefenderPlaced();
            gameObject.SetActive(false); // Hide node when occupied
            return true;
        }

        // Called when a defender dies or is destroyed
        public void Free()
        {
            occupied = false;
            gameObject.SetActive(true);  // Show node again
        }
    }
}