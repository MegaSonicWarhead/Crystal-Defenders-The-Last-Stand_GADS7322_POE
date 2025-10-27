using CrystalDefenders.Generation;
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
                // Instantiate a runtime copy so we never modify the prefab asset
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
                // detach so the fade-out can finish even after projectile destroy
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
            {
                var h = hitTarget.GetComponent<Health>();
                if (h != null)
                {
                    if (!string.IsNullOrEmpty(damageTag))
                        h.ApplyDamage(damage, damageTag);
                    else
                        h.ApplyDamage(damage);
                }
            }

            // Splash damage
            var colliders = Physics.OverlapSphere(hitPosition, explosionRadius);
            for (int i = 0; i < colliders.Length; i++)
            {
                var h = colliders[i].GetComponent<Health>();
                if (h != null)
                {
                    if (!string.IsNullOrEmpty(damageTag))
                        h.ApplyDamage(splashDamage, damageTag);
                    else
                        h.ApplyDamage(splashDamage);
                }
            }

            // find generators near impact and apply shock
            var generators = FindObjectsOfType<ProceduralTerrainGenerator>();
            foreach (var gen in generators)
            {
                // optionally check distance to hub or bounding box to avoid calling distant terrains
                float radius = explosionRadius; // or a different visual radius
                float strength = 0.5f; // vertical displacement in world units — tweak (0.2 - 2.0 typical)
                float duration = 0.9f;
                gen.ApplyShockwave(hitPosition, radius, strength, duration, restore: true);
            }
        }
    }
}