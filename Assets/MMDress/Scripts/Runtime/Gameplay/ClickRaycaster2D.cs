using UnityEngine;
using MMDress.Core;

/// <summary>
/// Raycast klik 2D sederhana. Pasang di Main Camera atau GameObject apa pun di scene.
/// Saat klik kiri, akan mencari komponen IClickable pada collider yang kena.
/// </summary>
public class ClickRaycaster2D : MonoBehaviour
{
    [SerializeField] private Camera targetCamera;   // optional; kalau kosong, pakai Camera.main
    [SerializeField] private LayerMask hitLayers = ~0; // default semua layer

    private Camera Cam => targetCamera != null ? targetCamera : Camera.main;

    void Update()
    {
        if (!Input.GetMouseButtonDown(0)) return;
        var cam = Cam;
        if (cam == null) return;

        Vector3 mouse = Input.mousePosition;
        Vector3 world = cam.ScreenToWorldPoint(mouse);
        Vector2 p = new Vector2(world.x, world.y);

        // Raycast 2D
        var hit = Physics2D.Raycast(p, Vector2.zero, 0f, hitLayers);
        if (!hit.collider) return;

        var clickable = hit.collider.GetComponent<IClickable>();
        if (clickable == null) clickable = hit.collider.GetComponentInParent<IClickable>();
        if (clickable != null) clickable.OnClick();
    }
}
