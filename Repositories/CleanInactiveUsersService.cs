using Microsoft.AspNetCore.Identity;
using PurchasingSystem.Models;

namespace PurchasingSystem.Repositories
{
    public class CleanInactiveUsersService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;

        public CleanInactiveUsersService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await CleanInactiveUsersAsync();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error cleaning inactive users: {ex.Message}");
                }

                // Jalankan setiap 10 menit
                await Task.Delay(TimeSpan.FromMinutes(10), stoppingToken);
            }
        }

        private async Task CleanInactiveUsersAsync()
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

                var inactiveThreshold = DateTime.UtcNow.AddMinutes(-10);
                var inactiveUsers = userManager.Users
                    .Where(u => u.IsOnline && u.LastActivityTime < inactiveThreshold)
                    .ToList();

                foreach (var user in inactiveUsers)
                {
                    user.IsOnline = false;
                    user.LastActivityTime = DateTime.Now;

                    await userManager.UpdateAsync(user);
                }

                Console.WriteLine("Inactive users cleaned successfully.");
            }
        }
    }

}
