// Assets/MMDress/Scripts/Runtime/UI/Character/CharacterOutfitController.cs
using UnityEngine;
using UnityEngine.UI;

namespace MMDress.UI
{
    [DisallowMultipleComponent]
    [AddComponentMenu("MMDress/UI/Character Outfit Controller (All-in)")]
    public sealed class CharacterOutfitController : MonoBehaviour
    {
        [Header("Refs (UI Image)")]
        [SerializeField] private Image topImage;
        [SerializeField] private Image bottomImage;

        public enum LayoutMode { ManualPureSwap, AutoLayout }
        [Header("Mode")]
        [SerializeField] private LayoutMode layoutMode = LayoutMode.ManualPureSwap;

        [Header("Common Options")]
        [SerializeField] private bool preserveAspect = true;
        [SerializeField] private bool disableRaycastOnImages = true;

        [Header("AutoLayout (aktif bila Mode = AutoLayout)")]
        [Range(0, 1)][SerializeField] private float topPosY01 = 0.62f;
        [Range(0, 1)][SerializeField] private float bottomPosY01 = 0.36f;
        [Range(0.05f, 1f)][SerializeField] private float topHeightFactor = 0.48f;
        [Range(0.05f, 1f)][SerializeField] private float bottomHeightFactor = 0.48f;
        [SerializeField] private bool useSafeArea = true;

        // cache baseline utk Manual mode (biar bisa reset kalau perlu)
        RectTransform _topRT, _botRT;
        Vector2 _topBasePos, _botBasePos;
        Vector2 _topBaseSize, _botBaseSize;
        Vector3 _topBaseScale, _botBaseScale;
        float _topBaseRotZ, _botBaseRotZ;

        Rect _lastSafeArea;
        RectTransform _root;

        void Awake()
        {
            _root = transform as RectTransform;
            _topRT = topImage ? topImage.rectTransform : null;
            _botRT = bottomImage ? bottomImage.rectTransform : null;

            // baseline (Manual mode pakai nilai editor—tidak diubah saat runtime)
            if (_topRT)
            {
                _topBasePos = _topRT.anchoredPosition;
                _topBaseSize = _topRT.sizeDelta;
                _topBaseScale = _topRT.localScale;
                _topBaseRotZ = _topRT.localEulerAngles.z;
            }
            if (_botRT)
            {
                _botBasePos = _botRT.anchoredPosition;
                _botBaseSize = _botRT.sizeDelta;
                _botBaseScale = _botRT.localScale;
                _botBaseRotZ = _botRT.localEulerAngles.z;
            }

            ConfigureImage(topImage);
            ConfigureImage(bottomImage);

            if (layoutMode == LayoutMode.AutoLayout)
                ApplyAutoLayout();
            else
                ApplyManualBaseline();
        }

        void OnEnable()
        {
            // pastikan layout settle saat pertama tampil
            ForceOneLayoutPass();
            if (layoutMode == LayoutMode.AutoLayout)
                ApplyAutoLayout();
        }

        void Update()
        {
            if (layoutMode != LayoutMode.AutoLayout) return;
            if (useSafeArea)
            {
                var sa = Screen.safeArea;
                if (sa != _lastSafeArea)
                {
                    _lastSafeArea = sa;
                    ApplyAutoLayout();
                }
            }
        }

        void OnRectTransformDimensionsChange()
        {
            if (!isActiveAndEnabled) return;
            if (layoutMode == LayoutMode.AutoLayout) ApplyAutoLayout();
        }

        void ConfigureImage(Image img)
        {
            if (!img) return;
            img.type = Image.Type.Simple;
            img.preserveAspect = preserveAspect;
            if (disableRaycastOnImages) img.raycastTarget = false;
        }

        void ApplyManualBaseline()
        {
            // kembalikan ke nilai editor; tidak disentuh lagi saat runtime
            if (_topRT)
            {
                _topRT.anchoredPosition = _topBasePos;
                _topRT.sizeDelta = _topBaseSize;
                _topRT.localScale = _topBaseScale;
                var e = _topRT.localEulerAngles; e.z = _topBaseRotZ; _topRT.localEulerAngles = e;
            }
            if (_botRT)
            {
                _botRT.anchoredPosition = _botBasePos;
                _botRT.sizeDelta = _botBaseSize;
                _botRT.localScale = _botBaseScale;
                var e = _botRT.localEulerAngles; e.z = _botBaseRotZ; _botRT.localEulerAngles = e;
            }
        }

        void ApplyAutoLayout()
        {
            if (_root == null) return;

            // area kerja: full rect atau safe area
            Rect rect = _root.rect;
            Vector2 size = rect.size;

            if (useSafeArea)
            {
                // konversi safeArea (px) ke local rect relatif _root (asumsi overlay canvas + scaler)
                var sa = Screen.safeArea;
                _lastSafeArea = sa;
                // normalize ke 0..1
                Vector2 screen = new(Screen.width, Screen.height);
                Vector2 min01 = sa.position / screen;
                Vector2 max01 = (sa.position + sa.size) / screen;
                // proyeksikan ke rect root
                Vector2 min = Vector2.Scale(min01, size) - size * 0.5f;
                Vector2 max = Vector2.Scale(max01, size) - size * 0.5f;
                size = max - min;
            }

            float H = Mathf.Max(1f, size.y);
            float W = Mathf.Max(1f, size.x);

            FitOne(_topRT, topPosY01, topHeightFactor, W, H);
            FitOne(_botRT, bottomPosY01, bottomHeightFactor, W, H);
            ForceOneLayoutPass();
        }

        static void FitOne(RectTransform rt, float posY01, float hFactor, float parentW, float parentH)
        {
            if (!rt) return;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            // pos Y relatif center
            rt.anchoredPosition = new Vector2(0f, Mathf.Lerp(-parentH * 0.5f, parentH * 0.5f, posY01));
            // tinggi absolut; lebar biar preservAspect yg ngatur
            float h = Mathf.Max(1f, hFactor * parentH);
            var cur = rt.sizeDelta;
            rt.sizeDelta = new Vector2(cur.x, h);
        }

        static void ForceOneLayoutPass()
        {
            Canvas.ForceUpdateCanvases();
        }

        // ===== API dipakai FittingRoomUI =====
        public void Clear()
        {
            SetImage(topImage, null, false);
            SetImage(bottomImage, null, false);
            ForceOneLayoutPass();
        }

        public void TryOn(MMDress.Data.ItemSO item, bool dim = false)
        {
            if (item == null) return;
            if (item.slot == MMDress.Data.OutfitSlot.Top)
                SetImage(topImage, item.sprite, dim);
            else
                SetImage(bottomImage, item.sprite, dim);

            ForceOneLayoutPass();
        }

        public void ApplyEquipped(MMDress.Data.ItemSO top, MMDress.Data.ItemSO bottom)
        {
            SetImage(topImage, top ? top.sprite : null, false);
            SetImage(bottomImage, bottom ? bottom.sprite : null, false);
            ForceOneLayoutPass();
        }

        void SetImage(Image img, Sprite s, bool dim)
        {
            if (!img) return;
            img.sprite = s;
            img.enabled = (s != null);
            ConfigureImage(img);
            // dim tanpa ubah alpha—biar konsisten
            float g = dim ? 0.6f : 1f;
            img.color = new Color(g, g, g, 1f);
        }

        // Exposed setters kalau mau ganti mode via Inspector/Debug
        public void SetLayoutMode(LayoutMode m)
        {
            layoutMode = m;
            if (m == LayoutMode.AutoLayout) ApplyAutoLayout();
            else ApplyManualBaseline();
        }
    }
}
