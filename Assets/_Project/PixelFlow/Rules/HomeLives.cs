using System;

namespace PixelFlow.Rules
{
    [Serializable]
    public sealed class HomeLives
    {
        public int Version = 1, Hearts = 5;
        public long NextHeartUtc, UnlimitedUntilUtc;
        public HomeLives Copy() => new() { Version = Version, Hearts = Hearts, NextHeartUtc = NextHeartUtc, UnlimitedUntilUtc = UnlimitedUntilUtc };
    }

    // Local state with an explicit clock. Native remote tuning is still a
    // reference gap; callers supply the configured max/interval, never UI time.
    public static class HomeLifeRules
    {
        public static HomeLives Advance(HomeLives source, long now, int maximum, int interval)
        {
            if (source == null || source.Version != 1 || maximum < 1 || interval < 1 || now < 0 || source.Hearts < 0 || source.Hearts > maximum || source.NextHeartUtc < 0 || source.UnlimitedUntilUtc < 0)
                throw new ArgumentException("Invalid lives state or tuning.");
            var next = source.Copy();
            if (next.Hearts == maximum) { next.NextHeartUtc = 0; return next; }
            if (next.NextHeartUtc == 0) next.NextHeartUtc = checked(now + interval);
            if (now < next.NextHeartUtc) return next;
            long generated = 1 + (now - next.NextHeartUtc) / interval;
            next.Hearts = (int)Math.Min(maximum, next.Hearts + generated);
            next.NextHeartUtc = next.Hearts == maximum ? 0 : checked(next.NextHeartUtc + generated * interval);
            return next;
        }
        public static bool CanPlay(HomeLives state, long now) => state.Hearts > 0 || state.UnlimitedUntilUtc > now;
        public static int SecondsToNext(HomeLives state, long now, int interval) => state.NextHeartUtc <= 0 ? 0 : (int)Math.Clamp(state.NextHeartUtc - now, 0, interval);
        public static HomeLives Spend(HomeLives source, long now, int maximum, int interval)
        {
            var next = Advance(source, now, maximum, interval);
            if (next.UnlimitedUntilUtc > now) return next;
            if (next.Hearts == 0) return null;
            next.Hearts--; if (next.NextHeartUtc == 0) next.NextHeartUtc = checked(now + interval); return next;
        }
    }
}
