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
using System.IO;
using System.Security.Cryptography;
using System.Text;
using IHostingEnvironment = Microsoft.AspNetCore.Hosting.IHostingEnvironment;

namespace PurchasingSystem.Areas.MasterData.Controllers
{
    [Area("MasterData")]
    [Route("MasterData/[Controller]/[Action]")]
    public class MeasurementController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ApplicationDbContext _applicationDbContext;
        private readonly IUserActiveRepository _userActiveRepository;
        private readonly IMeasurementRepository _measurementRepository;
        private readonly IProductRepository _productRepository;

        private readonly IDataProtector _protector;
        private readonly UrlMappingService _urlMappingService;
        private readonly IHostingEnvironment _hostingEnvironment;

        public MeasurementController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            ApplicationDbContext applicationDbContext,
            IMeasurementRepository MeasurementRepository,
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
            _measurementRepository = MeasurementRepository;
            _userActiveRepository = userActiveRepository;
            _productRepository = productRepository;

            _protector = provider.CreateProtector("UrlProtector");
            _urlMappingService = urlMappingService;
            _hostingEnvironment = hostingEnvironment;
        }

        
        [HttpGet]
        [Authorize(Roles = "ReadMeasurement")]
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

            var data = await _measurementRepository.GetAllMeasurementPageSize(searchTerm, page, pageSize, startDate, endDate);

            var model = new Pagination<Measurement>
            {
                Items = data.measurements,
                TotalCount = data.totalCountMeasurements,
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
        [Authorize(Roles = "CreateMeasurement")]
        public async Task<ViewResult> CreateMeasurement()
        {
            ViewBag.Active = "MasterData";
            var user = new MeasurementViewModel();
            var dateNow = DateTimeOffset.Now;
            var setDateNow = DateTimeOffset.Now.ToString("yyMMdd");

            var lastCode = _measurementRepository.GetAllMeasurement().Where(d => d.CreateDateTime.ToString("yyMMdd") == dateNow.ToString("yyMMdd")).OrderByDescending(k => k.MeasurementCode).FirstOrDefault();
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
        [Authorize(Roles = "CreateMeasurement")]
        public async Task<IActionResult> CreateMeasurement(MeasurementViewModel vm)
        {
            var dateNow = DateTimeOffset.Now;
            var setDateNow = DateTimeOffset.Now.ToString("yyMMdd");

            var lastCode = _measurementRepository.GetAllMeasurement().Where(d => d.CreateDateTime.ToString("yyMMdd") == dateNow.ToString("yyMMdd")).OrderByDescending(k => k.MeasurementCode).FirstOrDefault();
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

                var checkDuplicate = _measurementRepository.GetAllMeasurement().Where(c => c.MeasurementName == vm.MeasurementName).ToList();

                if (checkDuplicate.Count == 0)
                {
                    var result = _measurementRepository.GetAllMeasurement().Where(c => c.MeasurementName == vm.MeasurementName).FirstOrDefault();
                    if (result == null)
                    {
                        _measurementRepository.Tambah(Measurement);
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
        [Authorize(Roles = "UpdateMeasurement")]
        public async Task<IActionResult> DetailMeasurement(Guid Id)
        {
            ViewBag.Active = "MasterData";
            var Measurement = await _measurementRepository.GetMeasurementById(Id);

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
        [Authorize(Roles = "UpdateMeasurement")]
        public async Task<IActionResult> DetailMeasurement(MeasurementViewModel viewModel)
        {
            if (ModelState.IsValid)
            {
                var Measurement = await _measurementRepository.GetMeasurementByIdNoTracking(viewModel.MeasurementId);
                var getUser = _userActiveRepository.GetAllUserLogin().Where(u => u.UserName == User.Identity.Name).FirstOrDefault();
                var checkDuplicate = _measurementRepository.GetAllMeasurement().Where(d => d.MeasurementName == viewModel.MeasurementName).ToList();

                if (checkDuplicate.Count == 0 || checkDuplicate.Count == 1)
                {
                    var data = _measurementRepository.GetAllMeasurement().Where(d => d.MeasurementCode == viewModel.MeasurementCode).FirstOrDefault();

                    if (data != null)
                    {
                        Measurement.UpdateDateTime = DateTime.Now;
                        Measurement.UpdateBy = new Guid(getUser.Id);
                        Measurement.MeasurementCode = viewModel.MeasurementCode;
                        Measurement.MeasurementName = viewModel.MeasurementName;
                        Measurement.Note = viewModel.Note;

                        _measurementRepository.Update(Measurement);
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
        [Authorize(Roles = "DeleteMeasurement")]
        public async Task<IActionResult> DeleteMeasurement(Guid Id)
        {
            ViewBag.Active = "MasterData";
            var Measurement = await _measurementRepository.GetMeasurementById(Id);
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
        [Authorize(Roles = "DeleteMeasurement")]
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
