using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using PurchasingSystemStaging.Models;

namespace PurchasingSystemStaging.Repositories
{
    public class UpdateLastActivityMiddleware
    {
        private readonly RequestDelegate _next;

        public UpdateLastActivityMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            if (context.User?.Identity?.IsAuthenticated == true)
            {
                var userManager = context.RequestServices.GetRequiredService<UserManager<ApplicationUser>>();
                var user = await userManager.GetUserAsync(context.User);
                if (user != null)
                {
                    var utcTime = DateTimeOffset.UtcNow;
                    var localTime = utcTime.ToLocalTime();
                    user.LastActivityTime = localTime;
                    await userManager.UpdateAsync(user);
                }
            }

            await _next(context);
        }
    }
}
