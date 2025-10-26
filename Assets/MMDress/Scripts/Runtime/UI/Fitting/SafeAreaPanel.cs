using UnityEngine;

[RequireComponent(typeof(RectTransform))]
public class SafeAreaPanel : MonoBehaviour
{
    RectTransform _rt; Rect _last;

    void Awake() { _rt = GetComponent<RectTransform>(); Apply(); }
    void OnEnable() { Apply(); }
    void OnRectTransformDimensionsChange() { Apply(); }

    void Apply()
    {
        var sa = Screen.safeArea;
        if (sa == _last) return;
        _last = sa;

        var canvas = _rt.GetComponentInParent<Canvas>();
        if (!canvas) return;

        // konversi safeArea (pixel) ke anchor (0..1)
        var size = new Vector2(Screen.width, Screen.height);
        var min = sa.position / size;
        var max = (sa.position + sa.size) / size;

        _rt.anchorMin = min;
        _rt.anchorMax = max;
        _rt.offsetMin = Vector2.zero;
        _rt.offsetMax = Vector2.zero;
        _rt.pivot = new Vector2(0.5f, 0.5f);
        _rt.localScale = Vector3.one;
    }
}
