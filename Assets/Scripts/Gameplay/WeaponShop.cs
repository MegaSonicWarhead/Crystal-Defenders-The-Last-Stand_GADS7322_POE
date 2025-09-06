using CrystalDefenders.Combat;
using CrystalDefenders.Gameplay;
using CrystalDefenders.Units;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class WeaponShop : MonoBehaviour
{
    [Header("UI References")]
    public TMP_Text resourceText;
    public Button weaponTurretButton;
    public Button repairButton;

    public static WeaponShop Instance { get; private set; }
    public bool HasDefenderToPlace { get; private set; } = false;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        weaponTurretButton.onClick.AddListener(OnWeaponTurretButton);
        repairButton.onClick.AddListener(OnRepairButton);
    }

    private void Update()
    {
        if (resourceText != null)
        {
            resourceText.text = $"Resources: {ResourceManager.Instance.CurrentResources}";
        }

        UpdateButtonStates();
    }

    private void UpdateButtonStates()
    {
        // Disable Buy button if not enough resources OR already holding a defender
        weaponTurretButton.interactable =
            ResourceManager.Instance.CurrentResources >= Defender.Cost && !HasDefenderToPlace;

        // Disable Repair button if not enough resources OR no damaged defenders
        bool hasDamaged = Defender.Registry.Any(
            d => d != null && d.GetComponent<Health>().CurrentHealth < d.GetComponent<Health>().MaxHealth
        );
        repairButton.interactable = ResourceManager.Instance.CurrentResources >= Defender.RepairCost && hasDamaged;
    }

    private void OnWeaponTurretButton()
    {
        if (ResourceManager.Instance.Spend(Defender.Cost))
        {
            HasDefenderToPlace = true;
            Debug.Log("Bought defender! Now click a placement node to place it.");
        }
        else
        {
            Debug.Log("Not enough resources for a defender!");
        }
    }

    public void OnDefenderPlaced()
    {
        HasDefenderToPlace = false;
    }

    private void OnRepairButton()
    {
        var damaged = Defender.Registry
            .Where(d => d != null && d.GetComponent<Health>().CurrentHealth < d.GetComponent<Health>().MaxHealth)
            .ToList();

        if (damaged.Count == 0)
        {
            Debug.Log("No damaged defenders to repair.");
            return;
        }

        if (!ResourceManager.Instance.Spend(Defender.RepairCost))
        {
            Debug.Log("Not enough resources to repair.");
            return;
        }

        var target = damaged[Random.Range(0, damaged.Count)];
        var health = target.GetComponent<Health>();
        health.RestoreFullHealth();
        Debug.Log($"Repaired {target.name} for {Defender.RepairCost} resources.");
    }
}
