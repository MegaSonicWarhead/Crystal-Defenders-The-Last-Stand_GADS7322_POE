using CrystalDefenders.Combat;
using CrystalDefenders.Gameplay;
using CrystalDefenders.Generation;
using CrystalDefenders.Units;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class WeaponShop : MonoBehaviour
{
    [Header("UI References")]
    public TMP_Text resourceText;
    public Button weaponTurretButton;
    public Button poisonArcherButton;
    public Button fireMageButton;
    public Button repairButton;
    public Button upgradeButton;

    [Header("Defender Prefabs")]
    public Defender defaultDefenderPrefab;
    public Defender poisonArcherPrefab;
    public Defender fireMagePrefab;

    [Header("Costs")]
    public int poisonArcherCost = 150;
    public int fireMageCost = 200;

    [Header("Placement Node Settings")]
    public GameObject placementNodePrefab;
    public int placementNodeCost = 50;
    public int maxExtraNodes = 4;    // Max extra nodes player can buy
    private int purchasedNodes = 0;  // Tracks how many extra nodes bought

    public static WeaponShop Instance { get; private set; }
    public bool HasDefenderToPlace { get; private set; } = false;
    public Defender SelectedDefenderPrefab { get; private set; }

    private SelectableTower selectedTower;
    private DefenderPlacementManager placementManager;

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
        weaponTurretButton.onClick.AddListener(() => BuyDefender(defaultDefenderPrefab, Defender.Cost, "Defender"));
        if (poisonArcherButton != null) poisonArcherButton.onClick.AddListener(() => BuyDefender(poisonArcherPrefab, poisonArcherCost, "Poison Archer"));
        if (fireMageButton != null) fireMageButton.onClick.AddListener(() => BuyDefender(fireMagePrefab, fireMageCost, "Fire Mage"));
        repairButton.onClick.AddListener(OnRepairButton);
        if (upgradeButton != null) upgradeButton.onClick.AddListener(OnUpgradeButton);

        placementManager = FindObjectOfType<DefenderPlacementManager>();

        // Generate initial procedural nodes
        var generator = FindObjectOfType<ProceduralTerrainGenerator>();
        if (generator != null && placementManager != null)
            placementManager.CreateNodesNearPaths(generator);
    }

    private void Update()
    {
        if (resourceText != null)
            resourceText.text = $"Resources: {ResourceManager.Instance.CurrentResources}";

        if (HasDefenderToPlace && (Input.GetKeyDown(KeyCode.Escape) || Input.GetMouseButtonDown(1)))
            CancelPlacement();

        if (Input.GetMouseButtonDown(1) && selectedTower != null && !HasDefenderToPlace)
            DeselectTower();

        UpdateButtonStates();
    }

    // --- Extra node purchase ---
    public void BuyPlacementNode()
    {
        if (purchasedNodes >= maxExtraNodes)
        {
            Debug.Log("Max extra nodes purchased.");
            return;
        }

        if (!ResourceManager.Instance.CanAfford(placementNodeCost))
        {
            Debug.Log("Not enough resources to buy a placement node.");
            return;
        }

        if (ResourceManager.Instance.Spend(placementNodeCost))
        {
            purchasedNodes++;
            Debug.Log($"Extra placement node purchased! Remaining: {maxExtraNodes - purchasedNodes}");

            if (placementManager != null)
                placementManager.StartPlacingExtraNode(placementNodePrefab);
        }
    }

    private void UpdateButtonStates()
    {
        bool hasFreeNode = placementManager != null && placementManager.HasAvailableNode();

        weaponTurretButton.interactable =
            ResourceManager.Instance.CurrentResources >= Defender.Cost && !HasDefenderToPlace && hasFreeNode;

        if (poisonArcherButton != null)
            poisonArcherButton.interactable =
                ResourceManager.Instance.CurrentResources >= poisonArcherCost && !HasDefenderToPlace && hasFreeNode;

        if (fireMageButton != null)
            fireMageButton.interactable =
                ResourceManager.Instance.CurrentResources >= fireMageCost && !HasDefenderToPlace && hasFreeNode;

        bool hasDamaged = Defender.Registry.Any(d => d != null && d.GetComponent<Health>().CurrentHealth < d.GetComponent<Health>().MaxHealth);
        repairButton.interactable = ResourceManager.Instance.CurrentResources >= Defender.RepairCost && hasDamaged;

        if (upgradeButton != null)
        {
            if (selectedTower != null)
            {
                var up = selectedTower.GetUpgradeable();
                upgradeButton.interactable =
                    up != null && up.CanUpgrade() && UpgradeManager.Instance.CanAfford(100);
            }
            else upgradeButton.interactable = false;
        }
    }

    private void BuyDefender(Defender prefab, int cost, string name)
    {
        if (HasDefenderToPlace && SelectedDefenderPrefab == prefab)
        {
            CancelPlacement();
            return;
        }

        if (placementManager == null || !placementManager.HasAvailableNode())
        {
            Debug.Log("No available placement nodes.");
            return;
        }

        if (ResourceManager.Instance.Spend(cost))
        {
            SelectedDefenderPrefab = prefab;
            HasDefenderToPlace = true;
            Debug.Log($"Bought {name}! Now click a placement node to place it.");
        }
        else
        {
            Debug.Log($"Not enough resources for a {name}!");
        }
    }

    public void OnDefenderPlaced()
    {
        HasDefenderToPlace = false;
        SelectedDefenderPrefab = null;
        placementManager?.OnNodePlaced();
    }

    private void CancelPlacement()
    {
        HasDefenderToPlace = false;
        SelectedDefenderPrefab = null;
        Debug.Log("Placement cancelled.");
    }

    private void OnRepairButton()
    {
        var damaged = Defender.Registry.Where(d => d != null && d.GetComponent<Health>().CurrentHealth < d.GetComponent<Health>().MaxHealth).ToList();
        if (damaged.Count == 0) return;

        if (!ResourceManager.Instance.Spend(Defender.RepairCost)) return;

        var target = damaged[Random.Range(0, damaged.Count)];
        target.GetComponent<Health>().RestoreFullHealth();
    }

    private void OnUpgradeButton()
    {
        if (selectedTower == null) return;

        var upgradeable = selectedTower.GetUpgradeable();
        if (upgradeable == null || !upgradeable.CanUpgrade()) return;

        if (!UpgradeManager.Instance.CanAfford(100)) return;

        UpgradeManager.Instance.SpendResources(100);
        upgradeable.ApplyUpgrade();
    }

    public void SelectTower(SelectableTower newSelection)
    {
        if (selectedTower != null) selectedTower.SetSelected(false);
        selectedTower = newSelection;
        selectedTower.SetSelected(true);
    }

    public void DeselectTower()
    {
        if (selectedTower != null)
        {
            selectedTower.SetSelected(false);
            selectedTower = null;
        }
    }
}