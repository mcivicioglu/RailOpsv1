using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using RailOps.Context;
using RailOps.Entities;
using RailOps.Services;
using RailOps.Middleware;

namespace RailOps
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // 1. SQL Server bağlantısı
            var connectionString = builder.Configuration
                .GetConnectionString("DefaultConnection");

            builder.Services.AddDbContext<RailOpsContext>(options =>
                options.UseSqlServer(connectionString));

            // 2. MVC servisleri
            builder.Services.AddControllersWithViews();

            // 3. ASP.NET Core Identity
            builder.Services.AddDefaultIdentity<ApplicationUser>(options =>
            {
                // Geliştirme ortamı ayarları
                options.User.RequireUniqueEmail = false;
                options.SignIn.RequireConfirmedAccount = false;

                // Parola güvenliği
                options.Password.RequiredLength = 8;

                // Başarısız giriş denemeleri
                options.Lockout.MaxFailedAccessAttempts = 5;
            })
            .AddRoles<IdentityRole>()
            .AddEntityFrameworkStores<RailOpsContext>();

            // Pasif kullanıcıların yeni girişlerini engelleyen SignInManager
            builder.Services.AddScoped<
                SignInManager<ApplicationUser>,
                ActiveUserSignInManager>();

            builder.Services.AddRazorPages();

            // Arıza numarası ve kayıt işlemleri
            builder.Services.AddScoped<FaultCreationService>();

            // 4. Identity uygulama çerezi: pasif kullanıcıların oturumlarını reddet
            builder.Services.ConfigureApplicationCookie(options =>
            {
                options.Events.OnValidatePrincipal = async context =>
                {
                    // Identity'nin mevcut Security Stamp
                    // doğrulamasını koruyoruz.
                    await SecurityStampValidator
                        .ValidatePrincipalAsync(context);

                    // Identity zaten oturumu reddettiyse devam etme.
                    if (context.Principal?.Identity?.IsAuthenticated != true)
                        return;

                    var userManager = context.HttpContext
                        .RequestServices
                        .GetRequiredService<UserManager<ApplicationUser>>();

                    var user = await userManager
                        .GetUserAsync(context.Principal);

                    // Silinmiş veya pasifleştirilmiş hesap
                    if (user == null || !user.IsActive)
                    {
                        context.RejectPrincipal();

                        await context.HttpContext.SignOutAsync(
                            IdentityConstants.ApplicationScheme);
                    }
                };
            });

            builder.Services.Configure<SecurityStampValidatorOptions>(
                options =>
                {
                    options.ValidationInterval = TimeSpan.Zero;
                });

            // Servis kayıtlarının hepsi tamamlandı; uygulamayı oluştur
            var app = builder.Build();

            // 5. Yalnızca geliştirme ortamındaki test verileri
            if (app.Environment.IsDevelopment())
            {
                using var scope = app.Services.CreateScope();

                var roleManager = scope.ServiceProvider
                    .GetRequiredService<RoleManager<IdentityRole>>();

                var userManager = scope.ServiceProvider
                    .GetRequiredService<UserManager<ApplicationUser>>();

                // 1. Roller mevcut değilse oluştur
                string[] roles =
                {
                    "Admin",
                    "TechnicalStaff",
                    "OperationsStaff"
                };

                foreach (var roleName in roles)
                {
                    if (!await roleManager.RoleExistsAsync(roleName))
                    {
                        var result = await roleManager.CreateAsync(
                            new IdentityRole(roleName));

                        if (!result.Succeeded)
                        {
                            throw new InvalidOperationException(
                                string.Join("; ",
                                    result.Errors.Select(x => x.Description)));
                        }
                    }
                }


                // Geliştirme ortamı için test yöneticisi
                var adminUser =
                    await userManager.FindByNameAsync("90000");

                if (adminUser == null)
                {
                    var adminPassword =
                        app.Configuration["RailOpsTest:AdminPassword"];

                    if (string.IsNullOrWhiteSpace(adminPassword))
                    {
                        throw new InvalidOperationException(
                            "AdminPassword User Secrets içinde tanımlanmalı.");
                    }

                    adminUser = new ApplicationUser
                    {
                        UserName = "90000",
                        FullName = "Geliştirme Test Yöneticisi",
                        Email = null
                    };

                    var createResult = await userManager.CreateAsync(
                        adminUser, adminPassword);

                    if (!createResult.Succeeded)
                    {
                        throw new InvalidOperationException(
                            string.Join("; ",
                                createResult.Errors.Select(e => e.Description)));
                    }
                }

                // Mevcut hesap beklenen test hesabı mı?
                if (adminUser.FullName != "Geliştirme Test Yöneticisi")
                {
                    throw new InvalidOperationException(
                        "90000 sicil numarası başka bir kullanıcıya ait.");
                }

                // Admin rolünü ata
                if (!await userManager.IsInRoleAsync(
                    adminUser, "Admin"))
                {
                    var roleResult = await userManager.AddToRoleAsync(
                        adminUser, "Admin");

                    if (!roleResult.Succeeded)
                    {
                        throw new InvalidOperationException(
                            string.Join("; ",
                                roleResult.Errors.Select(e => e.Description)));
                    }
                }


                // 2. Mevcut teknik personeli bul
                var technicalUser =
                    await userManager.FindByNameAsync("885");

                if (technicalUser == null)
                {
                    throw new InvalidOperationException(
                        "885 sicil numaralı test kullanıcısı bulunamadı.");
                }

                // 3. TechnicalStaff rolünü ata
                if (!await userManager.IsInRoleAsync(
                    technicalUser, "TechnicalStaff"))
                {
                    var result = await userManager.AddToRoleAsync(
                        technicalUser, "TechnicalStaff");

                    if (!result.Succeeded)
                    {
                        throw new InvalidOperationException(
                            string.Join("; ",
                                result.Errors.Select(x => x.Description)));
                    }
                }

                // 4. İşletme test personelini bul
                var operationsUser =
                    await userManager.FindByNameAsync("67890");

                // Kullanıcı yoksa oluştur
                if (operationsUser == null)
                {
                    var password =
                        app.Configuration["RailOpsTest:OperationsPassword"];

                    if (string.IsNullOrWhiteSpace(password))
                    {
                        throw new InvalidOperationException(
                            "OperationsPassword User Secrets içinde tanımlanmalı.");
                    }

                    operationsUser = new ApplicationUser
                    {
                        UserName = "67890",
                        FullName = "İşletme Test Personeli",
                        Email = null
                    };

                    var createResult =
                        await userManager.CreateAsync(operationsUser, password);

                    if (!createResult.Succeeded)
                    {
                        throw new InvalidOperationException(
                            string.Join("; ",
                                createResult.Errors.Select(x => x.Description)));
                    }
                }

                // 5. OperationsStaff rolünü ata
                if (!await userManager.IsInRoleAsync(
                    operationsUser, "OperationsStaff"))
                {
                    var result = await userManager.AddToRoleAsync(
                        operationsUser, "OperationsStaff");

                    if (!result.Succeeded)
                    {
                        throw new InvalidOperationException(
                            string.Join("; ",
                                result.Errors.Select(x => x.Description)));
                    }
                }
            }


            // 6. HTTP Request Pipeline
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();

            app.UseRouting();

            // Authentication & Authorization
            app.UseAuthentication();
            app.UseMiddleware<ForcePasswordChangeMiddleware>();
            app.UseAuthorization();

            // Statik dosyalar
            app.MapStaticAssets();

            // MVC Routing
            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");

            // Identity Razor Pages
            app.MapRazorPages();

            // Uygulamayı başlat
            app.Run();
        }
    }
}
