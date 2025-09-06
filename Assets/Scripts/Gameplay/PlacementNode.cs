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

        private void OnMouseDown()
        {
            TryPlaceDefender();
        }

        public bool TryPlaceDefender()
        {
            if (occupied || defenderPrefab == null) return false;
            if (!WeaponShop.Instance.HasDefenderToPlace) return false;

            Instantiate(defenderPrefab, transform.position, Quaternion.identity);
            occupied = true;
            WeaponShop.Instance.OnDefenderPlaced();
            gameObject.SetActive(false); // Optional: hide node
            return true;
        }
    }
}


