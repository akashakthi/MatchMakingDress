namespace MMDress.Customer
{
    public sealed class WaitTimer
    {
        public float Duration { get; private set; }
        public float Remaining { get; private set; }
        public bool IsDone => Remaining <= 0f;

        public WaitTimer(float durationSec)
        {
            Reset(durationSec);
        }

        public void Reset(float durationSec)
        {
            Duration = durationSec > 0 ? durationSec : 0.0001f;
            Remaining = Duration;
        }

        public void Tick(float deltaTime)
        {
            if (IsDone) return;
            Remaining -= deltaTime;
            if (Remaining < 0f) Remaining = 0f;
        }

        public float Fraction => Duration <= 0f ? 0f : Remaining / Duration; // 1 → 0
    }
}
