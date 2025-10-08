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
			// Snap to terrain height to avoid sinking/falling due to slight offsets
			Vector3 probeStart = instance.transform.position + Vector3.up * 5f;
			if (Physics.Raycast(probeStart, Vector3.down, out RaycastHit hit, 50f))
			{
				instance.transform.position = hit.point;
			}
            occupied = true;
            WeaponShop.Instance.OnDefenderPlaced();
            gameObject.SetActive(false); // Optional: hide node
            return true;
        }
    }
}


