// SmartGPON v3 â€“ Program.cs
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SmartGPON.Core.Interfaces;
using SmartGPON.Infrastructure.Data;
using SmartGPON.Infrastructure.Services;

var builder = WebApplication.CreateBuilder(args);

// â”€â”€ Database â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"),
        sql => {
            sql.EnableRetryOnFailure(3);
            sql.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
        }));

// â”€â”€ Identity â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(opt =>
{
    opt.Password.RequireDigit           = true;
    opt.Password.RequiredLength         = 8;
    opt.Password.RequireNonAlphanumeric = false;
    opt.Password.RequireUppercase       = true;
    opt.Password.RequireLowercase       = true;
    opt.Lockout.MaxFailedAccessAttempts = 10;
    opt.SignIn.RequireConfirmedEmail    = false;
    opt.SignIn.RequireConfirmedAccount  = false;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

// â”€â”€ Auth cookie â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
builder.Services.ConfigureApplicationCookie(opt =>
{
    opt.LoginPath        = "/Account/Login";
    opt.AccessDeniedPath = "/Account/AccessDenied";
    opt.SlidingExpiration = true;
    opt.ExpireTimeSpan   = TimeSpan.FromHours(8);
    opt.Cookie.HttpOnly  = true;
    opt.Cookie.SameSite  = Microsoft.AspNetCore.Http.SameSiteMode.Lax;
});

// â”€â”€ Application services â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
builder.Services.AddMemoryCache();
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<ISecurityService, SecurityService>();
builder.Services.AddScoped<ITreeService, TreeService>();
builder.Services.AddControllersWithViews(options =>
{
    // FIX: EmpÃªche ASP.NET Core de traiter les navigation properties null! comme [Required]
    // Sans Ã§a, ModelState.IsValid = false sur tous les POST car Zone, Client, etc. sont null
    options.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true;
});

builder.Services.AddSingleton<SmartGPON.Web.Controllers.KioskProcessContext>();

var app = builder.Build();

// â”€â”€ Pipeline â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
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

// â”€â”€ Seed â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
using (var scope = app.Services.CreateScope())
{
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    try
    {
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        // Create the database only if it does not already exist (preserves existing data on restart).
        db.Database.EnsureCreated();

        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        foreach (var role in new[] { "Admin", "Technicien", "Lecteur" })
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                var r = await roleManager.CreateAsync(new IdentityRole(role));
                if (r.Succeeded) logger.LogInformation("Role created: {Role}", role);
                else logger.LogError("Role error: {E}", string.Join(";", r.Errors.Select(e => e.Description)));
            }
        }

        if (await userManager.FindByEmailAsync("admin@smartgpon.local") == null)
        {
            var admin = new ApplicationUser
            {
                UserName       = "admin@smartgpon.local",
                Email          = "admin@smartgpon.local",
                EmailConfirmed = true,
                SecurityStamp  = Guid.NewGuid().ToString()
            };
            var cr = await userManager.CreateAsync(admin, "Admin@12345");
            if (cr.Succeeded)
            {
                await userManager.AddToRoleAsync(admin, "Admin");
                logger.LogInformation("Admin created: admin@smartgpon.local");
            }
            else
                logger.LogError("Admin creation failed: {E}", string.Join(";", cr.Errors.Select(e => e.Description)));
        }
        else
            logger.LogInformation("Admin already exists.");
    }
    catch (Exception ex)
    {
        scope.ServiceProvider.GetRequiredService<ILogger<Program>>()
            .LogError(ex, "Seed error");
    }
}

// â”€â”€ Lancement Desktop (Edge Kiosk) â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
var lifetime = app.Services.GetRequiredService<IHostApplicationLifetime>();

lifetime.ApplicationStarted.Register(() =>
{
    // Retrieve the URL the application is listening on
    var url = app.Urls.FirstOrDefault() ?? "http://localhost:5000";

    // Pour éviter que Edge ne délègue l'onglet à une instance déjà ouverte (ce qui ferait quitter le processus
    // et arrêterait l'application ASP.NET Core prématurément), on force l'utilisation d'un profil Kiosk dédié.
    var profilePath = Path.Combine(Path.GetTempPath(), "SmartGPON_Kiosk");

    try
    {
        // Setup the browser process to run Edge in full kiosk mode
        var processStartInfo = new System.Diagnostics.ProcessStartInfo
        {
            FileName = "msedge",
            Arguments = $"--kiosk --no-first-run --disable-infobars --start-fullscreen --user-data-dir=\"{profilePath}\" \"{url}\"",
            UseShellExecute = true
        };

        var browserProcess = new System.Diagnostics.Process
        {
            StartInfo = processStartInfo,
            EnableRaisingEvents = true // Important: Required to fire the Exited event
        };

        app.Services.GetRequiredService<SmartGPON.Web.Controllers.KioskProcessContext>().BrowserProcess = browserProcess;

        // When the browser is closed, trigger the ASP.NET Core shutdown
        browserProcess.Exited += (sender, e) =>
        {
            app.Logger.LogInformation("Browser closed. Stopping ASP.NET server...");
            lifetime.StopApplication();
        };

        browserProcess.Start();
    }
    catch (Exception ex)
    {
        app.Logger.LogError(ex, "Failed to launch Edge in Kiosk mode.");
    }
});

app.Run();
