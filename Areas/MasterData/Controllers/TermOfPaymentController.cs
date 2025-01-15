﻿using Microsoft.AspNetCore.Authorization;
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
using System.Security.Cryptography;
using System.Text;
using IHostingEnvironment = Microsoft.AspNetCore.Hosting.IHostingEnvironment;

namespace PurchasingSystem.Areas.MasterData.Controllers
{
    [Area("MasterData")]
    [Route("MasterData/[Controller]/[Action]")]
    public class TermOfPaymentController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ApplicationDbContext _applicationDbContext;
        private readonly IUserActiveRepository _userActiveRepository;
        private readonly ITermOfPaymentRepository _termOfPaymentRepository;

        private readonly IDataProtector _protector;
        private readonly UrlMappingService _urlMappingService;
        private readonly IHostingEnvironment _hostingEnvironment;

        public TermOfPaymentController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            ApplicationDbContext applicationDbContext,
            ITermOfPaymentRepository TermOfPaymentRepository,
            IUserActiveRepository userActiveRepository,

            IDataProtectionProvider provider,
            UrlMappingService urlMappingService,
            IHostingEnvironment hostingEnvironment
        )
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _applicationDbContext = applicationDbContext;
            _termOfPaymentRepository = TermOfPaymentRepository;
            _userActiveRepository = userActiveRepository;

            _protector = provider.CreateProtector("UrlProtector");
            _urlMappingService = urlMappingService;
            _hostingEnvironment = hostingEnvironment;
        }        

        [HttpGet]
        [Authorize(Roles = "ReadTermOfPayment")]
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

            var data = await _termOfPaymentRepository.GetAllTermOfPaymentPageSize(searchTerm, page, pageSize, startDate, endDate);

            var model = new Pagination<TermOfPayment>
            {
                Items = data.termOfPayments,
                TotalCount = data.totalCountTermOfPayments,
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

        [HttpGet]
        [Authorize(Roles = "CreateTermOfPayment")]
        public async Task<ViewResult> CreateTermOfPayment()
        {
            ViewBag.Active = "MasterData";
            var user = new TermOfPaymentViewModel();
            var dateNow = DateTimeOffset.Now;
            var setDateNow = DateTimeOffset.Now.ToString("yyMMdd");

            var lastCode = _termOfPaymentRepository.GetAllTermOfPayment().Where(d => d.CreateDateTime.ToString("yyMMdd") == dateNow.ToString("yyMMdd")).OrderByDescending(k => k.TermOfPaymentCode).FirstOrDefault();
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
        [Authorize(Roles = "CreateTermOfPayment")]
        public async Task<IActionResult> CreateTermOfPayment(TermOfPaymentViewModel vm)
        {
            var dateNow = DateTimeOffset.Now;
            var setDateNow = DateTimeOffset.Now.ToString("yyMMdd");

            var lastCode = _termOfPaymentRepository.GetAllTermOfPayment().Where(d => d.CreateDateTime.ToString("yyMMdd") == dateNow.ToString("yyMMdd")).OrderByDescending(k => k.TermOfPaymentCode).FirstOrDefault();
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

                var checkDuplicate = _termOfPaymentRepository.GetAllTermOfPayment().Where(c => c.TermOfPaymentName == vm.TermOfPaymentName).ToList();

                if (checkDuplicate.Count == 0)
                {
                    var result = _termOfPaymentRepository.GetAllTermOfPayment().Where(c => c.TermOfPaymentName == vm.TermOfPaymentName).FirstOrDefault();
                    if (result == null)
                    {
                        _termOfPaymentRepository.Tambah(TermOfPayment);
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
        [Authorize(Roles = "UpdateTermOfPayment")]
        public async Task<IActionResult> DetailTermOfPayment(Guid Id)
        {
            ViewBag.Active = "MasterData";
            var TermOfPayment = await _termOfPaymentRepository.GetTermOfPaymentById(Id);

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
        [Authorize(Roles = "UpdateTermOfPayment")]
        public async Task<IActionResult> DetailTermOfPayment(TermOfPaymentViewModel viewModel)
        {
            if (ModelState.IsValid)
            {
                var TermOfPayment = await _termOfPaymentRepository.GetTermOfPaymentByIdNoTracking(viewModel.TermOfPaymentId);
                var getUser = _userActiveRepository.GetAllUserLogin().Where(u => u.UserName == User.Identity.Name).FirstOrDefault();
                var checkDuplicate = _termOfPaymentRepository.GetAllTermOfPayment().Where(d => d.TermOfPaymentName == viewModel.TermOfPaymentName).ToList();

                if (checkDuplicate.Count == 0 || checkDuplicate.Count == 1)
                {
                    var data = _termOfPaymentRepository.GetAllTermOfPayment().Where(d => d.TermOfPaymentCode == viewModel.TermOfPaymentCode).FirstOrDefault();

                    if (data != null)
                    {
                        TermOfPayment.UpdateDateTime = DateTime.Now;
                        TermOfPayment.UpdateBy = new Guid(getUser.Id);
                        TermOfPayment.TermOfPaymentCode = viewModel.TermOfPaymentCode;
                        TermOfPayment.TermOfPaymentName = viewModel.TermOfPaymentName;
                        TermOfPayment.Note = viewModel.Note;

                        _termOfPaymentRepository.Update(TermOfPayment);
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
        [Authorize(Roles = "DeleteTermOfPayment")]
        public async Task<IActionResult> DeleteTermOfPayment(Guid Id)
        {
            ViewBag.Active = "MasterData";
            var TermOfPayment = await _termOfPaymentRepository.GetTermOfPaymentById(Id);
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
        [Authorize(Roles = "DeleteTermOfPayment")]
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
