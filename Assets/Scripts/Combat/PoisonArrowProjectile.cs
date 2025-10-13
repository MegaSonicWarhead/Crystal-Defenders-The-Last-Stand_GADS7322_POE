using CrystalDefenders.Combat;
using CrystalDefenders.Units;
using System.Collections;
using UnityEngine;

namespace CrystalDefenders.Combat
{
    public class PoisonArrowProjectile : Projectile
    {
        [SerializeField] private int poisonTickDamage = 2;
        [SerializeField] private float poisonTickInterval = 0.5f;
        [SerializeField] private float poisonDuration = 4f;

        private void Awake()
        {
            // Ensure damageTag is set as early as possible
            if (string.IsNullOrEmpty(damageTag))
                damageTag = "poison";
        }

        private void Reset()
        {
            damageTag = "poison";
        }

        protected override void OnHit(Health health)
        {
            if (health == null || !gameObject.activeInHierarchy) return;

            // Debug: log to verify tags
            Debug.Log($"PoisonArrow hitting {health.gameObject.name} | damageTag={damageTag} | enemy requiredTag={health.requiredDamageTag}");

            // Apply initial hit damage
            if (!string.IsNullOrEmpty(damageTag))
                health.ApplyDamage(damage, damageTag);
            else
                health.ApplyDamage(damage);

            // Start poison DOT
            StartCoroutine(ApplyPoison(health));
        }

        private IEnumerator ApplyPoison(Health targetHealth)
        {
            if (targetHealth == null) yield break;

            // Trigger visual if the target has EnemyFast component
            var enemyFast = targetHealth.GetComponent<EnemyFast>();
            if (enemyFast != null)
                enemyFast.ApplyPoisonVisual(poisonDuration);

            float elapsed = 0f;

            while (elapsed < poisonDuration && targetHealth != null && targetHealth.CurrentHealth > 0)
            {
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