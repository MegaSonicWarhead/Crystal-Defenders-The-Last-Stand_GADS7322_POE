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
        [Header("Base Boss Stats")]
        [SerializeField] private int baseHealth = 500;
        [SerializeField] private int baseDamage = 25;
        [SerializeField] private float baseMoveSpeed = 0.8f;

        [Header("Scaling Factors")]
        [SerializeField] private float healthScalePerWave = 0.25f;
        [SerializeField] private float damageScalePerWave = 0.15f;
        [SerializeField] private float speedScalePerWave = 0.05f;

        [Header("Procedural Variance")]
        [SerializeField] private float healthVariance = 0.15f;
        [SerializeField] private float damageVariance = 0.10f;
        [SerializeField] private float speedVariance = 0.10f;

        [Header("Archetype & Abilities")]
        [SerializeField] private BossArchetype archetype = BossArchetype.None;
        [SerializeField] private List<BossAbility> abilities = new();
        private readonly Dictionary<BossAbility, int> abilityLevels = new();

        private List<Vector3> bossPath;
        // 🔹 No serialized 'health' field here — inherited from Enemy.
        public System.Action<int> OnDealDamage;

        public enum BossAbility { None, FireResist, PoisonResist, SpeedBoost, LifeSteal, Regeneration }
        public enum BossArchetype { None, Tank, Berserker, Vampire, Mutant, Elemental }

        private void Awake()
        {
            // use inherited field
            if (health == null)
                health = GetComponent<Health>();

            if (health != null)
                health.onDeath.AddListener(OnBossDeath);
        }

        private void Start()
        {
            if (bossPath == null || bossPath.Count == 0)
            {
                var generator = FindObjectOfType<ProceduralTerrainGenerator>();
                if (generator != null && generator.PathWaypoints.Count > 0)
                    SetBossPath(new List<Vector3>(generator.PathWaypoints[0]));
            }

            if (UIManager.Instance != null && health != null)
                UIManager.Instance.AttachHealthBar(health);
        }

        public void SetBossPath(List<Vector3> pathPoints)
        {
            bossPath = pathPoints ?? new List<Vector3>();
            if (bossPath.Count > 0)
            {
                transform.position = bossPath[0];
                SetPath(bossPath);
            }
        }

        public bool HasAbility(BossAbility ability) =>
            abilities != null && abilities.Contains(ability);

        public void ConfigureProceduralStats(float difficultyMultiplier, float defenderHealthFactor, int wave)
        {
            if (health == null) health = GetComponent<Health>();

            int scaledHealth = Mathf.RoundToInt(baseHealth * (1f + wave * healthScalePerWave) * Mathf.Pow(difficultyMultiplier, 1.25f));
            int scaledDamage = Mathf.RoundToInt(baseDamage * (1f + wave * damageScalePerWave) * difficultyMultiplier);
            float scaledSpeed = Mathf.Clamp(baseMoveSpeed * (1f + wave * speedScalePerWave), 0.6f, 2.0f);

            scaledHealth = Mathf.RoundToInt(scaledHealth * Random.Range(1f - healthVariance, 1f + healthVariance));
            scaledDamage = Mathf.RoundToInt(scaledDamage * Random.Range(1f - damageVariance, 1f + damageVariance));
            scaledSpeed *= Random.Range(1f - speedVariance, 1f + speedVariance);

            health.SetMaxHealth(scaledHealth, true);
            contactDamage = scaledDamage;
            moveSpeed = scaledSpeed;

            archetype = ChooseArchetype();
            AssignProceduralAbilities();

            Debug.Log($"[BossEnemy] Wave {wave} | Archetype={archetype} | HP={scaledHealth} | DMG={scaledDamage} | SPD={scaledSpeed:F2}");
        }

        private BossArchetype ChooseArchetype()
        {
            float h = healthVariance, d = damageVariance, s = speedVariance;
            var rolls = new Dictionary<BossArchetype, float>
            {
                { BossArchetype.Tank, h * 2.0f },
                { BossArchetype.Berserker, d * 1.8f },
                { BossArchetype.Vampire, h * 1.2f + d * 1.2f },
                { BossArchetype.Mutant, s * 1.5f },
                { BossArchetype.Elemental, (h + d + s) / 3f }
            };

            float total = 0f; foreach (var r in rolls) total += r.Value;
            float pick = Random.Range(0f, total);
            float cumulative = 0f;
            foreach (var r in rolls)
            {
                cumulative += r.Value;
                if (pick <= cumulative) return r.Key;
            }
            return BossArchetype.None;
        }

        private void AssignProceduralAbilities()
        {
            // 🔹 Randomly decide how many abilities this boss will have (1–5)
            int abilityCount = Random.Range(1, 6);
            abilities.Clear();
            abilityLevels.Clear();

            // Weighted chance based on archetype (same as before)
            var weightedAbilities = new Dictionary<BossAbility, float>
    {
        { BossAbility.FireResist,   archetype == BossArchetype.Elemental ? 2f : 1f },
        { BossAbility.PoisonResist, archetype == BossArchetype.Mutant ? 2f : 1f },
        { BossAbility.SpeedBoost,   archetype == BossArchetype.Berserker ? 2f : 1f },
        { BossAbility.LifeSteal,    archetype == BossArchetype.Vampire ? 2.5f : 1f },
        { BossAbility.Regeneration, archetype == BossArchetype.Tank ? 2.2f : 1f }
    };

            // 🔹 Select unique abilities up to abilityCount
            for (int i = 0; i < abilityCount; i++)
            {
                BossAbility chosen = WeightedPick(weightedAbilities);
                if (!abilities.Contains(chosen))
                {
                    abilities.Add(chosen);

                    // Each ability gets its own level (0–3)
                    int level = Random.Range(0, 4);
                    abilityLevels[chosen] = level;

                    if (level > 0)
                        ApplyAbility(chosen, level);
                }
            }

            // Visual feedback
            ApplyArchetypeVisuals();

            // Debug readout
            string abilityReport = "";
            foreach (var kv in abilityLevels)
                abilityReport += $"{kv.Key}(Lv.{kv.Value}) ";
            Debug.Log($"[BossEnemy] Assigned Abilities: {abilityReport}");
        }

        private BossAbility WeightedPick(Dictionary<BossAbility, float> weights)
        {
            float total = 0f;
            foreach (var w in weights) total += w.Value;
            float pick = Random.Range(0f, total);
            float sum = 0f;
            foreach (var w in weights)
            {
                sum += w.Value;
                if (pick <= sum) return w.Key;
            }
            return BossAbility.None;
        }

        private void ApplyAbility(BossAbility ability, int level)
        {
            if (level <= 0) return; // No effect if level 0 (still stored)

            switch (ability)
            {
                case BossAbility.FireResist:
                    // TODO: implement fire resistance scaling later
                    break;

                case BossAbility.PoisonResist:
                    // TODO: implement poison resistance scaling later
                    break;

                case BossAbility.SpeedBoost:
                    moveSpeed *= 1f + (0.15f * level);
                    break;

                case BossAbility.LifeSteal:
                    OnDealDamage += (int dmg) =>
                    {
                        if (health != null)
                            health.Heal(Mathf.RoundToInt(dmg * (0.1f * level)));
                    };
                    break;

                case BossAbility.Regeneration:
                    StartCoroutine(RegenerationRoutine(level));
                    break;
            }
        }

        private IEnumerator RegenerationRoutine(int level)
        {
            int healPerTick = 4 * level;
            float interval = Mathf.Max(0.5f, 2f - 0.3f * level);

            while (health != null && health.CurrentHealth > 0)
            {
                health.Heal(healPerTick);
                yield return new WaitForSeconds(interval);
            }
        }

        private void ApplyArchetypeVisuals()
        {
            var renderer = GetComponentInChildren<Renderer>();
            if (renderer == null) return;

            Color color = archetype switch
            {
                BossArchetype.Tank => Color.cyan,
                BossArchetype.Berserker => Color.red,
                BossArchetype.Vampire => new Color(0.5f, 0, 0.5f),
                BossArchetype.Mutant => Color.green,
                BossArchetype.Elemental => new Color(1f, 0.6f, 0.1f),
                _ => Color.white
            };

            renderer.material.SetColor("_EmissionColor", color * 1.5f);
        }

        private void OnBossDeath()
        {
            Debug.Log($"[BossEnemy] {archetype} Boss defeated!");
            Destroy(gameObject, 1.5f);
        }

        private void Update()
        {
            if (bossPath == null || bossPath.Count == 0) return;
            base.Update();
        }
    }
}