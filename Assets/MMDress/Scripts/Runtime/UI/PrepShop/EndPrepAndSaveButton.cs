using UnityEngine;
using UnityEngine.UI;
using MMDress.Runtime.Services.Persistence;
using MMDress.Runtime.Timer;

namespace MMDress.Runtime.UI.PrepShop
{
    /// Tombol untuk mengunci hasil fase Preparation:
    /// - paksa simpan PlayerPrefs (uang, material, item)
    /// - set flag "prep locked"
    /// - skip waktu ke fase Open (08:00)
    [DisallowMultipleComponent]
    public sealed class EndPrepAndSaveButton : MonoBehaviour
    {
        [Header("Refs")]
        [SerializeField] private Button button;
        [SerializeField] private PrepPersistenceService persistence;
        [SerializeField] private TimeOfDayService timeOfDay;

        [Header("Keys / Behaviour")]
        [SerializeField] private string lockKey = "MMDress.Prep.locked";
        [SerializeField] private bool skipToGameplay = true;

        void Reset()
        {
#if UNITY_2023_1_OR_NEWER
            button ??= GetComponent<Button>();
            persistence ??= UnityEngine.Object.FindFirstObjectByType<PrepPersistenceService>(FindObjectsInactive.Include);
            timeOfDay ??= UnityEngine.Object.FindFirstObjectByType<TimeOfDayService>(FindObjectsInactive.Include);
#else
            button      ??= GetComponent<Button>();
            persistence ??= FindObjectOfType<PrepPersistenceService>(true);
            timeOfDay   ??= FindObjectOfType<TimeOfDayService>(true);
#endif
        }

        void Awake()
        {
            if (button) button.onClick.AddListener(OnClick);
        }

        void OnClick()
        {
            // 1) Simpan semua snapshot (uang, material, garments)
            if (persistence) persistence.ForceSaveNow();

            // 2) Set flag prep terkunci (opsional jika dipakai logic lain)
            PlayerPrefs.SetInt(lockKey, 1);
            PlayerPrefs.Save();

            // 3) Skip ke fase Open (08:00) kalau diinginkan
            if (skipToGameplay && timeOfDay)
                TimeOfDayJumper.SkipToOpen(timeOfDay);

            Debug.Log("[Prep] Locked & saved. Skipped to Open.");
        }
    }
}
