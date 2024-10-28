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
    public class MeasurementController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ApplicationDbContext _applicationDbContext;
        private readonly IUserActiveRepository _userActiveRepository;
        private readonly IMeasurementRepository _MeasurementRepository;
        private readonly IProductRepository _productRepository;

        private readonly IHostingEnvironment _hostingEnvironment;

        public MeasurementController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            ApplicationDbContext applicationDbContext,
            IMeasurementRepository MeasurementRepository,
            IUserActiveRepository userActiveRepository,
            IProductRepository productRepository,


            IHostingEnvironment hostingEnvironment
        )
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _applicationDbContext = applicationDbContext;
            _MeasurementRepository = MeasurementRepository;
            _userActiveRepository = userActiveRepository;
            _productRepository = productRepository;

            _hostingEnvironment = hostingEnvironment;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            ViewBag.Active = "MasterData";
            var data = _MeasurementRepository.GetAllMeasurement();
            return View(data);
        }

        [HttpPost]
        public async Task<IActionResult> Index(DateTime tglAwalPencarian, DateTime tglAkhirPencarian)
        {
            ViewBag.Active = "MasterData";
            ViewBag.tglAwalPencarian = tglAwalPencarian.ToString("dd MMMM yyyy");
            ViewBag.tglAkhirPencarian = tglAkhirPencarian.ToString("dd MMMM yyyy");

            var data = _MeasurementRepository.GetAllMeasurement().Where(r => r.CreateDateTime.Date >= tglAwalPencarian && r.CreateDateTime.Date <= tglAkhirPencarian).ToList();
            return View(data);
        }

        [HttpGet]
        public async Task<ViewResult> CreateMeasurement()
        {
            ViewBag.Active = "MasterData";
            var user = new MeasurementViewModel();
            var dateNow = DateTimeOffset.Now;
            var setDateNow = DateTimeOffset.Now.ToString("yyMMdd");

            var lastCode = _MeasurementRepository.GetAllMeasurement().Where(d => d.CreateDateTime.ToString("yyMMdd") == dateNow.ToString("yyMMdd")).OrderByDescending(k => k.MeasurementCode).FirstOrDefault();
            if (lastCode == null)
            {
                user.MeasurementCode = "MSR" + setDateNow + "0001";
            }
            else
            {
                var lastCodeTrim = lastCode.MeasurementCode.Substring(3, 6);

                if (lastCodeTrim != setDateNow)
                {
                    user.MeasurementCode = "MSR" + setDateNow + "0001";
                }
                else
                {
                    user.MeasurementCode = "MSR" + setDateNow + (Convert.ToInt32(lastCode.MeasurementCode.Substring(9, lastCode.MeasurementCode.Length - 9)) + 1).ToString("D4");
                }
            }

            return View(user);
        }

        [HttpPost]
        public async Task<IActionResult> CreateMeasurement(MeasurementViewModel vm)
        {
            var dateNow = DateTimeOffset.Now;
            var setDateNow = DateTimeOffset.Now.ToString("yyMMdd");

            var lastCode = _MeasurementRepository.GetAllMeasurement().Where(d => d.CreateDateTime.ToString("yyMMdd") == dateNow.ToString("yyMMdd")).OrderByDescending(k => k.MeasurementCode).FirstOrDefault();
            if (lastCode == null)
            {
                vm.MeasurementCode = "MSR" + setDateNow + "0001";
            }
            else
            {
                var lastCodeTrim = lastCode.MeasurementCode.Substring(3, 6);

                if (lastCodeTrim != setDateNow)
                {
                    vm.MeasurementCode = "MSR" + setDateNow + "0001";
                }
                else
                {
                    vm.MeasurementCode = "MSR" + setDateNow + (Convert.ToInt32(lastCode.MeasurementCode.Substring(9, lastCode.MeasurementCode.Length - 9)) + 1).ToString("D4");
                }
            }

            var getUser = _userActiveRepository.GetAllUserLogin().Where(u => u.UserName == User.Identity.Name).FirstOrDefault();

            if (ModelState.IsValid)
            {
                var Measurement = new Measurement
                {
                    CreateDateTime = DateTime.Now,
                    CreateBy = new Guid(getUser.Id),
                    MeasurementId = vm.MeasurementId,
                    MeasurementCode = vm.MeasurementCode,
                    MeasurementName = vm.MeasurementName,
                    Note = vm.Note
                };

                var checkDuplicate = _MeasurementRepository.GetAllMeasurement().Where(c => c.MeasurementName == vm.MeasurementName).ToList();

                if (checkDuplicate.Count == 0)
                {
                    var result = _MeasurementRepository.GetAllMeasurement().Where(c => c.MeasurementName == vm.MeasurementName).FirstOrDefault();
                    if (result == null)
                    {
                        _MeasurementRepository.Tambah(Measurement);
                        TempData["SuccessMessage"] = "Name " + vm.MeasurementName + " Saved";
                        return RedirectToAction("Index", "Measurement");
                    }
                    else
                    {
                        TempData["WarningMessage"] = "Name " + vm.MeasurementName + " Already Exist !!!";
                        return View(vm);
                    }
                }
                else
                {
                    TempData["WarningMessage"] = "Name " + vm.MeasurementName + " There is duplicate data !!!";
                    return View(vm);
                }
            }
            else
            {               
                return View(vm);
            }
        }

        [HttpGet]
        public async Task<IActionResult> DetailMeasurement(Guid Id)
        {
            ViewBag.Active = "MasterData";
            var Measurement = await _MeasurementRepository.GetMeasurementById(Id);

            if (Measurement == null)
            {
                Response.StatusCode = 404;
                return View("UserNotFound", Id);
            }

            MeasurementViewModel viewModel = new MeasurementViewModel
            {
                MeasurementId = Measurement.MeasurementId,
                MeasurementCode = Measurement.MeasurementCode,
                MeasurementName = Measurement.MeasurementName,
                Note = Measurement.Note
            };
            return View(viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> DetailMeasurement(MeasurementViewModel viewModel)
        {
            if (ModelState.IsValid)
            {
                var Measurement = await _MeasurementRepository.GetMeasurementByIdNoTracking(viewModel.MeasurementId);
                var getUser = _userActiveRepository.GetAllUserLogin().Where(u => u.UserName == User.Identity.Name).FirstOrDefault();
                var checkDuplicate = _MeasurementRepository.GetAllMeasurement().Where(d => d.MeasurementName == viewModel.MeasurementName).ToList();

                if (checkDuplicate.Count == 0 || checkDuplicate.Count == 1)
                {
                    var data = _MeasurementRepository.GetAllMeasurement().Where(d => d.MeasurementCode == viewModel.MeasurementCode).FirstOrDefault();

                    if (data != null)
                    {
                        Measurement.UpdateDateTime = DateTime.Now;
                        Measurement.UpdateBy = new Guid(getUser.Id);
                        Measurement.MeasurementCode = viewModel.MeasurementCode;
                        Measurement.MeasurementName = viewModel.MeasurementName;
                        Measurement.Note = viewModel.Note;

                        _MeasurementRepository.Update(Measurement);
                        _applicationDbContext.SaveChanges();

                        TempData["SuccessMessage"] = "Name " + viewModel.MeasurementName + " Success Changes";
                        return RedirectToAction("Index", "Measurement");
                    }
                    else
                    {
                        TempData["WarningMessage"] = "Name " + viewModel.MeasurementName + " Already Exist !!!";
                        return View(viewModel);
                    }
                }
                else
                {
                    TempData["WarningMessage"] = "Name " + viewModel.MeasurementName + " There is duplicate data !!!";
                    return View(viewModel);
                }
            } 
            else
            {
                return View(viewModel);
            }            
        }

        [HttpGet]
        public async Task<IActionResult> DeleteMeasurement(Guid Id)
        {
            ViewBag.Active = "MasterData";
            var Measurement = await _MeasurementRepository.GetMeasurementById(Id);
            if (Measurement == null)
            {
                Response.StatusCode = 404;
                return View("MeasurementNotFound", Id);
            }

            MeasurementViewModel vm = new MeasurementViewModel
            {
                MeasurementId = Measurement.MeasurementId,
                MeasurementCode = Measurement.MeasurementCode,
                MeasurementName = Measurement.MeasurementName,
            };
            return View(vm);
        }

        [HttpPost]
        public async Task<IActionResult> DeleteMeasurement(MeasurementViewModel vm)
        {
            //Cek Relasi Principal dengan Produk
            var produk = _productRepository.GetAllProduct().Where(p => p.MeasurementId == vm.MeasurementId).FirstOrDefault();
            if (produk == null)
            {
                //Hapus Data
                var Measurement = _applicationDbContext.Measurements.FirstOrDefault(x => x.MeasurementId == vm.MeasurementId);
                _applicationDbContext.Attach(Measurement);
                _applicationDbContext.Entry(Measurement).State = EntityState.Deleted;
                _applicationDbContext.SaveChanges();

                TempData["SuccessMessage"] = "Name " + vm.MeasurementName + " Success Deleted";
                return RedirectToAction("Index", "Measurement");
            }
            else {
                TempData["WarningMessage"] = "Sorry, " + vm.MeasurementName + " In used by the product !";
                return View(vm);
            }
                
        }
    }
}
