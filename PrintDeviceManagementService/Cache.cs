using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;

namespace PrintDeviceManagementService
{
    internal class Cache
    {
        readonly static TimeSpan CleanupInterval = new(0, 5, 0);

        readonly ConcurrentDictionary<int, CachedValue> cache = new();

        public Cache()
        {
            _ = CleanupCacheAsync();
        }

        async Task CleanupCacheAsync()
        {
            PeriodicTimer timer = new(CleanupInterval);
            while (await timer.WaitForNextTickAsync())
                CleanUp();
        }
        void CleanUp()
        {
            foreach (int id in cache.Keys)
                if (cache[id].Expired)
                    cache.TryRemove(id, out _);
        }

        public bool TryGet(int id, [MaybeNullWhen(false)] out Installation installation)
        {
            installation = null;
            if (cache.TryGetValue(id, out CachedValue? cached))
                installation = cached.Installation;
            return installation != null;
        }
        public void Add(Installation installation) => cache.TryAdd(installation.ID, new(installation));
        public void Remove(int id) => cache.TryRemove(id, out _);
    }
}
