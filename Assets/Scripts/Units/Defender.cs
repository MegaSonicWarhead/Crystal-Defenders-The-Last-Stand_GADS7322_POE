using System.Collections.Generic;
using UnityEngine;
using CrystalDefenders.Combat;
using CrystalDefenders.Gameplay; // 👈 Needed for PlacementNode reference

namespace CrystalDefenders.Units
{
    [RequireComponent(typeof(Health))]
    [RequireComponent(typeof(AutoAttack))]
    public class Defender : MonoBehaviour
    {
        public const int Cost = 100;
        public const int RepairCost = 25;

        [Header("Placement Settings")]
        [SerializeField] private GameObject placementNodePrefab; // You can remove this now (no longer needed)

        protected Health health;
        private AutoAttack autoAttack;

        // 🔗 Link to the node that spawned this defender
        public PlacementNode OriginNode { get; set; }

        private static readonly List<Defender> registry = new List<Defender>();
        public static IReadOnlyList<Defender> Registry => registry;

        private void Awake()
        {
            health = GetComponent<Health>();
            health.SetMaxHealth(200, true);

            autoAttack = GetComponent<AutoAttack>();
            ConfigureBaseStats();

            if (UIManager.Instance != null)
                UIManager.Instance.AttachHealthBar(health);

            health.onDeath.AddListener(OnDefenderDestroyed);
        }

        protected virtual void ConfigureBaseStats()
        {
            autoAttack.range = 5f;
            autoAttack.shotsPerSecond = 2f;
            autoAttack.damagePerHit = 15;
        }

        private void OnEnable()
        {
            if (!registry.Contains(this)) registry.Add(this);
        }

        private void OnDisable()
        {
            registry.Remove(this);
        }

        public void RepairFull()
        {
            if (health != null)
                health.Heal(health.MaxHealth - health.CurrentHealth);
        }

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
        protected virtual void OnDefenderDestroyed()
        {
            Debug.Log($"{gameObject.name} destroyed!");

            // ✅ Instead of spawning a new node, free the original one
            if (OriginNode != null)
            {
                OriginNode.Free();
            }

            // Optional: delay destroy to allow death effects
            Destroy(gameObject);
        }
    }
}