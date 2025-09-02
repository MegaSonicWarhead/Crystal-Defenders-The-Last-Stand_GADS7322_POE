using System.Collections.Generic;
using UnityEngine;
using CrystalDefenders.Combat;
using CrystalDefenders.Gameplay;

namespace CrystalDefenders.Units
{
    [RequireComponent(typeof(Health))]
    public class Enemy : MonoBehaviour
    {
        [Header("Stats")]
        public float moveSpeed = 3f;
        public int contactDamage = 10;
        public float attackRange = 1.0f;
        public float attackCooldown = 1.0f;

        private readonly List<Vector3> waypoints = new List<Vector3>();
        private int currentWpIndex = 0;
        private float lastAttackTime = -999f;

        private Health health;

        private void Awake()
        {
            health = GetComponent<Health>();
            health.onDeath.AddListener(OnDeath);
            Debug.Log($"Enemy Awake: {gameObject.name}");
        }

        private void OnEnable()
        {
            EnemyRegistry.Register(this);
            Debug.Log($"Enemy Enabled: {gameObject.name}");
        }

        private void OnDisable()
        {
            EnemyRegistry.Unregister(this);
            Debug.Log($"Enemy Disabled: {gameObject.name}");
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

            if (waypoints.Count > 0)
            {
                Vector3 startPos = transform.position;
                startPos.y = waypoints[0].y + 0.05f;
                transform.position = startPos;
                //Debug.Log($"Enemy {gameObject.name} SetPath to start at {transform.position}");
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
               // Debug.Log($"Enemy {gameObject.name} reached waypoint {currentWpIndex}/{waypoints.Count}");
                return;
            }

            Vector3 dir = to.normalized;
            Vector3 newPos = pos + dir * (moveSpeed * Time.deltaTime);
            newPos.y = target.y + 0.05f;

            transform.position = newPos;
        }

        private void TryAttackTargets()
        {
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
                    Debug.Log($"Enemy {gameObject.name} attacked {closestTarget.name} for {contactDamage} damage");
                }
            }
        }

        private GameObject GetClosestDamageable()
        {
            GameObject best = null;
            float bestD = float.MaxValue;

            var gm = Gameplay.GameManager.Instance;
            if (gm != null && gm.Tower != null)
            {
                float dt = Vector3.Distance(transform.position, gm.Tower.position);
                if (dt < bestD)
                {
                    bestD = dt; best = gm.Tower.gameObject;
                }
            }

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

        public void OnDeath()
        {
            ResourceManager.Instance?.AddResources(25);
            WaveManager.Instance?.OnEnemyDied();
            Debug.Log($"Enemy {gameObject.name} died");
            Destroy(gameObject);
        }
    }
}



