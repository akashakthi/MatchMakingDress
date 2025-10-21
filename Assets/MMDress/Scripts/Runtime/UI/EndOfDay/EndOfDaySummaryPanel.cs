// Assets/MMDress/Scripts/Runtime/UI/EndOfDay/EndOfDaySummaryPanel.cs
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using MMDress.Services;
using MMDress.Core;
using MMDress.UI;
using MMDress.Runtime.Timer;
using MMDress.Data;

namespace MMDress.Runtime.UI.EndOfDay
{
    [DisallowMultipleComponent]
    public sealed class EndOfDaySummaryPanel : MonoBehaviour
    {
        [Header("Refs")]
        [SerializeField] private StockService stock;
        [SerializeField] private TimeOfDayService timeOfDay;
        [SerializeField] private CatalogSO catalog; // optional; kalau kosong, ambil dari stock.Catalog

        [Header("UI Texts")]
        [SerializeField] private Text titleText;   // "Sisa Stok Hari Ini"
        [SerializeField] private Text topsText;
        [SerializeField] private Text bottomsText;

        [Header("Auto-find")]
        [SerializeField] private bool autoFind = true;

        System.Action<EndOfDayArrived> _onEod;

        void Awake()
        {
            if (autoFind)
            {
#if UNITY_2023_1_OR_NEWER
                stock ??= Object.FindAnyObjectByType<StockService>(FindObjectsInactive.Include);
                timeOfDay ??= Object.FindAnyObjectByType<TimeOfDayService>(FindObjectsInactive.Include);
#else
                stock    ??= FindObjectOfType<StockService>(true);
                timeOfDay??= FindObjectOfType<TimeOfDayService>(true);
#endif
            }
            // panel hanya tampil saat EoD
            gameObject.SetActive(false);
        }

        void OnEnable()
        {
            _onEod = _ => ShowSummary();
            ServiceLocator.Events.Subscribe(_onEod);
        }

        void OnDisable()
        {
            if (_onEod != null) ServiceLocator.Events.Unsubscribe(_onEod);
        }

        void ShowSummary()
        {
            if (!stock) return;

            // pastikan katalog ada (boleh ambil dari StockService)
            var cat = catalog ? catalog : stock.Catalog;
            gameObject.SetActive(true);

            if (titleText) titleText.text = "Sisa Stok Hari Ini";

            // Render Top & Bottom dengan membaca CatalogSO + stok per ItemSO
            var sbTop = new StringBuilder();
            var sbBot = new StringBuilder();

            if (cat && cat.Items != null)
            {
                foreach (var item in cat.Items)
                {
                    if (!item) continue;
                    int count = stock.GetGarment(item); // SO-only
                    if (item.slot == OutfitSlot.Top)
                    {
                        if (sbTop.Length > 0) sbTop.Append('\n');
                        sbTop.Append($"{(string.IsNullOrEmpty(item.displayName) ? item.name : item.displayName)}: {count}");
                    }
                    else // Bottom
                    {
                        if (sbBot.Length > 0) sbBot.Append('\n');
                        sbBot.Append($"{(string.IsNullOrEmpty(item.displayName) ? item.name : item.displayName)}: {count}");
                    }
                }
            }
            else
            {
                // fallback jika tidak ada katalog (biar tidak null/empty)
                sbTop.Append("No catalog / items");
                sbBot.Append("No catalog / items");
            }

            if (topsText) topsText.text = sbTop.ToString();
            if (bottomsText) bottomsText.text = sbBot.ToString();
        }

        // Panggil dari tombol "OK"
        public void ClosePanel() => gameObject.SetActive(false);
    }
}
