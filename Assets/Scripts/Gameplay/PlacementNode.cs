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
            if (!Gameplay.ResourceManager.Instance.Spend(Defender.Cost)) return false;
            var d = Instantiate(defenderPrefab, transform.position, Quaternion.identity);
            occupied = true;
            // Optionally disable visual
            gameObject.SetActive(false);
            return true;
        }
    }
}


