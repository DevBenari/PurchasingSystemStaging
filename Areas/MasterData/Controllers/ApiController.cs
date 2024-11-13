using Microsoft.AspNetCore.Mvc;
using Microsoft.CodeAnalysis.Elfie.Model.Map;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PurchasingSystemStaging.Areas.MasterData.Models;
using PurchasingSystemStaging.Areas.MasterData.Repositories;
using PurchasingSystemStaging.Areas.Order.Models;
using PurchasingSystemStaging.Data;
using System.Data;
using System.Net.Http;
using System.Security.Policy;
using static System.Net.WebRequestMethods;

namespace PurchasingSystemStaging.Areas.MasterData.Controllers
{
    [Area("MasterData")]
    [Route("MasterData/[Controller]/[Action]")]
    public class ApiController : Controller
    {
        private readonly HttpClient _httpClient;
        private readonly ApplicationDbContext _applicationDbContext;
        private readonly IUserActiveRepository _userActiveRepository;
        private readonly IProductRepository _productRepository;
        private readonly ICategoryRepository _categoryRepository;
        private readonly IMeasurementRepository _MeasurementRepository;
        private readonly IDiscountRepository _discountRepository;
        private readonly ISupplierRepository _supplierRepository;

        public ApiController(
            ApplicationDbContext applicationDbContext,
            IUserActiveRepository userActiveRepository,
            IProductRepository productRepository,
            ICategoryRepository CategoryRepository,
            IMeasurementRepository MeasurementRepository,
            IDiscountRepository DiscountRepository,
            ISupplierRepository supplierRepository,

            HttpClient httpClient
        )
        {
            _applicationDbContext = applicationDbContext;
            _userActiveRepository = userActiveRepository;
            _httpClient = httpClient;
            _categoryRepository = CategoryRepository;
            _productRepository = productRepository;
            _MeasurementRepository = MeasurementRepository;
            _discountRepository = DiscountRepository;
            _supplierRepository = supplierRepository;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult GetData() { return View(); }
        public IActionResult GetDataSupplier() { return View(); }
        public IActionResult GetDataSatuan() { return View(); }
        public IActionResult GetDataKategoriObat() { return View(); }
        public IActionResult GetDataObat() { return View(); }
        public IActionResult GetDataDiskon() { return View(); }

        [HttpGet]
        public async Task<IActionResult> Impor(string apiCode)
        {
            var apiUrl = apiCode; // URL API GetProduct

            // Mengirimkan permintaan GET ke API
            var response = await _httpClient.GetAsync(apiUrl);

            if (response.IsSuccessStatusCode)
            {
                var jsonData = await response.Content.ReadAsStringAsync();
                var productData = JsonConvert.DeserializeObject<List<Product>>(jsonData); // Deserialisasi JSON ke model Product

                return View("Impor", productData); // Tampilkan data dalam View
            }
            else
            {
                // Jika request gagal
                return View("Error", "Gagal mengambil data dari API");
            }
        }

        [HttpGet]
        public async Task<IActionResult> CreateSupplier(string apiCode)
        {
            var apiUrl = apiCode; // URL API untuk mengambil data supplier

            // Menambahkan header Authorization
            _httpClient.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", "YWRtZWRpa2E6YWRtZWRpa2E"); // Pastikan token atau kredensial benar

            var getUser = _userActiveRepository.GetAllUserLogin()
                .Where(u => u.UserName == User.Identity.Name)
                .FirstOrDefault();
            var dateNow = DateTimeOffset.Now;
            var setDateNow = DateTimeOffset.Now.ToString("yyMMdd");
            try
            {
                // Mengirimkan permintaan GET ke API
                var response = await _httpClient.GetAsync(apiUrl);

                if (response.IsSuccessStatusCode)
                {
                    var jsonData = await response.Content.ReadAsStringAsync();

                    var responseObject = JsonConvert.DeserializeObject<dynamic>(jsonData);
                    var supplierData = responseObject.data.ToObject<List<dynamic>>();
                    var filterDisc = new List<dynamic>();

                    foreach (var item in supplierData)
                    {
                        //var getDiscVal = _supplierRepository.GetAllSupplier()
                        //    .FirstOrDefault(d => d.SupplierName == (string)Convert.ToDouble(item.nama_supp));

                        //if (getDiscVal == null)
                        //{
                        //filterDisc.Add(item);

                        // Mendapatkan kode measurement terakhir berdasarkan hari ini
                        var lastCode = _supplierRepository.GetAllSupplier()
                        .Where(d => d.CreateDateTime.ToString("yyMMdd") == setDateNow)
                        .OrderByDescending(k => k.SupplierCode)
                        .FirstOrDefault();

                        string SupplierCode;

                        if (lastCode == null)
                        {
                            SupplierCode = "PCP" + setDateNow + "0001";
                        }
                        else
                        {
                            var lastCodeTrim = lastCode.SupplierCode.Substring(3, 6);

                            if (lastCodeTrim != setDateNow)
                            {
                                SupplierCode = "PCP" + setDateNow + "0001";
                            }
                            else
                            {
                                SupplierCode = "PCP" + setDateNow +
                                                (Convert.ToInt32(lastCode.SupplierCode.Substring(9, lastCode.SupplierCode.Length - 9)) + 1)
                                                .ToString("D4");
                            }
                        }

                        var supplier = new Supplier
                        {
                            CreateDateTime = DateTime.Now,
                            CreateBy = new Guid(getUser.Id),
                            SupplierId = Guid.NewGuid(),
                            SupplierCode = SupplierCode,
                            SupplierName = item.nama_supp,
                            LeadTimeId = new Guid("28D557A4-DFF5-45D8-7AF6-08DCAD5EBA2E"),// Set Id LeadTime = 7
                            Address = item.alamat_supp,
                            Handphone = item.hp,
                            Email = "supplier@email.com",
                            Note = item.ket,
                            IsPKS = true,
                            IsActive = true,
                        };
                        // Simpan ke database
                        _supplierRepository.Tambah(supplier);
                        //}
                    }

                    return View("CreateSupplier", supplierData); // Kirimkan data ke View
                }
                else
                {
                    // Jika request gagal
                    return View("Error", "Gagal mengambil data dari API");
                }
            }
            catch (Exception ex)
            {
                // Tangani kesalahan jika ada masalah dengan koneksi atau pemrosesan data
                return View("Error", $"Terjadi kesalahan: {ex.Message}");
            }
        }

        [HttpGet]
        public async Task<IActionResult> CreateSatuan(string apiCode)
        {
            var apiUrl = apiCode; // URL API untuk mengambil data supplier

            var getUser = _userActiveRepository.GetAllUserLogin().Where(u => u.UserName == User.Identity.Name).FirstOrDefault();
            // Menambahkan header Authorization
            _httpClient.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", "YWRtZWRpa2E6YWRtZWRpa2E"); // Pastikan token atau kredensial benar

            try
            {
                // Mengirimkan permintaan GET ke API
                var response = await _httpClient.GetAsync(apiUrl);

                if (response.IsSuccessStatusCode)
                {
                    var jsonData = await response.Content.ReadAsStringAsync();
                    var responseObject = JsonConvert.DeserializeObject<dynamic>(jsonData);
                    var createSatuanList = responseObject.data.ToObject<List<dynamic>>();

                    // Format tanggal sekarang (yyMMdd)
                    var setDateNow = DateTime.Now.ToString("yyMMdd");

                    // Convert dynamic data to List of Measurement models and save them
                    foreach (var item in createSatuanList)
                    {
                        // Mendapatkan kode measurement terakhir berdasarkan hari ini
                        var lastCode = _MeasurementRepository.GetAllMeasurement()
                            .Where(d => d.CreateDateTime.ToString("yyMMdd") == setDateNow)
                            .OrderByDescending(k => k.MeasurementCode)
                            .FirstOrDefault();

                        string measurementCode;
                        if (lastCode == null)
                        {
                            measurementCode = "MSR" + setDateNow + "0001";
                        }
                        else
                        {
                            var lastCodeTrim = lastCode.MeasurementCode.Substring(3, 6);

                            if (lastCodeTrim != setDateNow)
                            {
                                measurementCode = "MSR" + setDateNow + "0001";
                            }
                            else
                            {
                                measurementCode = "MSR" + setDateNow + (Convert.ToInt32(lastCode.MeasurementCode.Substring(9, lastCode.MeasurementCode.Length - 9)) + 1).ToString("D4");
                            }
                        }

                        // Buat objek measurement baru
                        var measurement = new Measurement
                        {
                            CreateDateTime = DateTime.Now,
                            CreateBy = new Guid(getUser.Id),
                            MeasurementId = Guid.NewGuid(),
                            MeasurementName = item.satuan, // Pastikan item.satuan ada
                            MeasurementCode = measurementCode // Assign kode measurement yang baru
                        };

                        // Simpan ke database
                        _MeasurementRepository.Tambah(measurement);

                    }

                    return View("CreateSatuan", createSatuanList); // Kirimkan data ke View
                }
                else
                {
                    // Jika request gagal
                    return View("Error", "Gagal mengambil data dari API");
                }
            }
            catch (Exception ex)
            {
                // Tangani kesalahan jika ada masalah dengan koneksi atau pemrosesan data
                return View("Error", $"Terjadi kesalahan: {ex.Message}");
            }
        }

        [HttpGet]
        public async Task<IActionResult> CreateKategoriObat(string apiCode)
        {
            var apiUrl = apiCode; // URL API untuk mengambil data supplier

            var getUser = _userActiveRepository.GetAllUserLogin().Where(u => u.UserName == User.Identity.Name).FirstOrDefault();
            // Menambahkan header Authorization
            _httpClient.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", "YWRtZWRpa2E6YWRtZWRpa2E"); // Pastikan token atau kredensial benar

            try
            {
                // Mengirimkan permintaan GET ke API
                var response = await _httpClient.GetAsync(apiUrl);

                var dateNow = DateTimeOffset.Now;
                var setDateNow = DateTimeOffset.Now.ToString("yyMMdd");
                if (response.IsSuccessStatusCode)
                {
                    var jsonData = await response.Content.ReadAsStringAsync();

                    var responseObject = JsonConvert.DeserializeObject<dynamic>(jsonData);
                    var CreateKategoriObat = responseObject.data.ToObject<List<dynamic>>();
                    var createKategoriList = responseObject.data.ToObject<List<dynamic>>();

                    foreach (var item in createKategoriList)
                    {
                        // Mendapatkan kode measurement terakhir berdasarkan hari ini
                        var lastCode = _categoryRepository.GetAllCategory()
                            .Where(d => d.CreateDateTime.ToString("yyMMdd") == setDateNow)
                            .OrderByDescending(k => k.CategoryCode)
                            .FirstOrDefault();


                        string kategoriCode;
                        if (lastCode == null)
                        {
                            kategoriCode = "CTG" + setDateNow + "0001";
                        }
                        else
                        {
                            var lastCodeTrim = lastCode.CategoryCode.Substring(3, 6);

                            if (lastCodeTrim != setDateNow)
                            {
                                kategoriCode = "CTG" + setDateNow + "0001";
                            }
                            else
                            {
                                kategoriCode = "CTG" + setDateNow + (Convert.ToInt32(lastCode.CategoryCode.Substring(9, lastCode.CategoryCode.Length - 9)) + 1).ToString("D4");
                            }
                        }
                        // Buat objek measurement baru
                        var Category = new Category
                        {
                            CreateDateTime = DateTime.Now,
                            CreateBy = new Guid(getUser.Id),
                            CategoryId = Guid.NewGuid(),
                            CategoryCode = kategoriCode,
                            CategoryName = item.kel_brg,
                        };

                        // Simpan ke database
                        _categoryRepository.Tambah(Category);

                    }
                    return View("CreateKategoriObat", CreateKategoriObat); // Kirimkan data ke View
                }
                else
                {
                    // Jika request gagal
                    return View("Error", "Gagal mengambil data dari API");
                }
            }
            catch (Exception ex)
            {
                // Tangani kesalahan jika ada masalah dengan koneksi atau pemrosesan data
                return View("Error", $"Terjadi kesalahan: {ex.Message}");
            }
        }

        [HttpGet]
        public async Task<IActionResult> CreateDiskon(string apiCode)
        {
            var apiUrl = apiCode; // URL API untuk mengambil data supplier

            var getUser = _userActiveRepository.GetAllUserLogin()
                .Where(u => u.UserName == User.Identity.Name)
                .FirstOrDefault();

            // Menambahkan header Authorization
            _httpClient.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", "YWRtZWRpa2E6YWRtZWRpa2E"); // Pastikan token atau kredensial benar

            // Mengirimkan permintaan GET ke API
            var response = await _httpClient.GetAsync(apiUrl);

            var dateNow = DateTimeOffset.Now;
            var setDateNow = DateTimeOffset.Now.ToString("yyMMdd");

            if (response.IsSuccessStatusCode)
            {
                // Membaca konten API
                var jsonData = await response.Content.ReadAsStringAsync();
                var responseObject = JsonConvert.DeserializeObject<dynamic>(jsonData);
                var diskoniList = responseObject.data.ToObject<List<dynamic>>();
                var filterDisc = new List<dynamic>();

                foreach (var item in diskoniList)
                {
                    var getDiscVal = _discountRepository.GetAllDiscount().Where(d => d.DiscountValue == (int)Convert.ToDouble(item.disc_persen)).FirstOrDefault();

                    if (getDiscVal == null)
                    {
                        filterDisc.Add(item);

                        // Mendapatkan kode measurement terakhir berdasarkan hari ini
                        var lastCode = _discountRepository.GetAllDiscount()
                        .Where(d => d.CreateDateTime.ToString("yyMMdd") == setDateNow)
                        .OrderByDescending(k => k.DiscountCode)
                        .FirstOrDefault();

                        string DiscountCode;

                        if (lastCode == null)
                        {
                            DiscountCode = "DSC" + setDateNow + "0001";
                        }
                        else
                        {
                            var lastCodeTrim = lastCode.DiscountCode.Substring(3, 6);

                            if (lastCodeTrim != setDateNow)
                            {
                                DiscountCode = "DSC" + setDateNow + "0001";
                            }
                            else
                            {
                                DiscountCode = "DSC" + setDateNow +
                                                (Convert.ToInt32(lastCode.DiscountCode.Substring(9, lastCode.DiscountCode.Length - 9)) + 1)
                                                .ToString("D4");
                            }
                        }

                        // Membuat objek diskon baru// Cek apakah nilai disc_persen tidak null
                        var discountValue = item.disc_persen != null
                            ? (int)Convert.ToDouble(item.disc_persen) // Membuang angka desimal tanpa pembulatan
                            : 0;

                        var discount = new Discount
                        {
                            CreateDateTime = DateTimeOffset.Now,
                            CreateBy = new Guid(getUser.Id),
                            DiscountId = Guid.NewGuid(),
                            DiscountCode = DiscountCode,
                            DiscountValue = discountValue // Menangani null atau jika data tidak ada
                        };

                        // Simpan ke database
                        _discountRepository.Tambah(discount);
                    }
                }

                // Kirimkan data ke View
                return View("CreateDiskon", filterDisc); // Kirimkan hanya 100 data
            }
            else
            {
                // Jika request gagal, tampilkan pesan error
                return View("Error", "Gagal mengambil data dari API");
            }
        }

        private async Task<T> GetApiDataAsync<T>(string url)
        {
            var response = await _httpClient.GetAsync(url);
            if (response.IsSuccessStatusCode)
            {
                var jsonData = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<T>(jsonData);
            }
            return default(T); // Mengembalikan nilai default jika gagal
        }

        [HttpGet]
        public async Task<IActionResult> CreateObat(string apiCode, int limit = 20)
        {
            var apiUrlObat = "https://app.mmchospital.co.id/devel_mantap/api.php?mod=api&cmd=get_data_obat&return_type=json"; // URL API untuk mengambil data supplier
            var apiUrlDiskon = "https://app.mmchospital.co.id/devel_mantap/api.php?mod=api&cmd=get_diskon_obat&return_type=json";
            var apiUrlKategori = "https://app.mmchospital.co.id/devel_mantap/api.php?mod=api&cmd=get_kategori_obat&return_type=json";
            var apiUrlSatuan = "https://app.mmchospital.co.id/devel_mantap/api.php?mod=api&cmd=get_satuan&return_type=json";

            var getUser = _userActiveRepository.GetAllUserLogin()
                .Where(u => u.UserName == User.Identity.Name)
                .FirstOrDefault();

            // Menambahkan header Authorization
            _httpClient.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", "YWRtZWRpa2E6YWRtZWRpa2E"); // Pastikan token atau kredensial benar

            // Mengirimkan permintaan GET ke API
            var response = await _httpClient.GetAsync(apiUrlObat);
            var responseDisc = await _httpClient.GetAsync(apiUrlDiskon);
            var responseCat= await _httpClient.GetAsync(apiUrlKategori);
            var responseSat = await _httpClient.GetAsync(apiUrlSatuan);

            var dateNow = DateTimeOffset.Now;
            var setDateNow = DateTimeOffset.Now.ToString("yyMMdd");

            // Membaca konten API
            var jsonData = await response.Content.ReadAsStringAsync();
            var jsonDataDisc = await responseDisc.Content.ReadAsStringAsync();
            var jsonDataCat = await responseCat.Content.ReadAsStringAsync();
            var jsonDataSat = await responseSat.Content.ReadAsStringAsync();

            var responseObject = JsonConvert.DeserializeObject<dynamic>(jsonData);
            var responseObjectDisc = JsonConvert.DeserializeObject<dynamic>(jsonDataDisc);
            var responseObjectCat = JsonConvert.DeserializeObject<dynamic>(jsonDataCat);
            var responseObjectSat = JsonConvert.DeserializeObject<dynamic>(jsonDataSat);

            var productList = responseObject.data.ToObject<List<dynamic>>();
            var diskonList = responseObjectDisc.data.ToObject<List<dynamic>>();
            var kategoriList = responseObjectCat.data.ToObject<List<dynamic>>();
            var satuanList = responseObjectSat.data.ToObject<List<dynamic>>();

            var filterProd = new List<dynamic>();

            if (response.IsSuccessStatusCode)
            {
                foreach (var itemObat in productList)
                {
                    var getObat = _productRepository.GetAllProduct().Where(d => d.ProductName == Convert.ToString(itemObat.nama_brg)).FirstOrDefault();

                    if (getObat == null)
                    {
                        var apiDiscountUrl = $"https://app.mmchospital.co.id/devel_mantap/api.php?mod=api&cmd=get_diskon_obat&return_type=json&diskon_obat={itemObat.kode_brg}";
                        var apiCategoryUrl = $"https://app.mmchospital.co.id/devel_mantap/api.php?mod=api&cmd=get_kategori_obat&return_type=json&kategori_obat={itemObat.commodity}";
                        var apiSatuanUrl = $"https://app.mmchospital.co.id/devel_mantap/api.php?mod=api&cmd=get_satuan&return_type=json&satuan={itemObat.kode_satuan}";
                        var responseDiscount = GetApiDataAsync<dynamic>(apiDiscountUrl);
                        var responseCategory = GetApiDataAsync<dynamic>($"https://app.mmchospital.co.id/devel_mantap/api.php?mod=api&cmd=get_kategori_obat&return_type=json&kategori_obat={itemObat.commodity}");
                        var responseMeasurement = GetApiDataAsync<dynamic>($"https://app.mmchospital.co.id/devel_mantap/api.php?mod=api&cmd=get_satuan&return_type=json&satuan={itemObat.kode_satuan}");                        

                        // Tunggu semua hasil API
                        await Task.WhenAll(responseDiscount, responseCategory, responseMeasurement);

                        // Mendapatkan hasil API
                        var discountData = await responseDiscount;
                        var categoryData = await responseCategory;
                        var measurementData = await responseMeasurement;

                        if (discountData?.data == null)
                        {
                            /*supplierName = discountData.data[0]?.nama_supp?.ToString();*/ // Menggunakan nilai default jika kel_brg null
                            continue;
                        }
                        else
                       {
                            var getDiscountResponse = await _httpClient.GetAsync(apiDiscountUrl);
                            var getCategoryResponse = await _httpClient.GetAsync(apiCategoryUrl);

                            // 2. Baca konten sebagai string
                            var jsonDiscountResponse = await getDiscountResponse.Content.ReadAsStringAsync();
                            var jsonCategoryResponse = await getCategoryResponse.Content.ReadAsStringAsync();

                            // Parsing JSON ke JObject
                            var jObjectDiscount = JObject.Parse(jsonDiscountResponse);
                            var jObjectCategory = JObject.Parse(jsonCategoryResponse);

                            // 3. Cek apakah "data" ada dan merupakan objek
                            if (jObjectDiscount.TryGetValue("data", out JToken discountToken) && discountToken is JObject dataObjDiscount)
                            {                                                               
                                // 4. Ambil nilai "nama_supp" dari dataObj
                                var supplierName = dataObjDiscount["nama_supp"]?.ToString();
                                var discPersen = dataObjDiscount["disc_persen"]?.ToString();
                                var hargaObat = dataObjDiscount["harga"]?.ToString();

                                var getSupplier = _supplierRepository.GetAllSupplier().Where(s => s.SupplierName == supplierName).FirstOrDefault();
                                if (getSupplier != null)
                                {
                                    var getDiscount = _discountRepository.GetAllDiscount().Where(s => s.DiscountValue == (int)Convert.ToDouble(discPersen)).FirstOrDefault();
                                    if (getDiscount != null)
                                    {
                                        if (jObjectCategory["response"] != null)
                                        {
                                            // 5. Cek apakah "data" ada dan tipe datanya array
                                            var dataArray = jObjectCategory["response"]["data"] as JArray; /*InvalidOperationException: Cannot access child value on Newtonsoft.Json.Linq.JValue.*/
                                            if (dataArray != null && dataArray.Count > 0)
                                            {
                                                // 6. Ambil elemen pertama dari array "data" dan cari "nama_supp"
                                                var firstItem = dataArray[0];
                                                var namaSupp = firstItem["kel_brg"]?.ToString();
                                            }
                                            
                                            // 4. Ambil nilai "nama_supp" dari dataObj
                                            var kategory = dataObjDiscount["kel_brg"]?.ToString();

                                            var getCategory = _categoryRepository.GetAllCategory().Where(c => c.CategoryName == kategory).FirstOrDefault();
                                            if (getCategory != null)
                                            {
                                                var satuan = itemObat.kode_satuan;
                                                var getSatuan = _MeasurementRepository.GetAllMeasurement().Where(s => s.MeasurementName == satuan).FirstOrDefault();
                                                if (getSatuan != null)
                                                {
                                                    var lastCode = _productRepository.GetAllProduct().Where(d => d.CreateDateTime.ToString("yyMMdd") == setDateNow).OrderByDescending(k => k.ProductCode).FirstOrDefault();

                                                    string ProductCode;

                                                    if (lastCode == null)
                                                    {
                                                        ProductCode = "PDC" + setDateNow + "0001";
                                                    }
                                                    else
                                                    {
                                                        var lastCodeTrim = lastCode.ProductCode.Substring(3, 6);

                                                        if (lastCodeTrim != setDateNow)
                                                        {
                                                            ProductCode = "PDC" + setDateNow + "0001";
                                                        }
                                                        else
                                                        {
                                                            ProductCode = "PDC" + setDateNow +
                                                                            (Convert.ToInt32(lastCode.ProductCode.Substring(9, lastCode.ProductCode.Length - 9)) + 1)
                                                                            .ToString("D4");
                                                        }
                                                    }

                                                    var product = new Product
                                                    {
                                                        CreateDateTime = DateTime.Now,
                                                        CreateBy = new Guid(getUser.Id),
                                                        ProductId = Guid.NewGuid(),
                                                        ProductCode = ProductCode,
                                                        ProductName = itemObat.nama_brg,
                                                        SupplierId = getSupplier.SupplierId,
                                                        CategoryId = getCategory.CategoryId,
                                                        MeasurementId = getSatuan.MeasurementId,
                                                        DiscountId = getDiscount.DiscountId,
                                                        WarehouseLocationId = new Guid("4218A796-79B1-4F59-7767-08DCAE28EBBE"),
                                                        MinStock = 0,
                                                        MaxStock = 0,
                                                        BufferStock = 0,
                                                        Stock = 0,
                                                        Cogs = 0,
                                                        BuyPrice = Convert.ToDecimal(hargaObat),
                                                        RetailPrice = 0,
                                                        StorageLocation = "",
                                                        RackNumber = "",
                                                        Note = ""
                                                    };

                                                    // Simpan ke database
                                                    _productRepository.Tambah(product);
                                                }
                                            }
                                        }
                                        else
                                        { 
                                        
                                        }
                                    }                                    
                                }
                            }
                            else
                            {
                                Console.WriteLine("Data is not an object or is missing.");
                            }
                        }                                                
                    }

                }

                // Kirimkan data ke View
                return View("CreateDiskon", filterProd); // Kirimkan hanya 100 data
            }
            else
            {
                // Jika request gagal, tampilkan pesan error
                return View("Error", "Gagal mengambil data dari API");
            }
        }
    }
}
