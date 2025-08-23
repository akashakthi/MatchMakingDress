using System;
using UnityEngine;
using UnityEngine.UI;
using MMDress.Data;

namespace MMDress.UI
{
    [DisallowMultipleComponent]
    public class ItemButtonView : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private Image icon;
        [SerializeField] private Text label; // kalau pakai TMP ganti ke TMP_Text

        private ItemSO _data;
        public ItemSO Data => _data;                 // <- biar grid bisa cek item mana yg terpilih

        public event Action<ItemSO> Clicked;         // event, tidak boleh di-assign null dari luar

        void Awake()
        {
            if (button)
                button.onClick.AddListener(() =>
                {
                    if (_data != null) Clicked?.Invoke(_data);
                });
        }

        public void Bind(ItemSO data)
        {
            _data = data;

            if (icon)
            {
                icon.sprite = data ? data.sprite : null;
                icon.enabled = icon.sprite != null;   // kalau null, hide biar keliatan jelas
                                                      // bikin aman kalau sprite besar/kecil
                if (icon.preserveAspect && icon.sprite) icon.SetNativeSize();
                // pastikan type simple (bukan Filled dari sisa setting lain)
                icon.type = Image.Type.Simple;
                icon.color = Color.white; // reset dari highlight
            }

            if (label) label.text = data ? data.displayName : "-";
        }


        public void SetSelected(bool on)
        {
            if (icon) icon.color = on ? Color.white : new Color(1f, 1f, 1f, 0.85f);
        }
    }
}
