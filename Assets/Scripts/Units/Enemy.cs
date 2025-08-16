using System.Collections.Generic;
using UnityEngine;
using CrystalDefenders.Combat;
using CrystalDefenders.Gameplay;

namespace CrystalDefenders.Units
{
    [RequireComponent(typeof(Health))]
    public class Enemy : MonoBehaviour
    {
        [Header("Stats")] public float moveSpeed = 3f; public int contactDamage = 10; public float attackRange = 1.0f; public float attackCooldown = 1.0f;

        private readonly List<Vector3> waypoints = new List<Vector3>();
        private int currentWpIndex = 0;
        private float lastAttackTime = -999f;

        private Health health;

        private void Awake()
        {
            health = GetComponent<Health>();
            health.onDeath.AddListener(OnDeath);
        }

        private void OnEnable()
        {
            EnemyRegistry.Register(this);
        }

        private void OnDisable()
        {
            EnemyRegistry.Unregister(this);
        }

        private void Update()
        {
            MoveAlongPath();
            TryAttackTargets();
        }

        public void SetPath(IList<Vector3> worldWaypoints)
        {
            waypoints.Clear();
            waypoints.AddRange(worldWaypoints);
            currentWpIndex = 0;
            
            // Ensure enemy starts at proper height above the path
            if (waypoints.Count > 0)
            {
                Vector3 startPos = transform.position;
                startPos.y = waypoints[0].y + 0.05f; // Start above the first waypoint
                transform.position = startPos;
            }
        }

        private void MoveAlongPath()
        {
            if (waypoints.Count == 0 || currentWpIndex >= waypoints.Count) return;

            Vector3 target = waypoints[currentWpIndex];
            Vector3 pos = transform.position;
            Vector3 to = target - pos;
            to.y = 0f;
            float dist = to.magnitude;
            if (dist < 0.05f)
            {
                currentWpIndex++;
                return;
            }

            Vector3 dir = to.normalized;
            Vector3 newPos = pos + dir * (moveSpeed * Time.deltaTime);
            
            // Keep enemy at tile height (slightly above terrain)
            newPos.y = target.y + 0.05f; // 0.05f above the waypoint height
            
            transform.position = newPos;
        }

        private void TryAttackTargets()
        {
            // Attack nearest defender or tower within attackRange
            float now = Time.time;
            if (now - lastAttackTime < attackCooldown) return;

            var closestTarget = GetClosestDamageable();
            if (closestTarget == null) return;

            float d = Vector3.Distance(transform.position, closestTarget.transform.position);
            if (d <= attackRange)
            {
                var h = closestTarget.GetComponent<Health>();
                if (h != null)
                {
                    h.ApplyDamage(contactDamage);
                    lastAttackTime = now;
                }
            }
        }

        private GameObject GetClosestDamageable()
        {
            GameObject best = null; float bestD = float.MaxValue;

            // Check tower
            var gm = Gameplay.GameManager.Instance;
            if (gm != null && gm.Tower != null)
            {
                float dt = Vector3.Distance(transform.position, gm.Tower.position);
                if (dt < bestD)
                {
                    bestD = dt; best = gm.Tower.gameObject;
                }
            }

            // Check defenders
            foreach (var def in Defender.Registry)
            {
                if (def == null) continue;
                float dd = Vector3.Distance(transform.position, def.transform.position);
                if (dd < bestD)
                {
                    bestD = dd; best = def.gameObject;
                }
            }

            return best;
        }

        private void OnDeath()
        {
            // Resource reward and wave bookkeeping
            ResourceManager.Instance?.AddResources(25);
            WaveManager.Instance?.OnEnemyDied();
            Destroy(gameObject);
        }
    }
}


