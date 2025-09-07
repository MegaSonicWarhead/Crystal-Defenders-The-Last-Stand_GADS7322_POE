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

            // Spawn a projectile instead of applying instant damage
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

            Debug.Log($"{gameObject.name} fired a projectile at {target.name}");
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


