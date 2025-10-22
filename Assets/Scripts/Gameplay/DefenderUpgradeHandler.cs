using CrystalDefenders.Combat;
using CrystalDefenders.Units;
using UnityEngine;

public class DefenderUpgradeHandler : MonoBehaviour, IUpgradeable
{
    [Header("Upgrade Settings")]
    public int maxUpgradeTier = 3;
    public float healthMultiplierPerTier = 1.25f;
    public float damageMultiplierPerTier = 1.2f;

    [Header("Visual Feedback")]
    public Color[] upgradeColors = { Color.clear, Color.green, Color.blue, Color.yellow }; // one per tier
    public ParticleSystem upgradeEffect;

    private int currentTier = 0;
    private Health health;
    private AutoAttack autoAttack;
    private Renderer rend;
    private Color originalColor;

    private void Awake()
    {
        health = GetComponent<Health>();
        autoAttack = GetComponent<AutoAttack>();
        rend = GetComponentInChildren<Renderer>();

        if (rend != null)
        {
            // Store original mesh color
            if (rend.sharedMaterial.HasProperty("_BaseColor"))
                originalColor = rend.sharedMaterial.GetColor("_BaseColor");
            else if (rend.sharedMaterial.HasProperty("_Color"))
                originalColor = rend.sharedMaterial.GetColor("_Color");
            else
                originalColor = Color.white;

            // Set tier 0 to default
            upgradeColors[0] = originalColor;
        }

        // UI button setup
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

        // Scale stats
        int newMaxHealth = Mathf.RoundToInt(health.MaxHealth * healthMultiplierPerTier);
        health.SetMaxHealth(newMaxHealth, true);

        if (autoAttack != null)
            autoAttack.damagePerHit = Mathf.RoundToInt(autoAttack.damagePerHit * damageMultiplierPerTier);

        // Visual feedback
        UpdateVisuals();
        PlayUpgradeEffect();
    }

    public bool CanUpgrade() => currentTier < maxUpgradeTier;

    private void UpdateVisuals()
    {
        if (rend == null)
            return;

        Color newColor = currentTier == 0 ? originalColor : upgradeColors[Mathf.Clamp(currentTier, 0, upgradeColors.Length - 1)];

        // Apply color directly to the existing material
        if (rend.material.HasProperty("_BaseColor"))
            rend.material.SetColor("_BaseColor", newColor);
        else if (rend.material.HasProperty("_Color"))
            rend.material.SetColor("_Color", newColor);

        // Glow/emission for upgrades
        if (rend.material.HasProperty("_EmissionColor"))
        {
            if (currentTier == 0)
                rend.material.SetColor("_EmissionColor", Color.black);
            else
            {
                rend.material.EnableKeyword("_EMISSION");
                rend.material.SetColor("_EmissionColor", newColor * 1.5f);
            }
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