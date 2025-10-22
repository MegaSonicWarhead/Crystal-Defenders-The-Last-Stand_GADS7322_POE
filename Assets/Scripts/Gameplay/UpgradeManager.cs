using CrystalDefenders.Units;
using UnityEngine;

namespace CrystalDefenders.Gameplay
{
    /// <summary>
    /// Handles upgrading of towers and ensures resource checks via ResourceManager.
    /// Works in tandem with WeaponShop and SelectableTower.
    /// </summary>
    public class UpgradeManager : MonoBehaviour
    {
        // --- Singleton instance ---
        public static UpgradeManager Instance { get; private set; }

        [Header("Upgrade Settings")]
        [SerializeField] private int baseUpgradeCost = 100; // Default upgrade cost if not overridden per tower

        private IUpgradeable selectedTower;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        /// <summary>
        /// Selects a tower for upgrade operations.
        /// </summary>
        /// <param name="tower">The tower that implements IUpgradeable</param>
        public void SelectTower(IUpgradeable tower)
        {
            selectedTower = tower;
            if (tower != null)
                Debug.Log($"[UpgradeManager] Selected tower: {((MonoBehaviour)tower).gameObject.name}");
        }

        /// <summary>
        /// Deselects the currently selected tower.
        /// </summary>
        public void DeselectTower()
        {
            selectedTower = null;
        }

        /// <summary>
        /// Checks if the player can afford the default upgrade cost.
        /// </summary>
        public bool CanAfford(int cost)
        {
            return ResourceManager.Instance != null && ResourceManager.Instance.CanAfford(cost);
        }

        /// <summary>
        /// Spends resources if possible.
        /// </summary>
        public bool SpendResources(int cost)
        {
            if (ResourceManager.Instance == null)
            {
                Debug.LogWarning("[UpgradeManager] ResourceManager not found!");
                return false;
            }

            return ResourceManager.Instance.Spend(cost);
        }

        /// <summary>
        /// Attempts to upgrade the currently selected tower.
        /// </summary>
        public void TryUpgradeSelectedTower()
        {
            if (selectedTower == null)
            {
                Debug.Log("[UpgradeManager] No tower selected for upgrade.");
                return;
            }

            if (!selectedTower.CanUpgrade())
            {
                Debug.Log("[UpgradeManager] Tower is already at max upgrade tier.");
                return;
            }

            int cost = baseUpgradeCost;

            if (!SpendResources(cost))
            {
                Debug.Log("[UpgradeManager] Not enough resources to upgrade.");
                return;
            }

            selectedTower.ApplyUpgrade();
            Debug.Log("[UpgradeManager] Tower upgraded successfully!");
        }
    }
}