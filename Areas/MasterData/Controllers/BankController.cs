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
    public class BankController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ApplicationDbContext _applicationDbContext;
        private readonly IUserActiveRepository _userActiveRepository;
        private readonly IBankRepository _BankRepository;

        private readonly IHostingEnvironment _hostingEnvironment;

        public BankController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            ApplicationDbContext applicationDbContext,
            IBankRepository BankRepository,
            IUserActiveRepository userActiveRepository,

            IHostingEnvironment hostingEnvironment
        )
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _applicationDbContext = applicationDbContext;
            _BankRepository = BankRepository;
            _userActiveRepository = userActiveRepository;

            _hostingEnvironment = hostingEnvironment;
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> Index()
        {
            ViewBag.Active = "MasterData";
            var data = _BankRepository.GetAllBank();
            return View(data);
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> Index(DateTime tglAwalPencarian, DateTime tglAkhirPencarian)
        {
            ViewBag.Active = "MasterData";
            ViewBag.tglAwalPencarian = tglAwalPencarian.ToString("dd MMMM yyyy");
            ViewBag.tglAkhirPencarian = tglAkhirPencarian.ToString("dd MMMM yyyy");

            var data = _BankRepository.GetAllBank().Where(r => r.CreateDateTime.Date >= tglAwalPencarian && r.CreateDateTime.Date <= tglAkhirPencarian).ToList();
            return View(data);
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<ViewResult> CreateBank()
        {
            ViewBag.Active = "MasterData";
            var user = new BankViewModel();
            var dateNow = DateTimeOffset.Now;
            var setDateNow = DateTimeOffset.Now.ToString("yyMMdd");

            var lastCode = _BankRepository.GetAllBank().Where(d => d.CreateDateTime.ToString("yyMMdd") == dateNow.ToString("yyMMdd")).OrderByDescending(k => k.BankCode).FirstOrDefault();
            if (lastCode == null)
            {
                user.BankCode = "BNK" + setDateNow + "0001";
            }
            else
            {
                var lastCodeTrim = lastCode.BankCode.Substring(3, 6);

                if (lastCodeTrim != setDateNow)
                {
                    user.BankCode = "BNK" + setDateNow + "0001";
                }
                else
                {
                    user.BankCode = "BNK" + setDateNow + (Convert.ToInt32(lastCode.BankCode.Substring(9, lastCode.BankCode.Length - 9)) + 1).ToString("D4");
                }
            }

            return View(user);
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> CreateBank(BankViewModel vm)
        {
            var dateNow = DateTimeOffset.Now;
            var setDateNow = DateTimeOffset.Now.ToString("yyMMdd");

            var lastCode = _BankRepository.GetAllBank().Where(d => d.CreateDateTime.ToString("yyMMdd") == dateNow.ToString("yyMMdd")).OrderByDescending(k => k.BankCode).FirstOrDefault();
            if (lastCode == null)
            {
                vm.BankCode = "BNK" + setDateNow + "0001";
            }
            else
            {
                var lastCodeTrim = lastCode.BankCode.Substring(3, 6);

                if (lastCodeTrim != setDateNow)
                {
                    vm.BankCode = "BNK" + setDateNow + "0001";
                }
                else
                {
                    vm.BankCode = "BNK" + setDateNow + (Convert.ToInt32(lastCode.BankCode.Substring(9, lastCode.BankCode.Length - 9)) + 1).ToString("D4");
                }
            }

            var getUser = _userActiveRepository.GetAllUserLogin().Where(u => u.UserName == User.Identity.Name).FirstOrDefault();

            if (ModelState.IsValid)
            {
                var Bank = new Bank
                {
                    CreateDateTime = DateTime.Now,
                    CreateBy = new Guid(getUser.Id),
                    BankId = vm.BankId,
                    BankCode = vm.BankCode,
                    BankName = vm.BankName,
                    AccountNumber = vm.AccountNumber,
                    CardHolderName = vm.CardHolderName,
                    Note = vm.Note
                };

                var result = _BankRepository.GetAllBank().Where(c => c.BankName == vm.BankName).FirstOrDefault();
                if (result == null)
                {
                    _BankRepository.Tambah(Bank);
                    TempData["SuccessMessage"] = "Name " + vm.BankName + " Saved";
                    return RedirectToAction("Index", "Bank");
                }
                else
                {
                    TempData["WarningMessage"] = "Name " + vm.BankName + " Already Exist !!!";
                    return View(vm);
                }

            }

            return View();
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> DetailBank(Guid Id)
        {
            ViewBag.Active = "MasterData";
            var Bank = await _BankRepository.GetBankById(Id);

            if (Bank == null)
            {
                Response.StatusCode = 404;
                return View("UserNotFound", Id);
            }

            BankViewModel viewModel = new BankViewModel
            {
                BankId = Bank.BankId,
                BankCode = Bank.BankCode,
                BankName = Bank.BankName,
                AccountNumber = Bank.AccountNumber,
                CardHolderName = Bank.CardHolderName,
                Note = Bank.Note
            };
            return View(viewModel);
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> DetailBank(BankViewModel viewModel)
        {
            if (ModelState.IsValid)
            {
                var Bank = await _BankRepository.GetBankByIdNoTracking(viewModel.BankId);
                var getUser = _userActiveRepository.GetAllUserLogin().Where(u => u.UserName == User.Identity.Name).FirstOrDefault();
                var check = _BankRepository.GetAllBank().Where(d => d.BankCode == viewModel.BankCode).FirstOrDefault();

                if (check != null)
                {
                    Bank.UpdateDateTime = DateTime.Now;
                    Bank.UpdateBy = new Guid(getUser.Id);
                    Bank.BankCode = viewModel.BankCode;
                    Bank.BankName = viewModel.BankName;
                    Bank.AccountNumber = viewModel.AccountNumber;
                    Bank.CardHolderName = viewModel.CardHolderName;
                    Bank.Note = viewModel.Note;

                    _BankRepository.Update(Bank);
                    _applicationDbContext.SaveChanges();

                    TempData["SuccessMessage"] = "Name " + viewModel.BankName + " Success Changes";
                    return RedirectToAction("Index", "Bank");
                }
                else
                {
                    TempData["WarningMessage"] = "Name " + viewModel.BankName + " Already Exist !!!";
                    return View(viewModel);
                }
            }

            return View();
        }

        [HttpGet]
        [AllowAnonymous]
        //[Authorize]
        public async Task<IActionResult> DeleteBank(Guid Id)
        {
            ViewBag.Active = "MasterData";
            var Bank = await _BankRepository.GetBankById(Id);
            if (Bank == null)
            {
                Response.StatusCode = 404;
                return View("BankNotFound", Id);
            }

            BankViewModel vm = new BankViewModel
            {
                BankId = Bank.BankId,
                BankCode = Bank.BankCode,
                BankName = Bank.BankName,
            };
            return View(vm);
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> DeleteBank(BankViewModel vm)
        {
            //Hapus Data Profil
            var Bank = _applicationDbContext.Banks.FirstOrDefault(x => x.BankId == vm.BankId);
            _applicationDbContext.Attach(Bank);
            _applicationDbContext.Entry(Bank).State = EntityState.Deleted;
            _applicationDbContext.SaveChanges();

            TempData["SuccessMessage"] = "Name " + vm.BankName + " Success Deleted";
            return RedirectToAction("Index", "Bank");
        }
    }
}
