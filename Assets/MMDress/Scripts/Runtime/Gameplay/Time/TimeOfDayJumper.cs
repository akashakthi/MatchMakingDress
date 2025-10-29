// Assets/MMDress/Scripts/Runtime/Timer/TimeOfDayJumper.cs
using System.Reflection;
using MMDress.Runtime.Timer;

namespace MMDress.Runtime.Timer
{
    public static class TimeOfDayJumper
    {
        /// Lompat ke fase OPEN (08:00) dengan cara paling aman yang tersedia.
        public static void SkipToOpen(TimeOfDayService svc)
        {
            if (svc == null) return;

            // 1) Kalau ada API resmi, pakai itu.
            var tp = svc.GetType();
            var jumpOpen = tp.GetMethod("JumpToOpen", BindingFlags.Instance | BindingFlags.Public);
            if (jumpOpen != null) { jumpOpen.Invoke(svc, null); return; }

            var jumpPhase = tp.GetMethod("JumpToPhase", BindingFlags.Instance | BindingFlags.Public);
            if (jumpPhase != null) { jumpPhase.Invoke(svc, new object[] { DayPhase.Open }); return; }

            // 2) Fallback: refleksi ke field privat (aman sementara).
            var fIdx = tp.GetField("_idx", BindingFlags.Instance | BindingFlags.NonPublic);
            var fTimer = tp.GetField("_timer", BindingFlags.Instance | BindingFlags.NonPublic);
            var fCur = tp.GetProperty("CurrentPhase", BindingFlags.Instance | BindingFlags.Public);
            var evChanged = tp.GetEvent("DayPhaseChanged", BindingFlags.Instance | BindingFlags.Public);

            fIdx?.SetValue(svc, 2);      // Open = 2
            fTimer?.SetValue(svc, 0f);

            // set CurrentPhase kalau properti ada setter privat
            var setCur = fCur?.SetMethod;
            if (setCur != null) setCur.Invoke(svc, new object[] { DayPhase.Open });

            // invoke event jika memungkinkan
            var fiDelegate = tp.GetField("DayPhaseChanged", BindingFlags.Instance | BindingFlags.NonPublic);
            var dlg = fiDelegate?.GetValue(svc) as System.Delegate;
            dlg?.DynamicInvoke(DayPhase.Open);
        }
    }
}
