namespace PurchasingSystemStaging.Repositories
{
    public interface ISessionService
    {
        bool IsSessionActive(string userId);
        void CreateSession(string userId, string sessionId, DateTime expiration);
        void DeleteSession(string userId);
        void InvalidateAllSessions(string userId);
    }
}
