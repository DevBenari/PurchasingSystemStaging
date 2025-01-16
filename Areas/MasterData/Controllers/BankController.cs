using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PurchasingSystem.Areas.MasterData.Models;
using PurchasingSystem.Areas.MasterData.Repositories;
using PurchasingSystem.Areas.MasterData.ViewModels;
using PurchasingSystem.Data;
using PurchasingSystem.Models;
using PurchasingSystem.Repositories;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using IHostingEnvironment = Microsoft.AspNetCore.Hosting.IHostingEnvironment;

namespace PurchasingSystem.Areas.MasterData.Controllers
{
    [Area("MasterData")]
    [Route("MasterData/[Controller]/[Action]")]
    public class BankController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ApplicationDbContext _applicationDbContext;
        private readonly IUserActiveRepository _userActiveRepository;
        private readonly IBankRepository _bankRepository;

        private readonly IDataProtector _protector;
        private readonly UrlMappingService _urlMappingService;
        private readonly IHostingEnvironment _hostingEnvironment;

        public BankController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            ApplicationDbContext applicationDbContext,
            IBankRepository BankRepository,
            IUserActiveRepository userActiveRepository,

            IDataProtectionProvider provider,
            UrlMappingService urlMappingService,
            IHostingEnvironment hostingEnvironment
        )
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _applicationDbContext = applicationDbContext;
            _bankRepository = BankRepository;
            _userActiveRepository = userActiveRepository;

            _protector = provider.CreateProtector("UrlProtector");
            _urlMappingService = urlMappingService;
            _hostingEnvironment = hostingEnvironment;
        }       
        
        [Authorize(Roles = "ReadBank")]
        public async Task<IActionResult> Index(string filterOptions = "", string searchTerm = "", DateTimeOffset? startDate = null, DateTimeOffset? endDate = null, int page = 1, int pageSize = 10)
        {
            ViewBag.Active = "MasterData";
            ViewBag.SearchTerm = searchTerm;
            ViewBag.SelectedFilter = filterOptions;

            // Format tanggal untuk input[type="date"]
            ViewBag.StartDate = startDate?.ToString("yyyy-MM-dd");
            ViewBag.EndDate = endDate?.ToString("yyyy-MM-dd");

            // Format tanggal untuk tampilan (Indonesia)
            ViewBag.StartDateReadable = startDate?.ToString("dd MMMM yyyy");
            ViewBag.EndDateReadable = endDate?.ToString("dd MMMM yyyy");

            // Normalisasi tanggal untuk mengabaikan waktu
            if (startDate.HasValue) startDate = startDate.Value.Date;
            if (endDate.HasValue) endDate = endDate.Value.Date.AddDays(1).AddTicks(-1); // Sampai akhir hari

            // Tentukan range tanggal berdasarkan filterOptions
            if (!string.IsNullOrEmpty(filterOptions))
            {
                (startDate, endDate) = GetDateRangeHelper.GetDateRange(filterOptions);
            }

            var data = await _bankRepository.GetAllBankPageSize(searchTerm, page, pageSize, startDate, endDate);

            var model = new Pagination<Bank>
            {
                Items = data.banks,
                TotalCount = data.totalCountBanks,
                PageSize = pageSize,
                CurrentPage = page,
            };

            // Sertakan semua parameter untuk pagination
            ViewBag.FilterOptions = filterOptions;
            ViewBag.StartDateParam = startDate?.ToString("yyyy-MM-dd");
            ViewBag.EndDateParam = endDate?.ToString("yyyy-MM-dd");
            ViewBag.PageSize = pageSize;

            return View(model);
        }

        [Authorize(Roles = "CreateBank")]
        public async Task<ViewResult> CreateBank()
        {
            ViewBag.Active = "MasterData";
            var user = new BankViewModel();
            var dateNow = DateTimeOffset.Now;
            var setDateNow = DateTimeOffset.Now.ToString("yyMMdd");

            var lastCode = _bankRepository.GetAllBank().Where(d => d.CreateDateTime.ToString("yyMMdd") == dateNow.ToString("yyMMdd")).OrderByDescending(k => k.BankCode).FirstOrDefault();
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
        [Authorize(Roles = "CreateBank")]
        public async Task<IActionResult> CreateBank(BankViewModel vm)
        {
            var dateNow = DateTimeOffset.Now;
            var setDateNow = DateTimeOffset.Now.ToString("yyMMdd");

            var lastCode = _bankRepository.GetAllBank().Where(d => d.CreateDateTime.ToString("yyMMdd") == dateNow.ToString("yyMMdd")).OrderByDescending(k => k.BankCode).FirstOrDefault();
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

                var checkDuplicate = _bankRepository.GetAllBank().Where(c => c.BankName == vm.BankName).ToList();

                if (checkDuplicate.Count == 0)
                {
                    var result = _bankRepository.GetAllBank().Where(c => c.BankName == vm.BankName).FirstOrDefault();
                    if (result == null)
                    {
                        _bankRepository.Tambah(Bank);
                        TempData["SuccessMessage"] = "Name " + vm.BankName + " Saved";
                        return RedirectToAction("Index", "Bank", new { area = "MasterData" });
                    }
                    else
                    {
                        TempData["WarningMessage"] = "Name " + vm.BankName + " Already Exist !!!";
                        return View(vm);
                    }
                }
                else 
                {
                    TempData["WarningMessage"] = "Name " + vm.BankName + " There is duplicate data !!!";
                    return View(vm);
                }
            } 
            else
            {
                return View(vm);
            }            
        }

        [Authorize(Roles = "UpdateBank")]
        public async Task<IActionResult> DetailBank(Guid Id)
        {
            ViewBag.Active = "MasterData";
            var Bank = await _bankRepository.GetBankById(Id);

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
        [Authorize(Roles = "UpdateBank")]
        public async Task<IActionResult> DetailBank(BankViewModel viewModel)
        {
            if (ModelState.IsValid)
            {
                var Bank = await _bankRepository.GetBankByIdNoTracking(viewModel.BankId);
                var getUser = _userActiveRepository.GetAllUserLogin().Where(u => u.UserName == User.Identity.Name).FirstOrDefault();
                var checkDuplicate = _bankRepository.GetAllBank().Where(d => d.BankName == viewModel.BankName).ToList();

                if (checkDuplicate.Count == 0 || checkDuplicate.Count == 1)
                {
                    var data = _bankRepository.GetAllBank().Where(d => d.BankCode == viewModel.BankCode).FirstOrDefault();

                    if (data != null)
                    {
                        Bank.UpdateDateTime = DateTime.Now;
                        Bank.UpdateBy = new Guid(getUser.Id);
                        Bank.BankCode = viewModel.BankCode;
                        Bank.BankName = viewModel.BankName;
                        Bank.AccountNumber = viewModel.AccountNumber;
                        Bank.CardHolderName = viewModel.CardHolderName;
                        Bank.Note = viewModel.Note;

                        _bankRepository.Update(Bank);
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
                else 
                {
                    TempData["WarningMessage"] = "Name " + viewModel.BankName + " There is duplicate data !!!";
                    return View(viewModel);
                }
            }
            else
            {                
                return View(viewModel);
            }
        }

        [Authorize(Roles = "DeleteBank")]
        public async Task<IActionResult> DeleteBank(Guid Id)
        {
            ViewBag.Active = "MasterData";
            var Bank = await _bankRepository.GetBankById(Id);
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
        [Authorize(Roles = "DeleteBank")]
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
