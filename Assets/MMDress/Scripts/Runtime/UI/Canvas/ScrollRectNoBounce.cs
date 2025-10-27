// Assets/MMDress/Scripts/Runtime/UI/Utils/ScrollRectNoBounce.cs
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(ScrollRect))]
public sealed class ScrollRectNoBounce : MonoBehaviour
{
    [SerializeField] bool clampMovement = true;
    [SerializeField] bool disableInertia = true;
    [SerializeField] RectTransform viewport;   // assign via Inspector (child Viewport)
    [SerializeField] bool lockAfterLayout = true;

    ScrollRect _sr;
    bool _lockedOnce;

    void Reset()
    {
        _sr = GetComponent<ScrollRect>();
        if (transform.childCount > 0 && !viewport)
            viewport = transform.GetComponentInChildren<RectMask2D>()?.transform as RectTransform;
    }

    void Awake()
    {
        _sr = GetComponent<ScrollRect>();
        Apply();
    }

    void OnEnable()
    {
        // tunggu 1 frame biar CanvasScaler/Layout settle, lalu kunci posisi
        if (lockAfterLayout) StartCoroutine(DelayLock());
    }

    System.Collections.IEnumerator DelayLock()
    {
        yield return null; // 1 frame
        LockPositionClamp();
        _lockedOnce = true;
    }

    void Apply()
    {
        if (clampMovement) _sr.movementType = ScrollRect.MovementType.Clamped;
        if (disableInertia) _sr.inertia = false;
        if (viewport) _sr.viewport = viewport;
    }

    void LateUpdate()
    {
        // kalau layout rebake di runtime, kunci lagi agar tidak “snap balik”
        if (_lockedOnce) LockPositionClamp();
    }

    void LockPositionClamp()
    {
        if (_sr == null || _sr.content == null) return;

        // clamp anchoredPosition agar tetap dalam batas konten
        var vp = _sr.viewport ? _sr.viewport.rect.size : (_sr.transform as RectTransform).rect.size;
        var ct = _sr.content.rect.size;

        Vector2 pos = _sr.content.anchoredPosition;
        if (_sr.horizontal)
        {
            float maxX = Mathf.Max(0, ct.x - vp.x);
            pos.x = Mathf.Clamp(pos.x, -maxX, 0f);
        }
        if (_sr.vertical)
        {
            float maxY = Mathf.Max(0, ct.y - vp.y);
            pos.y = Mathf.Clamp(pos.y, 0f, maxY); // top-anchored content (pivot.y = 1)
        }
        _sr.content.anchoredPosition = pos;
    }
}
