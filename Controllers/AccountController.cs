using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PurchasingSystemStaging.Areas.MasterData.Repositories;
using PurchasingSystemStaging.Data;
using PurchasingSystemStaging.Models;
using PurchasingSystemStaging.Repositories;
using PurchasingSystemStaging.ViewModels;
using System.Reflection;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace PurchasingSystemStaging.Controllers
{
    public class AccountController : Controller
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private ILogger<AccountController> _logger;
        private readonly ApplicationDbContext _applicationDbContext;
        private readonly IUserActiveRepository _userActiveRepository;
        private readonly IRoleRepository _roleRepository;
        private readonly IGroupRoleRepository _groupRoleRepository;
        private readonly ISessionService _sessionService;

        private readonly IDataProtector _protector;
        private readonly UrlMappingService _urlMappingService;

        public AccountController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            ApplicationDbContext applicationDbContext,
            IUserActiveRepository userActiveRepository,
            IRoleRepository roleRepository,
            IGroupRoleRepository groupRoleRepository,
            ISessionService sessionService,
            ILogger<AccountController> logger,

            IDataProtectionProvider provider,
            UrlMappingService urlMappingService)
        {
            _applicationDbContext = applicationDbContext;
            _signInManager = signInManager;
            _userManager = userManager;
            _userActiveRepository = userActiveRepository;
            _logger = logger;
            _roleRepository = roleRepository;
            _groupRoleRepository = groupRoleRepository;
            _sessionService = sessionService;

            _protector = provider.CreateProtector("UrlProtector");
            _urlMappingService = urlMappingService;
        }

        public IList<AuthenticationScheme> ExternalLogins { get; set; }
        public IActionResult Index()
        {            
            return View();
        }        
      
        [AllowAnonymous]        
        public async Task<IActionResult> Login()
        {
            if (User.Identity.IsAuthenticated)
            {
                var userId = HttpContext.Session.GetString("UserId");

                if (!string.IsNullOrEmpty(userId) && _sessionService.IsSessionActive(userId))
                {
                    // Jika session aktif, arahkan ke dashboard
                    return RedirectToAction("Index", "Home");
                }
                else
                {
                    await HttpContext.SignOutAsync("CookieAuth");
                    HttpContext.Session.Clear();
                }
                return View();
            }
            else
            {
                var response = new LoginViewModel();
                return View(response);
            }            
        }

        [HttpPost]
        [AllowAnonymous]        
        public async Task<IActionResult> Login(LoginViewModel model/*, string returnUrl = null*/)
        {
            //returnUrl ??= Url.Content("~/");

            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();

            if (ModelState.IsValid)
            {
                if (User.Identity.IsAuthenticated)
                {
                    var userId = HttpContext.Session.GetString("UserId");

                    if (!string.IsNullOrEmpty(userId) && _sessionService.IsSessionActive(userId))
                    {
                        // Jika session aktif, arahkan ke dashboard
                        return RedirectToAction("Index", "Home");
                    }
                    else
                    {
                        await HttpContext.SignOutAsync("CookieAuth");
                        HttpContext.Session.Clear();
                    }
                }
                else
                {
                    var user = await _signInManager.UserManager.FindByNameAsync(model.Email);
                    if (user == null)
                    {
                        ModelState.AddModelError(string.Empty, "Invalid Login Attempt. ");
                        TempData["WarningMessage"] = "Sorry, Username And Password Not Registered !";
                        return View(model);
                    }
                    else if (user.IsActive == true && !_sessionService.IsSessionActive(user.Id))
                    {
                        var result = await _signInManager.CheckPasswordSignInAsync(user, model.Password, true);
                        if (result.Succeeded)
                        {                            
                            // Buat session baru
                            var sessionId = Guid.NewGuid().ToString();
                            HttpContext.Session.SetString("UserId", user.Id.ToString());
                            HttpContext.Session.SetString("SessionId", sessionId);

                            // Simpan session di server
                            _sessionService.CreateSession(user.Id, sessionId, DateTime.UtcNow.AddMinutes(30));

                            // Set cookie autentikasi
                            var claims = new[] { new Claim(ClaimTypes.Name, user.Email) };
                            var identity = new ClaimsIdentity(claims, "CookieAuth");
                            var principal = new ClaimsPrincipal(identity);                            

                            //Session akan di pertahankan jika browser di tutup tanpa di signout,
                            //maka ketika masuk ke browser akan langsung di arahkan ke dashboard
                            var authProperties = new AuthenticationProperties
                            {
                                IsPersistent = false // Cookie tidak akan disimpan setelah browser ditutup
                            };

                            await HttpContext.SignInAsync("CookieAuth", principal, authProperties);
                            await _signInManager.SignInWithClaimsAsync(user, model.RememberMe, claims);

                            // Role
                            List<string> roleNames; // Deklarasikan di luar

                            if (model.Email == "superadmin@admin.com")
                            {
                                roleNames = _roleRepository.GetRoles().Select(role => role.Name).ToList();
                            }
                            else
                            {
                                var userId = _userActiveRepository.GetAllUserLogin()
                                    .FirstOrDefault(u => u.UserName == model.Email)?.Id;

                                if (userId != null) // Pastikan userId tidak null
                                {                                    
                                    roleNames = (from role in _roleRepository.GetRoles()
                                                 join userRole in _groupRoleRepository.GetAllGroupRole()
                                                 on role.Id equals userRole.RoleId
                                                 where userRole.DepartemenId == userId
                                                 select role.Name).Union(
                                                     from role in _roleRepository.GetRoles()
                                                     join userRole in _groupRoleRepository.GetAllGroupRole()
                                                     on role.Id equals userRole.RoleId
                                                     where userRole.DepartemenId == userId
                                                     select role.ConcurrencyStamp).Distinct().ToList();
                                }
                                else
                                {
                                    roleNames = new List<string>(); // Jika userId tidak ditemukan, set roleNames ke list kosong
                                }
                            }

                            // Menyimpan daftar roleNames ke dalam session
                            HttpContext.Session.SetString("ListRole", string.Join(",", roleNames));                          

                            _logger.LogInformation("User logged in.");
                            return RedirectToAction("Index", "Home");
                        }

                        //if (result.RequiresTwoFactor)
                        //{
                        //    return RedirectToPage("./LoginWith2fa", new { ReturnUrl = returnUrl, model.RememberMe });
                        //}

                        if (result.IsLockedOut)
                        {
                            _logger.LogWarning("User account locked out.");
                            // HttpContext.session.Clear untuk menghapus session data pengguna tidak lagi tersimpan
                            HttpContext.Session.Clear();

                            // Hitung waktu yang tersisa
                            var lockTime = await _userManager.GetLockoutEndDateAsync(user);
                            var timeRemaining = lockTime.Value - DateTimeOffset.Now;

                            TempData["UserLockOut"] = "Sorry, your account is locked in " + timeRemaining.Minutes + " minutes " + timeRemaining.Seconds + " seconds";
                            return View(model);
                        }

                        ModelState.AddModelError(string.Empty, "Invalid login attempt.");
                        TempData["WarningMessage"] = "Sorry, Wrong Password !";
                        //return View(model);
                    }
                    else if (user.IsActive == true && _sessionService.IsSessionActive(user.Id))
                    {
                        TempData["UserOnlineMessage"] = "Sorry, your account is online, please wait until the session expires!";                       

                        return View(model);
                    }
                    else
                    {
                        TempData["UserActiveMessage"] = "Sorry, your account is not active !";
                        return View(model);
                    }
                }
            }
            else
            {
                TempData["UserActiveMessage"] = "Error";
                return View(model);
            }
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Logout()
        {           
            // Hapus session dari server-side storage
            var userId = HttpContext.Session.GetString("UserId");

            if (!string.IsNullOrEmpty(userId))
            {
                Console.WriteLine($"Invalidating session for user: {userId}");

                // Hapus session dari server-side storage
                _sessionService.DeleteSession(userId);
                _sessionService.InvalidateAllSessions(userId);

                // Clear session lokal
                HttpContext.Session.Clear();
            }

            // Hapus semua cookies
            foreach (var cookie in HttpContext.Request.Cookies.Keys)
            {
                HttpContext.Response.Cookies.Delete(cookie);
                Console.WriteLine($"Deleted cookie: {cookie}");
            }

            // Sign out autentikasi
            await HttpContext.SignOutAsync("CookieAuth");

            // Redirect ke halaman login
            return RedirectToAction("Login", "Account");
        }
    }
}
