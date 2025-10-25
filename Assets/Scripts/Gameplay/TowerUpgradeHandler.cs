using CrystalDefenders.Combat;
using CrystalDefenders.Units;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using CrystalDefenders.Gameplay;
using System.Collections.Generic;

public class TowerUpgradeHandler : MonoBehaviour, IUpgradeable
{
    [Header("Upgrade Settings")]
    public int maxUpgradeTier = 3;
    public float healthMultiplierPerTier = 1.25f;
    public float damageMultiplierPerTier = 1.2f;

    [Header("Visual Feedback")]
    public Color[] upgradeColors = { Color.clear, Color.green, Color.blue, Color.yellow };
    public ParticleSystem upgradeEffect;

    [Header("Scaling (Crystal only)")]
    [Tooltip("Assign the crystal mesh or child here.")]
    public Transform crystalPart;
    [Tooltip("How much to scale per upgrade step.")]
    public float scaleIncreasePerTier = 0.5f;

    private int currentTier = 0;
    private Health health;
    private AutoAttack autoAttack;
    private Renderer rend;
    private Color originalColor;
    private MaterialPropertyBlock propBlock;
    private Vector3 originalCrystalScale;

    private void Awake()
    {
        health = GetComponent<Health>();
        autoAttack = GetComponent<AutoAttack>();
        rend = GetComponentInChildren<Renderer>();
        propBlock = new MaterialPropertyBlock();

        // Store original crystal scale
        if (crystalPart != null)
            originalCrystalScale = crystalPart.localScale;

        // Capture the base color
        if (rend != null)
        {
            if (rend.sharedMaterial.HasProperty("_BaseColor"))
                originalColor = rend.sharedMaterial.GetColor("_BaseColor");
            else if (rend.sharedMaterial.HasProperty("_Color"))
                originalColor = rend.sharedMaterial.GetColor("_Color");
            else
                originalColor = Color.white;

            upgradeColors[0] = Color.clear;
        }

        // Attach the health bar upgrade button if available
        if (UIManager.Instance != null)
        {
            UIManager.Instance.AttachHealthBar(health);
            Transform hb = health.transform.Find("HealthBar");
            if (hb != null)
            {
                var upgradeButton = hb.gameObject.AddComponent<UpgradeButtonUI>();
                upgradeButton.Initialize(hb, this);
            }
        }

        UpdateVisuals();
    }

    public void ApplyUpgrade()
    {
        if (!CanUpgrade()) return;

        currentTier++;

        // Cap at max tier
        if (currentTier > maxUpgradeTier)
        {
            currentTier = maxUpgradeTier;
            return;
        }

        // Scale stats
        int newMaxHealth = Mathf.RoundToInt(health.MaxHealth * healthMultiplierPerTier);
        health.SetMaxHealth(newMaxHealth, true);

        if (autoAttack != null)
            autoAttack.damagePerHit = Mathf.RoundToInt(autoAttack.damagePerHit * damageMultiplierPerTier);

        // Apply visuals
        UpdateVisuals();
        PlayUpgradeEffect();

        // Scale the crystal only
        if (crystalPart != null)
        {
            float scaleFactor = 1f + (scaleIncreasePerTier * currentTier);
            crystalPart.localScale = originalCrystalScale * scaleFactor;
        }
    }

    public bool CanUpgrade() => currentTier < maxUpgradeTier;

    private void UpdateVisuals()
    {
        if (rend == null) return;

        Color tint = currentTier == 0 ? Color.clear : upgradeColors[Mathf.Clamp(currentTier, 0, upgradeColors.Length - 1)];

        rend.GetPropertyBlock(propBlock);

        // Tint overlay (adds subtle highlight rather than overwriting)
        if (rend.sharedMaterial.HasProperty("_BaseColor"))
            propBlock.SetColor("_BaseColor", originalColor + tint * 0.4f);
        else if (rend.sharedMaterial.HasProperty("_Color"))
            propBlock.SetColor("_Color", originalColor + tint * 0.4f);

        if (rend.sharedMaterial.HasProperty("_EmissionColor"))
        {
            if (currentTier == 0)
                propBlock.SetColor("_EmissionColor", Color.black);
            else
                propBlock.SetColor("_EmissionColor", tint * 1.2f);
        }

        rend.SetPropertyBlock(propBlock);
    }

    private void PlayUpgradeEffect()
    {
        if (upgradeEffect != null)
        {
            var fx = Instantiate(upgradeEffect, transform.position, Quaternion.identity);
            fx.Play();
            Destroy(fx.gameObject, 2f);
        }
    }
}