using CrystalDefenders.Combat;
using UnityEngine;

[DisallowMultipleComponent]
public class DefenderUpgradeHandler : MonoBehaviour, IUpgradeable
{
    [Header("Upgrade Settings")]
    public int maxUpgradeTier = 3;

    [Tooltip("Per-tier health multiplier (higher = tankier defenders)")]
    public float healthMultiplierPerTier = 1.35f;

    [Tooltip("Per-tier damage multiplier (higher = stronger attacks)")]
    public float damageMultiplierPerTier = 1.7f;

    [Header("Attack Scaling")]
    [Tooltip("Per-tier fire rate multiplier (1.15 = 15% faster each upgrade)")]
    public float attackSpeedMultiplierPerTier = 1.15f;

    [Tooltip("Per-tier attack range multiplier (1.05 = +5% range each upgrade)")]
    public float rangeMultiplierPerTier = 1.05f;

    [Header("Visual Feedback")]
    public Color[] upgradeColors = { Color.clear, Color.green, Color.blue, Color.yellow };
    public ParticleSystem upgradeEffect;

    [Header("Size Scaling")]
    [Tooltip("The visible part of the defender to scale when upgrading.")]
    public Transform scalablePart;
    [Tooltip("Increase in physical size per upgrade tier.")]
    public float scaleIncrement = 0.4f;
    private Vector3 baseScale;

    private int currentTier = 0;
    private Health health;
    private AutoAttack autoAttack;
    private Renderer rend;
    private Color originalColor;
    private MaterialPropertyBlock propBlock;

    private void Awake()
    {
        health = GetComponent<Health>();
        autoAttack = GetComponent<AutoAttack>();
        rend = GetComponentInChildren<Renderer>();
        propBlock = new MaterialPropertyBlock();

        if (scalablePart != null)
            baseScale = scalablePart.localScale;

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

        UpdateVisuals();
    }

    public void ApplyUpgrade()
    {
        if (!CanUpgrade()) return;

        currentTier++;
        if (currentTier > maxUpgradeTier)
        {
            currentTier = maxUpgradeTier;
            return;
        }

        //  Scale health and damage
        int newMaxHealth = Mathf.RoundToInt(health.MaxHealth * healthMultiplierPerTier);
        health.SetMaxHealth(newMaxHealth, true);

        if (autoAttack != null)
        {
            autoAttack.damagePerHit = Mathf.RoundToInt(autoAttack.damagePerHit * damageMultiplierPerTier);
            autoAttack.shotsPerSecond *= attackSpeedMultiplierPerTier;
            autoAttack.range *= rangeMultiplierPerTier;
        }

        //  Scale visible part size
        ScaleVisiblePart();

        //  Visual & effects
        UpdateVisuals();
        PlayUpgradeEffect();
    }

    public bool CanUpgrade() => currentTier < maxUpgradeTier;

    private void UpdateVisuals()
    {
        if (rend == null) return;

        Color tint = currentTier == 0
            ? Color.clear
            : upgradeColors[Mathf.Clamp(currentTier, 0, upgradeColors.Length - 1)];

        rend.GetPropertyBlock(propBlock);

        if (rend.sharedMaterial.HasProperty("_BaseColor"))
            propBlock.SetColor("_BaseColor", originalColor + tint * 0.4f);
        else if (rend.sharedMaterial.HasProperty("_Color"))
            propBlock.SetColor("_Color", originalColor + tint * 0.4f);

        if (rend.sharedMaterial.HasProperty("_EmissionColor"))
        {
            if (currentTier == 0)
                propBlock.SetColor("_EmissionColor", Color.black);
            else
                propBlock.SetColor("_EmissionColor", tint * (1.2f + 0.3f * currentTier));
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

    //  Handles physical size scaling per upgrade tier
    private void ScaleVisiblePart()
    {
        if (scalablePart == null) return;
        float scaleFactor = 1f + (currentTier * scaleIncrement);
        scalablePart.localScale = baseScale * scaleFactor;
    }
}