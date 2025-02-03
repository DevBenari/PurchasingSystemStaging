using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using PurchasingSystem.Areas.Administrator.Models;
using PurchasingSystem.Areas.Administrator.Repositories;
using PurchasingSystem.Areas.Administrator.ViewModels;
using PurchasingSystem.Areas.MasterData.Models;
using PurchasingSystem.Areas.MasterData.Repositories;
using PurchasingSystem.Data;
using PurchasingSystem.Hubs;
using PurchasingSystem.Models;
using PurchasingSystem.Repositories;
using PurchasingSystemStaging.Areas.Administrator.Models;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;

namespace PurchasingSystem.Areas.Administrator.Controllers
{
    [Area("Administrator")]
    [Route("Administrator/[controller]/[action]")]
    public class RoleUserController : Controller
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

        public RoleUserController(ApplicationDbContext applicationDbContext,
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
        [Authorize(Roles = "CreateRoleUser")]
        public async Task<IActionResult> CreateRoleUser(GroupUserViewModel vm)
        {
            ViewBag.Active = "Administrator";

            var getUser = _userActiveRepository.GetAllUserLogin()
                .FirstOrDefault(u => u.Email == vm.UserId);

            if (getUser == null)
            {
                TempData["WarningMessage"] = "Sorry, please select a user first !!!";
                return RedirectToAction("Index");
            }

            // Hapus semua user roles terkait
            var userRoles = _applicationDbContext.UserRoles
                .Where(ur => ur.UserId == getUser.Id)
                .ToList();
            var userGroup = _applicationDbContext.GroupUsers
                .Where(ug => ug.UserId == getUser.Id)
                .ToList();

            if (userRoles.Any() || userGroup.Any())
            {
                if (userRoles.Any())
                {
                    _applicationDbContext.UserRoles.RemoveRange(userRoles);
                }
                if (userGroup.Any())
                {
                    _applicationDbContext.GroupUsers.RemoveRange(userGroup);
                }

                // Simpan perubahan sekali
                _applicationDbContext.SaveChanges();
            }

            if (vm.DepartemenId != null)
            {
                foreach (var departemenId in vm.DepartemenId) // Iterasi DepartemenId
                {
                    // Validasi DepartemenId
                    if (!_applicationDbContext.GroupRoles.Any(gr => gr.DepartemenId == departemenId))
                    {
                        Console.WriteLine($"DepartemenId {departemenId} tidak valid.");
                        continue;
                    }

                    // Tambahkan data ke tabel GroupUsers
                    var groupRepository = new GroupUser
                    {
                        DepartemenId = departemenId,
                        UserId = getUser.Id,
                        CreateDateTime = DateTime.Now,
                        CreateBy = Guid.Parse(getUser.Id)
                    };

                    _groupRoleRepository.TambahGroup(groupRepository);

                    // Ambil semua RoleId terkait dengan DepartemenId ini
                    var roleIds = _applicationDbContext.GroupRoles
                        .Where(gr => gr.DepartemenId == departemenId)
                        .Select(gr => gr.RoleId)
                        .ToList();

                    // Proses setiap RoleId
                    foreach (var roleId in roleIds)
                    {
                        var existingUserRole = _applicationDbContext.UserRoles
                            .FirstOrDefault(ur => ur.UserId == getUser.Id && ur.RoleId == roleId);

                        if (existingUserRole == null)
                        {
                            var userRole = new IdentityUserRole<string>
                            {
                                UserId = getUser.Id,
                                RoleId = roleId
                            };

                            _applicationDbContext.UserRoles.Add(userRole);
                        }
                        else
                        {
                            Console.WriteLine($"UserRole dengan UserId: {getUser.Id} dan RoleId: {roleId} sudah ada.");
                        }
                    }
                }
            }

            // Simpan semua perubahan ke database
            await _applicationDbContext.SaveChangesAsync();



            TempData["SuccessMessage"] = "Roles successfully assigned to user";
            return RedirectToAction("Index");
        }


        [HttpGet]
        [AllowAnonymous]
        public IActionResult LoadRoles(string Email)
        {
            ViewBag.Active = "Administrator";
            if (!string.IsNullOrEmpty(Email))
            {
                var userId = _userActiveRepository.GetAllUserLogin()
                    .FirstOrDefault(u => u.UserName == Email);

                // Mengambil semua departemen yang terkait dengan user
                var departemenDipanggil = _applicationDbContext.GroupUsers
                    .Where(gr => gr.UserId == userId.Id)
                    .Select(gr => gr.DepartemenId)
                    .ToList();

                // Ambil semua posisi (departemen)
                var semuaDepartemen = _positionRepository.GetAllPosition();

                // Departemen yang dipanggil
                var dipanggil = semuaDepartemen
                    .Where(d => departemenDipanggil.Contains(d.PositionId.ToString()) &&
                    _applicationDbContext.GroupRoles.Any(gr => gr.DepartemenId == d.PositionId.ToString()))
                    .Select(d => new
                    {
                        Id = d.PositionId,      // Tetap gunakan "Id" agar cocok dengan frontend
                        Name = d.PositionName,
                        concurrencyStamp = d.Department.DepartmentName.Replace(' ', '_')
                    })
                    .ToList();

                // Departemen yang tidak dipanggil
                var tidakDipanggil = semuaDepartemen
                    .Where(d => !departemenDipanggil.Contains(d.PositionId.ToString()) &&
                    _applicationDbContext.GroupRoles.Any(gr => gr.DepartemenId == d.PositionId.ToString()))
                    .Select(d => new
                    {
                        Id = d.PositionId,      // Tetap gunakan "Id" agar cocok dengan frontend
                        Name = d.PositionName,
                        concurrencyStamp = d.Department.DepartmentName.Replace(' ', '_')
                    })
                    .ToList();

                var result = new
                {
                    RolesForDepartment = tidakDipanggil, // ini list menu
                    RolesNotForDepartment = dipanggil // menu yg dipilih
                };


                return Json(result);
            }
            else
            {
                var roles = _applicationDbContext.Positions
                        .Where(position => _applicationDbContext.GroupRoles
                        .Any(groupRole => groupRole.DepartemenId == position.PositionId.ToString())) // Filter hanya posisi yang ada di GroupRoles
                        .Select(position => new
                        {
                            Id = position.PositionId,                     // ID posisi
                            Name = position.PositionName,                 // Nama posisi
                            concurrencyStamp = position.Department != null
                                ? position.Department.DepartmentName.Replace(' ', '_')
                                : null                                    
                        })
                        .ToList();

                return Json(new
                {
                    RolesForDepartment = roles // semua list departemen
                });
            }
        }

        [AllowAnonymous]
        public JsonResult LoadUser()
        {
            var user = _userActiveRepository.GetAllUserLogin().ToList();
            return Json(user);
        }
    }
}
