using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using PurchasingSystem.Areas.MasterData.Repositories;
using PurchasingSystem.Areas.Order.Repositories;
using PurchasingSystem.Areas.Warehouse.Models;
using PurchasingSystem.Areas.Warehouse.Repositories;
using PurchasingSystem.Areas.Warehouse.ViewModels;
using PurchasingSystem.Data;
using PurchasingSystem.Hubs;
using PurchasingSystem.Models;
using PurchasingSystem.Repositories;
using System.Data.SqlClient;

namespace PurchasingSystem.Areas.Warehouse.Controllers
{
    [Area("Warehouse")]
    [Route("Warehouse/[Controller]/[Action]")]
    public class ApprovalProductReturnController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ApplicationDbContext _applicationDbContext;
        private readonly IUserActiveRepository _userActiveRepository;
        private readonly IApprovalProductReturnRepository _approvalRepository;
        private readonly IProductRepository _productRepository;
        private readonly IProductReturnRepository _productReturnRepository;

        public ApprovalProductReturnController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            ApplicationDbContext applicationDbContext,
            IApprovalProductReturnRepository ApprovalRepository,
            IUserActiveRepository userActiveRepository,
            IProductRepository productRepository,
            IProductReturnRepository productReturnRepository
        )
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _applicationDbContext = applicationDbContext;
            _approvalRepository = ApprovalRepository;
            _userActiveRepository = userActiveRepository;
            _productRepository = productRepository;
            _productReturnRepository = productReturnRepository;            
        }
        [HttpGet]
        [Authorize(Roles = "ReadApprovalProductReturn")]
        public async Task<IActionResult> Index(string filterOptions = "", string searchTerm = "", DateTimeOffset? startDate = null, DateTimeOffset? endDate = null, int page = 1, int pageSize = 10)
        {
            ViewBag.Active = "ApprovalProductReturn";
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

            var getUserLogin = _userActiveRepository.GetAllUserLogin().Where(u => u.UserName == User.Identity.Name).FirstOrDefault();

            if (getUserLogin.Email == "superadmin@admin.com")
            {
                var data = await _approvalRepository.GetAllApprovalPageSize(searchTerm, page, pageSize, startDate, endDate);                

                var model = new Pagination<ApprovalProductReturn>
                {
                    Items = data.ApprovalProductReturns,
                    TotalCount = data.totalCountApprovalProductReturns,
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
            else
            {
                var getUserActive = _userActiveRepository.GetAllUser().Where(c => c.UserActiveCode == getUserLogin.KodeUser).FirstOrDefault();
                var getUser1 = _approvalRepository.GetAllApproval().Where(a => a.ApprovalStatusUser == "User1" && a.UserApproveId == getUserActive.UserActiveId && a.Status == "Waiting Approval").ToList();
                var getUser2 = _approvalRepository.GetAllApproval().Where(a => a.ApprovalStatusUser == "User2" && a.UserApproveId == getUserActive.UserActiveId && a.Status == "User1Approve").ToList();
                var getUser3 = _approvalRepository.GetAllApproval().Where(a => a.ApprovalStatusUser == "User3" && a.UserApproveId == getUserActive.UserActiveId && a.Status == "User2Approve").ToList();

                var getUser1Approve = _approvalRepository.GetAllApproval().Where(a => a.ApprovalStatusUser == "User1" && a.UserApproveId == getUserActive.UserActiveId && a.Status == "Approve").ToList();
                var getUser2Approve = _approvalRepository.GetAllApproval().Where(a => a.ApprovalStatusUser == "User2" && a.UserApproveId == getUserActive.UserActiveId && a.Status == "Approve").ToList();
                var getUser3Approve = _approvalRepository.GetAllApproval().Where(a => a.ApprovalStatusUser == "User3" && a.UserApproveId == getUserActive.UserActiveId && a.Status == "Approve").ToList();

                var itemList = new List<ApprovalProductReturn>();

                if (getUser1 != null && getUser2 != null && getUser3 != null)
                {
                    itemList.AddRange(getUser1);
                    itemList.AddRange(getUser2);
                    itemList.AddRange(getUser3);

                    itemList.AddRange(getUser1Approve);
                    itemList.AddRange(getUser2Approve);
                    itemList.AddRange(getUser3Approve);                    

                    var model = new Pagination<ApprovalProductReturn>
                    {
                        Items = itemList,
                        TotalCount = itemList.Count,
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
                else if (getUser1 != null && getUser2 != null)
                {
                    itemList.AddRange(getUser1);
                    itemList.AddRange(getUser2);

                    itemList.AddRange(getUser1Approve);
                    itemList.AddRange(getUser2Approve);
                    
                    var model = new Pagination<ApprovalProductReturn>
                    {
                        Items = itemList,
                        TotalCount = itemList.Count,
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
                else if (getUser1 != null)
                {
                    itemList.AddRange(getUser1);

                    itemList.AddRange(getUser1Approve);                    

                    var model = new Pagination<ApprovalProductReturn>
                    {
                        Items = itemList,
                        TotalCount = itemList.Count,
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
            }
            return View();
        }

        [HttpGet]
        [Authorize(Roles = "UpdateApprovalProductReturn")]
        public async Task<ViewResult> DetailApprovalProductReturn(Guid Id)
        {
            ViewBag.Active = "ApprovalProductReturn";

            ViewBag.User = new SelectList(_userManager.Users, nameof(ApplicationUser.Id), nameof(ApplicationUser.NamaUser), SortOrder.Ascending);
            ViewBag.Product = new SelectList(await _productRepository.GetProducts(), "ProductId", "ProductName", SortOrder.Ascending);
            ViewBag.Approval = new SelectList(await _userActiveRepository.GetUserActives(), "UserActiveId", "FullName", SortOrder.Ascending);            

            var Approval = await _approvalRepository.GetApprovalById(Id);
            var getUser = _userActiveRepository.GetAllUserLogin().Where(u => u.UserName == User.Identity.Name).FirstOrDefault();

            if (Approval == null)
            {
                Response.StatusCode = 404;
                return View("ApprovalNotFound", Id);
            }

            ApprovalProductReturnViewModel viewModel = new ApprovalProductReturnViewModel()
            {
                ApprovalProductReturnId = Approval.ApprovalProductReturnId,
                ProductReturnId = Approval.ProductReturnId,
                ProductReturnNumber = Approval.ProductReturnNumber,
                UserAccessId = Approval.UserAccessId,                
                UserApproveId = Approval.UserApproveId,
                UserApprove = getUser.Email,
                ApprovalTime = "",
                ApproveBy = getUser.NamaUser,
                ApprovalDate = DateTime.Now,
                ApprovalStatusUser = Approval.ApprovalStatusUser,
                Status = Approval.Status,
                Note = Approval.Note
            };

            var getPrNumber = _productReturnRepository.GetAllProductReturn().Where(pr => pr.ProductReturnNumber == viewModel.ProductReturnNumber).FirstOrDefault();
            
            var ItemsList = new List<ProductReturnDetail>();

            foreach (var item in getPrNumber.ProductReturnDetails)
            {
                ItemsList.Add(new ProductReturnDetail
                {
                    ProductNumber = item.ProductNumber,
                    ProductName = item.ProductName,
                    Supplier = item.Supplier,
                    Measurement = item.Measurement,
                    WarehouseOrigin = item.WarehouseOrigin,
                    WarehouseExpired = item.WarehouseExpired,
                    Qty = item.Qty,
                    Price = Math.Truncate(item.Price),
                    Discount = item.Discount,
                    SubTotal = Math.Truncate(item.SubTotal)
                });
            }

            viewModel.ProductReturnDetails = ItemsList;

            return View(viewModel);
        }

        [HttpPost]
        [Authorize(Roles = "UpdateApprovalProductReturn")]
        public async Task<IActionResult> DetailApprovalProductReturn(ApprovalProductReturnViewModel viewModel)
        {
            ViewBag.Active = "ApprovalProductReturn";

            if (ModelState.IsValid)
            {
                var getUser = _userActiveRepository.GetAllUserLogin().Where(u => u.UserName == User.Identity.Name).FirstOrDefault();

                var approval = await _approvalRepository.GetApprovalByIdNoTracking(viewModel.ApprovalProductReturnId);
                var checkPR = _productReturnRepository.GetAllProductReturn().Where(c => c.ProductReturnNumber == viewModel.ProductReturnNumber).FirstOrDefault();
                var diffDate = DateTimeOffset.Now.Date - approval.CreateDateTime.Date;

                if (checkPR != null)
                {
                    if (approval.ApprovalStatusUser == "User1" && checkPR.ProductReturnNumber == approval.ProductReturnNumber)
                    {
                        var updateStatusUser1 = _approvalRepository.GetAllApproval().Where(c => c.ApprovalStatusUser == "User1" && c.ProductReturnId == viewModel.ProductReturnId).FirstOrDefault();
                        if (updateStatusUser1.Status == "Waiting Approval")
                        {
                            updateStatusUser1.Status = viewModel.Status;
                            updateStatusUser1.ApprovalDate = DateTimeOffset.Now;
                            updateStatusUser1.ApproveBy = getUser.NamaUser;
                            updateStatusUser1.ApprovalTime = diffDate.Days.ToString() + " Day";
                            updateStatusUser1.Message = viewModel.Message;

                            _applicationDbContext.Entry(updateStatusUser1).State = EntityState.Modified;
                            _applicationDbContext.SaveChanges();
                        }

                        if (viewModel.Status == "Approve")
                        {
                            var updateStatusUser2 = _approvalRepository.GetAllApproval().Where(c => c.ApprovalStatusUser == "User2" && c.ProductReturnId == viewModel.ProductReturnId).FirstOrDefault();
                            if (updateStatusUser2 != null)
                            {
                                updateStatusUser2.Status = "User1Approve";

                                _applicationDbContext.Entry(updateStatusUser2).State = EntityState.Modified;
                                _applicationDbContext.SaveChanges();
                            }

                            checkPR.ApproveStatusUser1 = viewModel.Status;
                            checkPR.MessageApprove1 = viewModel.Message;

                            _applicationDbContext.Entry(checkPR).State = EntityState.Modified;
                            _applicationDbContext.SaveChanges();
                        }
                        else
                        {
                            checkPR.ApproveStatusUser1 = viewModel.Status;
                            checkPR.MessageApprove1 = viewModel.Message;

                            _applicationDbContext.Entry(checkPR).State = EntityState.Modified;
                            _applicationDbContext.SaveChanges();
                        }
                    }
                    else if (approval.ApprovalStatusUser == "User2" && checkPR.ProductReturnNumber == approval.ProductReturnNumber)
                    {
                        var updateStatusUser2 = _approvalRepository.GetAllApproval().Where(c => c.ApprovalStatusUser == "User2" && c.ProductReturnId == viewModel.ProductReturnId).FirstOrDefault();
                        if (updateStatusUser2.Status == "User1Approve")
                        {
                            updateStatusUser2.Status = viewModel.Status;
                            updateStatusUser2.ApprovalDate = DateTimeOffset.Now;
                            updateStatusUser2.ApproveBy = getUser.NamaUser;
                            updateStatusUser2.ApprovalTime = diffDate.Days.ToString() + " Day";
                            updateStatusUser2.Message = viewModel.Message;

                            _applicationDbContext.Entry(updateStatusUser2).State = EntityState.Modified;
                            _applicationDbContext.SaveChanges();
                        }

                        if (viewModel.Status == "Approve")
                        {
                            var updateStatusUser3 = _approvalRepository.GetAllApproval().Where(c => c.ApprovalStatusUser == "User3" && c.ProductReturnId == viewModel.ProductReturnId).FirstOrDefault();
                            if (updateStatusUser3 != null)
                            {
                                updateStatusUser3.Status = "User2Approve";

                                _applicationDbContext.Entry(updateStatusUser3).State = EntityState.Modified;
                                _applicationDbContext.SaveChanges();
                            }
                            checkPR.ApproveStatusUser2 = viewModel.Status;
                            checkPR.MessageApprove2 = viewModel.Message;

                            _applicationDbContext.Entry(checkPR).State = EntityState.Modified;
                            _applicationDbContext.SaveChanges();
                        }
                        else
                        {
                            checkPR.ApproveStatusUser2 = viewModel.Status;
                            checkPR.MessageApprove2 = viewModel.Message;

                            _applicationDbContext.Entry(checkPR).State = EntityState.Modified;
                            _applicationDbContext.SaveChanges();
                        }
                    }
                    else if (approval.ApprovalStatusUser == "User3" && checkPR.ProductReturnNumber == approval.ProductReturnNumber)
                    {
                        var updateStatusUser3 = _approvalRepository.GetAllApproval().Where(c => c.ApprovalStatusUser == "User3" && c.ProductReturnId == viewModel.ProductReturnId).FirstOrDefault();
                        if (updateStatusUser3.Status == "User2Approve")
                        {
                            updateStatusUser3.Status = viewModel.Status;
                            updateStatusUser3.ApprovalDate = DateTimeOffset.Now;
                            updateStatusUser3.ApproveBy = getUser.NamaUser;
                            updateStatusUser3.ApprovalTime = diffDate.Days.ToString() + " Day";
                            updateStatusUser3.Message = viewModel.Message;

                            _applicationDbContext.Entry(updateStatusUser3).State = EntityState.Modified;
                            _applicationDbContext.SaveChanges();
                        }

                        checkPR.ApproveStatusUser3 = viewModel.Status;
                        checkPR.MessageApprove3 = viewModel.Message;

                        _applicationDbContext.Entry(checkPR).State = EntityState.Modified;
                        _applicationDbContext.SaveChanges();
                    }

                    //Jika semua sudah Approve langsung Pengurangan Stock
                    if (checkPR.ApproveStatusUser1 == "Approve" && checkPR.ApproveStatusUser2 == "Approve" && checkPR.ApproveStatusUser3 == "Approve")
                    {
                        checkPR.Status = viewModel.Status;

                        _applicationDbContext.Entry(checkPR).State = EntityState.Modified;
                        _applicationDbContext.SaveChanges();

                        //Update Stock
                        foreach (var item in checkPR.ProductReturnDetails)
                        {
                            var updateProduk = _productRepository.GetAllProduct().Where(c => c.ProductCode == item.ProductNumber).FirstOrDefault();
                            if (updateProduk != null)
                            {
                                updateProduk.UpdateDateTime = DateTime.Now;
                                updateProduk.UpdateBy = new Guid(getUser.Id);
                                updateProduk.Stock = updateProduk.Stock - item.Qty;

                                _applicationDbContext.Entry(updateProduk).State = EntityState.Modified;
                            }
                        }

                        //Update Status PRN Menjadi Finished
                        var updateProductReturn = _productReturnRepository.GetAllProductReturn().Where(c => c.ProductReturnId == viewModel.ProductReturnId).FirstOrDefault();
                        if (updateProductReturn != null)
                        {
                            {
                                updateProductReturn.Status = "Finished";
                            };
                            _applicationDbContext.Entry(updateProductReturn).State = EntityState.Modified;
                            _applicationDbContext.SaveChanges();
                        }                       

                        _approvalRepository.Update(approval);
                        _applicationDbContext.SaveChanges();

                        TempData["SuccessMessage"] = "Approve And Success Create Purchase Order";
                        return RedirectToAction("Index", "ApprovalProductReturn");
                    }
                    else
                    {
                        TempData["SuccessMessage"] = "Update Success";
                        return RedirectToAction("Index", "ApprovalProductReturn");
                    }
                }
            }

            return View();
        }
    }
}
