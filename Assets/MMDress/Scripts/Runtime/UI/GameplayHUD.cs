// Assets/MMDress/Scripts/Runtime/UI/HUD/GameplayHUD.cs
using UnityEngine;
using TMPro;
using MMDress.Core;
using MMDress.UI;                     // MoneyChanged, optional ScoreChanged
using MMDress.Runtime.Timer;         // TimeOfDayService
using RepService = MMDress.Runtime.Reputation.ReputationService;

namespace MMDress.Runtime.UI.HUD
{
    [DisallowMultipleComponent]
    [AddComponentMenu("MMDress/UI/Gameplay HUD (Clock, Money, Reputation)")]
    public sealed class GameplayHUD : MonoBehaviour
    {
        [Header("Services")]
        [SerializeField] private TimeOfDayService timeOfDay;
        [SerializeField] private RepService reputation;

        [Header("UI (TMP)")]
        [SerializeField] private TMP_Text clockText;
        [SerializeField] private TMP_Text moneyText;
        [SerializeField] private TMP_Text reputationText;   // contoh: "78% (Stage 2)"

        [Header("Options")]
        [SerializeField] private bool autoFind = true;
        [SerializeField] private bool autoFindTextsInChildren = false;
        [SerializeField] private string moneyPrefix = "Rp ";
        [SerializeField] private bool logScoreEvents = true;  // kalau ada ScoreChanged, hanya log empties

        // ───── Event delegates ─────
        System.Action<MoneyChanged> _onMoney;
        System.Action<ScoreChanged> _onScore;   // opsional: hanya logging

        void Awake()
        {
            if (autoFind)
            {
                if (!timeOfDay) timeOfDay = FindObjectOfType<TimeOfDayService>(true);
                if (!reputation) reputation = FindObjectOfType<RepService>(true);
            }

            if (autoFindTextsInChildren)
            {
                var tmps = GetComponentsInChildren<TMP_Text>(true);
                // Kalau kamu pakai naming, boleh deteksi by name:
                foreach (var t in tmps)
                {
                    var n = t.name.ToLowerInvariant();
                    if (!clockText && (n.Contains("clock") || n.Contains("time"))) clockText = t;
                    if (!moneyText && n.Contains("money")) moneyText = t;
                    if (!reputationText && (n.Contains("rep") || n.Contains("reputation"))) reputationText = t;
                }
            }

            // render awal biar HUD gak kosong
            RenderClock();
            RenderReputation();
            // biarkan money dirender saat Start oleh EconomyService (publish awal), atau set default 0:
            if (moneyText) moneyText.text = moneyPrefix + "0";
        }

        void OnEnable()
        {
            // Money
            _onMoney = e => { if (moneyText) moneyText.text = moneyPrefix + e.balance.ToString("N0"); };
            ServiceLocator.Events?.Subscribe(_onMoney);

            // Score (opsional logging saja)
            if (logScoreEvents)
            {
                _onScore = e =>
                {
                    if (e.empty > 0)
                        Debug.Log($"[HUD] ScoreChanged: empty={e.empty} (served={e.served}, totalScore={e.totalScore})");
                };
                ServiceLocator.Events?.Subscribe(_onScore);
            }

            // Reputation live update
            if (reputation != null)
            {
                reputation.ReputationChanged += _ => RenderReputation();
                reputation.ReputationStageChanged += (_, __, ___) => RenderReputation();
            }
        }

        void OnDisable()
        {
            if (_onMoney != null) ServiceLocator.Events?.Unsubscribe(_onMoney);
            if (_onScore != null) ServiceLocator.Events?.Unsubscribe(_onScore);

            if (reputation != null)
            {
                reputation.ReputationChanged -= _ => RenderReputation();
                reputation.ReputationStageChanged -= (_, __, ___) => RenderReputation();
            }
        }

        void Update()
        {
            // clock mulus per frame
            RenderClock();
        }

        // ───── Render helpers ─────
        void RenderClock()
        {
            if (clockText && timeOfDay)
                clockText.text = timeOfDay.GetClockText();
        }

        void RenderReputation()
        {
            if (!reputationText || !reputation) return;
            reputationText.text = $"{reputation.RepPercent:0}% (Stage {reputation.Stage})";
        }
    }
}
