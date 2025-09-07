using System.Collections.Generic;
using UnityEngine;
using CrystalDefenders.Combat;

namespace CrystalDefenders.Units
{
    [RequireComponent(typeof(Health))]
    [RequireComponent(typeof(AutoAttack))]
    public class Defender : MonoBehaviour
    {
        public const int Cost = 100;
        public const int RepairCost = 25;

        [Header("Placement Settings")]
        [SerializeField] private GameObject placementNodePrefab; // Assign in Inspector

        private Health health;
        private static readonly List<Defender> registry = new List<Defender>();
        public static IReadOnlyList<Defender> Registry => registry;

        private void Awake()
        {
            health = GetComponent<Health>();
            health.SetMaxHealth(200, true);

            var aa = GetComponent<AutoAttack>();
            aa.range = 5f;
            aa.shotsPerSecond = 2f;
            aa.damagePerHit = 15;

            if (UIManager.Instance != null)
                UIManager.Instance.AttachHealthBar(health);

            // Subscribe to death event
            health.onDeath.AddListener(OnDefenderDestroyed);
        }

        private void OnEnable()
        {
            if (!registry.Contains(this)) registry.Add(this);
        }

        private void OnDisable()
        {
            registry.Remove(this);
        }

        // --- Repair full health ---
        public void RepairFull()
        {
            if (health != null)
                health.Heal(health.MaxHealth - health.CurrentHealth);
        }

        // --- Repair partial (custom amount) ---
        public void RepairAmount(int amount)
        {
            if (health != null)
                health.Heal(amount);
        }

        public bool IsDamaged()
        {
            return health != null && health.CurrentHealth < health.MaxHealth;
        }

        // --- Handle Defender destruction ---
        private void OnDefenderDestroyed()
        {
            Debug.Log($"{gameObject.name} destroyed! Replacing with placement node.");

            if (placementNodePrefab != null)
            {
                Instantiate(placementNodePrefab, transform.position, Quaternion.identity);
            }

            Destroy(gameObject);
        }
    }
}


