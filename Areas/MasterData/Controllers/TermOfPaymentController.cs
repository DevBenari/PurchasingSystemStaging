﻿using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PurchasingSystemStaging.Areas.MasterData.Models;
using PurchasingSystemStaging.Areas.MasterData.Repositories;
using PurchasingSystemStaging.Areas.MasterData.ViewModels;
using PurchasingSystemStaging.Data;
using PurchasingSystemStaging.Models;
using PurchasingSystemStaging.Repositories;
using IHostingEnvironment = Microsoft.AspNetCore.Hosting.IHostingEnvironment;

namespace PurchasingSystemStaging.Areas.MasterData.Controllers
{
    [Area("MasterData")]
    [Route("MasterData/[Controller]/[Action]")]
    public class TermOfPaymentController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ApplicationDbContext _applicationDbContext;
        private readonly IUserActiveRepository _userActiveRepository;
        private readonly ITermOfPaymentRepository _TermOfPaymentRepository;

        private readonly IHostingEnvironment _hostingEnvironment;

        public TermOfPaymentController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            ApplicationDbContext applicationDbContext,
            ITermOfPaymentRepository TermOfPaymentRepository,
            IUserActiveRepository userActiveRepository,

            IHostingEnvironment hostingEnvironment
        )
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _applicationDbContext = applicationDbContext;
            _TermOfPaymentRepository = TermOfPaymentRepository;
            _userActiveRepository = userActiveRepository;

            _hostingEnvironment = hostingEnvironment;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            ViewBag.Active = "MasterData";
            var data = _TermOfPaymentRepository.GetAllTermOfPayment();
            return View(data);
        }

        [HttpPost]
        public async Task<IActionResult> Index(DateTime? tglAwalPencarian, DateTime? tglAkhirPencarian, string filterOptions)
        {
            ViewBag.Active = "MasterData";
            
            var data = _TermOfPaymentRepository.GetAllTermOfPayment();          

            if (tglAwalPencarian.HasValue && tglAkhirPencarian.HasValue)
            {
                data = data.Where(u => u.CreateDateTime.Date >= tglAwalPencarian.Value.Date
                                    && u.CreateDateTime.Date <= tglAkhirPencarian.Value.Date);
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

        [HttpGet]
        public async Task<ViewResult> CreateTermOfPayment()
        {
            ViewBag.Active = "MasterData";
            var user = new TermOfPaymentViewModel();
            var dateNow = DateTimeOffset.Now;
            var setDateNow = DateTimeOffset.Now.ToString("yyMMdd");

            var lastCode = _TermOfPaymentRepository.GetAllTermOfPayment().Where(d => d.CreateDateTime.ToString("yyMMdd") == dateNow.ToString("yyMMdd")).OrderByDescending(k => k.TermOfPaymentCode).FirstOrDefault();
            if (lastCode == null)
            {
                user.TermOfPaymentCode = "TOP" + setDateNow + "0001";
            }
            else
            {
                var lastCodeTrim = lastCode.TermOfPaymentCode.Substring(3, 6);

                if (lastCodeTrim != setDateNow)
                {
                    user.TermOfPaymentCode = "TOP" + setDateNow + "0001";
                }
                else
                {
                    user.TermOfPaymentCode = "TOP" + setDateNow + (Convert.ToInt32(lastCode.TermOfPaymentCode.Substring(9, lastCode.TermOfPaymentCode.Length - 9)) + 1).ToString("D4");
                }
            }

            return View(user);
        }

        [HttpPost]
        public async Task<IActionResult> CreateTermOfPayment(TermOfPaymentViewModel vm)
        {
            var dateNow = DateTimeOffset.Now;
            var setDateNow = DateTimeOffset.Now.ToString("yyMMdd");

            var lastCode = _TermOfPaymentRepository.GetAllTermOfPayment().Where(d => d.CreateDateTime.ToString("yyMMdd") == dateNow.ToString("yyMMdd")).OrderByDescending(k => k.TermOfPaymentCode).FirstOrDefault();
            if (lastCode == null)
            {
                vm.TermOfPaymentCode = "TOP" + setDateNow + "0001";
            }
            else
            {
                var lastCodeTrim = lastCode.TermOfPaymentCode.Substring(3, 6);

                if (lastCodeTrim != setDateNow)
                {
                    vm.TermOfPaymentCode = "TOP" + setDateNow + "0001";
                }
                else
                {
                    vm.TermOfPaymentCode = "TOP" + setDateNow + (Convert.ToInt32(lastCode.TermOfPaymentCode.Substring(9, lastCode.TermOfPaymentCode.Length - 9)) + 1).ToString("D4");
                }
            }

            var getUser = _userActiveRepository.GetAllUserLogin().Where(u => u.UserName == User.Identity.Name).FirstOrDefault();

            if (ModelState.IsValid)
            {
                var TermOfPayment = new TermOfPayment
                {
                    CreateDateTime = DateTime.Now,
                    CreateBy = new Guid(getUser.Id),
                    TermOfPaymentId = vm.TermOfPaymentId,
                    TermOfPaymentCode = vm.TermOfPaymentCode,
                    TermOfPaymentName = vm.TermOfPaymentName,
                    Note = vm.Note
                };

                var checkDuplicate = _TermOfPaymentRepository.GetAllTermOfPayment().Where(c => c.TermOfPaymentName == vm.TermOfPaymentName).ToList();

                if (checkDuplicate.Count == 0)
                {
                    var result = _TermOfPaymentRepository.GetAllTermOfPayment().Where(c => c.TermOfPaymentName == vm.TermOfPaymentName).FirstOrDefault();
                    if (result == null)
                    {
                        _TermOfPaymentRepository.Tambah(TermOfPayment);
                        TempData["SuccessMessage"] = "Name " + vm.TermOfPaymentName + " Saved";
                        return RedirectToAction("Index", "TermOfPayment");
                    }
                    else
                    {
                        TempData["WarningMessage"] = "Name " + vm.TermOfPaymentName + " Already Exist !!!";
                        return View(vm);
                    }
                } 
                else 
                {
                    TempData["WarningMessage"] = "Name " + vm.TermOfPaymentName + " There is duplicate data !!!";
                    return View(vm);
                }
            }
            else
            {
                return View(vm);
            }
        }

        [HttpGet]
        public async Task<IActionResult> DetailTermOfPayment(Guid Id)
        {
            ViewBag.Active = "MasterData";
            var TermOfPayment = await _TermOfPaymentRepository.GetTermOfPaymentById(Id);

            if (TermOfPayment == null)
            {
                Response.StatusCode = 404;
                return View("UserNotFound", Id);
            }

            TermOfPaymentViewModel viewModel = new TermOfPaymentViewModel
            {
                TermOfPaymentId = TermOfPayment.TermOfPaymentId,
                TermOfPaymentCode = TermOfPayment.TermOfPaymentCode,
                TermOfPaymentName = TermOfPayment.TermOfPaymentName,
                Note = TermOfPayment.Note
            };
            return View(viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> DetailTermOfPayment(TermOfPaymentViewModel viewModel)
        {
            if (ModelState.IsValid)
            {
                var TermOfPayment = await _TermOfPaymentRepository.GetTermOfPaymentByIdNoTracking(viewModel.TermOfPaymentId);
                var getUser = _userActiveRepository.GetAllUserLogin().Where(u => u.UserName == User.Identity.Name).FirstOrDefault();
                var checkDuplicate = _TermOfPaymentRepository.GetAllTermOfPayment().Where(d => d.TermOfPaymentName == viewModel.TermOfPaymentName).ToList();

                if (checkDuplicate.Count == 0 || checkDuplicate.Count == 1)
                {
                    var data = _TermOfPaymentRepository.GetAllTermOfPayment().Where(d => d.TermOfPaymentCode == viewModel.TermOfPaymentCode).FirstOrDefault();

                    if (data != null)
                    {
                        TermOfPayment.UpdateDateTime = DateTime.Now;
                        TermOfPayment.UpdateBy = new Guid(getUser.Id);
                        TermOfPayment.TermOfPaymentCode = viewModel.TermOfPaymentCode;
                        TermOfPayment.TermOfPaymentName = viewModel.TermOfPaymentName;
                        TermOfPayment.Note = viewModel.Note;

                        _TermOfPaymentRepository.Update(TermOfPayment);
                        _applicationDbContext.SaveChanges();

                        TempData["SuccessMessage"] = "Name " + viewModel.TermOfPaymentName + " Success Changes";
                        return RedirectToAction("Index", "TermOfPayment");
                    }
                    else
                    {
                        TempData["WarningMessage"] = "Name " + viewModel.TermOfPaymentName + " Already Exist !!!";
                        return View(viewModel);
                    }
                }
                else
                {
                    TempData["WarningMessage"] = "Name " + viewModel.TermOfPaymentName + " There is duplicate data !!!";
                    return View(viewModel);
                }
            }
            else
            {                
                return View(viewModel);
            }
        }

        [HttpGet]
        public async Task<IActionResult> DeleteTermOfPayment(Guid Id)
        {
            ViewBag.Active = "MasterData";
            var TermOfPayment = await _TermOfPaymentRepository.GetTermOfPaymentById(Id);
            if (TermOfPayment == null)
            {
                Response.StatusCode = 404;
                return View("TermOfPaymentNotFound", Id);
            }

            TermOfPaymentViewModel vm = new TermOfPaymentViewModel
            {
                TermOfPaymentId = TermOfPayment.TermOfPaymentId,
                TermOfPaymentCode = TermOfPayment.TermOfPaymentCode,
                TermOfPaymentName = TermOfPayment.TermOfPaymentName,
            };
            return View(vm);
        }

        [HttpPost]
        public async Task<IActionResult> DeleteTermOfPayment(TermOfPaymentViewModel vm)
        {
            //Hapus Data Profil
            var TermOfPayment = _applicationDbContext.TermOfPayments.FirstOrDefault(x => x.TermOfPaymentId == vm.TermOfPaymentId);
            _applicationDbContext.Attach(TermOfPayment);
            _applicationDbContext.Entry(TermOfPayment).State = EntityState.Deleted;
            _applicationDbContext.SaveChanges();

            TempData["SuccessMessage"] = "Name " + vm.TermOfPaymentName + " Success Deleted";
            return RedirectToAction("Index", "TermOfPayment");
        }
    }
}
