using CrystalDefenders.Combat;
using UnityEngine;
using CrystalDefenders.Gameplay;

namespace CrystalDefenders.Units
{
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
            var h = GetComponent<Health>();
            h.requiredDamageTag = "fire"; // only fire damages this enemy

            // Subscribe to death event
            h.onDeath.AddListener(OnDeath);
        }

        private void OnDeath()
        {
            // Destroy the enemy
            Destroy(gameObject);
            WaveManager.Instance?.OnEnemyDied();
        }

        private new void Update()
        {
            base.Update();
            TryAttackTargets();
        }

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

        private void ShootProjectileAt(GameObject target)
        {
            if (projectilePrefab == null) return;

            GameObject proj = Instantiate(projectilePrefab, transform.position, Quaternion.identity);
            Projectile projectile = proj.GetComponent<Projectile>();
            if (projectile != null)
            {
                projectile.damage = projectileDamage;
                projectile.damageTag = projectileDamageTag; // <--- Assign tag
                projectile.target = target.transform;
                projectile.speed = projectileSpeed;
            }
        }

        private GameObject GetClosestDamageable()
        {
            GameObject best = null;
            float bestDistance = float.MaxValue;

            var gm = Gameplay.GameManager.Instance;
            if (gm != null && gm.Tower != null)
            {
                float dt = Vector3.Distance(transform.position, gm.Tower.position);
                if (dt < bestDistance)
                {
                    bestDistance = dt;
                    best = gm.Tower.gameObject;
                }
            }

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