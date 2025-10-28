using CrystalDefenders.Combat;
using CrystalDefenders.Units;
using System.Collections;
using UnityEngine;

namespace CrystalDefenders.Combat
{
    [RequireComponent(typeof(Renderer))]
    public class PoisonArrowProjectile : Projectile
    {
        [Header("Poison Settings")]
        [SerializeField] private int poisonTickDamage = 2;
        [SerializeField] private float poisonTickInterval = 0.5f;
        [SerializeField] private float poisonDuration = 4f;

        [Header("Visual Settings")]
        [SerializeField] private Renderer projectileRenderer;
        [SerializeField] private float colorTransitionSpeed = 2f;
        [SerializeField] private Color baseColor = new Color(0.3f, 0.8f, 0.3f, 1f);
        [SerializeField] private Color poisonColor = new Color(0f, 1f, 0f, 1f);

        private Material materialInstance;
        private static readonly int PoisonStrengthID = Shader.PropertyToID("_PoisonStrength");
        private static readonly int BaseColorID = Shader.PropertyToID("_BaseColor");
        private static readonly int PoisonColorID = Shader.PropertyToID("_PoisonColor");

        private void Awake()
        {
            if (string.IsNullOrEmpty(damageTag))
                damageTag = "poison";

            if (projectileRenderer == null)
                projectileRenderer = GetComponent<Renderer>();

            if (projectileRenderer != null)
            {
                materialInstance = projectileRenderer.material;

                if (materialInstance.HasProperty(BaseColorID))
                    materialInstance.SetColor(BaseColorID, baseColor);

                if (materialInstance.HasProperty(PoisonColorID))
                    materialInstance.SetColor(PoisonColorID, poisonColor);

                if (materialInstance.HasProperty(PoisonStrengthID))
                    materialInstance.SetFloat(PoisonStrengthID, 0f);
            }
        }

        private void Update()
        {
            if (target == null) return;

            // Move toward the target
            transform.position = Vector3.MoveTowards(transform.position, target.position, speed * Time.deltaTime);

            // Rotate to face the target
            Vector3 dir = (target.position - transform.position).normalized;
            if (dir != Vector3.zero)
                transform.rotation = Quaternion.LookRotation(dir); // assumes arrow points +Z

            // Check if the arrow has reached the target
            if (Vector3.Distance(transform.position, target.position) <= 0.1f)
            {
                Health targetHealth = target.GetComponent<Health>();
                if (targetHealth != null)
                    OnHit(targetHealth);

                Destroy(gameObject); // Remove arrow after hitting
            }
        }

        private void Reset()
        {
            damageTag = "poison";
        }

        protected override void OnHit(Health health)
        {
            if (health == null || !gameObject.activeInHierarchy) return;

            if (!string.IsNullOrEmpty(damageTag))
                health.ApplyDamage(damage, damageTag);
            else
                health.ApplyDamage(damage);

            StartCoroutine(ApplyPoison(health));

            if (materialInstance != null && materialInstance.HasProperty(PoisonStrengthID))
                StartCoroutine(PulsePoisonColor());
        }

        private IEnumerator ApplyPoison(Health targetHealth)
        {
            if (targetHealth == null) yield break;

            var enemyFast = targetHealth.GetComponent<EnemyFast>();
            if (enemyFast != null)
                enemyFast.ApplyPoisonVisual(poisonDuration);

            float elapsed = 0f;
            while (elapsed < poisonDuration && targetHealth != null && targetHealth.CurrentHealth > 0)
            {
                if (!string.IsNullOrEmpty(damageTag))
                    targetHealth.ApplyDamage(poisonTickDamage, damageTag);
                else
                    targetHealth.ApplyDamage(poisonTickDamage);

                yield return new WaitForSeconds(poisonTickInterval);
                elapsed += poisonTickInterval;
            }
        }

        private IEnumerator PulsePoisonColor()
        {
            float t = 0f;

            while (t < 1f)
            {
                t += Time.deltaTime * colorTransitionSpeed;
                materialInstance.SetFloat(PoisonStrengthID, Mathf.SmoothStep(0f, 1f, t));
                yield return null;
            }

            yield return new WaitForSeconds(0.2f);

            while (t > 0f)
            {
                t -= Time.deltaTime * colorTransitionSpeed;
                materialInstance.SetFloat(PoisonStrengthID, Mathf.SmoothStep(0f, 1f, t));
                yield return null;
            }

            materialInstance.SetFloat(PoisonStrengthID, 0f);
        }
    }
}