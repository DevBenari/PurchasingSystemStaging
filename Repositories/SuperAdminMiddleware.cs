using System.Security.Claims;

namespace PurchasingSystemStaging.Repositories
{
    public class SuperAdminMiddleware
    {
        private readonly RequestDelegate _next;

        public SuperAdminMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            if (context.User.Identity != null && context.User.Identity.IsAuthenticated)
            {
                // Periksa apakah pengguna adalah superadmin berdasarkan email
                var email = context.User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
                if (email == "superadmin@admin.com")
                {
                    // Tandai pengguna sebagai otorisasi penuh
                    context.User.AddIdentity(new ClaimsIdentity(new[]
                    {
                    new Claim(ClaimTypes.Role, "SuperAdmin") // Menambahkan peran SuperAdmin
                }));
                }
            }

            await _next(context);
        }
    }

}
