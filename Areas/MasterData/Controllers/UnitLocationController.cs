﻿using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Data.SqlClient;
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
    public class UnitLocationController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ApplicationDbContext _applicationDbContext;
        private readonly IUserActiveRepository _userActiveRepository;
        private readonly IUnitLocationRepository _UnitLocationRepository;
        private readonly IProductRepository _productRepository;

        private readonly IDataProtector _protector;
        private readonly UrlMappingService _urlMappingService;
        private readonly IHostingEnvironment _hostingEnvironment;

        public UnitLocationController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            ApplicationDbContext applicationDbContext,
            IUnitLocationRepository UnitLocationRepository,
            IUserActiveRepository userActiveRepository,
            IProductRepository productRepository,

            IDataProtectionProvider provider,
            UrlMappingService urlMappingService,
            IHostingEnvironment hostingEnvironment
        )
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _applicationDbContext = applicationDbContext;
            _UnitLocationRepository = UnitLocationRepository;
            _userActiveRepository = userActiveRepository;
            _productRepository = productRepository;

            _protector = provider.CreateProtector("UrlProtector");
            _urlMappingService = urlMappingService;
            _hostingEnvironment = hostingEnvironment;
        }
        
        [HttpGet]
        [Authorize(Roles = "ReadUnitLocation")]
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

            var data = await _UnitLocationRepository.GetAllUnitLocationPageSize(searchTerm, page, pageSize, startDate, endDate);

            var model = new Pagination<UnitLocation>
            {
                Items = data.unitLocations,
                TotalCount = data.totalCountUnitLocations,
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
        [Authorize(Roles = "CreateUnitLocation")]
        public async Task<ViewResult> CreateUnitLocation()
        {
            ViewBag.Active = "MasterData";

            ViewBag.UnitManager = new SelectList(await _userActiveRepository.GetUserActives(), "UserActiveId", "FullName", SortOrder.Ascending);

            var user = new UnitLocationViewModel();
            var dateNow = DateTimeOffset.Now;
            var setDateNow = DateTimeOffset.Now.ToString("yyMMdd");

            var lastCode = _UnitLocationRepository.GetAllUnitLocation().Where(d => d.CreateDateTime.ToString("yyMMdd") == dateNow.ToString("yyMMdd")).OrderByDescending(k => k.UnitLocationCode).FirstOrDefault();
            if (lastCode == null)
            {
                user.UnitLocationCode = "UNT" + setDateNow + "0001";
            }
            else
            {
                var lastCodeTrim = lastCode.UnitLocationCode.Substring(3, 6);

                if (lastCodeTrim != setDateNow)
                {
                    user.UnitLocationCode = "UNT" + setDateNow + "0001";
                }
                else
                {
                    user.UnitLocationCode = "UNT" + setDateNow + (Convert.ToInt32(lastCode.UnitLocationCode.Substring(9, lastCode.UnitLocationCode.Length - 9)) + 1).ToString("D4");
                }
            }

            return View(user);
        }

        [HttpPost]
        [Authorize(Roles = "CreateUnitLocation")]
        public async Task<IActionResult> CreateUnitLocation(UnitLocationViewModel vm)
        {
            var dateNow = DateTimeOffset.Now;
            var setDateNow = DateTimeOffset.Now.ToString("yyMMdd");

            var lastCode = _UnitLocationRepository.GetAllUnitLocation().Where(d => d.CreateDateTime.ToString("yyMMdd") == dateNow.ToString("yyMMdd")).OrderByDescending(k => k.UnitLocationCode).FirstOrDefault();
            if (lastCode == null)
            {
                vm.UnitLocationCode = "UNT" + setDateNow + "0001";
            }
            else
            {
                var lastCodeTrim = lastCode.UnitLocationCode.Substring(3, 6);

                if (lastCodeTrim != setDateNow)
                {
                    vm.UnitLocationCode = "UNT" + setDateNow + "0001";
                }
                else
                {
                    vm.UnitLocationCode = "UNT" + setDateNow + (Convert.ToInt32(lastCode.UnitLocationCode.Substring(9, lastCode.UnitLocationCode.Length - 9)) + 1).ToString("D4");
                }
            }

            var getUser = _userActiveRepository.GetAllUserLogin().Where(u => u.UserName == User.Identity.Name).FirstOrDefault();

            if (ModelState.IsValid)
            {
                var UnitLocation = new UnitLocation
                {
                    CreateDateTime = DateTime.Now,
                    CreateBy = new Guid(getUser.Id),
                    UnitLocationId = vm.UnitLocationId,
                    UnitLocationCode = vm.UnitLocationCode,
                    UnitLocationName = vm.UnitLocationName,
                    UnitManagerId = vm.UnitManagerId,
                    Address = vm.Address
                };

                var checkDuplicate = _UnitLocationRepository.GetAllUnitLocation().Where(c => c.UnitLocationName == vm.UnitLocationName).ToList();

                if (checkDuplicate.Count == 0)
                {
                    var result = _UnitLocationRepository.GetAllUnitLocation().Where(c => c.UnitLocationName == vm.UnitLocationName).FirstOrDefault();
                    if (result == null)
                    {
                        _UnitLocationRepository.Tambah(UnitLocation);
                        TempData["SuccessMessage"] = "Name " + vm.UnitLocationName + " Saved";
                        return RedirectToAction("Index", "UnitLocation");
                    }
                    else
                    {
                        ViewBag.UnitManager = new SelectList(await _userActiveRepository.GetUserActives(), "UserActiveId", "FullName", SortOrder.Ascending);
                        TempData["WarningMessage"] = "Name " + vm.UnitLocationName + " Already Exist !!!";
                        return View(vm);
                    }
                }
                else
                {
                    ViewBag.UnitManager = new SelectList(await _userActiveRepository.GetUserActives(), "UserActiveId", "FullName", SortOrder.Ascending);
                    TempData["WarningMessage"] = "Name " + vm.UnitLocationName + " There is duplicate data !!!";
                    return View(vm);
                }
            }
            else
            {
                ViewBag.UnitManager = new SelectList(await _userActiveRepository.GetUserActives(), "UserActiveId", "FullName", SortOrder.Ascending);                
                return View(vm);
            }
        }        

        [HttpGet]
        [Authorize(Roles = "UpdateUnitLocation")]
        public async Task<IActionResult> DetailUnitLocation(Guid Id)
        {
            ViewBag.Active = "MasterData";

            ViewBag.UnitManager = new SelectList(await _userActiveRepository.GetUserActives(), "UserActiveId", "FullName", SortOrder.Ascending);

            var UnitLocation = await _UnitLocationRepository.GetUnitLocationById(Id);

            if (UnitLocation == null)
            {
                Response.StatusCode = 404;
                return View("UserNotFound", Id);
            }

            UnitLocationViewModel viewModel = new UnitLocationViewModel
            {
                UnitLocationId = UnitLocation.UnitLocationId,
                UnitLocationCode = UnitLocation.UnitLocationCode,
                UnitLocationName = UnitLocation.UnitLocationName,
                UnitManagerId = UnitLocation.UnitManagerId,
                Address = UnitLocation.Address
            };
            return View(viewModel);
        }

        [HttpPost]
        [Authorize(Roles = "UpdateUnitLocation")]
        public async Task<IActionResult> DetailUnitLocation(UnitLocationViewModel viewModel)
        {
            if (ModelState.IsValid)
            {
                var UnitLocation = await _UnitLocationRepository.GetUnitLocationByIdNoTracking(viewModel.UnitLocationId);
                var getUser = _userActiveRepository.GetAllUserLogin().Where(u => u.UserName == User.Identity.Name).FirstOrDefault();
                var checkDuplicate = _UnitLocationRepository.GetAllUnitLocation().Where(d => d.UnitLocationCode == viewModel.UnitLocationCode).ToList();

                if (checkDuplicate.Count == 0 || checkDuplicate.Count == 1)
                {
                    var check = _UnitLocationRepository.GetAllUnitLocation().Where(d => d.UnitLocationCode == viewModel.UnitLocationCode).FirstOrDefault();

                    if (check != null)
                    {
                        UnitLocation.UpdateDateTime = DateTime.Now;
                        UnitLocation.UpdateBy = new Guid(getUser.Id);
                        UnitLocation.UnitLocationCode = viewModel.UnitLocationCode;
                        UnitLocation.UnitLocationName = viewModel.UnitLocationName;
                        UnitLocation.UnitManagerId = viewModel.UnitManagerId;
                        UnitLocation.Address = viewModel.Address;

                        _UnitLocationRepository.Update(UnitLocation);
                        _applicationDbContext.SaveChanges();

                        TempData["SuccessMessage"] = "Name " + viewModel.UnitLocationName + " Success Changes";
                        return RedirectToAction("Index", "UnitLocation");
                    }
                    else
                    {
                        ViewBag.UnitManager = new SelectList(await _userActiveRepository.GetUserActives(), "UserActiveId", "FullName", SortOrder.Ascending);

                        TempData["WarningMessage"] = "Name " + viewModel.UnitLocationName + " Already Exist !!!";
                        return View(viewModel);
                    }
                }
                else
                {
                    ViewBag.UnitManager = new SelectList(await _userActiveRepository.GetUserActives(), "UserActiveId", "FullName", SortOrder.Ascending);

                    TempData["WarningMessage"] = "Name " + viewModel.UnitLocationName + " There is duplicate data !!!";
                    return View(viewModel);
                }
            }
            else
            {
                ViewBag.UnitManager = new SelectList(await _userActiveRepository.GetUserActives(), "UserActiveId", "FullName", SortOrder.Ascending);               
                return View(viewModel);
            }
        }

        [HttpGet]
        [Authorize(Roles = "DeleteUnitLocation")]
        public async Task<IActionResult> DeleteUnitLocation(Guid Id)
        {
            ViewBag.Active = "MasterData";
            var UnitLocation = await _UnitLocationRepository.GetUnitLocationById(Id);
            if (UnitLocation == null)
            {
                Response.StatusCode = 404;
                return View("UnitLocationNotFound", Id);
            }

            UnitLocationViewModel vm = new UnitLocationViewModel
            {
                UnitLocationId = UnitLocation.UnitLocationId,
                UnitLocationCode = UnitLocation.UnitLocationCode,
                UnitLocationName = UnitLocation.UnitLocationName,
            };
            return View(vm);
        }

        [HttpPost]
        [Authorize(Roles = "DeleteUnitLocation")]
        public async Task<IActionResult> DeleteUnitLocation(UnitLocationViewModel vm)
        {
            //Hapus Data
            var UnitLocation = _applicationDbContext.UnitLocations.FirstOrDefault(x => x.UnitLocationId == vm.UnitLocationId);
            _applicationDbContext.Attach(UnitLocation);
            _applicationDbContext.Entry(UnitLocation).State = EntityState.Deleted;
            _applicationDbContext.SaveChanges();

            TempData["SuccessMessage"] = "Name " + vm.UnitLocationName + " Success Deleted";
            return RedirectToAction("Index", "UnitLocation");
        }
    }
}
