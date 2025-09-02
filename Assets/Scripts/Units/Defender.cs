using System.Collections.Generic;
using UnityEngine;
using CrystalDefenders.Combat;

namespace CrystalDefenders.Units
{
    [RequireComponent(typeof(Health))]
    [RequireComponent(typeof(AutoAttack))]
    public class Defender : MonoBehaviour
    {
        public const int Cost = 100;
        public const int RepairCost = 25;

        private Health health;
        private static readonly List<Defender> registry = new List<Defender>();
        public static IReadOnlyList<Defender> Registry => registry;

        private void Awake()
        {
            health = GetComponent<Health>();
            health.SetMaxHealth(200, true);
            var aa = GetComponent<AutoAttack>();
            aa.range = 5f;
            aa.shotsPerSecond = 2f;
            aa.damagePerHit = 15;

            Debug.Log($"Defender Awake: {gameObject.name}");
        }

        private void OnEnable()
        {
            if (!registry.Contains(this)) registry.Add(this);
            Debug.Log($"Defender Enabled: {gameObject.name}, total registry: {registry.Count}");
        }

        private void OnDisable()
        {
            registry.Remove(this);
            Debug.Log($"Defender Disabled: {gameObject.name}, remaining registry: {registry.Count}");
        }
    }
}


