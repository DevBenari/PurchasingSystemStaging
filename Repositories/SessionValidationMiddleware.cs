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
            var userId = context.Session.GetString("UserId");

            if (!string.IsNullOrEmpty(userId) && !_sessionService.IsSessionActive(userId))
            {
                // Jika session tidak aktif, redirect ke login
                context.Session.Clear();
                context.Response.Redirect("/Account/Login");
                return;
            }

            await _next(context);
        }
    }
}
