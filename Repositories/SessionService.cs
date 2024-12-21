using Microsoft.Extensions.Caching.Memory;

namespace PurchasingSystemStaging.Repositories
{
    public class SessionService : ISessionService
    {
        private readonly IMemoryCache _cache;

        public SessionService(IMemoryCache cache)
        {
            _cache = cache;
        }

        public bool IsSessionActive(string userId)
        {
            // Cek apakah session ID ada di server-side storage
            return _cache.TryGetValue($"Session_{userId}", out _);
        }

        public void CreateSession(string userId, string sessionId, DateTime expiration)
        {
            // Simpan session di cache dengan masa berlaku
            _cache.Set($"Session_{userId}", sessionId, new MemoryCacheEntryOptions
            {
                AbsoluteExpiration = expiration
            });
        }

        public void DeleteSession(string userId)
        {
            // Menghapus session untuk userId
            _cache.Remove($"Session_{userId}");
        }

        public void InvalidateAllSessions(string userId)
        {
            // Sama seperti DeleteSession dalam konteks ini
            _cache.Remove($"Session_{userId}");
        }
    }
}
