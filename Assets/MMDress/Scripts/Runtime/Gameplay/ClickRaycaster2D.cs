// Assets/MMDress/Scripts/Runtime/Gameplay/ClickRaycaster2D.cs
using UnityEngine;
using MMDress.Core;
public class ClickRaycaster2D : MonoBehaviour
{
    [SerializeField] Camera targetCamera; [SerializeField] LayerMask hitLayers = ~0;
    Camera Cam => targetCamera ? targetCamera : Camera.main;
    void Update()
    {
        if (!Input.GetMouseButtonDown(0)) return;
        var cam = Cam; if (!cam) return;
        var p = (Vector2)cam.ScreenToWorldPoint(Input.mousePosition);
        var hit = Physics2D.Raycast(p, Vector2.zero, 0f, hitLayers);
        var c = hit.collider ? hit.collider.GetComponentInParent<IClickable>() : null;
        if (c != null) c.OnClick();
    }
}
