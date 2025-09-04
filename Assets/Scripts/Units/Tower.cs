using CrystalDefenders.Combat;
using TMPro;
using UnityEngine;

namespace CrystalDefenders.Units
{
    [RequireComponent(typeof(Health))]
    [RequireComponent(typeof(AutoAttack))]
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

            var aa = GetComponent<AutoAttack>();
            aa.range = 6f;
            aa.shotsPerSecond = 1.5f;
            aa.damagePerHit = 20;

            // Track this tower in the UI panel
            if (UIManager.Instance != null)
                UIManager.Instance.TrackTower(this);
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



