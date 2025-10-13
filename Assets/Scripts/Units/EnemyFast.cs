using CrystalDefenders.Combat;
using UnityEngine;
using CrystalDefenders.Gameplay;
using System.Collections;

namespace CrystalDefenders.Units
{
    [RequireComponent(typeof(Health))]
    public class EnemyFast : Enemy
    {
        private Renderer rend;
        private Color originalColor;

        private void Awake()
        {
            var h = GetComponent<Health>();
            h.requiredDamageTag = "poison"; // only poison damages this enemy
            h.onDeath.AddListener(OnDeath);

            rend = GetComponentInChildren<Renderer>();
            if (rend != null)
                originalColor = rend.material.color;
        }

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

        private void OnDeath()
        {
            Destroy(gameObject);
            WaveManager.Instance?.OnEnemyDied();
        }

        private void Reset()
        {
            moveSpeed = 5.5f;
            contactDamage = 6;
            attackRange = 4f;
            attackCooldown = 0.8f;
        }
    }
}