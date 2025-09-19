using System;
using UnityEngine;
using MMDress.Runtime.Inventory;
using MMDress.Core;   // ServiceLocator.Events
using MMDress.UI;     // InventoryChanged event

namespace MMDress.Services
{
    [DisallowMultipleComponent]
    public sealed class StockService : MonoBehaviour
    {
        [Header("Config (drag)")]
        [SerializeField] private ProcurementConfigSO config;

        [Header("Persist (PlayerPrefs)")]
        [SerializeField] private bool usePlayerPrefs = true;
        private const string PrefKey = "MMDress.Stock.v1";

        // Materials
        public int Cloth { get; private set; }
        public int Thread { get; private set; }

        // Garments (Top1..N, Bottom1..M)
        private int[] _tops;    // length = config.topTypes
        private int[] _bottoms; // length = config.bottomTypes

        [Serializable]
        private class SaveData
        {
            public int cloth;
            public int thread;
            public int[] tops;
            public int[] bottoms;
        }

        private void Awake()
        {
            if (!config) Debug.LogWarning("[StockService] Config belum diassign.");
        }

        private void OnEnable()
        {
            int topN = Mathf.Max(1, config ? config.topTypes : 5);
            int botN = Mathf.Max(1, config ? config.bottomTypes : 5);

            _tops = new int[topN];
            _bottoms = new int[botN];

            if (usePlayerPrefs && PlayerPrefs.HasKey(PrefKey))
            {
                try
                {
                    var json = PlayerPrefs.GetString(PrefKey, "{}");
                    var data = JsonUtility.FromJson<SaveData>(json) ?? new SaveData();
                    Cloth = Mathf.Max(0, data.cloth);
                    Thread = Mathf.Max(0, data.thread);

                    if (data.tops != null)
                        Array.Copy(data.tops, _tops, Math.Min(_tops.Length, data.tops.Length));
                    if (data.bottoms != null)
                        Array.Copy(data.bottoms, _bottoms, Math.Min(_bottoms.Length, data.bottoms.Length));
                }
                catch { }
            }

            PublishInventoryChanged();
        }

        private void OnDisable()
        {
            Save();
        }

        private void Save()
        {
            if (!usePlayerPrefs) return;
            var data = new SaveData
            {
                cloth = Cloth,
                thread = Thread,
                tops = _tops,
                bottoms = _bottoms
            };
            var json = JsonUtility.ToJson(data);
            PlayerPrefs.SetString(PrefKey, json);
        }

        private void PublishInventoryChanged()
        {
            ServiceLocator.Events?.Publish(new InventoryChanged());
        }

        // ===== API Material =====
        public void AddMaterial(MaterialType t, int qty)
        {
            if (qty <= 0) return;
            if (t == MaterialType.Cloth) Cloth += qty;
            else Thread += qty;
            Save(); PublishInventoryChanged();
        }

        public bool TryConsumeMaterial(MaterialType t, int qty)
        {
            if (qty <= 0) return false;
            if (t == MaterialType.Cloth)
            {
                if (Cloth < qty) return false;
                Cloth -= qty;
            }
            else
            {
                if (Thread < qty) return false;
                Thread -= qty;
            }
            Save(); PublishInventoryChanged();
            return true;
        }

        // ===== API Garment =====
        public void AddGarment(GarmentSlot slot, int typeIndex, int qty)
        {
            if (qty <= 0) return;
            if (slot == GarmentSlot.Top)
            {
                if (typeIndex < 0 || typeIndex >= _tops.Length) return;
                _tops[typeIndex] += qty;
            }
            else
            {
                if (typeIndex < 0 || typeIndex >= _bottoms.Length) return;
                _bottoms[typeIndex] += qty;
            }
            Save(); PublishInventoryChanged();
        }

        public int GetGarmentCount(GarmentSlot slot, int typeIndex)
        {
            if (slot == GarmentSlot.Top)
            {
                if (typeIndex < 0 || typeIndex >= _tops.Length) return 0;
                return _tops[typeIndex];
            }
            else
            {
                if (typeIndex < 0 || typeIndex >= _bottoms.Length) return 0;
                return _bottoms[typeIndex];
            }
        }

        public int TopTypes => _tops?.Length ?? 0;
        public int BottomTypes => _bottoms?.Length ?? 0;
    }
}
