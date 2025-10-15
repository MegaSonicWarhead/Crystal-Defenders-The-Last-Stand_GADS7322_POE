using UnityEngine;
using UnityEngine.Events;

namespace CrystalDefenders.Combat
{
    /// <summary>
    /// Manages health, damage, healing, and death events.
    /// Supports optional damage gating via `requiredDamageTag`.
    /// </summary>
    [DisallowMultipleComponent]
    public class Health : MonoBehaviour
    {
        [Header("Health Settings")]
        [SerializeField] private int maxHealth = 100;
        public int MaxHealth => maxHealth;

        [Header("Damage Gating")]
        [Tooltip("If empty or null, all damage is accepted. Otherwise, only matching damage tags apply.")]
        [SerializeField] public string requiredDamageTag = null;

        private int currentHealth;
        /// <summary>
        /// Current health value. Triggers events on change.
        /// </summary>
        public int CurrentHealth
        {
            get => currentHealth;
            set
            {
                if (currentHealth == value) return;

                int delta = value - currentHealth;
                currentHealth = Mathf.Clamp(value, 0, MaxHealth);

                // Trigger damage or healing events
                if (delta < 0)
                    onDamaged?.Invoke(-delta);
                else if (delta > 0)
                    onHealed?.Invoke(delta);

                // Trigger death event if health reaches 0
                if (currentHealth == 0)
                    onDeath?.Invoke();
            }
        }

        [Header("Events")]
        public UnityEvent onDeath;         // Invoked when CurrentHealth reaches 0
        public UnityEvent<int> onDamaged;  // Invoked when damage occurs, passes damage amount
        public UnityEvent<int> onHealed;   // Invoked when healing occurs, passes heal amount

        private void Awake()
        {
            // Initialize health at start
            CurrentHealth = maxHealth;
        }

        /// <summary>
        /// Set a new max health value. Optionally fill current health to max.
        /// </summary>
        public void SetMaxHealth(int value, bool fill = true)
        {
            maxHealth = Mathf.Max(1, value);
            if (fill) CurrentHealth = maxHealth;
        }

        /// <summary>
        /// Apply damage ignoring tags (useful for "any damage" scenarios)
        /// </summary>
        public void ApplyDamage(int amount)
        {
            if (amount <= 0 || CurrentHealth <= 0) return;
            CurrentHealth -= amount;
            // Optional: Debug.Log($"{gameObject.name} took {amount} damage, current: {CurrentHealth}/{maxHealth}");
        }

        /// <summary>
        /// Apply damage with a tag. Only applies if the tag matches requiredDamageTag (if set)
        /// </summary>
        public void ApplyDamage(int amount, string damageTag)
        {
            if (amount <= 0 || CurrentHealth <= 0) return;

            // Check damage gating by tag
            if (!string.IsNullOrEmpty(requiredDamageTag) &&
                !string.Equals(requiredDamageTag, damageTag, System.StringComparison.OrdinalIgnoreCase))
            {
                // Damage ignored due to tag mismatch
                // Optional: Debug.Log($"[Health] {gameObject.name} ignored {amount} damage (tag={damageTag}) | requiredTag={requiredDamageTag}");
                return;
            }

            CurrentHealth -= amount;
            // Optional: Debug.Log($"[Health] {gameObject.name} took {amount} damage (tag={damageTag}) | current: {CurrentHealth}/{MaxHealth}");
        }

        /// <summary>
        /// Fully restores health to maximum
        /// </summary>
        public void RestoreFullHealth()
        {
            CurrentHealth = MaxHealth;
        }

        /// <summary>
        /// Heals by a specified amount
        /// </summary>
        public void Heal(int amount)
        {
            if (amount <= 0 || CurrentHealth <= 0) return;
            CurrentHealth += amount;
            // Optional: Debug.Log($"{gameObject.name} healed {amount}, current: {CurrentHealth}/{maxHealth}");
        }
    }
}