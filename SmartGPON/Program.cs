using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SmartGPON.Core.Enums;
using SmartGPON.Core.Interfaces;
using SmartGPON.Infrastructure.Data;
using SmartGPON.Infrastructure.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"),
        sql =>
        {
            sql.EnableRetryOnFailure(3);
            sql.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
        }));

builder.Services.AddIdentity<ApplicationUser, IdentityRole>(opt =>
{
    opt.Password.RequireDigit = true;
    opt.Password.RequiredLength = 8;
    opt.Password.RequireNonAlphanumeric = false;
    opt.Password.RequireUppercase = true;
    opt.Password.RequireLowercase = true;
    opt.Lockout.MaxFailedAccessAttempts = 10;
    opt.SignIn.RequireConfirmedEmail = false;
    opt.SignIn.RequireConfirmedAccount = false;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(opt =>
{
    opt.LoginPath = "/Account/Login";
    opt.AccessDeniedPath = "/Account/AccessDenied";
    opt.SlidingExpiration = true;
    opt.ExpireTimeSpan = TimeSpan.FromHours(8);
    opt.Cookie.HttpOnly = true;
    opt.Cookie.SameSite = Microsoft.AspNetCore.Http.SameSiteMode.Lax;
});

builder.Services.AddMemoryCache();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<ISecurityService, SecurityService>();
builder.Services.AddScoped<ITreeService, TreeService>();
builder.Services.AddScoped<IAuthorizationScopeService, AuthorizationScopeService>();
builder.Services.AddScoped<IApprovalService, ApprovalService>();
builder.Services.AddScoped<IAuditService, AuditService>();
builder.Services.AddControllersWithViews(options =>
{
    options.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true;
});

builder.Services.AddSingleton<SmartGPON.Web.Controllers.KioskProcessContext>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

using (var scope = app.Services.CreateScope())
{
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    try
    {
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        // db.Database.EnsureDeleted(); // Uncomment ONLY for intentional full reset
        db.Database.EnsureCreated(); // Recreate schema from EF model

        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        // Seed all roles
        foreach (var role in new[] { UserRoles.Superviseur, UserRoles.ChefProjet, UserRoles.TechTerrain, UserRoles.TechDessin, UserRoles.Visiteur })
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                var r = await roleManager.CreateAsync(new IdentityRole(role));
                if (r.Succeeded) logger.LogInformation("Role created: {Role}", role);
                else logger.LogError("Role error: {E}", string.Join(";", r.Errors.Select(e => e.Description)));
            }
        }

        // Seed admin@smartgpon.local as Superviseur
        var adminEmail = "admin@smartgpon.local";
        if (await userManager.FindByEmailAsync(adminEmail) == null)
        {
            var admin = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                EmailConfirmed = true,
                FirstName = "Administrateur",
                LastName = "Système",
                SecurityStamp = Guid.NewGuid().ToString()
            };
            var cr = await userManager.CreateAsync(admin, "Admin@12345");
            if (cr.Succeeded)
            {
                await userManager.AddToRoleAsync(admin, UserRoles.Superviseur);
                logger.LogInformation("Admin seeded: {Email}", adminEmail);
            }
            else logger.LogError("Admin seed failed: {E}", string.Join(";", cr.Errors.Select(e => e.Description)));
        }
    }
    catch (Exception ex)
    {
        scope.ServiceProvider.GetRequiredService<ILogger<Program>>().LogError(ex, "Seed error");
    }
}

var lifetime = app.Services.GetRequiredService<IHostApplicationLifetime>();

lifetime.ApplicationStarted.Register(() =>
{
    var url = app.Urls.FirstOrDefault() ?? "http://localhost:5000";
    var profilePath = Path.Combine(Path.GetTempPath(), "SmartGPON_Kiosk");

    try
    {
        // Forcer la fermeture des instances fantômes de ce profil pour éviter la délégation de processus (proxy exit immédiat)
        var searcher = new System.Diagnostics.ProcessStartInfo("powershell", "-NoProfile -Command \"Get-WmiObject Win32_Process -Filter \\\"Name='msedge.exe'\\\" | Where-Object { $_.CommandLine -match 'SmartGPON_Kiosk' } | ForEach-Object { Stop-Process -Id $_.ProcessId -Force }\"")
        {
            CreateNoWindow = true,
            UseShellExecute = false
        };
        System.Diagnostics.Process.Start(searcher)?.WaitForExit();

        var processStartInfo = new System.Diagnostics.ProcessStartInfo
        {
            FileName = "msedge",
            Arguments = $"--kiosk --no-first-run --disable-infobars --start-fullscreen --user-data-dir=\"{profilePath}\" \"{url}\"",
            UseShellExecute = true
        };

        var browserProcess = new System.Diagnostics.Process
        {
            StartInfo = processStartInfo,
            EnableRaisingEvents = true
        };

        app.Services.GetRequiredService<SmartGPON.Web.Controllers.KioskProcessContext>().BrowserProcess = browserProcess;

        browserProcess.Start();
    }
    catch (Exception ex)
    {
        app.Logger.LogError(ex, "Failed to launch Edge in Kiosk mode.");
    }
});

app.Run();
