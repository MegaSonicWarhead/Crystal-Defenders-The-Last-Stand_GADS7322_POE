using UnityEngine;
using CrystalDefenders.Units;

namespace CrystalDefenders.Combat
{
    [DisallowMultipleComponent]
    public class AutoAttackBaseTower : MonoBehaviour
    {
        [Header("Attack Settings")]
        public float range = 6f;
        public float shotsPerSecond = 1.5f;
        public int baseDamage = 15; // Before reduction
        public float damageMultiplier = 0.5f; // 50% damage

        [Header("Projectile Variants")]
        public GameObject defaultProjectile;
        public GameObject fireProjectile;
        public GameObject poisonProjectile;

        [Header("References")]
        [SerializeField] private Transform firePoint; // muzzle

        [Header("Debug")]
        public bool debugLogs = true;

        private float lastShotTime = -999f;

        private void Update()
        {
            TryShoot();
        }

        private void TryShoot()
        {
            float now = Time.time;
            float cooldown = 1f / Mathf.Max(0.01f, shotsPerSecond);
            if (now - lastShotTime < cooldown) return;

            Enemy target = FindNearestEnemyInRange();
            if (target == null) return;

            // Pick best projectile based on enemy weakness
            GameObject chosenProjectile = SelectBestProjectileForEnemy(target);
            if (chosenProjectile == null) return;

            // Apply reduced damage
            int finalDamage = Mathf.RoundToInt(baseDamage * damageMultiplier);

            ShootProjectile(target, chosenProjectile, finalDamage);
            lastShotTime = now;
        }

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

        private GameObject SelectBestProjectileForEnemy(Enemy enemy)
        {
            var enemyHealth = enemy.GetComponent<Health>();
            if (enemyHealth == null) return defaultProjectile;

            string requiredTag = enemyHealth.requiredDamageTag;

            if (requiredTag == "fire" && fireProjectile != null) return fireProjectile;
            if (requiredTag == "poison" && poisonProjectile != null) return poisonProjectile;

            return defaultProjectile; // Default projectile for normal enemies
        }

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