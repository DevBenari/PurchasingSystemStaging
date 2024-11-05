using Azure;
using FastReport.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.EntityFrameworkCore;
using PurchasingSystemStaging.Areas.MasterData.Repositories;
using PurchasingSystemStaging.Areas.Order.Repositories;
using PurchasingSystemStaging.Areas.Transaction.Repositories;
using PurchasingSystemStaging.Areas.Warehouse.Repositories;
using PurchasingSystemStaging.Data;
using PurchasingSystemStaging.Hubs;
using PurchasingSystemStaging.Models;
using PurchasingSystemStaging.Repositories;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();


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

// konfigurasi session 
builder.Services.AddSession(options =>
{    
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.IdleTimeout = TimeSpan.FromSeconds(15);
});

//Script Auto Show Login Account First Time
builder.Services.AddAuthentication("CookieAuth")
    .AddCookie("CookieAuth", options =>
    {
        options.Cookie.Name = "AuthCookie";
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.ExpireTimeSpan = TimeSpan.FromSeconds(15);
        options.SlidingExpiration = true;

        //options.LoginPath = "/Account/Login";
        //options.LogoutPath = "/Account/Logout";
        //options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
        //options.SlidingExpiration = true;
    });

AddScope();
builder.Services.AddScoped<IUserClaimsPrincipalFactory<ApplicationUser>, ApplicationUserClaims>();

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
builder.Services.AddScoped<IApprovalRepository>();
builder.Services.AddScoped<IPurchaseOrderRepository>();
builder.Services.AddScoped<IEmailRepository>();
builder.Services.AddScoped<IApprovalQtyDifferenceRepository>();
#endregion

#region Areas Warehouse
builder.Services.AddScoped<IReceiveOrderRepository>();
builder.Services.AddScoped<IUnitOrderRepository>();
builder.Services.AddScoped<IWarehouseTransferRepository>();
builder.Services.AddScoped<IQtyDifferenceRepository>();
#endregion

#region Areas Unit Request
builder.Services.AddScoped<IUnitRequestRepository>();
builder.Services.AddScoped<IApprovalUnitRequestRepository>();
#endregion

//Initialize Fast Report
FastReport.Utils.RegisteredObjects.AddConnection(typeof(MsSqlDataConnection));
var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

//Tambahan Baru
app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

//   konfigurasi end session 
app.Use(async (context, next) =>
{
    // Hapus semua session & Hapus cookie UserName
    //context.Session.Clear();   
    //context.Response.Cookies.Delete("username");
    var returnUrl = context.Request.Path + context.Request.QueryString;
    var loginUrl = $"/Account/Login?ReturnUrl={Uri.EscapeDataString(returnUrl)}";

    if (context.Session.GetString("username") == null && context.Request.Path != loginUrl)
    {
        var username = context.User.Identity.Name;

        if (!string.IsNullOrEmpty(username))
        {
            

            UpdateDataForExpiredSession(username, app, context, returnUrl);
            

            //// Hapus semua session & Hapus cookie UserName
            //context.Session.Clear();
            //context.Response.Cookies.Delete("username");

            //await context.SignOutAsync("CookieAuth");
            ////await dbContext.SignOutAsync(IdentityConstants.ApplicationScheme);

            context.Response.Redirect(loginUrl);
            return;
        }
    }
    else
    {
        // Jika session masih aktif, perbarui waktu "LastActivity"
        context.Session.SetString("LastActivity", DateTime.Now.ToString());        
    }
    
    // Lanjutkan ke middleware berikutnya
    await next();
});

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

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

app.MapRazorPages();

using (var scope = app.Services.CreateScope())
{
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

    //var roles = new[] {
    //    #region Area Master Data Menu Role Pengguna
    //    "Role", "IndexRole", "CreateRole", "DetailRole", "DeleteRole",
    //    #endregion
        
    //};

    //foreach (var role in roles)
    //{
    //    if (!await roleManager.RoleExistsAsync(role))
    //        await roleManager.CreateAsync(new IdentityRole(role));
    //}
}

app.Run();

void AddScope()
{
    builder.Services.AddScoped<IRoleRepository, RoleRepository>();
    builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
    builder.Services.AddScoped<IAccountRepository, AccountRepository>();
}

async void UpdateDataForExpiredSession(string username, WebApplication app, HttpContext context, string returnUrl)
{
    // Logika update data
    // Tambahkan logika lain sesuai kebutuhan, misalnya memperbarui status user di database
    using (var scope = app.Services.CreateScope())
    { 
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var signInManager = scope.ServiceProvider.GetRequiredService<SignInManager<ApplicationUser>>();

        //Cari User
        var user = dbContext.Users.FirstOrDefault(u => u.Email == username);
        if (user != null)
        {            
            user.IsOnline = false;
            dbContext.SaveChanges();
        }

        // Hapus session dan sign out cookie
        context.Session.Remove("username");
        await context.SignOutAsync("CookieAuth");

        await signInManager.SignOutAsync();
        context.Response.Redirect(returnUrl);
    }
}