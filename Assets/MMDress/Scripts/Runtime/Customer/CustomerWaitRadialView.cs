using UnityEngine;
using UnityEngine.UI;

namespace MMDress.Customer
{
    /// UI murni: dengar event dari CustomerController dan atur Image (Filled/Radial).
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CustomerController))]
    public class CustomerWaitRadialView : MonoBehaviour
    {
        [Header("Assign di Inspector")]
        [Tooltip("Root panel (Canvas/GO) yang mau di-show/hide).")]
        [SerializeField] private GameObject panelRoot;

        [Tooltip("Image yang Type=Filled, FillMethod=Radial360 (foreground).")]
        [SerializeField] private Image fillImage;

        [Header("Opsional")]
        [SerializeField] private bool autoFindInChildren = true;
        [SerializeField] private bool tintByProgress = true; // hijau -> merah

        private CustomerController _ctrl;

        void Reset() { TryAutoFind(); }

        void Awake()
        {
            _ctrl = GetComponent<CustomerController>();
            if (autoFindInChildren && (!panelRoot || !fillImage)) TryAutoFind();

            if (fillImage != null)
            {
                // Pastikan setting Image benar untuk radial
                fillImage.type = Image.Type.Filled;
                fillImage.fillMethod = Image.FillMethod.Radial360;
                fillImage.fillOrigin = (int)Image.Origin360.Top;  // bebas: Top/Right/Bottom/Left
                fillImage.fillClockwise = false;
                fillImage.fillAmount = 1f;
            }
            if (panelRoot) panelRoot.SetActive(false);
        }

        void OnEnable()
        {
            _ctrl.OnWaitingStarted += OnWaitingStarted;
            _ctrl.OnWaitProgress += OnWaitProgress;
            _ctrl.OnFittingStarted += OnFittingStarted;
            _ctrl.OnLeavingStarted += OnLeavingStarted;
            _ctrl.OnTimedOut += OnTimedOutInternal;
        }

        void OnDisable()
        {
            _ctrl.OnWaitingStarted -= OnWaitingStarted;
            _ctrl.OnWaitProgress -= OnWaitProgress;
            _ctrl.OnFittingStarted -= OnFittingStarted;
            _ctrl.OnLeavingStarted -= OnLeavingStarted;
            _ctrl.OnTimedOut -= OnTimedOutInternal;
        }
        void OnFittingStarted(CustomerController c) => Hide();
        void OnLeavingStarted(CustomerController c) => Hide();
        void OnTimedOutInternal(CustomerController c) => Hide();
        void OnWaitingStarted(CustomerController c)
        {
            if (panelRoot) panelRoot.SetActive(true);
            OnWaitProgress(c, 1f);
        }

        void OnWaitProgress(CustomerController c, float frac)
        {
            if (!fillImage) return;
            frac = Mathf.Clamp01(frac);
            fillImage.fillAmount = frac;
            if (tintByProgress)
                fillImage.color = Color.Lerp(Color.red, Color.green, frac);
        }

        void Hide()
        {
            if (panelRoot) panelRoot.SetActive(false);
        }

        void TryAutoFind()
        {
            // Cari Canvas/Image di child (termasuk inactive)
            var images = GetComponentsInChildren<Image>(true);
            Image candidate = null;
            foreach (var img in images)
            {
                // Prioritaskan nama yang umum dipakai untuk foreground/fill
                string n = img.name.ToLowerInvariant();
                if (n.Contains("foreground") || n.Contains("fill"))
                {
                    candidate = img; break;
                }
            }
            if (!candidate && images.Length > 0) candidate = images[0];

            fillImage = candidate;
            if (candidate)
            {
                var canvas = candidate.GetComponentInParent<Canvas>(true);
                panelRoot = canvas ? canvas.gameObject : candidate.gameObject;
            }
        }
    }
}
