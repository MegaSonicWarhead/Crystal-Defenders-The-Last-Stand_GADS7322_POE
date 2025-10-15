using UnityEngine;
using CrystalDefenders.Units;

namespace CrystalDefenders.Combat
{
    [DisallowMultipleComponent]
    public class AutoAttack : MonoBehaviour
    {
        [Header("Attack Settings")]
        public float range = 5f; // Maximum distance to acquire targets
        public float shotsPerSecond = 2f; // Fire rate
        public int damagePerHit = 15; // Damage applied per shot

        [Header("Projectile Settings")]
        [SerializeField] private GameObject projectilePrefab; // Prefab for this tower's projectile
        [SerializeField] private Transform firePoint; // Muzzle point for spawning projectiles

        private float lastShotTime = -999f; // Tracks last shot to implement cooldown
        private string cachedDamageTag; // Cached projectile damage type to avoid repeated GetComponent checks

        private void Awake()
        {
            // Cache the damage type of the projectile (fire, poison, or default)
            cachedDamageTag = DetectDamageTagFromProjectile();
        }

        private void Update()
        {
            // Try shooting every frame
            TryShoot();
        }

        /// <summary>
        /// Checks cooldown and finds nearest valid enemy in range
        /// </summary>
        private void TryShoot()
        {
            float now = Time.time;
            float cooldown = 1f / Mathf.Max(0.01f, shotsPerSecond); // Avoid division by zero
            if (now - lastShotTime < cooldown) return;

            Enemy target = FindNearestEnemyInRange();
            if (target == null) return;

            ShootProjectile(target);
            lastShotTime = now; // Reset cooldown
        }

        /// <summary>
        /// Instantiates a projectile and assigns its target
        /// </summary>
        private void ShootProjectile(Enemy target)
        {
            if (projectilePrefab == null || firePoint == null) return;

            GameObject proj = Instantiate(projectilePrefab, firePoint.position, Quaternion.identity);
            Projectile p = proj.GetComponent<Projectile>();
            if (p != null)
            {
                p.Initialize(target.transform, damagePerHit);
            }

            // Optional debug
            // Debug.Log($"{gameObject.name} fired a projectile at {target.name}");
        }

        /// <summary>
        /// Finds the nearest enemy in range that this tower can damage
        /// </summary>
        private Enemy FindNearestEnemyInRange()
        {
            Enemy best = null;
            float bestD = float.MaxValue;

            foreach (var e in EnemyRegistry.Enemies)
            {
                if (e == null) continue;

                // Skip enemies this projectile can't harm
                if (!CanDamageEnemy(e, cachedDamageTag)) continue;

                float d = Vector3.Distance(transform.position, e.transform.position);
                if (d < range && d < bestD)
                {
                    bestD = d;
                    best = e;
                }
            }
            return best;
        }

        /// <summary>
        /// Detects the type of damage this projectile applies
        /// </summary>
        private string DetectDamageTagFromProjectile()
        {
            if (projectilePrefab == null) return null;

            if (projectilePrefab.GetComponent<PoisonArrowProjectile>() != null) return "poison";
            if (projectilePrefab.GetComponent<FireballAoEProjectile>() != null) return "fire";

            // Default projectile (normal damage)
            return null;
        }

        /// <summary>
        /// Returns true if this projectile can damage the enemy based on the requiredDamageTag
        /// </summary>
        private bool CanDamageEnemy(Enemy enemy, string towerDamageTag)
        {
            var enemyHealth = enemy.GetComponent<Health>();
            if (enemyHealth == null) return true; // No health component? Assume damage is allowed

            string enemyRequiredTag = enemyHealth.requiredDamageTag;

            // Default projectile logic (no tag)
            if (string.IsNullOrEmpty(towerDamageTag))
            {
                return string.IsNullOrEmpty(enemyRequiredTag); // Only hit enemies without tags
            }

            // Special projectiles (fire / poison)
            if (string.IsNullOrEmpty(enemyRequiredTag))
                return false; // Don't hit enemies without tags

            // Hit only if tags match
            return enemyRequiredTag == towerDamageTag;
        }
    }
}