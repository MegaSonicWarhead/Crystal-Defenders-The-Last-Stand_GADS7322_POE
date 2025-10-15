using CrystalDefenders.Combat;
using UnityEngine;
using CrystalDefenders.Gameplay;

namespace CrystalDefenders.Units
{
    /// <summary>
    /// Ranged enemy type. Only vulnerable to fire damage.
    /// Shoots projectiles at closest tower or defender.
    /// </summary>
    [RequireComponent(typeof(Health))]
    public class EnemyRanged : Enemy
    {
        [Header("Ranged Attack Settings")]
        public GameObject projectilePrefab;
        public float projectileSpeed = 10f;
        public int projectileDamage = 8;
        public string projectileDamageTag = "fire"; // tag assigned to projectile

        private void Awake()
        {
            // Only fire damage affects this enemy
            var h = GetComponent<Health>();
            h.requiredDamageTag = "fire";

            // Subscribe to death event
            h.onDeath.AddListener(OnDeath);
        }

        private void OnDeath()
        {
            Destroy(gameObject);
            WaveManager.Instance?.OnEnemyDied();
        }

        private new void Update()
        {
            base.Update();
            TryAttackTargets();
        }

        /// <summary>
        /// Attack closest valid target if cooldown elapsed
        /// </summary>
        private void TryAttackTargets()
        {
            if (Time.time - lastAttackTime < attackCooldown) return;

            var closestTarget = GetClosestDamageable();
            if (closestTarget == null) return;

            if (Vector3.Distance(transform.position, closestTarget.transform.position) <= attackRange)
            {
                ShootProjectileAt(closestTarget);
                lastAttackTime = Time.time;
            }
        }

        /// <summary>
        /// Instantiates projectile and assigns damage, tag, and speed
        /// </summary>
        private void ShootProjectileAt(GameObject target)
        {
            if (projectilePrefab == null || target == null) return;

            GameObject proj = Instantiate(projectilePrefab, transform.position, Quaternion.identity);
            Projectile projectile = proj.GetComponent<Projectile>();
            if (projectile != null)
            {
                projectile.damage = projectileDamage;
                projectile.damageTag = projectileDamageTag; // ensure projectile respects damage type
                projectile.target = target.transform;
                projectile.speed = projectileSpeed;
            }
        }

        /// <summary>
        /// Finds the closest damageable object (tower or defender)
        /// </summary>
        private GameObject GetClosestDamageable()
        {
            GameObject best = null;
            float bestDistance = float.MaxValue;

            // Check tower first
            var gm = GameManager.Instance;
            if (gm != null && gm.Tower != null)
            {
                float dt = Vector3.Distance(transform.position, gm.Tower.position);
                if (dt < bestDistance)
                {
                    bestDistance = dt;
                    best = gm.Tower.gameObject;
                }
            }

            // Then check defenders
            foreach (var def in Defender.Registry)
            {
                if (def == null) continue;
                float dd = Vector3.Distance(transform.position, def.transform.position);
                if (dd < bestDistance)
                {
                    bestDistance = dd;
                    best = def.gameObject;
                }
            }

            return best;
        }
    }
}