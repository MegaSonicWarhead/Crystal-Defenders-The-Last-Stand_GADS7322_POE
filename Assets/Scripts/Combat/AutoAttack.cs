using UnityEngine;
using CrystalDefenders.Units;
using System;

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
        private Enemy currentTarget;

        public Action OnBeforeShoot; // Hook for Tower to choose projectile dynamically

        private void Update()
        {
            TryShoot();
        }

        private void TryShoot()
        {
            float now = Time.time;
            float cooldown = 1f / Mathf.Max(0.01f, shotsPerSecond);
            if (now - lastShotTime < cooldown) return;

            currentTarget = FindNearestEnemyInRange();
            if (currentTarget == null) return;

            // Allow Tower to select proper projectile
            OnBeforeShoot?.Invoke();

            // Spawn projectile
            ShootProjectile(currentTarget);

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
            string towerDamageTag = GetTowerDamageTag();

            foreach (var e in EnemyRegistry.Enemies)
            {
                if (e == null) continue;

                if (!CanDamageEnemy(e, towerDamageTag)) continue;

                float d = Vector3.Distance(transform.position, e.transform.position);
                if (d < range && d < bestD)
                {
                    bestD = d;
                    best = e;
                }
            }
            return best;
        }

        private string GetTowerDamageTag()
        {
            if (projectilePrefab == null) return null;

            if (projectilePrefab.GetComponent<PoisonArrowProjectile>() != null) return "poison";
            if (projectilePrefab.GetComponent<FireballAoEProjectile>() != null) return "fire";
            return null;
        }

        private bool CanDamageEnemy(Enemy enemy, string towerDamageTag)
        {
            var enemyHealth = enemy.GetComponent<Health>();
            if (enemyHealth == null) return true;

            string enemyRequiredTag = enemyHealth.requiredDamageTag;
            if (string.IsNullOrEmpty(enemyRequiredTag)) return true;

            return enemyRequiredTag == towerDamageTag;
        }

        public void SetProjectilePrefab(GameObject prefab) => projectilePrefab = prefab;
        public Transform GetFirePoint() => firePoint;
        public Enemy GetCurrentTarget() => currentTarget;
    }
}
