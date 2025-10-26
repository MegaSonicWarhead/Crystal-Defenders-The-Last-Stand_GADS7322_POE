using System.Collections.Generic;
using UnityEngine;
using CrystalDefenders.Combat;
using CrystalDefenders.Gameplay;

namespace CrystalDefenders.Units
{
    /// <summary>
    /// Base enemy class responsible for movement, targeting, attacking, and death handling.
    /// Enemies traverse along assigned waypoints and interact with defenders and the main tower.
    /// </summary>
    [RequireComponent(typeof(Health))]
    public class Enemy : MonoBehaviour
    {
        [Header("Stats")]
        [Tooltip("Movement speed of the enemy along the path.")]
        public float moveSpeed = 3f;

        [Tooltip("Damage dealt when attacking a defender or the main tower.")]
        public int contactDamage = 10;

        [Tooltip("Maximum distance within which the enemy can attack a target.")]
        public float attackRange = 5.0f;

        [Tooltip("Cooldown time between consecutive attacks.")]
        public float attackCooldown = 1.0f;

        // List of path nodes the enemy will follow
        private readonly List<Vector3> waypoints = new List<Vector3>();

        // Current index in the waypoint list
        private int currentWpIndex = 0;

        // Timestamp when the last attack was executed
        public float lastAttackTime = -999f;

        // Reference to this enemy's health component
        public Health health;

        // Flag to ensure death logic is triggered only once
        private bool isDead = false;

        private void Awake()
        {
            // Cache Health component and subscribe to death event
            health = GetComponent<Health>();
            health.onDeath.AddListener(OnDeath);
        }

        private void OnEnable()
        {
            // Register enemy into the active enemy registry
            EnemyRegistry.Register(this);
        }

        private void OnDisable()
        {
            // Unregister enemy to maintain clean registry state
            EnemyRegistry.Unregister(this);
        }

        public void Update()
        {
            MoveAlongPath();
            TryAttackTargets();

            // Safety fallback to ensure enemies die correctly if damage bypasses events
            if (!isDead && health != null && health.CurrentHealth <= 0)
            {
                OnDeath();
            }
        }

        /// <summary>
        /// Assigns a movement path for the enemy using world-space waypoints.
        /// </summary>
        public void SetPath(IList<Vector3> worldWaypoints)
        {
            waypoints.Clear();
            waypoints.AddRange(worldWaypoints);
            currentWpIndex = 0;

            // Slight vertical offset to avoid ground clipping
            if (waypoints.Count > 0)
            {
                Vector3 startPos = transform.position;
                startPos.y = waypoints[0].y + 0.05f;
                transform.position = startPos;
            }
        }

        /// <summary>
        /// Handles linear movement between waypoints.
        /// </summary>
        private void MoveAlongPath()
        {
            if (waypoints.Count == 0 || currentWpIndex >= waypoints.Count) return;

            Vector3 target = waypoints[currentWpIndex];
            Vector3 pos = transform.position;
            Vector3 to = target - pos;
            to.y = 0f; // Ensure no vertical drift
            float dist = to.magnitude;

            // Check if waypoint reached, then advance to next
            if (dist < 0.05f)
            {
                currentWpIndex++;
                return;
            }

            // Move towards next waypoint
            Vector3 dir = to.normalized;
            Vector3 newPos = pos + dir * (moveSpeed * Time.deltaTime);
            newPos.y = target.y + 0.05f; // Maintain slight elevation
            transform.position = newPos;
        }

        /// <summary>
        /// Attempts to find and attack the closest valid target within range.
        /// </summary>
        private void TryAttackTargets()
        {
            float now = Time.time;
            if (now - lastAttackTime < attackCooldown) return;

            // Locate the nearest valid attack target
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

        /// <summary>
        /// Returns the closest damageable object: prioritizes main tower, then defenders.
        /// </summary>
        private GameObject GetClosestDamageable()
        {
            GameObject best = null;
            float bestD = float.MaxValue;

            // Check distance to main tower
            var gm = Gameplay.GameManager.Instance;
            if (gm != null && gm.Tower != null)
            {
                float dt = Vector3.Distance(transform.position, gm.Tower.position);
                if (dt < bestD)
                {
                    bestD = dt; best = gm.Tower.gameObject;
                }
            }

            // Check all defenders
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

        /// <summary>
        /// Triggers when enemy health reaches zero. Awards resources and notifies wave manager.
        /// </summary>
        public void OnDeath()
        {
            if (isDead) return;
            isDead = true;

            // Reward player resources for kill
            ResourceManager.Instance?.AddResources(25);

            // Inform wave system of enemy death
            WaveManager.Instance?.OnEnemyDied();

            // Destroy enemy object
            Destroy(gameObject);
        }
    }
}