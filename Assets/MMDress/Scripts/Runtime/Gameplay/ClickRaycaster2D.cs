using UnityEngine;
using MMDress.Core;
using MMDress.Gameplay;

public class ClickRaycaster2D : MonoBehaviour
{
    [SerializeField] private Camera targetCamera;
    [SerializeField] private LayerMask hitLayers = ~0;

    bool _locked;

    Camera Cam => targetCamera ? targetCamera : Camera.main;

    void OnEnable()
    {
        if (ServiceLocator.Events == null) ServiceLocator.Events = new SimpleEventBus();
        ServiceLocator.Events.Subscribe<FittingUIOpened>(_ => _locked = true);
        ServiceLocator.Events.Subscribe<FittingUIClosed>(_ => _locked = false);
    }
    void OnDisable()
    {
        if (ServiceLocator.Events == null) return;
        ServiceLocator.Events.Unsubscribe<FittingUIOpened>(_ => _locked = true);
        ServiceLocator.Events.Unsubscribe<FittingUIClosed>(_ => _locked = false);
    }

    void Update()
    {
        if (_locked) return;
        if (!Input.GetMouseButtonDown(0)) return;
        var cam = Cam; if (!cam) return;

        var p = (Vector2)cam.ScreenToWorldPoint(Input.mousePosition);
        var hit = Physics2D.Raycast(p, Vector2.zero, 0f, hitLayers);
        var c = hit.collider ? hit.collider.GetComponentInParent<IClickable>() : null;
        if (c != null) c.OnClick();
    }
}
