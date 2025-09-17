using UnityEngine;
using UnityEngine.UI;
using MMDress.Runtime.Reputation;

namespace MMDress.Runtime.UI.Popup
{
    public sealed class ReputationPopupView : MonoBehaviour
    {
        [SerializeField] private ReputationService reputation;
        [SerializeField] private Image badgeImage;
        [SerializeField] private Sprite stageUpSprite;
        [SerializeField] private Sprite stageDownSprite;
        [SerializeField] private float showSeconds = 1.5f;

        private float _timer;

        private void OnEnable()
        {
            if (reputation != null)
                reputation.ReputationStageChanged += OnStageChanged;
        }

        private void OnDisable()
        {
            if (reputation != null)
                reputation.ReputationStageChanged -= OnStageChanged;
        }

        private void Update()
        {
            if (_timer > 0f)
            {
                _timer -= Time.deltaTime;
                if (_timer <= 0f)
                {
                    gameObject.SetActive(false);
                }
            }
        }


        private void OnStageChanged(int prev, int next, int dir)
        {
            if (!badgeImage) return;
            badgeImage.sprite = dir >= 0 ? stageUpSprite : stageDownSprite;
            gameObject.SetActive(true);
            _timer = showSeconds;
        }
    }
}
