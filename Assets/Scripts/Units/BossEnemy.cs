using CrystalDefenders.Combat;
using CrystalDefenders.Generation;
using UnityEngine;
using System.Collections.Generic;

namespace CrystalDefenders.Units
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Health))]
    public class BossEnemy : Enemy
    {
        [Header("Base Boss Stats (used for scaling, not runtime health)")]
        [SerializeField] private int baseHealth = 500;
        [SerializeField] private int baseDamage = 25;
        [SerializeField] private float baseMoveSpeed = 0.8f;

        [Header("Adaptive Scaling Factors")]
        [SerializeField] private float healthScalePerWave = 0.25f;
        [SerializeField] private float damageScalePerWave = 0.15f;
        [SerializeField] private float speedScalePerWave = 0.05f;

        // Boss path points assigned externally
        private List<Vector3> bossPath;

        private void Awake()
        {
            health = GetComponent<Health>();
            if (health == null)
                Debug.LogError("[BossEnemy] Missing Health component!");
            else
                health.onDeath.AddListener(OnBossDeath);
        }

        private void Start()
        {
            // Auto-assign a path if not already set
            if (bossPath == null || bossPath.Count == 0)
            {
                var generator = FindObjectOfType<ProceduralTerrainGenerator>();
                if (generator != null && generator.PathWaypoints.Count > 0)
                {
                    // Convert IReadOnlyList<Vector3> to List<Vector3>
                    var pathList = new List<Vector3>(generator.PathWaypoints[0]);
                    SetBossPath(pathList); // Now it works
                }
            }

            // Attach health bar on spawn
            if (UIManager.Instance != null && health != null)
            {
                UIManager.Instance.AttachHealthBar(health);
            }
        }

        /// <summary>
        /// Assigns the path for the boss to follow.
        /// Must be called after instantiation if using custom paths.
        /// </summary>
        public void SetBossPath(List<Vector3> pathPoints)
        {
            if (pathPoints == null || pathPoints.Count == 0)
            {
                Debug.LogError("[BossEnemy] Invalid path assigned!");
                return;
            }

            bossPath = pathPoints;

            // Move boss to the start of the path immediately
            transform.position = bossPath[0];

            SetPath(bossPath); // Calls Enemy.SetPath to move along these waypoints
        }

        /// <summary>
        /// Configure adaptive boss stats based on difficulty, defender health, and wave.
        /// </summary>
        public void ConfigureAdaptiveStats(float difficultyMultiplier, float defenderHealthFactor, int wave)
        {
            if (health == null)
                health = GetComponent<Health>();

            int scaledHealth = Mathf.RoundToInt(baseHealth * (1f + wave * healthScalePerWave) * Mathf.Pow(difficultyMultiplier, 1.25f));

            float pressureAdj = defenderHealthFactor >= 0.8f ? 1.3f :
                                defenderHealthFactor >= 0.6f ? 1.0f : 0.7f;

            int scaledDamage = Mathf.RoundToInt(baseDamage * (1f + wave * damageScalePerWave) * difficultyMultiplier * pressureAdj);

            float scaledSpeed = Mathf.Clamp(
                baseMoveSpeed * (1f + wave * speedScalePerWave) * Mathf.Lerp(0.8f, 1.4f, difficultyMultiplier - 0.8f),
                0.6f, 2.0f);

            health.SetMaxHealth(scaledHealth, true);
            contactDamage = scaledDamage;
            moveSpeed = scaledSpeed;

            Debug.Log($"[BossEnemy] Initialized | Wave={wave} | HP={scaledHealth} | DMG={scaledDamage} | SPD={scaledSpeed:F2}");
        }

        private void OnBossDeath()
        {
            Debug.Log("[BossEnemy] Boss defeated!");
            Destroy(gameObject, 1.5f);
        }

        private void Update()
        {
            if (bossPath == null || bossPath.Count == 0)
            {
                Debug.Log("[BossEnemy] Waiting for path... (no movement)");
                return;
            }

            base.Update(); // Moves along the assigned path
        }
    }
}