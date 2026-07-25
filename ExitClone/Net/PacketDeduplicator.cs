using System;
using System.Collections.Generic;

namespace ExitClone.Net
{
    /// <summary>
    /// When the same packet is sent down several routes the game must only ever see one copy.
    /// Duplicates are recognised by a rolling hash of the payload inside a short time window.
    /// </summary>
    public class PacketDeduplicator
    {
        private readonly Dictionary<ulong, DateTime> _seen = new Dictionary<ulong, DateTime>();
        private readonly object _sync = new object();
        private readonly TimeSpan _window;
        private DateTime _lastSweep = DateTime.UtcNow;

        public PacketDeduplicator(int windowMs = 750)
        {
            _window = TimeSpan.FromMilliseconds(windowMs);
        }

        public long Dropped { get; private set; }

        public bool IsDuplicate(byte[] payload, int offset, int count)
        {
            var hash = Fnv1A(payload, offset, count);
            var now = DateTime.UtcNow;
            lock (_sync)
            {
                if (now - _lastSweep > _window)
                {
                    Sweep(now);
                    _lastSweep = now;
                }
                DateTime when;
                if (_seen.TryGetValue(hash, out when) && now - when < _window)
                {
                    Dropped++;
                    return true;
                }
                _seen[hash] = now;
                return false;
            }
        }

        private void Sweep(DateTime now)
        {
            var stale = new List<ulong>();
            foreach (var kv in _seen)
                if (now - kv.Value > _window) stale.Add(kv.Key);
            foreach (var key in stale) _seen.Remove(key);
        }

        private static ulong Fnv1A(byte[] data, int offset, int count)
        {
            const ulong prime = 1099511628211UL;
            ulong hash = 14695981039346656037UL;
            for (int i = offset; i < offset + count; i++)
            {
                hash ^= data[i];
                hash *= prime;
            }
            // Length is mixed in so that truncated copies are not treated as the same packet.
            hash ^= (ulong)count;
            return hash * prime;
        }
    }
}
