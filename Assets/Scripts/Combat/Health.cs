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
        [Tooltip("If empty or null, all damage is accepted")]
        [SerializeField] public string requiredDamageTag = null;

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
        }

        public void SetMaxHealth(int value, bool fill = true)
        {
            maxHealth = Mathf.Max(1, value);
            if (fill) CurrentHealth = maxHealth;
        }

        /// <summary>Apply damage ignoring tags (useful for "any damage")</summary>
        public void ApplyDamage(int amount)
        {
            if (amount <= 0 || CurrentHealth <= 0) return;
            CurrentHealth -= amount;
            //Debug.Log($"{gameObject.name} took {amount} damage, current: {CurrentHealth}/{maxHealth}");
        }

        /// <summary>Apply damage with a tag, respects requiredDamageTag if set</summary>
        public void ApplyDamage(int amount, string damageTag)
        {
            if (amount <= 0 || CurrentHealth <= 0) return;

            // Check tag gating
            if (!string.IsNullOrEmpty(requiredDamageTag) &&
                !string.Equals(requiredDamageTag, damageTag, System.StringComparison.OrdinalIgnoreCase))
            {
                //Debug.Log($"[Health] {gameObject.name} ignored {amount} damage (tag={damageTag}) | requiredTag={requiredDamageTag}");
                return;
            }

            CurrentHealth -= amount;

            //Debug.Log($"[Health] {gameObject.name} took {amount} damage (tag={damageTag}) | current: {CurrentHealth}/{MaxHealth}");
        }

        public void RestoreFullHealth()
        {
            CurrentHealth = MaxHealth;
        }

        public void Heal(int amount)
        {
            if (amount <= 0 || CurrentHealth <= 0) return;
            CurrentHealth += amount;
            //Debug.Log($"{gameObject.name} healed {amount}, current: {CurrentHealth}/{maxHealth}");
        }
    }
}