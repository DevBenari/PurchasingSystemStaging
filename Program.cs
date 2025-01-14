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
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using PurchasingSystemStaging.Areas.MasterData.Repositories;
using PurchasingSystemStaging.Areas.Order.Repositories;
using PurchasingSystemStaging.Areas.Report.Repositories;
using PurchasingSystemStaging.Areas.Transaction.Repositories;
using PurchasingSystemStaging.Areas.Warehouse.Repositories;
using PurchasingSystemStaging.Data;
using PurchasingSystemStaging.Helpers;
using PurchasingSystemStaging.Hubs;
using PurchasingSystemStaging.Models;
using PurchasingSystemStaging.Repositories;
using System.Globalization;
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

// Tambahkan layanan Data Protection
var dataProtectionProvider = DataProtectionProvider.Create("PurchasingSystemStaging");
var dataProtector = dataProtectionProvider.CreateProtector("Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationMiddleware", "Cookies", "v2");

// Konfigurasi JWT
var jwtSettings = builder.Configuration.GetSection("Jwt");
var key = Encoding.UTF8.GetBytes(jwtSettings["Key"]);

//Tambahan Baru
builder.Services.AddHttpClient();
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));
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
        // Tambahkan ini jika ingin menggunakan chunking
        options.CookieManager = new ChunkingCookieManager { };
    });

// Tambahkan otorisasi
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("SuperAdminPolicy", policy =>
        policy.RequireAssertion(context =>
            context.User.HasClaim(c => c.Type == ClaimTypes.Role && c.Value == "SuperAdmin")));
});

// konfigurasi session 
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

builder.Services.AddDistributedMemoryCache();
builder.Services.AddMemoryCache();

AddScope();
builder.Services.AddScoped<IUserClaimsPrincipalFactory<ApplicationUser>, ApplicationUserClaims>();
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
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
// Tambahkan middleware untuk dekripsi URL
//app.UseMiddleware<DecryptUrlMiddleware>();

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    await RoleHelper.CreateRoles(services);
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

//Tambahan Baru
app.UseAuthentication();
app.UseMiddleware<SuperAdminMiddleware>();
app.UseAuthorization();
app.UseSession();

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