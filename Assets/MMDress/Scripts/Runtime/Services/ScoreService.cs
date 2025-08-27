using UnityEngine;
using MMDress.Core;
using MMDress.UI;

namespace MMDress.Services
{
    /// Service sederhana untuk menghitung skor berbasis event checkout.
    /// Formula contoh: +10 per item yang ter-equip.
    [DisallowMultipleComponent]
    [AddComponentMenu("MMDress/Services/Score Service")]
    public class ScoreService : MonoBehaviour
    {
        [SerializeField] private int pointsPerItem = 10;

        int _served;      // checkout dengan item>0
        int _empty;       // checkout item==0
        int _score;

        System.Action<CustomerCheckout> _onCheckout;

        void OnEnable()
        {
            _onCheckout = e => {
                if (e.itemsEquipped > 0) _served++; else _empty++;
                _score += e.itemsEquipped * pointsPerItem;
                ServiceLocator.Events?.Publish(new ScoreChanged(_served, _empty, _score));
            };
            ServiceLocator.Events?.Subscribe(_onCheckout);
        }

        void OnDisable()
        {
            if (_onCheckout != null) ServiceLocator.Events?.Unsubscribe(_onCheckout);
        }

        public void ResetScore()
        {
            _served = _empty = _score = 0;
            ServiceLocator.Events?.Publish(new ScoreChanged(_served, _empty, _score));
        }
    }
}
