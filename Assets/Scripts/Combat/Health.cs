using UnityEngine;
using UnityEngine.Events;

namespace CrystalDefenders.Combat
{
    [DisallowMultipleComponent]
    public class Health : MonoBehaviour
    {
        [SerializeField] private int maxHealth = 100;
        public int MaxHealth => maxHealth;

        [Header("Damage Gating")]
        [SerializeField] public string requiredDamageTag = null; // if set, only damage with this tag applies

        private int currentHealth;
        public int CurrentHealth
        {
            get => currentHealth;
            set
            {
                if (currentHealth == value) return;

                int delta = value - currentHealth;
                currentHealth = Mathf.Clamp(value, 0, MaxHealth);

                if (delta < 0)
                    onDamaged?.Invoke(-delta);
                else if (delta > 0)
                    onHealed?.Invoke(delta);

                if (currentHealth == 0)
                    onDeath?.Invoke();
            }
        }

        public UnityEvent onDeath;
        public UnityEvent<int> onDamaged;
        public UnityEvent<int> onHealed;

        private void Awake()
        {
            CurrentHealth = maxHealth;
            //Debug.Log($"{gameObject.name} Health Awake: {CurrentHealth}/{maxHealth}");
        }

        public void SetMaxHealth(int value, bool fill = true)
        {
            maxHealth = Mathf.Max(1, value);
            if (fill) CurrentHealth = maxHealth;
            //Debug.Log($"{gameObject.name} SetMaxHealth: {CurrentHealth}/{maxHealth}");
        }

        public void ApplyDamage(int amount)
        {
            if (amount <= 0 || CurrentHealth <= 0) return;
            CurrentHealth -= amount;
            Debug.Log($"{gameObject.name} took {amount} damage, current: {CurrentHealth}/{maxHealth}");
        }

        public void ApplyDamage(int amount, string damageTag)
        {
            if (amount <= 0 || CurrentHealth <= 0) return;
            if (!string.IsNullOrEmpty(requiredDamageTag) && requiredDamageTag != damageTag) return;
            CurrentHealth -= amount;
            Debug.Log($"{gameObject.name} took {amount} damage (tag={damageTag}), current: {CurrentHealth}/{maxHealth}");
        }

        public void RestoreFullHealth()
        {
            CurrentHealth = MaxHealth;
        }

        public void Heal(int amount)
        {
            if (amount <= 0 || CurrentHealth <= 0) return;
            CurrentHealth += amount;
            //Debug.Log($"{gameObject.name} healed, current: {CurrentHealth}/{maxHealth}");
        }
    }
}


