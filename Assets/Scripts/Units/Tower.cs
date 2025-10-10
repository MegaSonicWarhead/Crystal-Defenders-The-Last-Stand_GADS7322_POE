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
        public TMP_Text healthText;

        [Header("Projectile Prefabs")]
        public GameObject fireProjectile;
        public GameObject poisonProjectile;
        public GameObject regularProjectile;

        private void Awake()
        {
            health = GetComponent<Health>();
            health.SetMaxHealth(1000, true);
            health.onDeath.AddListener(OnTowerDestroyed);

            var aa = GetComponent<AutoAttack>();
            aa.range = 6f;
            aa.shotsPerSecond = 1.5f;
            aa.damagePerHit = 20;

            // Hook into AutoAttack to select correct projectile
            aa.OnBeforeShoot = SelectProjectileBasedOnEnemy;

            // Track this tower in the UI panel
            if (UIManager.Instance != null)
                UIManager.Instance.TrackTower(this);
        }

        private void SelectProjectileBasedOnEnemy()
        {
            var aa = GetComponent<AutoAttack>();
            var target = aa.GetCurrentTarget();
            if (target == null) return;

            var enemyHealth = target.GetComponent<Health>();
            if (enemyHealth == null) return;

            switch (enemyHealth.requiredDamageTag)
            {
                case "fire":
                    aa.SetProjectilePrefab(fireProjectile);
                    break;
                case "poison":
                    aa.SetProjectilePrefab(poisonProjectile);
                    break;
                default:
                    aa.SetProjectilePrefab(regularProjectile);
                    break;
            }
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

            if (healthText != null)
                healthText.text = "Tower Destroyed";
        }
    }
}