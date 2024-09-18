using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PurchasingSystemApps.Areas.MasterData.Models;
using PurchasingSystemApps.Areas.MasterData.Repositories;
using PurchasingSystemApps.Areas.MasterData.ViewModels;
using PurchasingSystemApps.Data;
using PurchasingSystemApps.Models;
using PurchasingSystemApps.Repositories;
using IHostingEnvironment = Microsoft.AspNetCore.Hosting.IHostingEnvironment;

namespace PurchasingSystemApps.Areas.MasterData.Controllers
{
    [Area("MasterData")]
    [Route("MasterData/[Controller]/[Action]")]
    public class PrincipalController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ApplicationDbContext _applicationDbContext;
        private readonly IUserActiveRepository _userActiveRepository;
        private readonly IPrincipalRepository _principalRepository;
        private readonly IProductRepository _productRepository;

        private readonly IHostingEnvironment _hostingEnvironment;

        public PrincipalController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            ApplicationDbContext applicationDbContext,
            IPrincipalRepository principalRepository,
            IUserActiveRepository userActiveRepository,
            IProductRepository productRepository,

            IHostingEnvironment hostingEnvironment
        )
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _applicationDbContext = applicationDbContext;
            _principalRepository = principalRepository;
            _userActiveRepository = userActiveRepository;
            _productRepository = productRepository;

            _hostingEnvironment = hostingEnvironment;
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> Index()
        {
            ViewBag.Active = "MasterData";
            var data = _principalRepository.GetAllPrincipal();
            return View(data);
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> Index(DateTime tglAwalPencarian, DateTime tglAkhirPencarian)
        {
            ViewBag.Active = "MasterData";
            ViewBag.tglAwalPencarian = tglAwalPencarian.ToString("dd MMMM yyyy");
            ViewBag.tglAkhirPencarian = tglAkhirPencarian.ToString("dd MMMM yyyy");

            var data = _principalRepository.GetAllPrincipal().Where(r => r.CreateDateTime.Date >= tglAwalPencarian && r.CreateDateTime.Date <= tglAkhirPencarian).ToList();
            return View(data);
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<ViewResult> CreatePrincipal()
        {
            ViewBag.Active = "MasterData";
            var user = new PrincipalViewModel();
            var dateNow = DateTimeOffset.Now;
            var setDateNow = DateTimeOffset.Now.ToString("yyMMdd");

            var lastCode = _principalRepository.GetAllPrincipal().Where(d => d.CreateDateTime.ToString("yyMMdd") == dateNow.ToString("yyMMdd")).OrderByDescending(k => k.PrincipalCode).FirstOrDefault();
            if (lastCode == null)
            {
                user.PrincipalCode = "PCP" + setDateNow + "0001";
            }
            else
            {
                var lastCodeTrim = lastCode.PrincipalCode.Substring(3, 6);

                if (lastCodeTrim != setDateNow)
                {
                    user.PrincipalCode = "PCP" + setDateNow + "0001";
                }
                else
                {
                    user.PrincipalCode = "PCP" + setDateNow + (Convert.ToInt32(lastCode.PrincipalCode.Substring(9, lastCode.PrincipalCode.Length - 9)) + 1).ToString("D4");
                }
            }

            return View(user);
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> CreatePrincipal(PrincipalViewModel vm)
        {
            var dateNow = DateTimeOffset.Now;
            var setDateNow = DateTimeOffset.Now.ToString("yyMMdd");

            var lastCode = _principalRepository.GetAllPrincipal().Where(d => d.CreateDateTime.ToString("yyMMdd") == dateNow.ToString("yyMMdd")).OrderByDescending(k => k.PrincipalCode).FirstOrDefault();
            if (lastCode == null)
            {
                vm.PrincipalCode = "PCP" + setDateNow + "0001";
            }
            else
            {
                var lastCodeTrim = lastCode.PrincipalCode.Substring(3, 6);

                if (lastCodeTrim != setDateNow)
                {
                    vm.PrincipalCode = "PCP" + setDateNow + "0001";
                }
                else
                {
                    vm.PrincipalCode = "PCP" + setDateNow + (Convert.ToInt32(lastCode.PrincipalCode.Substring(9, lastCode.PrincipalCode.Length - 9)) + 1).ToString("D4");
                }
            }

            var getUser = _userActiveRepository.GetAllUserLogin().Where(u => u.UserName == User.Identity.Name).FirstOrDefault();

            if (ModelState.IsValid)
            {
                var principal = new Principal
                {
                    CreateDateTime = DateTime.Now,
                    CreateBy = new Guid(getUser.Id),
                    PrincipalId = vm.PrincipalId,
                    PrincipalCode = vm.PrincipalCode,
                    PrincipalName = vm.PrincipalName,                    
                    Address = vm.Address,
                    Handphone = vm.Handphone,
                    Email = vm.Email,
                    Note = vm.Note
                };
              
                var result = _principalRepository.GetAllPrincipal().Where(c => c.PrincipalName == vm.PrincipalName).FirstOrDefault();                
                if (result == null)
                {
                    _principalRepository.Tambah(principal);
                    TempData["SuccessMessage"] = "Name " + vm.PrincipalName + " Saved";
                    return RedirectToAction("Index", "Principal");
                }
                else
                {
                    TempData["WarningMessage"] = "Name " + vm.PrincipalName + " Already Exist !!!";
                    return View(vm);
                }

            }

            return View();
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> DetailPrincipal(Guid Id)
        {
            ViewBag.Active = "MasterData";
            var principal = await _principalRepository.GetPrincipalById(Id);

            if (principal == null)
            {
                Response.StatusCode = 404;
                return View("UserNotFound", Id);
            }

            PrincipalViewModel viewModel = new PrincipalViewModel
            {
                PrincipalId = principal.PrincipalId,
                PrincipalCode = principal.PrincipalCode,
                PrincipalName = principal.PrincipalName,               
                Address = principal.Address,
                Handphone = principal.Handphone,
                Email = principal.Email,
                Note = principal.Note
            };
            return View(viewModel);
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> DetailPrincipal(PrincipalViewModel viewModel)
        {
            if (ModelState.IsValid)
            {
                var principal = await _principalRepository.GetPrincipalByIdNoTracking(viewModel.PrincipalId);
                var getUser = _userActiveRepository.GetAllUserLogin().Where(u => u.UserName == User.Identity.Name).FirstOrDefault();                
                var check = _principalRepository.GetAllPrincipal().Where(d => d.PrincipalCode == viewModel.PrincipalCode).FirstOrDefault();

                if (check != null)
                {
                    principal.UpdateDateTime = DateTime.Now;
                    principal.UpdateBy = new Guid(getUser.Id);
                    principal.PrincipalCode = viewModel.PrincipalCode;
                    principal.PrincipalName = viewModel.PrincipalName;
                    principal.Address = viewModel.Address;
                    principal.Handphone = viewModel.Handphone;
                    principal.Email = viewModel.Email;
                    principal.Note = viewModel.Note;

                    _principalRepository.Update(principal);
                    _applicationDbContext.SaveChanges();

                    TempData["SuccessMessage"] = "Name " + viewModel.PrincipalName + " Success Changes";
                    return RedirectToAction("Index", "Principal");
                }
                else
                {
                    TempData["WarningMessage"] = "Name " + viewModel.PrincipalName + " Already Exist !!!";
                    return View(viewModel);
                }
            }

            return View();
        }

        [HttpGet]
        [AllowAnonymous]
        //[Authorize]
        public async Task<IActionResult> DeletePrincipal(Guid Id)
        {
            ViewBag.Active = "MasterData";
            var principal = await _principalRepository.GetPrincipalById(Id);
            if (principal == null)
            {
                Response.StatusCode = 404;
                return View("PrincipalNotFound", Id);
            }

            PrincipalViewModel vm = new PrincipalViewModel
            {
                PrincipalId = principal.PrincipalId,
                PrincipalCode = principal.PrincipalCode,
                PrincipalName = principal.PrincipalName,
            };
            return View(vm);
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> DeletePrincipal(PrincipalViewModel vm)
        {
            //Cek Relasi
            var produk = _productRepository.GetAllProduct().Where(p => p.PrincipalId == vm.PrincipalId).FirstOrDefault();
            if (produk == null)
            {
                //Hapus Data
                var Principal = _applicationDbContext.Principals.FirstOrDefault(x => x.PrincipalId == vm.PrincipalId);
                _applicationDbContext.Attach(Principal);
                _applicationDbContext.Entry(Principal).State = EntityState.Deleted;
                _applicationDbContext.SaveChanges();

                TempData["SuccessMessage"] = "Name " + vm.PrincipalName + " Success Deleted";
                return RedirectToAction("Index", "Principal");
            }
            else {
                TempData["WarningMessage"] = "Sorry, " + vm.PrincipalName + " In used by the product !";
                return View(vm);
            }
        }
    }
}
