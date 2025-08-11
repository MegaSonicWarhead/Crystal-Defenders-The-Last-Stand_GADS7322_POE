using System.Collections;
using UnityEngine;

namespace CrystalDefenders.Gameplay
{
    public class ResourceManager : MonoBehaviour
    {
        public static ResourceManager Instance { get; private set; }

        [SerializeField] private int startingResources = 300;
        [SerializeField] private int passiveGain = 5; // every 10 seconds
        [SerializeField] private float passiveIntervalSeconds = 10f;

        public int CurrentResources { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            CurrentResources = startingResources;
        }

        private void Start()
        {
            StartCoroutine(PassiveGainRoutine());
        }

        public bool CanAfford(int cost) => CurrentResources >= cost;
        public bool Spend(int cost)
        {
            if (!CanAfford(cost)) return false;
            CurrentResources -= cost; return true;
        }

        public void AddResources(int amount)
        {
            CurrentResources += Mathf.Max(0, amount);
        }

        public void AddWaveBonus(int amount)
        {
            AddResources(Mathf.Max(0, amount));
        }

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


