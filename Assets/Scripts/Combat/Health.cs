using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace CrystalDefenders.Combat
{
    [DisallowMultipleComponent]
    public class Health : MonoBehaviour
    {
        [Header("Health Settings")]
        [SerializeField] private int maxHealth = 100;
        public int MaxHealth => maxHealth;
        public IReadOnlyList<string> RequiredDamageTags => requiredDamageTags;

        [Header("Damage Gating")]
        [Tooltip("If empty or null, all damage is accepted. Otherwise, only matching damage tags apply.")]
        [SerializeField] private List<string> requiredDamageTags = new();

        // Backwards compatibility for old single-tag code
        [SerializeField, HideInInspector]
        public string requiredDamageTag
        {
            get => (requiredDamageTags != null && requiredDamageTags.Count > 0) ? requiredDamageTags[0] : null;
            set
            {
                if (requiredDamageTags == null) requiredDamageTags = new List<string>();
                if (!string.IsNullOrEmpty(value) && !requiredDamageTags.Contains(value))
                    requiredDamageTags.Add(value);
            }
        }

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

        [Header("Events")]
        public UnityEvent onDeath;
        public UnityEvent<int> onDamaged;
        public UnityEvent<int> onHealed;
        public UnityEvent<int> onMaxHealthChanged;

        private void Awake()
        {
            CurrentHealth = maxHealth;
        }

        public void SetMaxHealth(int value, bool fill = true)
        {
            int oldMax = maxHealth;
            maxHealth = Mathf.Max(1, value);

            if (fill)
                CurrentHealth = maxHealth;
            else if (CurrentHealth > maxHealth)
                CurrentHealth = maxHealth;

            // Fire event if max health changed
            if (oldMax != maxHealth)
                onMaxHealthChanged?.Invoke(maxHealth);
        }

        public void ApplyDamage(int amount)
        {
            if (amount <= 0 || CurrentHealth <= 0) return;
            CurrentHealth -= amount;
        }

        public void ApplyDamage(int amount, string damageTag)
        {
            if (amount <= 0 || CurrentHealth <= 0) return;

            // If no tags specified, allow all damage
            if (requiredDamageTags == null || requiredDamageTags.Count == 0)
            {
                CurrentHealth -= amount;
                return;
            }

            // If damage has a tag, check for a match
            if (!string.IsNullOrEmpty(damageTag))
            {
                foreach (var tag in requiredDamageTags)
                {
                    if (string.Equals(tag, damageTag, System.StringComparison.OrdinalIgnoreCase))
                    {
                        CurrentHealth -= amount;
                        return;
                    }
                }
            }
            else
            {
                // Damage without a tag hits if "Default" is allowed
                if (requiredDamageTags.Contains("Default"))
                {
                    CurrentHealth -= amount;
                    return;
                }
            }
        }

        public void RestoreFullHealth() => CurrentHealth = MaxHealth;

        public void Heal(int amount)
        {
            if (amount <= 0 || CurrentHealth <= 0) return;
            CurrentHealth += amount;
        }

        public void SetRequiredTags(params string[] tags)
        {
            requiredDamageTags.Clear();
            requiredDamageTags.AddRange(tags);
        }
    }
}