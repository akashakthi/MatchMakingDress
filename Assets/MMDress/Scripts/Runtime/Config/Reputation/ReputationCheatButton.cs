using UnityEngine;
using UnityEngine.UI;
using MMDress.Core;
using MMDress.Gameplay;   // CustomerCheckout

namespace MMDress.Runtime.UI.Cheats    // <<< GANTI: bukan lagi .Debug
{
    /// <summary>
    /// Tombol cheat sederhana untuk MENAMBAH reputasi beberapa persen sekaligus.
    /// Cara kerja:
    /// - Sistem reputasi naik +1% setiap ada CustomerCheckout "served" (items >= 2, benar).
    /// - Script ini mem-publish event CustomerCheckout beberapa kali saat tombol diklik.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Button))]
    public sealed class ReputationCheatButton : MonoBehaviour
    {
        [Header("Cheat Config")]
        [Tooltip("Berapa % reputasi yang mau ditambah per klik (pakai step 1% per event).")]
        [SerializeField, Min(1)]
        private int addPercent = 10;

        [Tooltip("Log di Console tiap kali cheat dipakai (buat ngecek).")]
        [SerializeField]
        private bool verbose = true;

        private Button _button;

        private void Awake()
        {
            _button = GetComponent<Button>();
            if (_button != null)
            {
                _button.onClick.RemoveAllListeners();
                _button.onClick.AddListener(ApplyCheat);
            }
        }

        private void OnDestroy()
        {
            if (_button != null)
                _button.onClick.RemoveListener(ApplyCheat);
        }

        /// <summary>
        /// Dipanggil saat tombol di-klik.
        /// </summary>
        private void ApplyCheat()
        {
            if (addPercent <= 0) return;

            var bus = ServiceLocator.Events;
            if (bus == null)
            {
                UnityEngine.Debug.LogWarning("[ReputationCheatButton] ServiceLocator.Events null, tidak bisa publish event.");
                return;
            }

            // 1 event CustomerCheckout "served benar" ≈ +1% reputasi (sesuai logic ReputationOnCheckout).
            for (int i = 0; i < addPercent; i++)
            {
                var evt = new CustomerCheckout(
                    customer: null,      // aman selama ReputationOnCheckout tidak pakai field customer-nya
                    itemsEquipped: 2,    // dianggap full served
                    isCorrectOrder: true // order benar
                );
                bus.Publish(evt);
            }

            if (verbose)
                UnityEngine.Debug.Log($"[ReputationCheatButton] Cheat reputasi +{addPercent}% dikirim lewat CustomerCheckout event.");
        }

        /// <summary>
        /// Biar bisa di-trigger dari Button OnClick (Inspector) juga kalau mau manual.
        /// </summary>
        public void ApplyCheatPublic()
        {
            ApplyCheat();
        }
    }
}
