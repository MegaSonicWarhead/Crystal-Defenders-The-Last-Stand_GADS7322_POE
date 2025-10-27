using UnityEngine;

[DisallowMultipleComponent]
[ExecuteInEditMode]
public class LowHealthPostEffect : MonoBehaviour
{
    [Range(0f, 1f)] public float intensity = 0f;
    public Color tintColor = Color.red;

    private Material _material;
    private static readonly int IntensityID = Shader.PropertyToID("_Intensity");
    private static readonly int ColorID = Shader.PropertyToID("_Color");

    void OnEnable()
    {
        var shader = Shader.Find("Hidden/LowHealthVignette");
        if (shader != null)
            _material = new Material(shader);
    }

    void OnRenderImage(RenderTexture src, RenderTexture dest)
    {
        if (_material == null)
        {
            Graphics.Blit(src, dest);
            return;
        }

        _material.SetFloat(IntensityID, intensity);
        _material.SetColor(ColorID, tintColor);
        Graphics.Blit(src, dest, _material);
    }
}
