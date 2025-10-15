using System.Collections;
using UnityEngine;

namespace CrystalDefenders.Gameplay
{
    /// <summary>
    /// Manages player resources for the game.
    /// Handles spending, earning, passive income, and wave bonuses.
    /// Implements a singleton pattern for global access.
    /// </summary>
    public class ResourceManager : MonoBehaviour
    {
        // --- Singleton instance ---
        public static ResourceManager Instance { get; private set; }

        [Header("Resource Settings")]
        [SerializeField] private int startingResources = 300;     // Initial resources at game start
        [SerializeField] private int passiveGain = 5;             // Amount gained automatically every interval
        [SerializeField] private float passiveIntervalSeconds = 10f; // Interval for passive income in seconds

        /// <summary>
        /// Current available resources.
        /// </summary>
        public int CurrentResources { get; private set; }

        private void Awake()
        {
            // Singleton enforcement: destroy duplicate instances
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            // Initialize starting resources
            CurrentResources = startingResources;
        }

        private void Start()
        {
            // Begin passive resource gain coroutine
            StartCoroutine(PassiveGainRoutine());
        }

        /// <summary>
        /// Checks if the player has enough resources for a given cost.
        /// </summary>
        /// <param name="cost">The cost to check against</param>
        /// <returns>True if affordable, false otherwise</returns>
        public bool CanAfford(int cost) => CurrentResources >= cost;

        /// <summary>
        /// Spends resources if affordable.
        /// </summary>
        /// <param name="cost">Amount to spend</param>
        /// <returns>True if successfully spent, false if not enough resources</returns>
        public bool Spend(int cost)
        {
            if (!CanAfford(cost)) return false;
            CurrentResources -= cost;
            return true;
        }

        /// <summary>
        /// Adds a positive amount of resources to the current total.
        /// </summary>
        /// <param name="amount">Amount to add (non-negative)</param>
        public void AddResources(int amount)
        {
            CurrentResources += Mathf.Max(0, amount);
        }

        /// <summary>
        /// Adds a resource bonus for completing a wave.
        /// </summary>
        /// <param name="amount">Bonus amount (non-negative)</param>
        public void AddWaveBonus(int amount)
        {
            AddResources(Mathf.Max(0, amount));
        }

        /// <summary>
        /// Coroutine that adds passive resources at fixed intervals.
        /// </summary>
        private IEnumerator PassiveGainRoutine()
        {
            var wait = new WaitForSeconds(passiveIntervalSeconds);
            while (true)
            {
                yield return wait;
                AddResources(passiveGain);
            }
        }
    }
}