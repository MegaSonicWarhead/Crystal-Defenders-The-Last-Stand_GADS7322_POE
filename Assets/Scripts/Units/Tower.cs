using UnityEngine;
using CrystalDefenders.Combat;

namespace CrystalDefenders.Units
{
    [RequireComponent(typeof(Health))]
    [RequireComponent(typeof(AutoAttack))]
    public class Tower : MonoBehaviour
    {
        private Health health;

        private void Awake()
        {
            health = GetComponent<Health>();
            health.SetMaxHealth(1000, true);
            health.onDeath.AddListener(OnTowerDestroyed);

            var aa = GetComponent<AutoAttack>();
            aa.range = 6f;
            aa.shotsPerSecond = 1.5f;
            aa.damagePerHit = 20;
        }

        private void OnTowerDestroyed()
        {
            Gameplay.GameManager.Instance?.OnTowerDestroyed();
        }
    }
}


