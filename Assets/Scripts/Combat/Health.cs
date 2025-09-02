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
        public UnityEvent<int> onDamaged;
        public UnityEvent<int> onHealed;

        private void Awake()
        {
            CurrentHealth = maxHealth;
            Debug.Log($"{gameObject.name} Health Awake: {CurrentHealth}/{maxHealth}");
        }

        public void SetMaxHealth(int value, bool fill = true)
        {
            maxHealth = Mathf.Max(1, value);
            if (fill) CurrentHealth = maxHealth;
            Debug.Log($"{gameObject.name} SetMaxHealth: {CurrentHealth}/{maxHealth}");
        }

        public void ApplyDamage(int amount)
        {
            if (amount <= 0 || CurrentHealth <= 0) return;
            CurrentHealth = Mathf.Max(0, CurrentHealth - amount);
            Debug.Log($"{gameObject.name} took {amount} damage, current: {CurrentHealth}/{maxHealth}");
            onDamaged?.Invoke(amount);
            if (CurrentHealth == 0)
            {
                Debug.Log($"{gameObject.name} died");
                onDeath?.Invoke();
            }
        }

        public void Heal(int amount)
        {
            if (amount <= 0 || CurrentHealth <= 0) return;
            int before = CurrentHealth;
            CurrentHealth = Mathf.Min(maxHealth, CurrentHealth + amount);
            Debug.Log($"{gameObject.name} healed {CurrentHealth - before}, current: {CurrentHealth}/{maxHealth}");
            onHealed?.Invoke(CurrentHealth - before);
        }
    }
}


