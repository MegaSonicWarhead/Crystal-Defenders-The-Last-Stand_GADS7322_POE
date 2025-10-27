using CrystalDefenders.Combat;
using TMPro;
using UnityEngine;

namespace CrystalDefenders.Units
{
    /// <summary>
    /// Represents the main crystal tower that must be defended.
    /// Handles health, UI updates, and integrates with the BaseTower auto-attack system.
    /// Also controls the red vignette shader effect when the tower's health is low.
    /// </summary>
    [RequireComponent(typeof(Health))]
    [RequireComponent(typeof(AutoAttackBaseTower))]
    public class Tower : MonoBehaviour
    {
        private Health health;
        private LowHealthPostEffect postEffect; // reference to the camera effect

        [Header("UI Panel")]
        [Tooltip("Reference to the UI Text element used to display tower health.")]
        public TMP_Text healthText; // Assign in inspector (UI panel)

        [Header("Visual Warning Settings")]
        [Tooltip("Minimum health percentage before red vignette starts showing.")]
        [Range(0f, 1f)] public float warningThreshold = 0.5f;
        [Tooltip("How quickly the red vignette fades in/out.")]
        public float vignetteFadeSpeed = 4f;

        private void Awake()
        {
            // Initialize and configure health component
            health = GetComponent<Health>();
            health.SetMaxHealth(2000, true);
            health.onDeath.AddListener(OnTowerDestroyed);

            // Subscribe to health events
            health.onDamaged.AddListener(UpdateHealthUI);
            health.onHealed.AddListener(UpdateHealthUI);
            health.onDamaged.AddListener(UpdateVignetteEffect);
            health.onHealed.AddListener(UpdateVignetteEffect);

            // Configure the tower's offensive capabilities
            var aa = GetComponent<AutoAttackBaseTower>();
            aa.range = 6f;
            aa.shotsPerSecond = 1.5f;
            aa.baseDamage = 20;
            aa.damageMultiplier = 0.5f;

            // Register this tower with the UI manager
            if (UIManager.Instance != null)
                UIManager.Instance.TrackTower(this);

            // Find camera post effect
            if (Camera.main != null)
                postEffect = Camera.main.GetComponent<LowHealthPostEffect>();

            // Initialize visuals and UI
            UpdateHealthUI(0);
            UpdateVignetteEffect(0);
        }

        /// <summary>
        /// Updates the tower's health UI whenever damage or healing occurs.
        /// </summary>
        private void UpdateHealthUI(int _)
        {
            if (healthText != null)
            {
                healthText.text = $"Crystal Tower Health: {health.CurrentHealth}/{health.MaxHealth}";
            }
        }

        /// <summary>
        /// Updates the vignette shader intensity based on the tower's current health.
        /// </summary>
        private void UpdateVignetteEffect(int _)
        {
            if (postEffect == null) return;

            float ratio = (float)health.CurrentHealth / health.MaxHealth;

            // Start vignette fade-in only when below threshold
            float targetIntensity = ratio < warningThreshold ? Mathf.Lerp(1f, 0f, ratio / warningThreshold) : 0f;

            // Smoothly interpolate toward target intensity
            postEffect.intensity = Mathf.Lerp(postEffect.intensity, targetIntensity, Time.deltaTime * vignetteFadeSpeed);
        }

        /// <summary>
        /// Triggered when the tower's health reaches zero.
        /// </summary>
        private void OnTowerDestroyed()
        {
            Debug.Log($"Tower {gameObject.name} destroyed");

            Gameplay.GameManager.Instance?.OnTowerDestroyed();

            if (healthText != null)
                healthText.text = "Tower Destroyed";

            if (postEffect != null)
                postEffect.intensity = 1f; // Max red overlay at death
        }

        private void Update()
        {
            // Optional: continuously smooth vignette fade each frame
            if (postEffect != null && health != null)
            {
                float ratio = (float)health.CurrentHealth / health.MaxHealth;
                float targetIntensity = ratio < warningThreshold ? Mathf.Lerp(1f, 0f, ratio / warningThreshold) : 0f;
                postEffect.intensity = Mathf.Lerp(postEffect.intensity, targetIntensity, Time.deltaTime * vignetteFadeSpeed);
            }
        }
    }
}