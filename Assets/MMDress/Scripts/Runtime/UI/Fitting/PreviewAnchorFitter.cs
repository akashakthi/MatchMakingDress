using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class PreviewAnchorFitter : MonoBehaviour
{
    [Header("Refs")]
    public RectTransform topImage;
    public RectTransform bottomImage;

    [Header("Normalized pos (0..1, di dalam PreviewRoot)")]
    [Range(0, 1)] public float topPosY01 = 0.62f; // 0 = bawah, 1 = atas
    [Range(0, 1)] public float bottomPosY01 = 0.36f;

    [Header("Relative size terhadap tinggi PreviewRoot")]
    [Range(0, 1)] public float topHeightFactor = 0.48f; // 48% dari tinggi area
    [Range(0, 1)] public float bottomHeightFactor = 0.48f;

    RectTransform _root;

    void Awake() { _root = (RectTransform)transform; Apply(); }
    void OnEnable() { Apply(); }
    void OnRectTransformDimensionsChange() { Apply(); } // penting: update saat resolusi/safe area berubah

    void Apply()
    {
        if (!_root) return;

        var h = _root.rect.height;
        var w = _root.rect.width;

        if (topImage)
        {
            var th = h * topHeightFactor;
            var tw = th * 0.66f; // rasio frame (ubah sesuai art)
            topImage.anchorMin = topImage.anchorMax = new Vector2(0.5f, 0.5f);
            topImage.pivot = new Vector2(0.5f, 0.5f);
            topImage.sizeDelta = new Vector2(tw, th);
            topImage.anchoredPosition = new Vector2(0f, Mathf.Lerp(-h * 0.5f, h * 0.5f, topPosY01));
        }

        if (bottomImage)
        {
            var bh = h * bottomHeightFactor;
            var bw = bh * 0.66f;
            bottomImage.anchorMin = bottomImage.anchorMax = new Vector2(0.5f, 0.5f);
            bottomImage.pivot = new Vector2(0.5f, 0.5f);
            bottomImage.sizeDelta = new Vector2(bw, bh);
            bottomImage.anchoredPosition = new Vector2(0f, Mathf.Lerp(-h * 0.5f, h * 0.5f, bottomPosY01));
        }
    }
}
