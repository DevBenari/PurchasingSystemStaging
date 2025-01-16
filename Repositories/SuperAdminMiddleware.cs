using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

public class SuperAdminMiddleware
{
    private readonly RequestDelegate _next;

    public SuperAdminMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, IServiceProvider serviceProvider)
    {
        // Periksa apakah user sudah login
        if (context.User.Identity.IsAuthenticated)
        {
            // Ambil email user dari klaim
            var userEmail = context.User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;

            if (userEmail == "superadmin@admin.com")
            {
                // Buat scope baru untuk mengambil RoleManager
                using (var scope = serviceProvider.CreateScope())
                {
                    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

                    // Ambil semua role dari database
                    var allRoles = roleManager.Roles.Select(r => r.Name).ToList();

                    // Tambahkan semua role ke klaim user
                    var identity = context.User.Identity as ClaimsIdentity;
                    if (identity != null)
                    {
                        foreach (var role in allRoles)
                        {
                            if (!context.User.IsInRole(role))
                            {
                                identity.AddClaim(new Claim(ClaimTypes.Role, role));
                            }
                        }
                    }
                }
            }
        }

        // Lanjutkan ke middleware berikutnya
        await _next(context);
    }
}
