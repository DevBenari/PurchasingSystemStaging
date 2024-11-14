﻿using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PurchasingSystemStaging.Areas.MasterData.Models;
using PurchasingSystemStaging.Areas.MasterData.Repositories;
using PurchasingSystemStaging.Areas.MasterData.ViewModels;
using PurchasingSystemStaging.Data;
using PurchasingSystemStaging.Models;
using PurchasingSystemStaging.Repositories;
using System.Reflection;
using IHostingEnvironment = Microsoft.AspNetCore.Hosting.IHostingEnvironment;

namespace PurchasingSystemStaging.Areas.MasterData.Controllers
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

        private readonly IHostingEnvironment _hostingEnvironment;

        public BankController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            ApplicationDbContext applicationDbContext,
            IBankRepository BankRepository,
            IUserActiveRepository userActiveRepository,
            IDataProtectionProvider provider,

            IHostingEnvironment hostingEnvironment
        )
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _applicationDbContext = applicationDbContext;
            _bankRepository = BankRepository;
            _userActiveRepository = userActiveRepository;
            _protector = provider.CreateProtector("UrlProtector");

            _hostingEnvironment = hostingEnvironment;
        }

        public IActionResult RedirectToIndex()
        {
            // Enkripsi path URL untuk "Index"
            string originalPath = "MasterData/Bank/Index";
            string encryptedPath = _protector.Protect(originalPath);

            // Redirect ke URL terenkripsi
            return Redirect("/" + encryptedPath);
        }

        [HttpGet]
        //[Authorize(Roles = "IndexBank")]
        public async Task<IActionResult> Index()
        {
            ViewBag.Active = "MasterData";
            var data = _bankRepository.GetAllBank();
            return View(data);
        }

        [HttpPost]
        public async Task<IActionResult> Index(DateTime? tglAwalPencarian, DateTime? tglAkhirPencarian, string filterOptions)
        {
            ViewBag.Active = "MasterData";

            var data = _bankRepository.GetAllBank();

            if (tglAwalPencarian.HasValue && tglAkhirPencarian.HasValue)
            {
                data = data.Where(u => u.CreateDateTime.Date >= tglAwalPencarian.Value.Date &&
                                       u.CreateDateTime.Date <= tglAkhirPencarian.Value.Date);
            }
            else if (!string.IsNullOrEmpty(filterOptions))
            {
                var today = DateTime.Today;
                switch (filterOptions)
                {
                    case "Today":
                        data = data.Where(u => u.CreateDateTime.Date == today);
                        break;
                    case "Last Day":
                        data = data.Where(x => x.CreateDateTime.Date == today.AddDays(-1));
                        break;

                    case "Last 7 Days":
                        var last7Days = today.AddDays(-7);
                        data = data.Where(x => x.CreateDateTime.Date >= last7Days && x.CreateDateTime.Date <= today);
                        break;

                    case "Last 30 Days":
                        var last30Days = today.AddDays(-30);
                        data = data.Where(x => x.CreateDateTime.Date >= last30Days && x.CreateDateTime.Date <= today);
                        break;

                    case "This Month":
                        var firstDayOfMonth = new DateTime(today.Year, today.Month, 1);
                        data = data.Where(x => x.CreateDateTime.Date >= firstDayOfMonth && x.CreateDateTime.Date <= today);
                        break;

                    case "Last Month":
                        var firstDayOfLastMonth = today.AddMonths(-1).Date.AddDays(-(today.Day - 1));
                        var lastDayOfLastMonth = today.Date.AddDays(-today.Day);
                        data = data.Where(x => x.CreateDateTime.Date >= firstDayOfLastMonth && x.CreateDateTime.Date <= lastDayOfLastMonth);
                        break;
                    default:
                        break;
                }
            }

            ViewBag.tglAwalPencarian = tglAwalPencarian?.ToString("dd MMMM yyyy");
            ViewBag.tglAkhirPencarian = tglAkhirPencarian?.ToString("dd MMMM yyyy");
            ViewBag.SelectedFilter = filterOptions;
            return View(data);
        }

        public IActionResult RedirectToCreate()
        {
            // Enkripsi path URL untuk "Index"
            string originalPath = "MasterData/Bank/CreateBank";
            string encryptedPath = _protector.Protect(originalPath);

            // Redirect ke URL terenkripsi
            return Redirect("/" + encryptedPath);
        }

        [HttpGet]
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
                        return RedirectToAction("RedirectToIndex", "Bank");
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
        public IActionResult RedirectToDetail(Guid Id)
        {
            // Enkripsi path URL untuk "Index"
            string originalPath = $"MasterData/Bank/DetailBank/{Id}";
            string encryptedPath = _protector.Protect(originalPath);

            // Redirect ke URL terenkripsi
            return Redirect("/" + encryptedPath);
        }

        [HttpGet]
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
                        return RedirectToAction("RedirectToIndex", "Bank");
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

        public IActionResult RedirectToDelete(Guid Id)
        {
            // Enkripsi path URL untuk "Index"
            string originalPath = $"MasterData/Bank/DeleteBank/{Id}";
            string encryptedPath = _protector.Protect(originalPath);

            // Redirect ke URL terenkripsi
            return Redirect("/" + encryptedPath);
        }

        [HttpGet]
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
        public async Task<IActionResult> DeleteBank(BankViewModel vm)
        {
            //Hapus Data Profil
            var Bank = _applicationDbContext.Banks.FirstOrDefault(x => x.BankId == vm.BankId);
            _applicationDbContext.Attach(Bank);
            _applicationDbContext.Entry(Bank).State = EntityState.Deleted;
            _applicationDbContext.SaveChanges();

            TempData["SuccessMessage"] = "Name " + vm.BankName + " Success Deleted";
            return RedirectToAction("RedirectToIndex", "Bank");
        }
    }
}
