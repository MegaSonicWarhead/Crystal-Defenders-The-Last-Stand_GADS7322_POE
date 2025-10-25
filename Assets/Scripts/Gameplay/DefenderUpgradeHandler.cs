using CrystalDefenders.Combat;
using CrystalDefenders.Units;
using UnityEngine;
using System.Collections.Generic;

public class DefenderUpgradeHandler : MonoBehaviour, IUpgradeable
{
    [Header("Upgrade Settings")]
    public int maxUpgradeTier = 3;
    public float healthMultiplierPerTier = 1.25f;
    public float damageMultiplierPerTier = 1.2f;
    public float scaleIncrement = 0.5f;

    [Header("Visual Feedback")]
    public Color[] upgradeColors = { Color.clear, Color.green, Color.blue, Color.yellow }; // one per tier
    public ParticleSystem upgradeEffect;
    public Transform scalablePart; // assign the visible weapon part

    private int currentTier = 0;
    private Health health;
    private AutoAttack autoAttack;
    private Renderer[] renderers;
    private List<Color> originalColors = new List<Color>();
    private MaterialPropertyBlock propBlock;
    private Vector3 baseScale;

    private void Awake()
    {
        health = GetComponent<Health>();
        autoAttack = GetComponent<AutoAttack>();
        renderers = GetComponentsInChildren<Renderer>();
        propBlock = new MaterialPropertyBlock();

        // Store all original colors
        foreach (var r in renderers)
        {
            if (r.sharedMaterial.HasProperty("_BaseColor"))
                originalColors.Add(r.sharedMaterial.GetColor("_BaseColor"));
            else if (r.sharedMaterial.HasProperty("_Color"))
                originalColors.Add(r.sharedMaterial.GetColor("_Color"));
            else
                originalColors.Add(Color.white);
        }

        upgradeColors[0] = Color.clear; // no tint for tier 0

        if (scalablePart != null)
            baseScale = scalablePart.localScale;

        // Attach upgrade button
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

        // --- Stat Scaling ---
        int newMaxHealth = Mathf.RoundToInt(health.MaxHealth * healthMultiplierPerTier);
        health.SetMaxHealth(newMaxHealth, true);

        if (autoAttack != null)
            autoAttack.damagePerHit = Mathf.RoundToInt(autoAttack.damagePerHit * damageMultiplierPerTier);

        // --- Visuals ---
        ScaleVisiblePart();
        UpdateVisuals();
        PlayUpgradeEffect();
    }

    public bool CanUpgrade() => currentTier < maxUpgradeTier;

    private void ScaleVisiblePart()
    {
        if (scalablePart == null) return;
        float scaleFactor = 1f + (currentTier * scaleIncrement);
        scalablePart.localScale = baseScale * scaleFactor;
    }

    private void UpdateVisuals()
    {
        if (renderers == null || renderers.Length == 0) return;

        Color tint = currentTier == 0 ? Color.clear : upgradeColors[Mathf.Clamp(currentTier, 0, upgradeColors.Length - 1)];

        for (int i = 0; i < renderers.Length; i++)
        {
            var r = renderers[i];
            if (r == null) continue;

            r.GetPropertyBlock(propBlock);

            Color newColor = originalColors[i] + tint * 0.4f;

            if (r.sharedMaterial.HasProperty("_BaseColor"))
                propBlock.SetColor("_BaseColor", newColor);
            else if (r.sharedMaterial.HasProperty("_Color"))
                propBlock.SetColor("_Color", newColor);

            if (r.sharedMaterial.HasProperty("_EmissionColor"))
            {
                if (currentTier == 0)
                    propBlock.SetColor("_EmissionColor", Color.black);
                else
                    propBlock.SetColor("_EmissionColor", tint * 1.2f);
            }

            r.SetPropertyBlock(propBlock);
        }
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