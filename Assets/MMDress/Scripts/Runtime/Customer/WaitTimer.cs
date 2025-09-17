namespace MMDress.Customer
{
    /// <summary>Timer ringan (non-MonoBehaviour) dengan external speed factor.</summary>
    public sealed class WaitTimer
    {
        public float Duration { get; private set; }
        public float Remaining { get; private set; }
        public bool IsDone => Remaining <= 0f;

        private float _externalSpeedFactor = 1f; // 1.0 = normal

        public WaitTimer(float durationSec) => Reset(durationSec);

        public void Reset(float durationSec)
        {
            Duration = durationSec > 0 ? durationSec : 0.0001f;
            Remaining = Duration;
        }

        public void Tick(float deltaTime)
        {
            if (IsDone) return;

            float eff = deltaTime * (_externalSpeedFactor <= 0f ? 0.0001f : _externalSpeedFactor);
            Remaining -= eff;
            if (Remaining < 0f) Remaining = 0f;
        }

        public float Fraction => Duration <= 0f ? 0f : Remaining / Duration; // 1 → 0

        public void SetExternalSpeedFactor(float factor)
        {
            _externalSpeedFactor = factor > 0f ? factor : 0.0001f;
        }
    }
}
