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
            // Periksa apakah session user aktif di cache
            return _cache.TryGetValue($"UserSession_{userId}", out _);
        }

        public void CreateSession(string userId, string sessionId, DateTime expiration)
        {
            // Simpan session di cache dengan masa berlaku
            _cache.Set($"UserSession_{userId}", sessionId, new MemoryCacheEntryOptions
            {
                AbsoluteExpiration = expiration
            });
        }

        public void DeleteSession(string userId)
        {
            // Hapus session user dari cache
            _cache.Remove($"UserSession_{userId}");
        }

        public void InvalidateAllSessions(string userId)
        {
            // Invalidate semua session dengan userId (opsional untuk multi-session management)
            if (int.TryParse(userId, out var userIdInt))
            {
                // Hapus session dari cache
                _cache.Remove($"UserSession_{userIdInt}");
            }
        }
    }
}
