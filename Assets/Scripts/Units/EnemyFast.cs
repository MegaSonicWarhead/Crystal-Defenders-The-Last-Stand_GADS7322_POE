using CrystalDefenders.Combat;
using UnityEngine;
using CrystalDefenders.Gameplay;
using System.Collections;

namespace CrystalDefenders.Units
{
    /// <summary>
    /// Fast enemy type. Only vulnerable to poison damage.
    /// Changes color to green when poisoned and notifies WaveManager on death.
    /// </summary>
    [RequireComponent(typeof(Health))]
    public class EnemyFast : Enemy
    {
        private Renderer rend;
        private Color originalColor;

        private void Awake()
        {
            // Set health tag so only poison damages this enemy
            var h = GetComponent<Health>();
            h.requiredDamageTag = "poison";
            h.onDeath.AddListener(OnDeath);

            // Cache renderer and original color for poison visual
            rend = GetComponentInChildren<Renderer>();
            if (rend != null)
                originalColor = rend.material.color;
        }

        /// <summary>
        /// Apply visual effect for poison (temporarily changes color)
        /// </summary>
        public void ApplyPoisonVisual(float duration)
        {
            if (rend != null)
                StartCoroutine(PoisonColorCoroutine(duration));
        }

        private IEnumerator PoisonColorCoroutine(float duration)
        {
            rend.material.color = Color.green;
            yield return new WaitForSeconds(duration);
            rend.material.color = originalColor;
        }

        /// <summary>
        /// Handles death: destroy enemy and notify WaveManager
        /// </summary>
        private void OnDeath()
        {
            Destroy(gameObject);
            WaveManager.Instance?.OnEnemyDied();
        }

        /// <summary>
        /// Default configuration when resetting in editor
        /// </summary>
        private void Reset()
        {
            moveSpeed = 5.5f;
            contactDamage = 6;
            attackRange = 4f;
            attackCooldown = 0.8f;
        }
    }
}