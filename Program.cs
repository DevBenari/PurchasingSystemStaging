using FastReport.Data;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using PurchasingSystem.Areas.Administrator.Models;
using PurchasingSystem.Areas.Administrator.Repositories;
using PurchasingSystem.Areas.MasterData.Repositories;
using PurchasingSystem.Areas.Order.Repositories;
using PurchasingSystem.Areas.Report.Repositories;
using PurchasingSystem.Areas.Transaction.Repositories;
using PurchasingSystem.Areas.Warehouse.Repositories;
using PurchasingSystem.Data;
using PurchasingSystem.Hubs;
using PurchasingSystem.Models;
using PurchasingSystem.Repositories;
using System.Globalization;
using System.Reflection;
using System.Security.Claims;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

var cultureInfo = new CultureInfo("id-ID");
CultureInfo.DefaultThreadCurrentCulture = cultureInfo;
CultureInfo.DefaultThreadCurrentUICulture = cultureInfo;

builder.Services.AddSingleton<UrlMappingService>();

// Add services to the container.
builder.Services.AddControllersWithViews(options =>
{
    options.Filters.Add(new IgnoreAntiforgeryTokenAttribute());
});

// Konfigurasi JWT
var jwtSettings = builder.Configuration.GetSection("Jwt");
var key = Encoding.UTF8.GetBytes(jwtSettings["Key"]);

//Tambahan Baru
builder.Services.AddHttpClient();
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        sqlOptions =>
        {
            // Enable retry on failure to handle transient errors
            sqlOptions.EnableRetryOnFailure(
                maxRetryCount: 5,          // Number of retry attempts
                maxRetryDelay: TimeSpan.FromSeconds(10), // Delay between retries
                errorNumbersToAdd: null   // Additional SQL error numbers to consider transient
            );
        });
});

builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.Password.RequireNonAlphanumeric = false;

    options.Lockout.MaxFailedAccessAttempts = 3;
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
}).AddDefaultTokenProviders().AddEntityFrameworkStores<ApplicationDbContext>();

builder.Services.AddMvc(options =>
{
    var policy = new AuthorizationPolicyBuilder()
    .RequireAuthenticatedUser()
    .Build();
    options.Filters.Add(new AuthorizeFilter(policy));
}).AddXmlSerializerFormatters().AddNewtonsoftJson(options =>
{
    options.SerializerSettings.ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore;
});

builder.Services.AddDistributedMemoryCache();

// konfigurasi session 
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Tambahkan layanan Data Protection
var dataProtectionProvider = DataProtectionProvider.Create("PurchasingSystem");
var dataProtector = dataProtectionProvider.CreateProtector("Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationMiddleware", "Cookies", "v2");

//Script Auto Show Login Account First Time
builder.Services.AddAuthentication("CookieAuth")
    .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings["Issuer"],
            ValidAudience = jwtSettings["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(key)
        };
    })
    .AddCookie("CookieAuth", options =>
    {
        options.TicketDataFormat = new CustomCompressedTicketDataFormat(dataProtector);
        options.Cookie.Name = "AuthCookie";
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/AccessDenied"; // Path untuk akses ditolak
        options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
        options.SlidingExpiration = true;

        //Event untuk membuat pengguna yang sedang aktif di paksa signout saat deployment selesai
        options.Events.OnValidatePrincipal = async context =>
        {
            // Logika validasi
            if (context.Principal == null || !context.Principal.Identity.IsAuthenticated)
            {
                // Jika principal tidak valid, redirect ke login
                context.RejectPrincipal();
                await context.HttpContext.SignOutAsync("CookieAuth");
                context.HttpContext.Response.Redirect("/Account/Login");
            }

            await Task.CompletedTask;
        };

        // Tambahkan ini jika ingin menggunakan chunking
        options.CookieManager = new ChunkingCookieManager { };
    });

builder.Services.AddMemoryCache();

AddScope();
//builder.Services.AddScoped<IUserClaimsPrincipalFactory<ApplicationUser>, ApplicationUserClaims>();
builder.Services.AddScoped<ISessionService, SessionService>();

#region Areas Master Data
builder.Services.AddScoped<IUserActiveRepository>();
builder.Services.AddScoped<ISupplierRepository>();
builder.Services.AddScoped<IDiscountRepository>();
builder.Services.AddScoped<IMeasurementRepository>();
builder.Services.AddScoped<ICategoryRepository>();
builder.Services.AddScoped<ITermOfPaymentRepository>();
builder.Services.AddScoped<IBankRepository>();
builder.Services.AddScoped<IProductRepository>();
builder.Services.AddScoped<ILeadTimeRepository>();
builder.Services.AddScoped<IInitialStockRepository>();
builder.Services.AddScoped<IWarehouseLocationRepository>();
builder.Services.AddScoped<IUnitLocationRepository>();
builder.Services.AddScoped<IDepartmentRepository>();
builder.Services.AddScoped<IPositionRepository>();
builder.Services.AddScoped<IGroupRoleRepository>();
builder.Services.AddSignalR();
#endregion

#region Areas Order
builder.Services.AddScoped<IPurchaseRequestRepository>();
builder.Services.AddScoped<IApprovalPurchaseRequestRepository>();
builder.Services.AddScoped<IPurchaseOrderRepository>();
builder.Services.AddScoped<IEmailRepository>();
builder.Services.AddScoped<IApprovalQtyDifferenceRepository>();
#endregion

#region Areas Warehouse
builder.Services.AddScoped<IReceiveOrderRepository>();
builder.Services.AddScoped<IUnitOrderRepository>();
builder.Services.AddScoped<IWarehouseTransferRepository>();
builder.Services.AddScoped<IQtyDifferenceRepository>();
builder.Services.AddScoped<IProductReturnRepository>();
builder.Services.AddScoped<IApprovalProductReturnRepository>();
#endregion

#region Areas Unit Request
builder.Services.AddScoped<IUnitRequestRepository>();
builder.Services.AddScoped<IApprovalUnitRequestRepository>();
#endregion

#region Areas Report
builder.Services.AddScoped<IClosingPurchaseOrderRepository>();
#endregion

//Jika ingin Add-Migration ini harus di non aktifin
builder.Services.AddHostedService<CleanInactiveUsersService>();

//Initialize Fast Report
FastReport.Utils.RegisteredObjects.AddConnection(typeof(MsSqlDataConnection));

var app = builder.Build();

//builder.Services.AddDataProtection();

// Configure the HTTP request pipeline.
if (!app.Environment.IsProduction())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
// Tambahkan middleware untuk dekripsi URL
//app.UseMiddleware<DecryptUrlMiddleware>();


using (var scope = app.Services.CreateScope())
{
    var serviceProvider = scope.ServiceProvider;
    var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();
    var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();

    await SeedRolesAndSuperAdmin(app, userManager, roleManager);
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

//Tambahan Baru
app.UseSession();
app.UseAuthentication();
app.Use(async (context, next) =>
{
    if (context.User.Identity.IsAuthenticated)
    {
        // Ambil klaim role yang dikompresi
        var compressedRoles = context.User.FindFirst("CompressedRoles")?.Value;
        if (!string.IsNullOrEmpty(compressedRoles))
        {
            // Dekompresi role menjadi array
            var roles = compressedRoles.Split(',');

            // Buat ClaimsIdentity baru dengan klaim role yang didekompresi
            var claimsIdentity = new ClaimsIdentity(context.User.Identity);

            foreach (var role in roles)
            {
                claimsIdentity.AddClaim(new Claim(ClaimTypes.Role, role));
            }

            // Ganti User di context dengan ClaimsPrincipal baru
            context.User = new ClaimsPrincipal(claimsIdentity);
        }
    }

    await next.Invoke();
});
app.UseAuthorization();

app.UseFastReport();

app.UseEndpoints(endpoints =>
{
    // SignalR
    endpoints.MapHub<ChatHub>("/chathub");
    // End SignalR
    endpoints.MapDefaultControllerRoute();

    endpoints.MapControllerRoute(
        name: "MyArea",
        pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");
});

app.Run();

void AddScope()
{
    builder.Services.AddScoped<IRoleRepository, RoleRepository>();
    builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
    builder.Services.AddScoped<IAccountRepository, AccountRepository>();
}

static async Task SeedRolesAndSuperAdmin(WebApplication app, UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
{
    // 1. Buat role otomatis berdasarkan controller dan action
    var controllers = Assembly.GetExecutingAssembly().GetTypes()
                .Where(type => typeof(Controller).IsAssignableFrom(type) && !type.IsAbstract)
                .ToList();

    foreach (var controllerType in controllers)
    {
        var controllerName = controllerType.Name.Replace("Controller", ""); // Nama controller tanpa "Controller"
        var controllerActions = controllerType.GetMethods(BindingFlags.Instance | BindingFlags.Public)
            //.Where(method => method.IsPublic && !method.IsSpecialName && method.DeclaringType == controllerType)
            .Where(method =>
            method.IsPublic &&
            !method.IsSpecialName &&
            !method.Name.StartsWith("Redirect") &&
            !method.Name.StartsWith("Load") &&
            !method.Name.StartsWith("Get") &&
            !method.Name.StartsWith("Impor") &&
            !method.Name.StartsWith("Chart") &&
            !method.Name.StartsWith("KpiJson") &&
            !method.Name.StartsWith("PostData") &&
            !method.GetCustomAttributes(typeof(NonActionAttribute), false).Any() &&
            method.DeclaringType == controllerType && // Hanya metode dari controller itu sendiri
            (method.GetCustomAttributes(typeof(HttpGetAttribute), false).Any() ||
             method.GetCustomAttributes(typeof(HttpPostAttribute), false).Any() ||
             method.GetCustomAttributes(typeof(HttpPutAttribute), false).Any() ||
             method.GetCustomAttributes(typeof(HttpDeleteAttribute), false).Any() ||
             !method.GetCustomAttributes(typeof(HttpMethodAttribute), false).Any()))
            .Select(method => method.Name)
            .ToList();

        if (controllerName != "Account")
        {
            if (controllerName != "Auth")
            {
                if (controllerName != "Dashboard")
                {
                    if (controllerName != "Home")
                    {
                        foreach (var action in controllerActions)
                        {
                            string roleName = action;

                            // Jika aksi adalah "Index", tambahkan nama controller ke role
                            if (action.StartsWith("Index"))
                            {
                                roleName = $"Read{controllerName}";  // Misalnya, "ReadBank"
                            }

                            if (action.StartsWith("Detail"))
                            {
                                roleName = $"Update{controllerName}"; // Misalnya : "UpdateBank"
                            }

                            // Periksa apakah role sudah ada
                            var roleExists = await roleManager.RoleExistsAsync(roleName);
                            if (!roleExists)
                            {
                                IdentityRole role = new IdentityRole
                                {
                                    Name = roleName,  // Nama asli role (misalnya, "AdminIndex")
                                    ConcurrencyStamp = controllerName
                                };

                                var result = await roleManager.CreateAsync(role);
                                if (!result.Succeeded)
                                {
                                    foreach (var error in result.Errors)
                                    {
                                        Console.WriteLine($"Error creating role {roleName}: {error.Description}");
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
    }

    // Cek user superadmin@admin.com
    var superAdminEmail = "superadmin@admin.com";
    var superAdminUser = await userManager.FindByEmailAsync(superAdminEmail);

    if (superAdminUser != null)
    {
        // Tambahkan semua role ke tabel GroupRole
        using (var scope = app.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var allRoles = await roleManager.Roles.ToListAsync();
            var departemenId = superAdminUser.Id;

            foreach (var role in allRoles)
            {
                if (!await userManager.IsInRoleAsync(superAdminUser, role.Name))
                {
                    await userManager.AddToRoleAsync(superAdminUser, role.Name);
                }

                // Cek apakah sudah ada di GroupRole
                var exists = dbContext.GroupRoles.Any(gr => gr.DepartemenId == departemenId && gr.RoleId == role.Id);
                if (!exists)
                {
                    var groupRole = new GroupRole
                    {
                        DepartemenId = departemenId,
                        RoleId = role.Id,
                        CreateDateTime = DateTime.Now,
                        CreateBy = Guid.Parse(superAdminUser.Id)
                    };

                    dbContext.GroupRoles.Add(groupRole);
                }
            }

            await dbContext.SaveChangesAsync();
        }
    }
    else
    {
        Console.WriteLine($"User with email {superAdminEmail} not found in the database.");
    }
}