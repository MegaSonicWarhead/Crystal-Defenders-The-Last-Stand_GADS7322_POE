using CrystalDefenders.Combat;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CrystalDefenders.Combat
{
    /// <summary>
    /// Base class for all projectiles.
    /// Handles movement towards a target, impact, and applying damage.
    /// </summary>
    public class Projectile : MonoBehaviour
    {
        [Header("Projectile Settings")]
        public float speed = 12f;                     // Units per second
        [SerializeField] public int damage;           // Base damage of this projectile
        [SerializeField] public Transform target;     // Target to move toward
        [SerializeField] public string damageTag = null; // Optional damage type ("poison", "fire", etc.)

        /// <summary>
        /// Initialize the projectile with a target, damage, and optional damage tag
        /// </summary>
        public virtual void Initialize(Transform target, int damage, string tag = null)
        {
            this.target = target;
            this.damage = damage;

            // Ensure damageTag is set if provided
            if (!string.IsNullOrEmpty(tag))
                this.damageTag = tag;
        }

        /// <summary>
        /// Moves the projectile toward its target each frame
        /// </summary>
        protected virtual void Update()
        {
            if (target == null)
            {
                Destroy(gameObject);
                return;
            }

            // Move toward the target
            transform.position = Vector3.MoveTowards(
                transform.position,
                target.position,
                speed * Time.deltaTime
            );

            // Check if close enough to impact
            if (Vector3.Distance(transform.position, target.position) < 0.1f)
            {
                OnImpact(transform.position, target);
                Destroy(gameObject);
            }
        }

        /// <summary>
        /// Called when projectile reaches target position
        /// Can be overridden for AoE or other effects
        /// </summary>
        protected virtual void OnImpact(Vector3 hitPosition, Transform hitTarget)
        {
            var health = hitTarget != null ? hitTarget.GetComponent<Health>() : null;
            if (health != null)
            {
                OnHit(health);
            }
        }

        /// <summary>
        /// Apply damage to the Health component respecting damage tags
        /// </summary>
        protected virtual void OnHit(Health health)
        {
            if (health == null) return;

            // Debug logging
            Debug.Log($"Projectile hitting {health.gameObject.name} | damageTag={damageTag} | enemy requiredTag={health.requiredDamageTag}");

            // Apply damage respecting requiredDamageTag
            if (!string.IsNullOrEmpty(damageTag))
                health.ApplyDamage(damage, damageTag);
            else
                health.ApplyDamage(damage);
        }
    }
}