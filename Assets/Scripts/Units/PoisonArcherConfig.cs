using CrystalDefenders.Combat;
using UnityEngine;

namespace CrystalDefenders.Units
{
    /// <summary>
    /// Configures a Poison Archer defender with its projectile, range, and attack stats.
    /// </summary>
    [RequireComponent(typeof(Defender))]
    [RequireComponent(typeof(AutoAttack))]
    [RequireComponent(typeof(GroundAnchor))]
    public class PoisonArcherConfig : MonoBehaviour
    {
        [Header("Projectile Settings")]
        [SerializeField] private GameObject poisonArrowProjectilePrefab;

        [Header("Attack Stats")]
        [SerializeField] private float range = 6f;
        [SerializeField] private float shotsPerSecond = 1.8f;
        [SerializeField] private int damagePerHit = 10;

        private void Awake()
        {
            // Configure AutoAttack component with this defender's stats
            var aa = GetComponent<AutoAttack>();
            aa.range = range;
            aa.shotsPerSecond = shotsPerSecond;
            aa.damagePerHit = damagePerHit;

            // Assign projectile prefab safely via reflection (private field)
            var projField = typeof(AutoAttack).GetField(
                "projectilePrefab",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance
            );
            if (projField != null && poisonArrowProjectilePrefab != null)
            {
                projField.SetValue(aa, poisonArrowProjectilePrefab);
            }
        }
    }
}