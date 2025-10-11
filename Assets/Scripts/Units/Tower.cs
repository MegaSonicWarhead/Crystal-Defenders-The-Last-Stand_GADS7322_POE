using CrystalDefenders.Combat;
using TMPro;
using UnityEngine;

namespace CrystalDefenders.Units
{
    [RequireComponent(typeof(Health))]
    [RequireComponent(typeof(AutoAttackBaseTower))] // ✅ Use BaseTower attack script
    public class Tower : MonoBehaviour
    {
        private Health health;

        [Header("UI Panel")]
        public TMP_Text healthText; // Assign in your UI panel

        private void Awake()
        {
            health = GetComponent<Health>();
            health.SetMaxHealth(1000, true);
            health.onDeath.AddListener(OnTowerDestroyed);

            // ✅ Subscribe to damage/heal events to update UI
            health.onDamaged.AddListener(UpdateHealthUI);
            health.onHealed.AddListener(UpdateHealthUI);

            // Configure the BaseTower attack script
            var aa = GetComponent<AutoAttackBaseTower>();
            aa.range = 6f;
            aa.shotsPerSecond = 1.5f;
            aa.baseDamage = 20;       // Base damage before multiplier
            aa.damageMultiplier = 0.5f; // 50% damage output

            // Track this tower in the UI panel
            if (UIManager.Instance != null)
                UIManager.Instance.TrackTower(this);

            // Update UI at start
            UpdateHealthUI(0);
        }

        private void UpdateHealthUI(int _)
        {
            if (healthText != null)
            {
                healthText.text = $"Crystal Tower Health: {health.CurrentHealth}/{health.MaxHealth}";
            }
        }

        private void OnTowerDestroyed()
        {
            Debug.Log($"Tower {gameObject.name} destroyed");
            Gameplay.GameManager.Instance?.OnTowerDestroyed();

            // Optionally clear UI
            if (healthText != null)
                healthText.text = "Tower Destroyed";
        }
    }
}