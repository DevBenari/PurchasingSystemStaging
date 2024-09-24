using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using PurchasingSystemApps.Data;
using PurchasingSystemApps.Models;
using PurchasingSystemApps.ViewModels;
using System.Security.Claims;

namespace PurchasingSystemApps.Controllers
{
    public class AccountController : Controller
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private ILogger<AccountController> _logger;
        private readonly ApplicationDbContext _applicationDbContext;

        public AccountController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            ApplicationDbContext applicationDbContext,
            ILogger<AccountController> logger)
        {
            _applicationDbContext = applicationDbContext;
            _signInManager = signInManager;
            _userManager = userManager;
            _logger = logger;
        }

        public IList<AuthenticationScheme> ExternalLogins { get; set; }
        public IActionResult Index()
        {            
            return View();
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Login()
        {
            var response = new LoginViewModel();
            return View(response);
            //return RedirectToAction("Index", "Login");
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> Login(LoginViewModel model, string returnUrl = null)
        {
            returnUrl ??= Url.Content("~/");

            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();

            if (ModelState.IsValid)
            {
                var user = await _signInManager.UserManager.FindByNameAsync(model.Email);
                if (user == null)
                {
                    ModelState.AddModelError(string.Empty, "Invalid Login Attempt. ");
                    TempData["WarningMessage"] = "Sorry, Username & Password Not Registered !";
                    return View(model);
                }
                else if (user.IsActive == true) 
                {
                    var result = await _signInManager.CheckPasswordSignInAsync(user, model.Password, false);
                    if (result.Succeeded)
                    {
                        var claims = new List<Claim>
                    {
                        new Claim("amr", "pwd"),
                    };

                        var roles = await _signInManager.UserManager.GetRolesAsync(user);

                        if (roles.Any())
                        {
                            var roleClaim = string.Join(",", roles);
                            claims.Add(new Claim("Roles", roleClaim));
                        }

                        await _signInManager.SignInWithClaimsAsync(user, model.RememberMe, claims);

                        user.IsOnline = true;

                        await _userManager.UpdateAsync(user);

                        _logger.LogInformation("User logged in.");
                        return RedirectToAction("Index", "Home");
                    }
                    else if (result.RequiresTwoFactor)
                    {
                        return RedirectToPage("./LoginWith2fa", new { ReturnUrl = returnUrl, model.RememberMe });
                    }

                    else if (result.IsLockedOut)
                    {
                        _logger.LogWarning("User account locked out.");
                        return RedirectToPage("./Lockout");
                    }
                    else
                    {
                        ModelState.AddModelError(string.Empty, "Invalid login attempt.");
                        TempData["WarningMessage"] = "Sorry, Wrong Password !";
                        return View(model);
                    }
                }
                else
                {
                    TempData["UserActiveMessage"] = "Sorry, your user is not active !";
                    return View(model);
                }
                
                //if (result.Succeeded)
                //{
                //    _logger.LogInformation("User Loggin in.");
                //    return RedirectToAction("Index", "Home");
                //}                
            }
            return View(model);
        }
    }
}
