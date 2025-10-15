using System.Collections.Generic;
using UnityEngine;
using CrystalDefenders.Combat;
using CrystalDefenders.Gameplay; // Needed for PlacementNode reference

namespace CrystalDefenders.Units
{
    /// <summary>
    /// Base class for all defender units.
    /// Handles health, auto-attack, placement, repair, and destruction logic.
    /// </summary>
    [RequireComponent(typeof(Health))]
    [RequireComponent(typeof(AutoAttack))]
    public class Defender : MonoBehaviour
    {
        // --- Cost Constants ---
        public const int Cost = 100;          // Resource cost to place this defender
        public const int RepairCost = 25;     // Cost to repair a defender partially

        [Header("Placement Settings")]
        [SerializeField] private GameObject placementNodePrefab; // Legacy; no longer used

        protected Health health;               // Reference to Health component
        private AutoAttack autoAttack;         // Reference to AutoAttack component

        /// <summary>
        /// Reference to the PlacementNode that spawned this defender.
        /// Used to free the node upon destruction.
        /// </summary>
        public PlacementNode OriginNode { get; set; }

        // --- Static registry of all active defenders ---
        private static readonly List<Defender> registry = new List<Defender>();
        public static IReadOnlyList<Defender> Registry => registry;

        private void Awake()
        {
            // Initialize health and set maximum
            health = GetComponent<Health>();
            health.SetMaxHealth(200, true);

            // Initialize auto-attack and configure default stats
            autoAttack = GetComponent<AutoAttack>();
            ConfigureBaseStats();

            // Attach health bar to UI if available
            if (UIManager.Instance != null)
                UIManager.Instance.AttachHealthBar(health);

            // Subscribe to death event
            health.onDeath.AddListener(OnDefenderDestroyed);
        }

        /// <summary>
        /// Sets default attack stats.
        /// Can be overridden by derived classes for specialized defenders.
        /// </summary>
        protected virtual void ConfigureBaseStats()
        {
            autoAttack.range = 5f;
            autoAttack.shotsPerSecond = 2f;
            autoAttack.damagePerHit = 15;
        }

        private void OnEnable()
        {
            // Add to global registry for easy tracking
            if (!registry.Contains(this)) registry.Add(this);
        }

        private void OnDisable()
        {
            // Remove from registry to prevent stale references
            registry.Remove(this);
        }

        // --- Repair Logic ---
        /// <summary>
        /// Fully repairs the defender to maximum health.
        /// </summary>
        public void RepairFull()
        {
            if (health != null)
                health.Heal(health.MaxHealth - health.CurrentHealth);
        }

        /// <summary>
        /// Repairs the defender by a specific amount.
        /// </summary>
        public void RepairAmount(int amount)
        {
            if (health != null)
                health.Heal(amount);
        }

        /// <summary>
        /// Checks if the defender is currently damaged.
        /// </summary>
        public bool IsDamaged()
        {
            return health != null && health.CurrentHealth < health.MaxHealth;
        }

        // --- Defender Destruction ---
        /// <summary>
        /// Handles defender destruction logic.
        /// Frees its placement node and destroys the GameObject.
        /// Can be overridden for custom behavior in subclasses.
        /// </summary>
        protected virtual void OnDefenderDestroyed()
        {
            Debug.Log($"{gameObject.name} destroyed!");

            // Free the original placement node instead of spawning a new one
            if (OriginNode != null)
            {
                OriginNode.Free();
            }

            // Optional: destroy defender GameObject (death effects can run before this)
            Destroy(gameObject);
        }
    }
}