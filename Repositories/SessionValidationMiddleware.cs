using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;

namespace PurchasingSystemStaging.Repositories
{
    public class SessionValidationMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ISessionService _sessionService;

        public SessionValidationMiddleware(RequestDelegate next, ISessionService sessionService)
        {
            _next = next;
            _sessionService = sessionService;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            if (context.User.Identity.IsAuthenticated)
            {
                // Ambil UserId dari klaim
                var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (!string.IsNullOrEmpty(userId) && !_sessionService.IsSessionActive(userId))
                {
                    // Hapus cookie dan session
                    await context.SignOutAsync("CookieAuth");
                    context.Session.Clear();
                    context.Response.Redirect("/Account/Login");
                    return;
                }
            }

            await _next(context);
        }
    }
}
