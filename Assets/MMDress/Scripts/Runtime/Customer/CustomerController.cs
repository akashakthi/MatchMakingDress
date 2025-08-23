// Assets/MMDress/Scripts/Runtime/Customer/CustomerController.cs
using System;
using UnityEngine;
using MMDress.Core;
using MMDress.Gameplay;

namespace MMDress.Customer
{
    [RequireComponent(typeof(Collider2D))]
    public class CustomerController : MonoBehaviour, IClickable
    {
        [Header("Movement")]
        [SerializeField] float moveSpeed = 2f;
        [SerializeField] float arriveThreshold = 0.05f;

        [Header("Waiting")]
        [SerializeField] float waitDurationSec = 15f; // default; bisa di-override saat Init
        [SerializeField] Vector3 barOffset = new Vector3(0f, 1.1f, 0f);

        public Character.CharacterOutfitController Outfit { get; private set; }

        Vector3 _seatPos, _exitPos;
        State _state = State.Idle;
        Collider2D _col;

        // seat management
        int _seatIndex = -1;
        Action<int> _onSeatFreed;

        // timer
        float _remaining;
        Transform _barRoot;
        SpriteRenderer _barFill;
        static Sprite _whitePx;

        enum State { Idle, Entering, Waiting, Fitting, Leaving }

        void Awake()
        {
            Outfit = GetComponentInChildren<Character.CharacterOutfitController>();
            _col = GetComponent<Collider2D>();
            EnsureBar();
        }

        void EnsureBar()
        {
            if (_barRoot) return;

            _barRoot = new GameObject("WaitBar").transform;
            _barRoot.SetParent(transform, false);
            _barRoot.localPosition = barOffset;

            var bg = new GameObject("BG").AddComponent<SpriteRenderer>();
            bg.transform.SetParent(_barRoot, false);
            bg.sprite = WhitePx(); bg.color = new Color(0, 0, 0, 0.6f);
            bg.transform.localScale = new Vector3(1.2f, 0.12f, 1);

            _barFill = new GameObject("Fill").AddComponent<SpriteRenderer>();
            _barFill.transform.SetParent(_barRoot, false);
            _barFill.sprite = WhitePx();
            _barFill.color = Color.green;
            _barFill.transform.localPosition = new Vector3(-0.6f, 0, 0); // pivot kiri
            _barFill.transform.localScale = new Vector3(1.2f, 0.1f, 1);

            _barRoot.gameObject.SetActive(false);
        }

        static Sprite WhitePx()
        {
            if (_whitePx) return _whitePx;
            var tex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            tex.SetPixel(0, 0, Color.white); tex.Apply();
            _whitePx = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 100);
            return _whitePx;
        }

        public void Init(Vector3 seatPosition, Vector3 exitPosition, int seatIndex, Action<int> onSeatFreed, float waitSec)
        {
            _seatPos = seatPosition; _exitPos = exitPosition;
            _seatIndex = seatIndex; _onSeatFreed = onSeatFreed;
            waitDurationSec = waitSec > 0 ? waitSec : waitDurationSec;
            _remaining = waitDurationSec;
            _state = State.Entering;
            if (_col) _col.enabled = true;
        }

        void Update()
        {
            switch (_state)
            {
                case State.Entering:
                    if (MoveTo(_seatPos))
                    {
                        _state = State.Waiting;
                        _barRoot.gameObject.SetActive(true);
                        UpdateBar(1f);
                    }
                    break;

                case State.Waiting:
                    _remaining -= Time.deltaTime;
                    float t = Mathf.Clamp01(_remaining / waitDurationSec);
                    UpdateBar(t);
                    if (_remaining <= 0f)
                    {
                        // timeout → kursi bebas & pergi tanpa skor
                        FreeSeat();
                        BeginLeaving();
                        ServiceLocator.Events?.Publish(new CustomerTimedOut(this));
                    }
                    break;

                case State.Leaving:
                    if (MoveTo(_exitPos)) Destroy(gameObject);
                    break;
            }
        }

        bool MoveTo(Vector3 target)
        {
            transform.position = Vector3.MoveTowards(transform.position, target, moveSpeed * Time.deltaTime);
            return Vector3.SqrMagnitude(transform.position - target) <= arriveThreshold * arriveThreshold;
        }

        void UpdateBar(float frac)
        {
            if (!_barFill) return;
            frac = Mathf.Clamp01(frac);
            // skala fill dari kiri (pivot manual)
            _barFill.transform.localScale = new Vector3(1.2f * frac, 0.1f, 1f);
            _barFill.transform.localPosition = new Vector3(-0.6f + 0.6f * frac, 0, 0);
            // warna hijau → merah
            _barFill.color = Color.Lerp(Color.red, Color.green, frac);
        }

        public void OnClick()
        {
            if (_state != State.Waiting) return; // hanya saat menunggu
            Debug.Log("[MMDress] Customer clicked.");
            _state = State.Fitting;
            _barRoot.gameObject.SetActive(false); // pause bar
            ServiceLocator.Events.Publish(new CustomerSelected(this));
        }

        public void FinishFitting() // dipanggil oleh UI saat Close
        {
            if (_state != State.Fitting) return;
            // beri skor langsung, dan tinggalkan seat
            ServiceLocator.Score?.Add(1);
            ServiceLocator.Events?.Publish(new CustomerServed(this, 1));
            FreeSeat();
            BeginLeaving();
        }

        void FreeSeat()
        {
            if (_seatIndex >= 0)
            {
                _onSeatFreed?.Invoke(_seatIndex);
                _seatIndex = -1;
            }
        }

        void BeginLeaving()
        {
            _state = State.Leaving;
            if (_col) _col.enabled = false;
            if (_barRoot) _barRoot.gameObject.SetActive(false);
        }
    }
}
