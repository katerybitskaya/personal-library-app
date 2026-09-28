using System.Collections.Concurrent;

namespace PersonalLibrary.Security
{
    public class LoginAttemptTracker
    {
        private sealed class Entry
        {
            public int Failures;
            public DateTime WindowStart;
            public DateTime? LockedUntil;
        }

        private const string GlobalKey = "*";
        private readonly ConcurrentDictionary<string, Entry> _entries = new();

        public TimeSpan? GetLockout(string ip)
        {
            var now = DateTime.UtcNow;
            var left = Remaining(ip, now);
            var global = Remaining(GlobalKey, now);
            if (left == null) return global;
            if (global == null) return left;
            return left > global ? left : global;
        }

        public void RegisterFailure(string ip, int maxPerIp, int maxGlobal, int lockoutMinutes)
        {
            var now = DateTime.UtcNow;
            var window = TimeSpan.FromMinutes(Math.Max(1, lockoutMinutes));
            Register(ip, Math.Max(1, maxPerIp), window, now);
            Register(GlobalKey, Math.Max(1, maxGlobal), window, now);
        }

        public void RegisterSuccess(string ip) => _entries.TryRemove(ip, out _);

        private void Register(string key, int max, TimeSpan window, DateTime now)
        {
            var entry = _entries.GetOrAdd(key, _ => new Entry { WindowStart = now });
            lock (entry)
            {
                if (entry.LockedUntil is { } until && until > now) return;
                if (entry.LockedUntil != null || now - entry.WindowStart > window)
                {
                    entry.Failures = 0;
                    entry.WindowStart = now;
                    entry.LockedUntil = null;
                }
                entry.Failures++;
                if (entry.Failures >= max)
                    entry.LockedUntil = now + window;
            }
        }

        private TimeSpan? Remaining(string key, DateTime now)
        {
            if (!_entries.TryGetValue(key, out var entry)) return null;
            lock (entry)
            {
                if (entry.LockedUntil is { } until && until > now) return until - now;
                return null;
            }
        }
    }
}
