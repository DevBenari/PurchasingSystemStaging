using Microsoft.AspNetCore.DataProtection;

namespace PurchasingSystemStaging.Repositories
{
    public class DecryptUrlMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IDataProtector _protector;

        public DecryptUrlMiddleware(RequestDelegate next, IDataProtectionProvider provider)
        {
            _next = next;
            _protector = provider.CreateProtector("UrlProtector");
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var path = context.Request.Path.Value;

            if (!string.IsNullOrEmpty(path))
            {
                try
                {
                    // Dekripsi URL path
                    string decryptedPath = _protector.Unprotect(path.Trim('/'));

                    // Pisahkan path menjadi segmen (misalnya: area/controller/action/id)
                    var segments = decryptedPath.Split('/');
                    if (segments.Length >= 3)
                    {
                        // Ganti request path ke path asli
                        context.Request.Path = new PathString("/" + string.Join("/", segments.Take(3)));

                        // Tambahkan id sebagai query string jika ada
                        if (segments.Length == 4)
                        {
                            context.Request.QueryString = new QueryString($"?id={segments[3]}");
                        }
                    }
                }
                catch
                {
                    // Jika dekripsi gagal, biarkan URL tidak berubah
                }
            }

            await _next(context);
        }
    }    
}
