using CrystalDefenders.Combat;
using UnityEngine;
using CrystalDefenders.Gameplay;

namespace CrystalDefenders.Units
{
    [RequireComponent(typeof(Health))]
    public class EnemyFast : Enemy
    {
        private void Awake()
        {
            var h = GetComponent<Health>();
            h.requiredDamageTag = "poison"; // only poison damages this enemy

            // Subscribe to death event
            h.onDeath.AddListener(OnDeath);
        }

        private void OnDeath()
        {
            Destroy(gameObject); // or play death animation before destroying
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