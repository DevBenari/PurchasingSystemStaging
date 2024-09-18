using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PurchasingSystemApps.Areas.MasterData.Models;
using PurchasingSystemApps.Areas.MasterData.Repositories;
using PurchasingSystemApps.Areas.MasterData.ViewModels;
using PurchasingSystemApps.Data;
using PurchasingSystemApps.Models;
using IHostingEnvironment = Microsoft.AspNetCore.Hosting.IHostingEnvironment;

namespace PurchasingSystemApps.Areas.MasterData.Controllers
{
    [Area("MasterData")]
    [Route("MasterData/[Controller]/[Action]")]
    public class DueDateController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ApplicationDbContext _applicationDbContext;
        private readonly IUserActiveRepository _userActiveRepository;
        private readonly IDueDateRepository _dueDateRepository;
        private readonly IProductRepository _productRepository;

        private readonly IHostingEnvironment _hostingEnvironment;

        public DueDateController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            ApplicationDbContext applicationDbContext,
            IDueDateRepository DueDateRepository,
            IUserActiveRepository userActiveRepository,
            IProductRepository productRepository,

            IHostingEnvironment hostingEnvironment
        )
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _applicationDbContext = applicationDbContext;
            _dueDateRepository = DueDateRepository;
            _userActiveRepository = userActiveRepository;
            _productRepository = productRepository;

            _hostingEnvironment = hostingEnvironment;
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> Index()
        {
            ViewBag.Active = "MasterData";
            var data = _dueDateRepository.GetAllDueDate();
            return View(data);
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> Index(DateTime tglAwalPencarian, DateTime tglAkhirPencarian)
        {
            ViewBag.Active = "MasterData";
            ViewBag.tglAwalPencarian = tglAwalPencarian.ToString("dd MMMM yyyy");
            ViewBag.tglAkhirPencarian = tglAkhirPencarian.ToString("dd MMMM yyyy");

            var data = _dueDateRepository.GetAllDueDate().Where(r => r.CreateDateTime.Date >= tglAwalPencarian && r.CreateDateTime.Date <= tglAkhirPencarian).ToList();
            return View(data);
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<ViewResult> CreateDueDate()
        {
            ViewBag.Active = "MasterData";
            var user = new DueDateViewModel();
            
            return View(user);
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> CreateDueDate(DueDateViewModel vm)
        {
            var dateNow = DateTimeOffset.Now;            

            var getUser = _userActiveRepository.GetAllUserLogin().Where(u => u.UserName == User.Identity.Name).FirstOrDefault();

            if (ModelState.IsValid)
            {
                var dueDate = new DueDate
                {
                    CreateDateTime = DateTime.Now,
                    CreateBy = new Guid(getUser.Id),
                    DueDateId = vm.DueDateId,
                    Value = vm.Value
                };

                var result = _dueDateRepository.GetAllDueDate().Where(c => c.Value == vm.Value).FirstOrDefault();
                if (result == null)
                {
                    _dueDateRepository.Tambah(dueDate);
                    TempData["SuccessMessage"] = "Due Date " + vm.Value + " hari Saved";
                    return RedirectToAction("Index", "DueDate");
                }
                else
                {
                    TempData["WarningMessage"] = "DueDate " + vm.Value + " hari Already Exist !!!";
                    return View(vm);
                }

            }

            return View();
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> DetailDueDate(Guid Id)
        {
            ViewBag.Active = "MasterData";
            var DueDate = await _dueDateRepository.GetDueDateById(Id);

            if (DueDate == null)
            {
                Response.StatusCode = 404;
                return View("DueDateNotFound", Id);
            }

            DueDateViewModel viewModel = new DueDateViewModel
            {
                DueDateId = DueDate.DueDateId,
                Value = DueDate.Value
            };
            return View(viewModel);
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> DetailDueDate(DueDateViewModel viewModel)
        {
            if (ModelState.IsValid)
            {
                var DueDate = await _dueDateRepository.GetDueDateByIdNoTracking(viewModel.DueDateId);
                var getUser = _userActiveRepository.GetAllUserLogin().Where(u => u.UserName == User.Identity.Name).FirstOrDefault();
                var check = _dueDateRepository.GetAllDueDate().Where(d => d.Value == viewModel.Value).FirstOrDefault();

                if (check != null)
                {
                    DueDate.UpdateDateTime = DateTime.Now;
                    DueDate.UpdateBy = new Guid(getUser.Id);
                    DueDate.Value = viewModel.Value;

                    _dueDateRepository.Update(DueDate);
                    _applicationDbContext.SaveChanges();

                    TempData["SuccessMessage"] = "DueDate " + viewModel.Value + " hari Success Changes";
                    return RedirectToAction("Index", "DueDate");
                }
                else
                {
                    TempData["WarningMessage"] = "DueDate " + viewModel.Value + " hari Already Exist !!!";
                    return View(viewModel);
                }
            }

            return View();
        }

        [HttpGet]
        [AllowAnonymous]
        //[Authorize]
        public async Task<IActionResult> DeleteDueDate(Guid Id)
        {
            ViewBag.Active = "MasterData";
            var DueDate = await _dueDateRepository.GetDueDateById(Id);
            if (DueDate == null)
            {
                Response.StatusCode = 404;
                return View("DueDateNotFound", Id);
            }

            DueDateViewModel vm = new DueDateViewModel
            {
                DueDateId = DueDate.DueDateId,
                Value = DueDate.Value,
            };
            return View(vm);
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> DeleteDueDate(DueDateViewModel vm)
        {
            //Cek Relasi
            //Hapus Data
            var dueDate = _applicationDbContext.DueDates.FirstOrDefault(x => x.DueDateId == vm.DueDateId);
            _applicationDbContext.Attach(dueDate);
            _applicationDbContext.Entry(dueDate).State = EntityState.Deleted;
            _applicationDbContext.SaveChanges();

            TempData["SuccessMessage"] = "DueDate " + vm.Value + " hari Success Deleted";
            return RedirectToAction("Index", "DueDate");
        }
    }
}
