using CrystalDefenders.Combat;
using CrystalDefenders.Units;
using CrystalDefenders.Gameplay;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("Prefabs & UI References")]
    public GameObject healthBarPrefab;   // Slider prefab
    public Transform healthBarContainer; // Canvas container for enemy bars

    [Header("Tower UI")]
    public TMP_Text towerHealthText;     // Assign your UI panel text here

    private Dictionary<Health, Slider> healthBars = new Dictionary<Health, Slider>();
    private Tower trackedTower;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Update()
    {
        UpdateHealthBarPositions();
    }

    // ================= Enemy Health Bars =================
    public void AttachHealthBar(Health health)
    {
        if (health == null || healthBars.ContainsKey(health)) return;

        GameObject hbObj = Instantiate(healthBarPrefab, healthBarContainer);
        Slider bar = hbObj.GetComponent<Slider>();

        if (bar == null)
        {
            Debug.LogError("HealthBarPrefab must have a Slider component!");
            Destroy(hbObj);
            return;
        }

        bar.minValue = 0;
        bar.maxValue = health.MaxHealth;
        bar.value = health.CurrentHealth;

        // Update slider on health changes
        health.onDamaged.AddListener(_ => UpdateBarValue(health));
        health.onHealed.AddListener(_ => UpdateBarValue(health));

        // Remove bar when enemy dies
        health.onDeath.AddListener(() =>
        {
            if (healthBars.ContainsKey(health))
                healthBars.Remove(health);
            Destroy(hbObj);
        });

        healthBars.Add(health, bar);
    }

    private void UpdateBarValue(Health health)
    {
        if (health != null && healthBars.TryGetValue(health, out Slider bar) && bar != null)
        {
            bar.value = health.CurrentHealth;
        }
    }

    private void UpdateHealthBarPositions()
    {
        List<Health> toRemove = null;

        foreach (var kvp in healthBars)
        {
            var health = kvp.Key;
            var bar = kvp.Value;

            if (health == null || bar == null)
            {
                if (toRemove == null) toRemove = new List<Health>();
                toRemove.Add(health);
                continue;
            }

            Vector3 screenPos = Camera.main.WorldToScreenPoint(health.transform.position + Vector3.up * 2f);
            bar.transform.position = screenPos;
        }

        if (toRemove != null)
        {
            foreach (var h in toRemove)
                healthBars.Remove(h);
        }
    }

    // ================= Tower Health Panel =================
    public void TrackTower(Tower tower)
    {
        if (tower == null) return;

        // Stop tracking previous tower
        if (trackedTower != null)
        {
            var prevHealth = trackedTower.GetComponent<Health>();
            prevHealth.onDamaged.RemoveListener(UpdateTowerHealthUI);
            prevHealth.onHealed.RemoveListener(UpdateTowerHealthUI);
            trackedTower = null;
        }

        trackedTower = tower;
        var health = tower.GetComponent<Health>();
        if (health != null)
        {
            health.onDamaged.AddListener(UpdateTowerHealthUI);
            health.onHealed.AddListener(UpdateTowerHealthUI);
            UpdateTowerHealthUI(0);
        }
    }

    private void UpdateTowerHealthUI(int _)
    {
        if (trackedTower == null || towerHealthText == null) return;

        var health = trackedTower.GetComponent<Health>();
        towerHealthText.text = $"Crystal Tower Health: {health.CurrentHealth}/{health.MaxHealth}";
    }
}
