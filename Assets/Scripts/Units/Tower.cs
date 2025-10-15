using CrystalDefenders.Combat;
using TMPro;
using UnityEngine;

namespace CrystalDefenders.Units
{
    /// <summary>
    /// Represents the main crystal tower that must be defended.
    /// Handles health, UI updates, and integrates with the BaseTower auto-attack system.
    /// </summary>
    [RequireComponent(typeof(Health))]
    [RequireComponent(typeof(AutoAttackBaseTower))] // Ensures the tower has offensive capability
    public class Tower : MonoBehaviour
    {
        // Cached health reference
        private Health health;

        [Header("UI Panel")]
        [Tooltip("Reference to the UI Text element used to display tower health.")]
        public TMP_Text healthText; // Assign in inspector (UI panel)

        private void Awake()
        {
            // Initialize and configure health component
            health = GetComponent<Health>();
            health.SetMaxHealth(2000, true); // Set starting health and apply immediately
            health.onDeath.AddListener(OnTowerDestroyed);

            // Subscribe to health change events to keep UI updated
            health.onDamaged.AddListener(UpdateHealthUI);
            health.onHealed.AddListener(UpdateHealthUI);

            // Configure the tower's offensive capabilities using its AutoAttack script
            var aa = GetComponent<AutoAttackBaseTower>();
            aa.range = 6f;
            aa.shotsPerSecond = 1.5f;
            aa.baseDamage = 20;           // Raw damage before multiplier is applied
            aa.damageMultiplier = 0.5f;   // Scales base damage output to 50%

            // Register this tower with the UI manager for tracking
            if (UIManager.Instance != null)
                UIManager.Instance.TrackTower(this);

            // Initialize health display at game start
            UpdateHealthUI(0);
        }

        /// <summary>
        /// Updates the tower's health UI whenever damage or healing occurs.
        /// </summary>
        private void UpdateHealthUI(int _)
        {
            if (healthText != null)
            {
                healthText.text = $"Crystal Tower Health: {health.CurrentHealth}/{health.MaxHealth}";
            }
        }

        /// <summary>
        /// Triggered when the tower's health reaches zero. Handles destruction and game fail state.
        /// </summary>
        private void OnTowerDestroyed()
        {
            Debug.Log($"Tower {gameObject.name} destroyed");

            // Notify the game manager of defeat condition
            Gameplay.GameManager.Instance?.OnTowerDestroyed();

            // Optionally reflect the destruction in the UI
            if (healthText != null)
                healthText.text = "Tower Destroyed";
        }
    }
}