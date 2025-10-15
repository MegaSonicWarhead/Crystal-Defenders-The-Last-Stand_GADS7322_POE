using System.Collections.Generic;
using UnityEngine;

namespace CrystalDefenders.Combat
{
    /// <summary>
    /// Fireball projectile with arc trajectory and area-of-effect splash damage.
    /// Inherits from Projectile.
    /// </summary>
    public class FireballAoEProjectile : Projectile
    {
        [Header("Fireball Settings")]
        [SerializeField] private float explosionRadius = 2.5f; // Radius for AOE damage
        [SerializeField] private int splashDamage = 10;        // Damage dealt to nearby enemies
        [SerializeField] private float arcHeight = 2.5f;       // Max height of parabolic arc
        [SerializeField] private float projectileSpeed = 12f;  // Controls travel time to target

        private Vector3 startPosition;
        private Vector3 targetSnapshot; // Take a snapshot of target's position at spawn
        private float travelTime;
        private float elapsed;

        private void Awake()
        {
            // Ensure damage tag is set for tower targeting
            if (string.IsNullOrEmpty(damageTag))
                damageTag = "fire";
        }

        private void Reset()
        {
            // Ensures default tag in editor reset
            damageTag = "fire";
        }

        /// <summary>
        /// Initializes projectile with target, damage, and optional tag.
        /// </summary>
        public override void Initialize(Transform target, int damage, string tag = null)
        {
            base.Initialize(target, damage, tag);

            startPosition = transform.position;
            targetSnapshot = target != null ? target.position : transform.position + transform.forward * 5f;

            // Calculate travel time based on distance and speed
            float distance = Vector3.Distance(startPosition, targetSnapshot);
            float speed = projectileSpeed > 0.01f ? projectileSpeed : 12f;
            travelTime = Mathf.Max(0.1f, distance / speed);

            elapsed = 0f;
        }

        protected override void Update()
        {
            // Move projectile along a parabolic arc
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / travelTime);
            Vector3 pos = Vector3.Lerp(startPosition, targetSnapshot, t);
            pos.y += 4f * arcHeight * t * (1f - t); // Parabola formula: peaks at t=0.5
            transform.position = pos;

            // Impact at destination
            if (t >= 1f)
            {
                OnImpact(transform.position, null);
                Destroy(gameObject);
            }
        }

        /// <summary>
        /// Handles both direct hit and splash damage.
        /// </summary>
        protected override void OnImpact(Vector3 hitPosition, Transform hitTarget)
        {
            Debug.Log($"Fireball impact at {hitPosition} | damageTag={damageTag}");

            // Direct hit damage
            if (hitTarget != null)
            {
                var h = hitTarget.GetComponent<Health>();
                if (h != null)
                {
                    if (!string.IsNullOrEmpty(damageTag))
                        h.ApplyDamage(damage, damageTag);
                    else
                        h.ApplyDamage(damage);
                }
            }

            // Splash damage to all enemies within explosion radius
            var colliders = Physics.OverlapSphere(hitPosition, explosionRadius);
            for (int i = 0; i < colliders.Length; i++)
            {
                var h = colliders[i].GetComponent<Health>();
                if (h != null)
                {
                    Debug.Log($"Fireball splash hitting {h.gameObject.name} | damageTag={damageTag} | enemy requiredTag={h.requiredDamageTag}");

                    if (!string.IsNullOrEmpty(damageTag))
                        h.ApplyDamage(splashDamage, damageTag);
                    else
                        h.ApplyDamage(splashDamage);
                }
            }
        }
    }
}