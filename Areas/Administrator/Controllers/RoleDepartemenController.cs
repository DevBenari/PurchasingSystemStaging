﻿using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using PurchasingSystem.Areas.Administrator.Models;
using PurchasingSystem.Areas.Administrator.Repositories;
using PurchasingSystem.Areas.Administrator.ViewModels;
using PurchasingSystem.Areas.MasterData.Repositories;
using PurchasingSystem.Data;
using PurchasingSystem.Hubs;
using PurchasingSystem.Models;
using PurchasingSystem.Repositories;
using System.Reflection;

namespace PurchasingSystemStaging.Areas.Administrator.Controllers
{
    [Area("Administrator")]
    [Route("Administrator/[controller]/[action]")]
    public class RoleDepartemenController : Controller
    {
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ApplicationDbContext _applicationDbContext;
        private readonly IUserActiveRepository _userActiveRepository;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IGroupRoleRepository _groupRoleRepository;
        private readonly IRoleRepository _roleRepository;
        private readonly IHubContext<ChatHub> _hubContext;
        private readonly IPositionRepository _positionRepository;

        private readonly IDataProtector _protector;
        private readonly UrlMappingService _urlMappingService;

        public RoleDepartemenController(ApplicationDbContext applicationDbContext,
            IUserActiveRepository userActiveRepository,
            SignInManager<ApplicationUser> signInManager,
            RoleManager<IdentityRole> roleManager,
            IGroupRoleRepository groupRoleRepository,
            IRoleRepository roleRepository,
            IHubContext<ChatHub> hubContext,
            IPositionRepository PositionRepository,

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
            _positionRepository = PositionRepository;

            _protector = provider.CreateProtector("UrlProtector");
            _urlMappingService = urlMappingService;
        }

        [Authorize(Roles = "ReadRoleUser")]
        public IActionResult Index()
        {
            ViewBag.Active = "Administrator";
            return View(); // Kirim data role ke view
        }

        [Authorize(Roles = "CreateRoleUser")]
        public async Task<IActionResult> CreateRoleUser()
        {
            ViewBag.Active = "Administrator";
            var roles = await _roleManager.Roles
                      .Select(r => new
                      {
                          r.Id,
                          r.Name,
                          r.NormalizedName
                      })
                      .ToListAsync();
            // Kirim data role langsung ke view
            return View(roles);
        }
        [HttpPost]
        [Authorize(Roles = "CreateRolePosition")]
        public async Task<IActionResult> CreateRolePosition(GroupRoleViewModel vm)
        {
            ViewBag.Active = "Administrator";
            var dateNow = DateTimeOffset.Now;
            var setDateNow = DateTimeOffset.Now.ToString("yyMMdd");

            var getUser = _userActiveRepository.GetAllUserLogin().Where(u => u.UserName == User.Identity.Name).FirstOrDefault();

            var getPosition = _positionRepository.GetAllPosition()
            .FirstOrDefault(u => u.PositionCode == vm.DepartemenId);

            if (getPosition == null)
            {
                TempData["WarningMessage"] = "Sorry, please select a Position first !!!";
                return RedirectToAction("Index"); // atau aksi lain sesuai kebutuhan                
            }
            else
            {
                var departemenId = getPosition.PositionId.ToString();
                var roleIds = vm.RoleId;
                // Hapus semua user roles terkait
                _groupRoleRepository.DeleteByDepartmentId(departemenId);
                var userRoles = _applicationDbContext.UserRoles
                    .Where(ur => ur.UserId == departemenId)
                    .ToList();
                if (userRoles.Any())
                {
                    _applicationDbContext.UserRoles.RemoveRange(userRoles);
                    _applicationDbContext.SaveChanges();  // Simpan perubahan ke database
                }
                // End Hapus 

                if (ModelState.IsValid)
                {
                    // Simpan ID Peran
                    foreach (var roleId in roleIds)
                    {
                        var groupRole = new GroupRole
                        {
                            DepartemenId = departemenId,
                            RoleId = roleId, // Gunakan roleId dari loop
                            CreateDateTime = DateTime.Now,
                            CreateBy = new Guid(getUser.Id)
                        };

                        _groupRoleRepository.Tambah(groupRole);

                    }
                }

                TempData["SuccessMessage"] = "Role successfully assigned to Position";
                return RedirectToAction("Index"); // atau aksi lain sesuai kebutuhan
            }
        }


        [HttpPost]
        [Authorize(Roles = "CreateRoleUserNavbar")]
        public async Task<IActionResult> CreateRoleUserNavbar()
        {
            ViewBag.Active = "Administrator";
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
                                    var roleExists = await _roleManager.RoleExistsAsync(roleName);
                                    if (!roleExists)
                                    {
                                        IdentityRole role = new IdentityRole
                                        {
                                            Name = roleName,  // Nama asli role (misalnya, "AdminIndex")
                                            ConcurrencyStamp = controllerName
                                        };

                                        var result = await _roleManager.CreateAsync(role);
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

            await _hubContext.Clients.All.SendAsync("UpdateDataCount", '0');
            return Json(new { success = true, message = "Role untuk semua controller berhasil dibuat." });
        }


        [HttpGet]
        [AllowAnonymous]
        public IActionResult LoadRoles(string Position)
        {
            ViewBag.Active = "Administrator";
            if (!string.IsNullOrEmpty(Position))
            {
                var userId = _positionRepository.GetAllPosition()
                    .FirstOrDefault(u => u.PositionCode == Position);
                // Mengambil bukan DepartemenId
                var roleIdsNotForDep = _applicationDbContext.GroupRoles
                .Where(gr => gr.DepartemenId == userId.PositionId.ToString())
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

        [AllowAnonymous]
        public JsonResult LoadUser()
        {
            var user = _userActiveRepository.GetAllUser().ToList();
            return Json(user);
        }

        [AllowAnonymous]
        public JsonResult LoadPosition()
        {
            var user = _positionRepository.GetAllPosition().ToList();
            return Json(user);
        }
    }
}
