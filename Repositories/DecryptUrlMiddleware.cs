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
                    // Dekripsi path
                    string decryptedPath = _protector.Unprotect(path.Trim('/'));
                    context.Request.Path = new PathString("/" + decryptedPath);
                }
                catch
                {
                    // Jika gagal, lanjutkan tanpa perubahan
                }
            }

            await _next(context);
        }
    }    
}
