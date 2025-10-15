using CrystalDefenders.Combat;
using CrystalDefenders.Units;
using System.Collections;
using UnityEngine;

namespace CrystalDefenders.Combat
{
    /// <summary>
    /// Projectile that applies initial damage and a poison DoT effect over time.
    /// Only affects enemies whose Health component allows poison damage.
    /// </summary>
    public class PoisonArrowProjectile : Projectile
    {
        [Header("Poison Settings")]
        [SerializeField] private int poisonTickDamage = 2;           // Damage per tick
        [SerializeField] private float poisonTickInterval = 0.5f;    // Interval between ticks
        [SerializeField] private float poisonDuration = 4f;          // Total duration of poison effect

        private void Awake()
        {
            // Ensure damageTag is set for damage filtering
            if (string.IsNullOrEmpty(damageTag))
                damageTag = "poison";
        }

        private void Reset()
        {
            // Default inspector reset
            damageTag = "poison";
        }

        /// <summary>
        /// Triggered on direct hit with a Health component
        /// </summary>
        protected override void OnHit(Health health)
        {
            if (health == null || !gameObject.activeInHierarchy) return;

            // Debug logging
            Debug.Log($"PoisonArrow hitting {health.gameObject.name} | damageTag={damageTag} | enemy requiredTag={health.requiredDamageTag}");

            // Apply initial hit damage
            if (!string.IsNullOrEmpty(damageTag))
                health.ApplyDamage(damage, damageTag);
            else
                health.ApplyDamage(damage);

            // Start poison DoT coroutine
            StartCoroutine(ApplyPoison(health));
        }

        /// <summary>
        /// Applies poison damage over time to the target Health component
        /// </summary>
        private IEnumerator ApplyPoison(Health targetHealth)
        {
            if (targetHealth == null) yield break;

            // Optional: apply visual effect for EnemyFast
            var enemyFast = targetHealth.GetComponent<EnemyFast>();
            if (enemyFast != null)
                enemyFast.ApplyPoisonVisual(poisonDuration);

            float elapsed = 0f;

            while (elapsed < poisonDuration && targetHealth != null && targetHealth.CurrentHealth > 0)
            {
                // Apply poison tick damage respecting tags
                if (!string.IsNullOrEmpty(damageTag))
                    targetHealth.ApplyDamage(poisonTickDamage, damageTag);
                else
                    targetHealth.ApplyDamage(poisonTickDamage);

                yield return new WaitForSeconds(poisonTickInterval);
                elapsed += poisonTickInterval;
            }
        }
    }
}