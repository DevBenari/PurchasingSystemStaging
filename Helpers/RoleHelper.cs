using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using System.Reflection;

namespace PurchasingSystem.Helpers
{
    public static class RoleHelper
    {
        public static async Task CreateRoles(IServiceProvider serviceProvider)
        {
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();

            // Ambil semua controller di assembly ini
            var controllers = Assembly.GetExecutingAssembly().GetTypes()
                .Where(type => typeof(Controller).IsAssignableFrom(type) && !type.IsAbstract)
                .ToList();

            foreach (var controllerType in controllers)
            {
                var controllerName = controllerType.Name.Replace("Controller", ""); // Nama controller tanpa "Controller"
                var controllerActions = controllerType.GetMethods(BindingFlags.Instance | BindingFlags.Public)
                    //.Where(method => method.IsPublic && !method.IsSpecialName && method.DeclaringType == controllerType)
                    .Where(method =>
                    method.IsPublic &&
                    !method.IsSpecialName &&
                    !method.Name.StartsWith("Redirect") &&
                    !method.Name.StartsWith("Load") &&
                    !method.Name.StartsWith("Get") &&
                    !method.Name.StartsWith("Impor") &&
                    !method.Name.StartsWith("Chart") &&
                    !method.Name.StartsWith("KpiJson") &&
                    !method.Name.StartsWith("PostData") &&
                    !method.GetCustomAttributes(typeof(NonActionAttribute), false).Any() &&
                    method.DeclaringType == controllerType && // Hanya metode dari controller itu sendiri
                    (method.GetCustomAttributes(typeof(HttpGetAttribute), false).Any() ||
                     method.GetCustomAttributes(typeof(HttpPostAttribute), false).Any() ||
                     method.GetCustomAttributes(typeof(HttpPutAttribute), false).Any() ||
                     method.GetCustomAttributes(typeof(HttpDeleteAttribute), false).Any() ||
                     !method.GetCustomAttributes(typeof(HttpMethodAttribute), false).Any()))
                    .Select(method => method.Name)
                    .ToList();

                if (controllerName != "Account")
                {
                    if (controllerName != "Auth")
                    {
                        if (controllerName != "Dashboard")
                        {
                            if (controllerName != "Home")
                            {
                                foreach (var action in controllerActions)
                                {
                                    string roleName = action;

                                    // Jika aksi adalah "Index", tambahkan nama controller ke role
                                    if (action.StartsWith("Index"))
                                    {
                                        roleName = $"Read{controllerName}";  // Misalnya, "ReadBank"
                                    }

                                    if (action.StartsWith("Detail"))
                                    {
                                        roleName = $"Update{controllerName}"; // Misalnya : "UpdateBank"
                                    }

                                    // Periksa apakah role sudah ada
                                    var roleExists = await roleManager.RoleExistsAsync(roleName);
                                    if (!roleExists)
                                    {
                                        IdentityRole role = new IdentityRole
                                        {
                                            Name = roleName,  // Nama asli role (misalnya, "AdminIndex")
                                            ConcurrencyStamp = controllerName
                                        };

                                        var result = await roleManager.CreateAsync(role);
                                        if (!result.Succeeded)
                                        {
                                            foreach (var error in result.Errors)
                                            {
                                                Console.WriteLine($"Error creating role {roleName}: {error.Description}");
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}
