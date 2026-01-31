// Assets/CafeMerge/Scripts/Runtime/UI/VidPlayer.cs
using UnityEngine;
using UnityEngine.Video;

namespace CafeMerge.UI
{
    /// <summary>
    /// Pemutar video tunggal berbasis URL.
    /// - Menggunakan 1 URL spesifik.
    /// - Otomatis prepare & play saat GameObject aktif (jika playOnEnable true).
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(VideoPlayer))]
    public sealed class VidPlayer : MonoBehaviour
    {
        [Header("Video Settings")]
        [Tooltip("URL Video yang akan diputar.")]
        [SerializeField]
        private string videoUrl = "https://raw.githubusercontent.com/akashakthi/AssetVideo/main/IklanKompres.mp4";

        [Header("Behaviour")]
        [Tooltip("Kalau true, akan langsung memutar video saat GameObject di-enable.")]
        [SerializeField] private bool playOnEnable = true;

        private VideoPlayer _videoPlayer;

        private void Awake()
        {
            _videoPlayer = GetComponent<VideoPlayer>();

            // Setup default VideoPlayer
            _videoPlayer.source = VideoSource.Url;
            _videoPlayer.playOnAwake = false;
            _videoPlayer.waitForFirstFrame = true;

            // Subscribe event saat video selesai loading (prepared)
            _videoPlayer.prepareCompleted += OnVideoPrepared;
        }

        private void OnEnable()
        {
            if (playOnEnable)
            {
                PlayVideo();
            }
        }

        private void OnDisable()
        {
            if (_videoPlayer != null)
            {
                _videoPlayer.Stop();
            }
        }

        private void OnDestroy()
        {
            if (_videoPlayer != null)
            {
                _videoPlayer.prepareCompleted -= OnVideoPrepared;
            }
        }

        /// <summary>
        /// Mulai memuat dan memutar video dari URL yang diset.
        /// </summary>
        public void PlayVideo()
        {
            if (_videoPlayer == null) return;

            if (string.IsNullOrWhiteSpace(videoUrl))
            {
                Debug.LogWarning($"{nameof(VidPlayer)}: URL video kosong.", this);
                return;
            }

            _videoPlayer.url = videoUrl;
            _videoPlayer.Prepare();
        }

        private void OnVideoPrepared(VideoPlayer source)
        {
            // Dipanggil otomatis setelah Prepare() selesai
            _videoPlayer.Play();
        }
    }
}