using UnityEngine;
using UnityEngine.Rendering;

[DisallowMultipleComponent]
public class SelectableTower : MonoBehaviour
{
    [Header("Highlight Settings")]
    public Color highlightColor = Color.yellow;
    public float outlineWidth = 4f;

    private Color originalColor;
    private Renderer towerRenderer;
    private MaterialPropertyBlock propertyBlock;
    private bool isSelected = false;

    private void Awake()
    {
        towerRenderer = GetComponentInChildren<Renderer>();
        if (towerRenderer != null)
        {
            propertyBlock = new MaterialPropertyBlock();
            towerRenderer.GetPropertyBlock(propertyBlock);
            originalColor = towerRenderer.material.color;
        }
    }

    private void OnMouseDown()
    {
        // Deselect any previously selected tower
        WeaponShop.Instance.SelectTower(this);
    }

    public void SetSelected(bool selected)
    {
        isSelected = selected;

        if (towerRenderer == null) return;

        towerRenderer.GetPropertyBlock(propertyBlock);
        propertyBlock.SetColor("_Color", selected ? highlightColor : originalColor);
        towerRenderer.SetPropertyBlock(propertyBlock);
    }

    public IUpgradeable GetUpgradeable()
    {
        return GetComponent<IUpgradeable>();
    }
}