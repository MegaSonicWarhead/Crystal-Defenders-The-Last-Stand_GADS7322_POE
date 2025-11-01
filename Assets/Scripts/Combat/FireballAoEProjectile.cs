using CrystalDefenders.Generation;
using CrystalDefenders.Units;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CrystalDefenders.Combat
{
    /// <summary>
    /// Fireball projectile with arc trajectory, particle effects, and area-of-effect splash damage.
    /// </summary>
    public class FireballAoEProjectile : Projectile
    {
        [Header("Fireball Settings")]
        [SerializeField] private float explosionRadius = 2.5f; // Radius for AOE damage
        [SerializeField] private int splashDamage = 10;        // Damage dealt to nearby enemies
        [SerializeField] private float arcHeight = 2.5f;       // Max height of parabolic arc
        [SerializeField] private float projectileSpeed = 12f;  // Controls travel time to target

        [Header("Visual Effects")]
        [SerializeField] private ParticleSystem travelParticles; // optional child PS for flight
        [SerializeField] private GameObject impactVFXPrefab;     // prefab to spawn at impact
        [SerializeField] private Material shockwaveMaterial;

        private Vector3 startPosition;
        private Vector3 targetSnapshot;
        private float travelTime;
        private float elapsed;

        private void Awake()
        {
            if (string.IsNullOrEmpty(damageTag))
                damageTag = "fire";
        }

        private void Reset()
        {
            damageTag = "fire";
        }

        public override void Initialize(Transform target, int damage, string tag = null)
        {
            base.Initialize(target, damage, tag);

            startPosition = transform.position;
            targetSnapshot = target != null ? target.position : transform.position + transform.forward * 5f;

            float distance = Vector3.Distance(startPosition, targetSnapshot);
            float speed = projectileSpeed > 0.01f ? projectileSpeed : 12f;
            travelTime = Mathf.Max(0.1f, distance / speed);
            elapsed = 0f;

            // Play travel particles if assigned
            if (travelParticles != null)
            {
                travelParticles = Instantiate(travelParticles, transform.position, transform.rotation, transform);
                travelParticles.Clear(true);
                travelParticles.Play(true);
            }
        }

        protected override void Update()
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / travelTime);

            // Parabolic interpolation
            Vector3 pos = Vector3.Lerp(startPosition, targetSnapshot, t);
            pos.y += 4f * arcHeight * t * (1f - t);
            transform.position = pos;

            // Impact at destination
            if (t >= 1f)
            {
                OnImpact(transform.position, null);
                Destroy(gameObject);
            }
        }

        protected override void OnImpact(Vector3 hitPosition, Transform hitTarget)
        {
            // Stop travel particles
            if (travelParticles != null)
            {
                travelParticles.Stop(true, ParticleSystemStopBehavior.StopEmitting);
                travelParticles.transform.SetParent(null);
                Destroy(travelParticles.gameObject, 2f);
            }

            // Spawn impact VFX
            if (impactVFXPrefab != null)
            {
                GameObject impact = Instantiate(impactVFXPrefab, hitPosition, Quaternion.identity);
                var ps = impact.GetComponent<ParticleSystem>();
                if (ps != null)
                {
                    ps.Play();
                    Destroy(impact, ps.main.duration + ps.main.startLifetime.constantMax);
                }
                else
                {
                    Destroy(impact, 3f);
                }
            }

            // Direct hit damage
            if (hitTarget != null)
                ApplyFireDamage(hitTarget, damage);

            // Splash damage
            var colliders = Physics.OverlapSphere(hitPosition, explosionRadius);
            foreach (var col in colliders)
            {
                if (col.transform == hitTarget) continue;
                ApplyFireDamage(col.transform, splashDamage);
            }

            // Shockwave on terrain
            var generators = FindObjectsOfType<ProceduralTerrainGenerator>();
            foreach (var gen in generators)
            {
                gen.ApplyShockwave(hitPosition, explosionRadius, 0.5f, 0.9f, restore: true);
            }
        }

        private void ApplyFireDamage(Transform target, int baseDamage)
        {
            var h = target.GetComponent<Health>();
            if (h == null) return;

            int finalDamage = baseDamage;

            var boss = h.GetComponent<BossEnemy>();
            if (boss != null)
            {
                // Check for fire resistance ability
                if (boss.HasAbility(BossEnemy.BossAbility.FireResist))
                {
                    finalDamage = Mathf.RoundToInt(finalDamage * 0.5f); // reduce damage by 50%
                }
            }

            if (!string.IsNullOrEmpty(damageTag))
                h.ApplyDamage(finalDamage, damageTag);
            else
                h.ApplyDamage(finalDamage);
        }
    }
}