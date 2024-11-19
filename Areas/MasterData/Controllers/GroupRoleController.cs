using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using PurchasingSystemStaging.Areas.MasterData.Models;
using PurchasingSystemStaging.Areas.MasterData.Repositories;
using PurchasingSystemStaging.Areas.MasterData.ViewModels;
using PurchasingSystemStaging.Data;
using PurchasingSystemStaging.Hubs;
using PurchasingSystemStaging.Models;
using Microsoft.AspNetCore.Authorization;
using System.Data;
using PurchasingSystemStaging.Repositories;
using Microsoft.AspNetCore.Hosting;
using System.Reflection;
using PurchasingSystemStaging.Controllers;
using System;
using System.Text;
using Microsoft.AspNetCore.DataProtection;
using System.Security.Cryptography;

namespace PurchasingSystemStaging.Areas.MasterData.Controllers
{
    [Area("MasterData")]
    [Route("MasterData/[controller]/[action]")]
    public class GroupRoleController : Controller
    {

        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ApplicationDbContext _applicationDbContext;
        private readonly IUserActiveRepository _userActiveRepository;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IGroupRoleRepository _groupRoleRepository;
        private readonly IRoleRepository _roleRepository;
        private readonly IHubContext<ChatHub> _hubContext;

        private readonly IDataProtector _protector;
        private readonly UrlMappingService _urlMappingService;

        public GroupRoleController(ApplicationDbContext applicationDbContext,
            IUserActiveRepository userActiveRepository,
            SignInManager<ApplicationUser> signInManager,
            RoleManager<IdentityRole> roleManager,
            IGroupRoleRepository groupRoleRepository,
            IRoleRepository roleRepository,
            IHubContext<ChatHub> hubContext,

            IDataProtectionProvider provider,
            UrlMappingService urlMappingService
        )
        {
            _applicationDbContext = applicationDbContext;
            _roleManager = roleManager;
            _signInManager = signInManager;
            _hubContext = hubContext;
            _userActiveRepository = userActiveRepository;
            _groupRoleRepository = groupRoleRepository;
            _roleRepository = roleRepository;

            _protector = provider.CreateProtector("UrlProtector");
            _urlMappingService = urlMappingService;
        }

        public IActionResult RedirectToIndex()
        {            
            // Bangun originalPath dengan format tanggal ISO 8601
            string originalPath = $"Page:MasterData/GroupRole/Index";
            string encryptedPath = _protector.Protect(originalPath);

            // Hash GUID-like code (SHA256 truncated to 36 characters)
            string guidLikeCode = Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(encryptedPath)))
                .Replace('+', '-')
                .Replace('/', '_')
                .Substring(0, 36);

            // Simpan mapping GUID-like code ke encryptedPath di penyimpanan sementara (misalnya, cache)
            _urlMappingService.InMemoryMapping[guidLikeCode] = encryptedPath;

            return Redirect("/" + guidLikeCode);
        }

        public IActionResult Index()
        {
            return View(); // Kirim data role ke view
        }        

        [HttpGet]
        [AllowAnonymous]
        public IActionResult LoadRoles(string Email)
        {

            if (!string.IsNullOrEmpty(Email))
            {
                var userId = _userActiveRepository.GetAllUserLogin()
                    .FirstOrDefault(u => u.UserName == Email);
                // Mengambil bukan DepartemenId
                var roleIdsNotForDep = _applicationDbContext.GroupRoles
                .Where(gr => gr.DepartemenId == userId.Id)
                .Select(gr => gr.RoleId)
                .ToList();

                var rolesNotForDep = _roleManager.Roles
                    .Where(role => roleIdsNotForDep.Contains(role.Id))
                    .OrderBy(role => role.ConcurrencyStamp)
                    .ToList();

                // Filter roles yang hanya ada di roleIds dan bukan di roleIdsNotForDep
                var allRoles = _roleManager.Roles.ToList();
                var rolesForDep = allRoles
                    .Where(role => !roleIdsNotForDep.Contains(role.Id))
                    .OrderBy(role => role.ConcurrencyStamp)
                    .ToList();

                var result = new
                {
                    RolesForDepartment = rolesForDep,
                    RolesNotForDepartment = rolesNotForDep
                };

                return Json(result);
            }
            else
            {
                var roles = _roleManager.Roles
                    .OrderBy(role => role.ConcurrencyStamp)
                    .ToList();
                return Json(new
                {
                    RolesForDepartment = roles
                });
            }
        }


        [HttpGet]
        [AllowAnonymous]
        public JsonResult LoadUser()
        {
            var user = _userActiveRepository.GetAllUser().ToList();
            return Json(user);
        }

    }
}
