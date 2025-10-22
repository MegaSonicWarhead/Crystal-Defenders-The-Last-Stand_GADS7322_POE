using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using CrystalDefenders.Gameplay;

public class UpgradeButtonUI : MonoBehaviour
{
    [Header("Upgrade Settings")]
    public int upgradeCost = 100;
    public float buttonOffsetX = 50f; // Distance from health bar
    public Vector2 buttonSize = new Vector2(40, 40);

    private Button button;
    private Image buttonImage;
    private RectTransform buttonRect;
    private Transform targetHealthBar;
    private IUpgradeable target;

    /// <summary>
    /// Initialize the upgrade button next to a health bar
    /// </summary>
    public void Initialize(Transform healthBarTransform, IUpgradeable upgradeTarget)
    {
        targetHealthBar = healthBarTransform;
        target = upgradeTarget;

        // Create button GameObject
        GameObject go = new GameObject("UpgradeButton");
        go.transform.SetParent(targetHealthBar.parent, false);

        // Add UI components
        button = go.AddComponent<Button>();
        buttonImage = go.AddComponent<Image>();
        buttonImage.color = Color.green;

        // RectTransform
        buttonRect = go.GetComponent<RectTransform>();
        buttonRect.sizeDelta = buttonSize;

        // Initial position
        buttonRect.anchoredPosition = ((RectTransform)targetHealthBar).anchoredPosition + new Vector2(buttonOffsetX, 0);

        // Add click listener
        button.onClick.AddListener(OnClick);
    }

    private void Update()
    {
        if (button == null || target == null) return;

        // Enable/disable based on resources and upgrade availability
        bool canUpgrade = target.CanUpgrade() && UpgradeManager.Instance.CanAfford(upgradeCost);
        button.interactable = canUpgrade;
        buttonImage.color = canUpgrade ? Color.green : Color.grey;

        // Keep button following health bar
        if (targetHealthBar != null)
            buttonRect.anchoredPosition = ((RectTransform)targetHealthBar).anchoredPosition + new Vector2(buttonOffsetX, 0);
    }

    private void OnClick()
    {
        if (!UpgradeManager.Instance.CanAfford(upgradeCost) || !target.CanUpgrade()) return;

        UpgradeManager.Instance.SpendResources(upgradeCost);
        target.ApplyUpgrade();
    }
}

/// <summary>
/// Interface for anything that can be upgraded
/// </summary>
public interface IUpgradeable
{
    void ApplyUpgrade();
    bool CanUpgrade();
}