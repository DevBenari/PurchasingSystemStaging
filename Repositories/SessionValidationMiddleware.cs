using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;

namespace PurchasingSystemStaging.Repositories
{
    public class SessionValidationMiddleware
    {
        //private readonly RequestDelegate _next;
        //private readonly ISessionService _sessionService;

        //public SessionValidationMiddleware(RequestDelegate next, ISessionService sessionService)
        //{
        //    _next = next;
        //    _sessionService = sessionService;
        //}

        //public async Task InvokeAsync(HttpContext context)
        //{
        //    if (context.User.Identity.IsAuthenticated)
        //    {
        //        var userId = context.Session.GetString("UserId");

        //        if (!string.IsNullOrEmpty(userId))
        //        {
        //            if (!_sessionService.IsSessionActive(userId))
        //            {
        //                context.Session.Clear();
        //                context.Response.Redirect("/Account/Login");
        //                return;
        //            }

        //            var roles = _sessionService.GetRoles(userId);
        //            if (roles == null || roles.Count == 0)
        //            {
        //                context.Session.Clear();
        //                context.Response.Redirect("/Account/Login");
        //                return;
        //            }
        //        }
        //    }

        //    await _next(context);
        //}
    }
}
