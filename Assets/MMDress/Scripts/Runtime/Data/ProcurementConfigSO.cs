using UnityEngine;

namespace MMDress.Runtime.Inventory
{
    [CreateAssetMenu(fileName = "ProcurementConfig", menuName = "MMDress/Procurement Config")]
    public sealed class ProcurementConfigSO : ScriptableObject
    {
        [Header("Uang awal (satu kali, saat hari pertama dimulai)")]
        public int startingMoney = 10000;

        [Header("Durasi real fase Prep (detik)")]
        public float prepDurationSeconds = 120f; // 2 menit

        [Header("Harga material")]
        public int clothPrice = 50;
        public int threadPrice = 20;

        [Header("Jumlah tipe baju")]
        [Tooltip("Jumlah variasi tipe atasan (Top1..TopN)")]
        public int topTypes = 5;

        [Tooltip("Jumlah variasi tipe bawahan (Bottom1..BottomN)")]
        public int bottomTypes = 5;
    }
}
