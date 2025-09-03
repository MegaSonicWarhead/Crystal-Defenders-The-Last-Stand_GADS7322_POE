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
    [Header("Prefabs & UI References")]
    public GameObject healthBarPrefab;   // should be a Slider prefab
    public GameObject damageNumberPrefab;
    public Transform healthBarContainer;
    public TMP_Text resourceText;
    public List<Button> defenderButtons;
    public TMP_Text[] defenderCostTexts;

    private Dictionary<Health, Slider> healthBars = new Dictionary<Health, Slider>();

    private void Start()
    {
        UpdateResourcesUI();
        SetupDefenderButtons();
        SubscribeToDefenderHealth();
        SubscribeToTowerHealth();
        SubscribeToEnemyHealth();
    }

    private void Update()
    {
        UpdateHealthBars();
    }

    #region Health Bars

    private void SubscribeToDefenderHealth()
    {
        foreach (var def in Defender.Registry)
        {
            AddHealthBar(def.GetComponent<Health>());
        }
    }

    private void SubscribeToTowerHealth()
    {
        var tower = FindObjectOfType<Tower>();
        if (tower != null)
        {
            var h = tower.GetComponent<Health>();
            AddHealthBar(h);
            h.onDeath.AddListener(OnTowerDestroyed);
        }
    }

    private void SubscribeToEnemyHealth()
    {
        foreach (var enemy in EnemyRegistry.Enemies)
        {
            AddHealthBar(enemy.GetComponent<Health>());
        }
    }

    private void AddHealthBar(Health health)
    {
        if (health == null || healthBars.ContainsKey(health)) return;

        GameObject hbObj = Instantiate(healthBarPrefab, healthBarContainer);
        Slider bar = hbObj.GetComponent<Slider>();

        if (bar == null)
        {
            Debug.LogError("HealthBarPrefab is missing a Slider component!");
            Destroy(hbObj);
            return;
        }

        // Set slider min/max
        bar.minValue = 0;
        bar.maxValue = health.MaxHealth;
        bar.value = health.CurrentHealth;

        health.onDamaged.AddListener(amount =>
        {
            bar.value = health.CurrentHealth;
            ShowDamageNumber(health.transform.position, amount);
        });

        health.onHealed.AddListener(amount =>
        {
            bar.value = health.CurrentHealth;
        });

        health.onDeath.AddListener(() => { Destroy(hbObj); });

        healthBars.Add(health, bar);
    }

    private void UpdateHealthBars()
    {
        foreach (var kvp in healthBars)
        {
            var h = kvp.Key;
            var bar = kvp.Value;
            if (h != null && bar != null)
            {
                bar.value = h.CurrentHealth;
                Vector3 screenPos = Camera.main.WorldToScreenPoint(h.transform.position + Vector3.up * 2f);
                bar.transform.position = screenPos;
            }
        }
    }

    #endregion

    #region Damage Numbers

    private void ShowDamageNumber(Vector3 worldPos, int amount)
    {
        GameObject dmg = Instantiate(damageNumberPrefab, healthBarContainer);
        dmg.GetComponent<TMP_Text>().text = amount.ToString();
        dmg.transform.position = Camera.main.WorldToScreenPoint(worldPos + Vector3.up * 2f);
        Destroy(dmg, 1f); // auto-destroy after 1 second
    }

    #endregion

    #region Resources UI

    public void UpdateResourcesUI()
    {
        if (resourceText != null && ResourceManager.Instance != null)
        {
            resourceText.text = $"Resources: {ResourceManager.Instance.CurrentResources}";
        }

        UpdateDefenderButtonStates();
    }

    private void SetupDefenderButtons()
    {
        for (int i = 0; i < defenderButtons.Count; i++)
        {
            int cost = Defender.Cost;
            if (i < defenderCostTexts.Length)
                defenderCostTexts[i].text = cost.ToString();

            int index = i;
            defenderButtons[i].onClick.AddListener(() =>
            {
                TryPlaceDefender(index);
            });
        }
    }

    private void UpdateDefenderButtonStates()
    {
        foreach (var btn in defenderButtons)
        {
            btn.interactable = ResourceManager.Instance.CurrentResources >= Defender.Cost;
        }
    }

    private void TryPlaceDefender(int index)
    {
        if (ResourceManager.Instance.CurrentResources >= Defender.Cost)
        {
            ResourceManager.Instance.Spend(Defender.Cost);
            UpdateResourcesUI();
            Debug.Log($"Placed Defender #{index}");
        }
    }

    #endregion

    #region Tower Destroyed

    private void OnTowerDestroyed()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("GameOver"); // <-- replace with your Game Over scene name
    }

    #endregion
}
