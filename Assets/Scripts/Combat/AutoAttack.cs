using UnityEngine;
using CrystalDefenders.Units;

namespace CrystalDefenders.Combat
{
    [DisallowMultipleComponent]
    public class AutoAttack : MonoBehaviour
    {
        public float range = 5f;
        public float shotsPerSecond = 2f;
        public int damagePerHit = 15;

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

            var h = target.GetComponent<Health>();
            if (h != null)
            {
                h.ApplyDamage(damagePerHit);
                lastShotTime = now;
            }
        }

        private Enemy FindNearestEnemyInRange()
        {
            Enemy best = null; float bestD = float.MaxValue;
            foreach (var e in EnemyRegistry.Enemies)
            {
                if (e == null) continue;
                float d = Vector3.Distance(transform.position, e.transform.position);
                if (d < range && d < bestD)
                {
                    bestD = d; best = e;
                }
            }
            return best;
        }
    }
}


