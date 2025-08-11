using UnityEngine;
using UnityEngine.Events;

namespace CrystalDefenders.Combat
{
    [DisallowMultipleComponent]
    public class Health : MonoBehaviour
    {
        [SerializeField] private int maxHealth = 100;
        public int MaxHealth => maxHealth;
        public int CurrentHealth { get; private set; }

        public UnityEvent onDeath;
        public UnityEvent<int> onDamaged; // damage amount
        public UnityEvent<int> onHealed; // heal amount

        private void Awake()
        {
            CurrentHealth = maxHealth;
        }

        public void SetMaxHealth(int value, bool fill = true)
        {
            maxHealth = Mathf.Max(1, value);
            if (fill) CurrentHealth = maxHealth;
        }

        public void ApplyDamage(int amount)
        {
            if (amount <= 0 || CurrentHealth <= 0) return;
            CurrentHealth = Mathf.Max(0, CurrentHealth - amount);
            onDamaged?.Invoke(amount);
            if (CurrentHealth == 0)
            {
                onDeath?.Invoke();
            }
        }

        public void Heal(int amount)
        {
            if (amount <= 0 || CurrentHealth <= 0) return;
            int before = CurrentHealth;
            CurrentHealth = Mathf.Min(maxHealth, CurrentHealth + amount);
            onHealed?.Invoke(CurrentHealth - before);
        }
    }
}


