using System.Collections.Generic;
using UnityEngine;
using MMDress.Data;

namespace MMDress.Services
{
    public interface IInventoryService
    {
        bool Has(string itemId);
        void Grant(string itemId); // preload/dev tool
    }

    public interface IWalletService
    {
        int Balance { get; }
        bool Spend(int amount);
        void Add(int amount);
    }

    public interface ISaveService
    {
        void Save<T>(string key, T data);
        T Load<T>(string key, T @default = default);
    }

    public class DevInventoryService : IInventoryService
    {
        private readonly HashSet<string> _owned = new();

        public DevInventoryService(InventorySO preload)
        {
            if (preload != null)
                foreach (var id in preload.ownedItemIds) _owned.Add(id);
        }

        public bool Has(string itemId) => _owned.Contains(itemId);
        public void Grant(string itemId) => _owned.Add(itemId);
    }

    public class DevWalletService : IWalletService
    {
        public int Balance { get; private set; } = 999_999;
        public bool Spend(int amount)
        {
            if (amount <= 0) return true;
            if (Balance < amount) return false;
            Balance -= amount; return true;
        }
        public void Add(int amount) { if (amount > 0) Balance += amount; }
    }

    public class PlayerPrefsSaveService : ISaveService
    {
        public void Save<T>(string key, T data)
        {
            PlayerPrefs.SetString(key, JsonUtility.ToJson(data));
            PlayerPrefs.Save();
        }

        public T Load<T>(string key, T @default = default)
        {
            if (!PlayerPrefs.HasKey(key)) return @default;
            var json = PlayerPrefs.GetString(key);
            return JsonUtility.FromJson<T>(json);
        }
    }
}
