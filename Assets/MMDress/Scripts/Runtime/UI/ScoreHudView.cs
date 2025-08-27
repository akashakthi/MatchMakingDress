using UnityEngine;
using UnityEngine.UI;
using MMDress.Core;
using MMDress.UI;

namespace MMDress.UI
{
    /// Menampilkan served/empty/score saat ScoreService publish ScoreChanged.
    [DisallowMultipleComponent]
    [AddComponentMenu("MMDress/UI/Score HUD View")]
    public class ScoreHudView : MonoBehaviour
    {
        [SerializeField] private Text servedText;
        [SerializeField] private Text emptyText;
        [SerializeField] private Text scoreText;
        [SerializeField] private bool autoFindInChildren = true;

        System.Action<ScoreChanged> _onScore;

        void Awake()
        {
            if (autoFindInChildren)
            {
                servedText ??= transform.Find("ServedText")?.GetComponent<Text>();
                emptyText ??= transform.Find("EmptyText")?.GetComponent<Text>();
                scoreText ??= transform.Find("ScoreText")?.GetComponent<Text>();
            }
            Render(0, 0, 0);
        }

        void OnEnable()
        {
            _onScore = e => Render(e.served, e.empty, e.totalScore);
            ServiceLocator.Events?.Subscribe(_onScore);
        }

        void OnDisable()
        {
            if (_onScore != null) ServiceLocator.Events?.Unsubscribe(_onScore);
        }

        void Render(int served, int empty, int score)
        {
            if (servedText) servedText.text = $"Served: {served}";
            if (emptyText) emptyText.text = $"Empty: {empty}";
            if (scoreText) scoreText.text = $"Score: {score}";
        }
    }
}
