﻿using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.DataProtection;
using System.Text;
using PurchasingSystemStaging.Repositories;

public class DecryptUrlMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IDataProtector _protector;
    private readonly UrlMappingService _urlMappingService;

    public DecryptUrlMiddleware(RequestDelegate next, IDataProtectionProvider provider, UrlMappingService urlMappingService)
    {
        _next = next;
        _protector = provider.CreateProtector("UrlProtector");
        _urlMappingService = urlMappingService;
    }
  
    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value.Trim('/');

        // Periksa apakah path adalah GUID-like code
        if (!string.IsNullOrEmpty(path) && path.Length == 36 && _urlMappingService.InMemoryMapping.TryGetValue(path, out var encryptedPath))
        {
            try
            {
                // Dekripsi path asli
                var decryptedPath = _protector.Unprotect(encryptedPath);                

                // Periksa prefix untuk menentukan jenis URL
                if (decryptedPath.StartsWith("Create:"))
                {
                    // URL untuk detail produk
                    var actualPath = decryptedPath.Substring(7); // Hilangkan prefix "Detail:"
                    context.Request.Path = new PathString("/" + actualPath);
                }
                else if (decryptedPath.StartsWith("Detail:"))
                {
                    // URL untuk detail produk
                    var actualPath = decryptedPath.Substring(7); // Hilangkan prefix "Detail:"
                    var parts = actualPath.Split('/');
                    if (parts.Length == 4)
                    {
                        context.Request.Path = new PathString("/" + string.Join("/", parts.Take(3)));
                        context.Request.QueryString = new QueryString($"?id={parts[3]}");
                    }
                }
                else if (decryptedPath.StartsWith("Delete:"))
                {
                    // URL untuk detail produk
                    var actualPath = decryptedPath.Substring(7); // Hilangkan prefix "Detail:"
                    var parts = actualPath.Split('/');
                    if (parts.Length == 4)
                    {
                        context.Request.Path = new PathString("/" + string.Join("/", parts.Take(3)));
                        context.Request.QueryString = new QueryString($"?id={parts[3]}");
                    }
                }
                else if (decryptedPath.StartsWith("Page:"))
                {
                    // URL untuk paginasi
                    var actualPath = decryptedPath.Substring(5); // Hilangkan prefix "Page:"
                    var parts = actualPath.Split('?');
                    if (parts.Length > 0)
                    {
                        context.Request.Path = new PathString("/" + parts[0]);
                        if (parts.Length > 1)
                        {
                            context.Request.QueryString = new QueryString("?" + parts[1]);
                        }
                    }
                }
            }
            catch
            {
                // Jika dekripsi gagal, kembalikan error
                context.Response.StatusCode = 400;
                await context.Response.WriteAsync("Invalid URL");
                return;
            }
        }

        await _next(context);
    }
}
