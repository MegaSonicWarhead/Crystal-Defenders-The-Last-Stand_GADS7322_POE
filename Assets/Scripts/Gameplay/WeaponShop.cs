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
    public Button poisonArcherButton;
    public Button fireMageButton;
    public Button repairButton;

    [Header("Defender Prefabs")]
    public Defender defaultDefenderPrefab;
    public Defender poisonArcherPrefab;
    public Defender fireMagePrefab;

    [Header("Costs")]
    public int poisonArcherCost = 150;
    public int fireMageCost = 200;

    public static WeaponShop Instance { get; private set; }
    public bool HasDefenderToPlace { get; private set; } = false;
    public Defender SelectedDefenderPrefab { get; private set; }

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
        if (poisonArcherButton != null) poisonArcherButton.onClick.AddListener(OnPoisonArcherButton);
        if (fireMageButton != null) fireMageButton.onClick.AddListener(OnFireMageButton);
        repairButton.onClick.AddListener(OnRepairButton);
    }

    private void Update()
    {
        if (resourceText != null)
        {
            resourceText.text = $"Resources: {ResourceManager.Instance.CurrentResources}";
        }

        // Allow user to cancel placement with Escape or Right-Click
        if (HasDefenderToPlace && (Input.GetKeyDown(KeyCode.Escape) || Input.GetMouseButtonDown(1)))
        {
            CancelPlacement();
        }

        UpdateButtonStates();
    }

    private void UpdateButtonStates()
    {
        // Disable Buy buttons if not enough resources OR already holding a defender OR no free nodes
        bool hasFreeNode = HasAnyAvailablePlacementNode();
        weaponTurretButton.interactable =
            ResourceManager.Instance.CurrentResources >= Defender.Cost && !HasDefenderToPlace && hasFreeNode;

        if (poisonArcherButton != null)
        {
            poisonArcherButton.interactable =
                ResourceManager.Instance.CurrentResources >= poisonArcherCost && !HasDefenderToPlace && hasFreeNode;
        }
        if (fireMageButton != null)
        {
            fireMageButton.interactable =
                ResourceManager.Instance.CurrentResources >= fireMageCost && !HasDefenderToPlace && hasFreeNode;
        }

        // Disable Repair button if not enough resources OR no damaged defenders
        bool hasDamaged = Defender.Registry.Any(
            d => d != null && d.GetComponent<Health>().CurrentHealth < d.GetComponent<Health>().MaxHealth
        );
        repairButton.interactable = ResourceManager.Instance.CurrentResources >= Defender.RepairCost && hasDamaged;
    }

    private bool HasAnyAvailablePlacementNode()
    {
        var nodes = FindObjectsOfType<PlacementNode>(includeInactive: false);
        for (int i = 0; i < nodes.Length; i++)
        {
            if (nodes[i] != null && nodes[i].IsAvailable) return true;
        }
        return false;
    }

    private void OnWeaponTurretButton()
    {
        // Toggle selection if already holding same type
        if (HasDefenderToPlace && SelectedDefenderPrefab == defaultDefenderPrefab)
        {
            CancelPlacement();
            return;
        }

        if (!HasAnyAvailablePlacementNode())
        {
            Debug.Log("No available placement nodes.");
            return;
        }

        if (ResourceManager.Instance.Spend(Defender.Cost))
        {
            SelectedDefenderPrefab = defaultDefenderPrefab;
            HasDefenderToPlace = true;
            Debug.Log("Bought defender! Now click a placement node to place it.");
        }
        else
        {
            Debug.Log("Not enough resources for a defender!");
        }
    }

    private void OnPoisonArcherButton()
    {
        if (HasDefenderToPlace && SelectedDefenderPrefab == poisonArcherPrefab)
        {
            CancelPlacement();
            return;
        }

        if (!HasAnyAvailablePlacementNode())
        {
            Debug.Log("No available placement nodes.");
            return;
        }

        if (ResourceManager.Instance.Spend(poisonArcherCost))
        {
            SelectedDefenderPrefab = poisonArcherPrefab;
            HasDefenderToPlace = true;
            Debug.Log("Bought Poison Archer! Now click a placement node to place it.");
        }
        else
        {
            Debug.Log("Not enough resources for a Poison Archer!");
        }
    }

    private void OnFireMageButton()
    {
        if (HasDefenderToPlace && SelectedDefenderPrefab == fireMagePrefab)
        {
            CancelPlacement();
            return;
        }

        if (!HasAnyAvailablePlacementNode())
        {
            Debug.Log("No available placement nodes.");
            return;
        }

        if (ResourceManager.Instance.Spend(fireMageCost))
        {
            SelectedDefenderPrefab = fireMagePrefab;
            HasDefenderToPlace = true;
            Debug.Log("Bought Fire Mage! Now click a placement node to place it.");
        }
        else
        {
            Debug.Log("Not enough resources for a Fire Mage!");
        }
    }

    public void OnDefenderPlaced()
    {
        HasDefenderToPlace = false;
        SelectedDefenderPrefab = null;
    }

    private void CancelPlacement()
    {
        HasDefenderToPlace = false;
        SelectedDefenderPrefab = null;
        Debug.Log("Placement cancelled.");
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
