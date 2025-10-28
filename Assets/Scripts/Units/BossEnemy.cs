using CrystalDefenders.Combat;
using CrystalDefenders.Generation;
using UnityEngine;
using System.Collections;
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

        [Header("Procedural Variance (randomized per boss)")]
        [SerializeField] private float healthVariance = 0.15f;
        [SerializeField] private float damageVariance = 0.10f;
        [SerializeField] private float speedVariance = 0.10f;

        [Header("Procedural Abilities")]
        [SerializeField] private BossAbility assignedAbility = BossAbility.None;
        public BossAbility AssignedAbility => assignedAbility;

        public enum BossAbility
        {
            None,
            FireResist,
            PoisonResist,
            SpeedBoost,
            LifeSteal,
            Regeneration
        }

        private List<Vector3> bossPath;
        public System.Action<int> OnDealDamage;

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
            if (bossPath == null || bossPath.Count == 0)
            {
                var generator = FindObjectOfType<ProceduralTerrainGenerator>();
                if (generator != null && generator.PathWaypoints.Count > 0)
                {
                    SetBossPath(new List<Vector3>(generator.PathWaypoints[0]));
                }
            }

            if (UIManager.Instance != null && health != null)
                UIManager.Instance.AttachHealthBar(health);
        }

        public void SetBossPath(List<Vector3> pathPoints)
        {
            if (pathPoints == null || pathPoints.Count == 0)
            {
                Debug.LogError("[BossEnemy] Invalid path assigned!");
                return;
            }

            bossPath = pathPoints;
            transform.position = bossPath[0];
            SetPath(bossPath);
        }

        public void ConfigureProceduralStats(float difficultyMultiplier, float defenderHealthFactor, int wave)
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

            float healthMult = Random.Range(1f - healthVariance, 1f + healthVariance);
            float damageMult = Random.Range(1f - damageVariance, 1f + damageVariance);
            float speedMult = Random.Range(1f - speedVariance, 1f + speedVariance);

            int finalHealth = Mathf.RoundToInt(scaledHealth * healthMult);
            int finalDamage = Mathf.RoundToInt(scaledDamage * damageMult);
            float finalSpeed = scaledSpeed * speedMult;

            health.SetMaxHealth(finalHealth, true);
            contactDamage = finalDamage;
            moveSpeed = finalSpeed;

            AssignRandomAbility();

            Debug.Log($"[BossEnemy] Procedural Init | Wave={wave} | HP={finalHealth} | DMG={finalDamage} | SPD={finalSpeed:F2} | Ability={assignedAbility}");
        }

        private void AssignRandomAbility()
        {
            var values = System.Enum.GetValues(typeof(BossAbility));
            assignedAbility = (BossAbility)values.GetValue(Random.Range(0, values.Length));

            switch (assignedAbility)
            {
                case BossAbility.FireResist:
                    break;
                case BossAbility.PoisonResist:
                    break;
                case BossAbility.SpeedBoost:
                    moveSpeed *= 1.3f;
                    break;
                case BossAbility.LifeSteal:
                    OnDealDamage += LifeStealHandler;
                    break;
                case BossAbility.Regeneration:
                    StartCoroutine(RegenerationRoutine());
                    break;
            }
        }

        private void LifeStealHandler(int damage)
        {
            if (health != null)
                health.Heal(Mathf.RoundToInt(damage * 0.2f));
        }

        private IEnumerator RegenerationRoutine()
        {
            while (health != null && health.CurrentHealth > 0)
            {
                health.Heal(5);
                yield return new WaitForSeconds(2f);
            }
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

            base.Update();
        }
    }
}