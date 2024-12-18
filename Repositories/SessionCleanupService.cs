using Microsoft.AspNetCore.Identity;
using PurchasingSystemStaging.Models;

namespace PurchasingSystemStaging.Repositories
{
    public class SessionCleanupService : BackgroundService
    {
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly TimeSpan _idleTimeout = TimeSpan.FromMinutes(1);
        private readonly TimeSpan _checkInterval = TimeSpan.FromSeconds(5);

        public SessionCleanupService(IServiceScopeFactory serviceScopeFactory)
        {
            _serviceScopeFactory = serviceScopeFactory;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                using (var scope = _serviceScopeFactory.CreateScope())
                {
                    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
                    var now = DateTime.UtcNow;

                    // Ambil semua user yang IsOnline = true, dan cek apakah LastActivityTime + idleTimeout < now
                    // Asumsikan Anda punya banyak user, lebih baik batasi query atau gunakan index
                    var users = userManager.Users.Where(u => u.IsOnline == true && u.LastActivityTime.HasValue);

                    foreach (var user in users)
                    {
                        if (now - user.LastActivityTime.Value > _idleTimeout)
                        {
                            user.IsOnline = false;
                            await userManager.UpdateAsync(user);
                        }
                    }
                }

                // Tunggu interval sebelum cek lagi
                await Task.Delay(_checkInterval, stoppingToken);
            }
        }
    }
}
