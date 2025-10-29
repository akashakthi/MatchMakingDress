// Assets/MMDress/Scripts/Runtime/Services/Persistence/SaveUtil.cs
using UnityEngine;

namespace MMDress.Runtime.Services.Persistence
{
    public static class SaveUtil
    {
        public static void SetInt(string key, int val) { PlayerPrefs.SetInt(key, val); }
        public static int GetInt(string key, int def = 0) => PlayerPrefs.GetInt(key, def);

        public static void SetString(string key, string val) { PlayerPrefs.SetString(key, val); }
        public static string GetString(string key, string def = "") => PlayerPrefs.GetString(key, def);

        public static void Save() => PlayerPrefs.Save();
        public static bool HasKey(string key) => PlayerPrefs.HasKey(key);
        public static void DeleteKey(string key) => PlayerPrefs.DeleteKey(key);
    }
}
