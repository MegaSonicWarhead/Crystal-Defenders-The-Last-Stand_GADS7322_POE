using UnityEngine;
using CrystalDefenders.Units;

namespace CrystalDefenders.Combat
{
    [DisallowMultipleComponent]
    public class AutoAttackBaseTower : MonoBehaviour
    {
        [Header("Attack Settings")]
        public float range = 6f; // Base tower attack radius
        public float shotsPerSecond = 1.5f; // Fire rate
        public int baseDamage = 15; // Base damage before multiplier
        public float damageMultiplier = 0.5f; // Damage reduction factor (50%)

        [Header("Projectile Variants")]
        public GameObject defaultProjectile; // Normal projectile
        public GameObject fireProjectile;    // Fire projectile
        public GameObject poisonProjectile;  // Poison projectile

        [Header("References")]
        [SerializeField] private Transform firePoint; // Point from which projectiles spawn

        [Header("Debug")]
        public bool debugLogs = true; // Enable debug logging for shots

        private float lastShotTime = -999f; // Tracks last shot time for cooldown

        private void Update()
        {
            // Check every frame if we can shoot
            TryShoot();
        }

        /// <summary>
        /// Attempts to shoot at a target if cooldown allows
        /// </summary>
        private void TryShoot()
        {
            float now = Time.time;
            float cooldown = 1f / Mathf.Max(0.01f, shotsPerSecond); // Prevent division by zero
            if (now - lastShotTime < cooldown) return;

            Enemy target = FindNearestEnemyInRange();
            if (target == null) return;

            // Choose the projectile that matches enemy's weakness
            GameObject chosenProjectile = SelectBestProjectileForEnemy(target);
            if (chosenProjectile == null) return;

            // Apply reduced damage
            int finalDamage = Mathf.RoundToInt(baseDamage * damageMultiplier);

            ShootProjectile(target, chosenProjectile, finalDamage);
            lastShotTime = now; // Reset cooldown
        }

        /// <summary>
        /// Instantiates and initializes the projectile
        /// </summary>
        private void ShootProjectile(Enemy target, GameObject projectilePrefab, int damage)
        {
            if (projectilePrefab == null || firePoint == null) return;

            GameObject proj = Instantiate(projectilePrefab, firePoint.position, Quaternion.identity);
            Projectile p = proj.GetComponent<Projectile>();
            if (p != null)
            {
                p.Initialize(target.transform, damage);
            }

            if (debugLogs)
                Debug.Log($"[BaseTower] Fired {projectilePrefab.name} at {target.name} for {damage} damage");
        }

        /// <summary>
        /// Chooses the most effective projectile based on the enemy's requiredDamageTag
        /// </summary>
        private GameObject SelectBestProjectileForEnemy(Enemy enemy)
        {
            var enemyHealth = enemy.GetComponent<Health>();
            if (enemyHealth == null) return defaultProjectile;

            string requiredTag = enemyHealth.requiredDamageTag;

            if (requiredTag == "fire" && fireProjectile != null) return fireProjectile;
            if (requiredTag == "poison" && poisonProjectile != null) return poisonProjectile;

            return defaultProjectile; // Fallback for normal enemies
        }

        /// <summary>
        /// Finds the nearest enemy within range
        /// </summary>
        private Enemy FindNearestEnemyInRange()
        {
            Enemy best = null;
            float bestD = float.MaxValue;

            foreach (var e in EnemyRegistry.Enemies)
            {
                if (e == null) continue;

                float d = Vector3.Distance(transform.position, e.transform.position);
                if (d < range && d < bestD)
                {
                    bestD = d;
                    best = e;
                }
            }

            return best;
        }
    }
}