using UnityEngine;
using CrystalDefenders.Units;

namespace CrystalDefenders.Combat
{
    [DisallowMultipleComponent]
    public class AutoAttack : MonoBehaviour
    {
        [Header("Attack Settings")]
        public float range = 5f;
        public float shotsPerSecond = 2f;
        public int damagePerHit = 15;

        [Header("Projectile Settings")]
        [SerializeField] private GameObject projectilePrefab;
        [SerializeField] private Transform firePoint; // Empty child as muzzle

        private float lastShotTime = -999f;
        private string cachedDamageTag; // ✅ Cache to avoid GetComponent every frame

        private void Awake()
        {
            // ✅ Cache damage tag on startup for performance
            cachedDamageTag = DetectDamageTagFromProjectile();
        }

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

            ShootProjectile(target);
            lastShotTime = now;
        }

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

        private Enemy FindNearestEnemyInRange()
        {
            Enemy best = null;
            float bestD = float.MaxValue;

            foreach (var e in EnemyRegistry.Enemies)
            {
                if (e == null) continue;

                // ✅ Smart targeting: Only shoot enemies this tower can actually damage
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

        // ✅ Fast detection of what type of damage this projectile applies
        private string DetectDamageTagFromProjectile()
        {
            if (projectilePrefab == null) return null;

            if (projectilePrefab.GetComponent<PoisonArrowProjectile>() != null) return "poison";
            if (projectilePrefab.GetComponent<FireballAoEProjectile>() != null) return "fire";

            // Default projectile (normal damage)
            return null;
        }

        // ✅ Checks if this projectile can hurt the enemy based on `requiredDamageTag`
        private bool CanDamageEnemy(Enemy enemy, string towerDamageTag)
        {
            var enemyHealth = enemy.GetComponent<Health>();
            if (enemyHealth == null) return true;

            string enemyRequiredTag = enemyHealth.requiredDamageTag;

            // --- Default projectile logic ---
            if (string.IsNullOrEmpty(towerDamageTag))
            {
                // Default projectile should only hit enemies with NO tag
                return string.IsNullOrEmpty(enemyRequiredTag);
            }

            // --- Special projectiles (fire / poison) ---
            if (string.IsNullOrEmpty(enemyRequiredTag))
                return false; // Don't hit tagless enemies with fire/poison

            // --- Only hit if tags match ---
            return enemyRequiredTag == towerDamageTag;
        }
    }
}