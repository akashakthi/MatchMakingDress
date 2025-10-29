using System;
using System.Collections.Generic;
using UnityEngine;
using MMDress.Services;
using MMDress.Data;

namespace MMDress.Runtime.Services.Persistence
{
    [DefaultExecutionOrder(1000)] // pastikan jalan SETELAH StockService (100)
    [DisallowMultipleComponent]
    public sealed class PrepPersistenceService : MonoBehaviour
    {
        [Header("Refs")]
        [SerializeField] private EconomyService economy;
        [SerializeField] private ProcurementService procurement;
        [SerializeField] private StockService stock;
        [SerializeField] private CatalogSO catalog;

        [Header("Material Keys (SO)")]
        [SerializeField] private MaterialSO clothSO;
        [SerializeField] private MaterialSO threadSO;

        [Header("PlayerPrefs Keys")]
        [SerializeField] private string keyPrefix = "MMDress.Prep.";
        [SerializeField] private string moneyKey = "money";
        [SerializeField] private string clothKey = "mat.cloth";
        [SerializeField] private string threadKey = "mat.thread";
        [SerializeField] private string garmentsKey = "garments"; // JSON

        [Header("Polling fallback (optional)")]
        [SerializeField] private bool enablePollingSave = false;
        [SerializeField, Min(0.1f)] private float pollInterval = 0.5f;

        float _t;

        // ==== lifecycle ====
        void Start()
        {
            // Apply saat boot supaya UI/stock langsung sinkron
            ForceApplyNow();
            Debug.Log("[Persist] Bootstrap ready.");
        }

        void Update()
        {
            if (!enablePollingSave) return;
            _t += Time.deltaTime;
            if (_t >= pollInterval)
            {
                _t = 0f;
                ForceSaveNow(); // auto-save ringan, aman untuk debug
            }
        }

        // ==== API publik → dipanggil dari tombol Lock Prep juga ====
        public void ForceSaveNow()
        {
            SaveMoney();
            SaveMaterials();
            SaveGarments();
            PlayerPrefs.Save();
        }

        public void ForceApplyNow()
        {
            ApplyMoney();
            ApplyMaterials();
            ApplyGarments();
        }

        // ==== MONEY ====
        void SaveMoney()
        {
            if (!economy) return;
            PlayerPrefs.SetInt(P(keyPrefix, moneyKey), economy.Balance);
        }

        void ApplyMoney()
        {
            if (!economy) return;
            if (PlayerPrefs.HasKey(P(keyPrefix, moneyKey)))
            {
                int bal = PlayerPrefs.GetInt(P(keyPrefix, moneyKey), economy.Balance);
                // EconomyService nggak punya setter langsung → trik: hitung delta
                int delta = bal - economy.Balance;
                if (delta != 0) economy.Add(delta);
            }
        }

        // ==== MATERIALS ====
        void SaveMaterials()
        {
            if (!procurement) return;
            if (clothSO) PlayerPrefs.SetInt(P(keyPrefix, clothKey), procurement.GetMaterial(clothSO));
            if (threadSO) PlayerPrefs.SetInt(P(keyPrefix, threadKey), procurement.GetMaterial(threadSO));
        }

        void ApplyMaterials()
        {
            if (!procurement) return;
            if (clothSO && PlayerPrefs.HasKey(P(keyPrefix, clothKey)))
                procurement.SetMaterial(clothSO, Mathf.Max(0, PlayerPrefs.GetInt(P(keyPrefix, clothKey), 0)));
            if (threadSO && PlayerPrefs.HasKey(P(keyPrefix, threadKey)))
                procurement.SetMaterial(threadSO, Mathf.Max(0, PlayerPrefs.GetInt(P(keyPrefix, threadKey), 0)));
        }

        // ==== GARMENTS (ItemSO) ====
        [Serializable] struct GarmentEntry { public string id; public int count; }
        [Serializable] struct GarmentList { public List<GarmentEntry> items; }

        void SaveGarments()
        {
            if (!stock || !catalog) return;

            var list = new GarmentList { items = new List<GarmentEntry>(32) };
            foreach (var it in catalog.Items)
            {
                if (!it || string.IsNullOrEmpty(it.id)) continue;
                int c = stock.GetGarment(it);
                if (c > 0) list.items.Add(new GarmentEntry { id = it.id, count = c });
            }

            string json = JsonUtility.ToJson(list, false);
            PlayerPrefs.SetString(P(keyPrefix, garmentsKey), json);
#if UNITY_EDITOR
            Debug.Log($"[Persist] Saved garments: {list.items.Count} entries");
#endif
        }

        void ApplyGarments()
        {
            if (!stock || !catalog) return;

            if (!PlayerPrefs.HasKey(P(keyPrefix, garmentsKey)))
            {
#if UNITY_EDITOR
                Debug.Log("[Persist] Apply garments: (no key) → skip");
#endif
                return;
            }

            string json = PlayerPrefs.GetString(P(keyPrefix, garmentsKey), "{}");
            var list = new GarmentList { items = new List<GarmentEntry>() };
            try { JsonUtility.FromJsonOverwrite(string.IsNullOrEmpty(json) ? "{}" : json, list); }
            catch { /* ignore json error */ }

            // nolkan dulu (agar id yang tak tersimpan ikut 0)
            foreach (var it in catalog.Items)
                if (it) stock.SetGarment(it, 0);

            int applied = 0;
            foreach (var e in list.items)
            {
                if (string.IsNullOrEmpty(e.id) || e.count <= 0) continue;
                var item = FindItemById(e.id);
                if (!item)
                {
#if UNITY_EDITOR
                    Debug.LogWarning($"[Persist] Item id '{e.id}' tidak ditemukan di CatalogSO.");
#endif
                    continue;
                }
                stock.SetGarment(item, Mathf.Max(0, e.count));
                applied++;
            }

#if UNITY_EDITOR
            Debug.Log($"[Persist] Apply garments: {applied} entries");
#endif
        }

        ItemSO FindItemById(string id)
        {
            foreach (var it in catalog.Items)
                if (it && it.id == id) return it;
            return null;
        }

        // ==== util ====
        static string P(string pref, string key) => string.IsNullOrEmpty(pref) ? key : (pref + key);
    }
}
