using System.Collections.Generic;
using UnityEngine;

namespace CrystalDefenders.Combat
{
    public class FireballAoEProjectile : Projectile
    {
        [SerializeField] private float explosionRadius = 2.5f;
        [SerializeField] private int splashDamage = 10;
        [SerializeField] private float arcHeight = 2.5f;
        [SerializeField] private float projectileSpeed = 12f; // controls travel time

        private Vector3 startPosition;
        private Vector3 targetSnapshot;
        private float travelTime;
        private float elapsed;

        private void Awake()
        {
            // Ensure damageTag is set as early as possible
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
        }

        protected override void Update()
        {
            // Arc along a simple parabolic path towards the snapshot target
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / travelTime);
            Vector3 pos = Vector3.Lerp(startPosition, targetSnapshot, t);
            pos.y += 4f * arcHeight * t * (1f - t); // parabola peaking at t=0.5
            transform.position = pos;

            if (t >= 1f)
            {
                OnImpact(transform.position, null);
                Destroy(gameObject);
            }
        }

        protected override void OnImpact(Vector3 hitPosition, Transform hitTarget)
        {
            // Debug: log to verify tags
            Debug.Log($"Fireball impact at {hitPosition} | damageTag={damageTag}");

            // Deal direct hit damage first
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

            // Splash damage to nearby enemies
            var colliders = Physics.OverlapSphere(hitPosition, explosionRadius);
            for (int i = 0; i < colliders.Length; i++)
            {
                var h = colliders[i].GetComponent<Health>();
                if (h != null)
                {
                    // Debug: log each affected enemy
                    Debug.Log($"Fireball splash hitting {h.gameObject.name} | damageTag={damageTag} | enemy requiredTag={h.requiredDamageTag}");

                    if (!string.IsNullOrEmpty(damageTag))
                        h.ApplyDamage(splashDamage, damageTag);
                    else
                        h.ApplyDamage(splashDamage);
                }
            }
        }
    }
}