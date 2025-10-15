using CrystalDefenders.Combat;
using UnityEngine;

namespace CrystalDefenders.Units
{
    /// <summary>
    /// Configures a Fire Mage defender at runtime.
    /// Sets up AutoAttack parameters and assigns the fireball projectile prefab.
    /// </summary>
    [RequireComponent(typeof(Defender))]
    [RequireComponent(typeof(AutoAttack))]
    [RequireComponent(typeof(GroundAnchor))]
    public class FireMageConfig : MonoBehaviour
    {
        [Header("Fire Mage Settings")]
        [SerializeField] private GameObject fireballProjectilePrefab;
        [SerializeField] private float range = 5.5f;
        [SerializeField] private float shotsPerSecond = 1.2f;
        [SerializeField] private int damagePerHit = 12;

        private void Awake()
        {
            var autoAttack = GetComponent<AutoAttack>();

            // Set basic attack stats
            autoAttack.range = range;
            autoAttack.shotsPerSecond = shotsPerSecond;
            autoAttack.damagePerHit = damagePerHit;

            // Assign projectile prefab (private field via reflection)
            var projField = typeof(AutoAttack).GetField(
                "projectilePrefab",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance
            );

            if (projField != null && fireballProjectilePrefab != null)
            {
                projField.SetValue(autoAttack, fireballProjectilePrefab);
            }
        }
    }
}