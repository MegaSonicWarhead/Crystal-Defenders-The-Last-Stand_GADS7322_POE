using CrystalDefenders.Combat;
using CrystalDefenders.Units;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using CrystalDefenders.Gameplay;

public class TowerUpgradeHandler : MonoBehaviour, IUpgradeable
{
    [Header("Upgrade Settings")]
    public int maxUpgradeTier = 3;
    public int upgradeCost = 150;
    public float healthMultiplierPerTier = 1.3f;
    public float damageMultiplierPerTier = 1.25f;

    [Header("Visual Feedback")]
    public Color[] upgradeColors = { Color.white, Color.cyan, Color.magenta, Color.yellow };
    public ParticleSystem upgradeEffect;

    [Header("UI")]
    public Button upgradeButton;
    public TMP_Text tierText;

    private int currentTier = 0;
    private Tower tower;
    private Health health;
    private AutoAttackBaseTower autoAttack;
    private Renderer rend;
    private MaterialPropertyBlock mpb;

    private void Awake()
    {
        tower = GetComponent<Tower>();
        health = GetComponent<Health>();
        autoAttack = GetComponent<AutoAttackBaseTower>();
        rend = GetComponentInChildren<Renderer>();

        mpb = new MaterialPropertyBlock();

        if (upgradeButton != null)
            upgradeButton.onClick.AddListener(TryUpgrade);

        UpdateVisuals();
    }

    private void Update()
    {
        if (upgradeButton == null) return;

        upgradeButton.interactable = currentTier < maxUpgradeTier &&
                                     ResourceManager.Instance.CanAfford(upgradeCost);

        var colors = upgradeButton.colors;
        colors.normalColor = upgradeButton.interactable ? Color.green : Color.grey;
        upgradeButton.colors = colors;

        if (tierText != null)
            tierText.text = $"Tower Tier: {currentTier}/{maxUpgradeTier}";
    }

    private void TryUpgrade()
    {
        if (currentTier >= maxUpgradeTier) return;
        if (!ResourceManager.Instance.Spend(upgradeCost)) return;

        currentTier++;
        ApplyUpgrade();
    }

    public void ApplyUpgrade()
    {
        int newMax = Mathf.RoundToInt(health.MaxHealth * healthMultiplierPerTier);
        health.SetMaxHealth(newMax, true);

        if (autoAttack != null)
            autoAttack.baseDamage = Mathf.RoundToInt(autoAttack.baseDamage * damageMultiplierPerTier);

        UpdateVisuals();
        PlayUpgradeEffect();
    }

    public bool CanUpgrade() => currentTier < maxUpgradeTier;

    private void UpdateVisuals()
    {
        if (rend == null) return;

        var block = new MaterialPropertyBlock();
        rend.GetPropertyBlock(block);

        Color newColor = upgradeColors[Mathf.Clamp(currentTier, 0, upgradeColors.Length - 1)];

        // Detect which property name is available
        if (rend.sharedMaterial.HasProperty("_BaseColor"))
        {
            block.SetColor("_BaseColor", newColor);
            Debug.Log($"[TowerUpgradeHandler] Applied color to _BaseColor: {newColor}");
        }
        else if (rend.sharedMaterial.HasProperty("_Color"))
        {
            block.SetColor("_Color", newColor);
            Debug.Log($"[TowerUpgradeHandler] Applied color to _Color: {newColor}");
        }
        else if (rend.sharedMaterial.HasProperty("_TintColor"))
        {
            block.SetColor("_TintColor", newColor);
            Debug.Log($"[TowerUpgradeHandler] Applied color to _TintColor: {newColor}");
        }
        else
        {
            Debug.LogWarning($"[TowerUpgradeHandler] No color property found on {rend.sharedMaterial.name}");
        }

        if (rend.sharedMaterial.HasProperty("_EmissionColor"))
            block.SetColor("_EmissionColor", newColor * 1.5f);

        rend.SetPropertyBlock(block);
    }

    private void PlayUpgradeEffect()
    {
        if (upgradeEffect != null)
        {
            var fx = Instantiate(upgradeEffect, transform.position + Vector3.up * 2f, Quaternion.identity);
            fx.Play();
            Destroy(fx.gameObject, 2f);
        }
    }
}