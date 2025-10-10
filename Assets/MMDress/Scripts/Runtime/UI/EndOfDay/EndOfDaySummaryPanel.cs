using UnityEngine;
using UnityEngine.UI;
using MMDress.Services;
using MMDress.Runtime.Inventory;
using MMDress.Core;
using MMDress.UI;
using MMDress.Runtime.Timer;

namespace MMDress.Runtime.UI.EndOfDay
{
    [DisallowMultipleComponent]
    public sealed class EndOfDaySummaryPanel : MonoBehaviour
    {
        [Header("Refs")]
        [SerializeField] private StockService stock;
        [SerializeField] private TimeOfDayService timeOfDay;

        [Header("UI Texts")]
        [SerializeField] private Text titleText;   // "Sisa Stok Hari Ini"
        [SerializeField] private Text topsText;
        [SerializeField] private Text bottomsText;

        [Header("Auto-find")]
        [SerializeField] private bool autoFind = true;

        System.Action<EndOfDayArrived> _onEod;

        [Header("Persist")]
        [SerializeField] private bool usePlayerPrefs = true;
        const string KEY = "MMDress.Stock.";

        [Header("Runtime")]
        [SerializeField] private int cloth;
        [SerializeField] private int thread;
        [SerializeField] private int[] tops;     // per type
        [SerializeField] private int[] bottoms;  // per type


        private void Awake()
        {
            if (autoFind)
            {
                stock ??= FindObjectOfType<StockService>(true);
                timeOfDay ??= FindObjectOfType<TimeOfDayService>(true);
            }
            gameObject.SetActive(false);
        }

        private void OnEnable()
        {
            _onEod = _ => ShowSummary();
            ServiceLocator.Events.Subscribe(_onEod);
        }

        private void OnDisable()
        {
            if (_onEod != null) ServiceLocator.Events.Unsubscribe(_onEod);
        }

        private void ShowSummary()
        {
            if (!stock) return;
            gameObject.SetActive(true);
            if (titleText) titleText.text = "Sisa Stok Hari Ini";

            var sbTop = new System.Text.StringBuilder();
            int tn = stock.TopTypes;
            for (int i = 0; i < tn; i++)
            {
                if (i > 0) sbTop.Append("\n");
                sbTop.Append($"Top{i + 1}: {stock.GetGarmentCount(GarmentSlot.Top, i)}");
            }
            if (topsText) topsText.text = sbTop.ToString();

            var sbBot = new System.Text.StringBuilder();
            int bn = stock.BottomTypes;
            for (int i = 0; i < bn; i++)
            {
                if (i > 0) sbBot.Append("\n");
                sbBot.Append($"Bottom{i + 1}: {stock.GetGarmentCount(GarmentSlot.Bottom, i)}");
            }
            if (bottomsText) bottomsText.text = sbBot.ToString();
        }
       
        // ✅ Tambahan ini untuk dipakai EndOfDaySummaryPanel
        public int TopTypes => tops != null ? tops.Length : 0;
        public int BottomTypes => bottoms != null ? bottoms.Length : 0;

        public void InitArrays(int topTypes, int bottomTypes)
        {
            if (tops == null || tops.Length != topTypes) tops = new int[topTypes];
            if (bottoms == null || bottoms.Length != bottomTypes) bottoms = new int[bottomTypes];
        }

        // Panggil dari tombol "OK"
        public void ClosePanel() => gameObject.SetActive(false);
    }
}
