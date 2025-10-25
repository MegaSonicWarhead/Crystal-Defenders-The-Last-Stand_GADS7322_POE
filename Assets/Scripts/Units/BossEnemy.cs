using CrystalDefenders.Combat;
using UnityEngine;

namespace CrystalDefenders.Units
{
    /// <summary>
    /// Procedurally adaptive boss enemy that scales with player performance.
    /// </summary>
    [RequireComponent(typeof(Health))]
    [DisallowMultipleComponent]
    public class BossEnemy : Enemy
    {
        [Header("Base Boss Settings")]
        [SerializeField] private int baseHealth = 500;
        [SerializeField] private int baseDamage = 25;
        [SerializeField] private float baseMoveSpeed = 0.8f;

        private Health health;

        private void Awake()
        {
            health = GetComponent<Health>();
        }

        /// <summary>
        /// Configures boss stats adaptively using player performance metrics.
        /// </summary>
        public void ConfigureAdaptiveStats(float difficultyMultiplier, float defenderHealthFactor, int wave)
        {
            // Scale health — increases more sharply per wave & difficulty
            int scaledHealth = Mathf.RoundToInt(baseHealth * (1f + (wave * 0.2f)) * difficultyMultiplier);

            // Scale damage — increases if player is strong, but reduces if struggling
            float pressureAdj = defenderHealthFactor >= 0.8f ? 1.25f :
                                defenderHealthFactor >= 0.5f ? 1.0f : 0.75f;
            int scaledDamage = Mathf.RoundToInt(baseDamage * difficultyMultiplier * pressureAdj);

            // Scale move speed adaptively
            float scaledSpeed = baseMoveSpeed * Mathf.Lerp(0.8f, 1.4f, difficultyMultiplier - 0.8f);

            // Apply scaled stats
            health.SetMaxHealth(scaledHealth, true);
            moveSpeed = scaledSpeed;
            contactDamage = scaledDamage;

            Debug.Log($"[BossEnemy] Adapted stats | HP={scaledHealth} | DMG={scaledDamage} | SPD={scaledSpeed:F2}");
        }
    }
}