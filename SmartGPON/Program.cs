// SmartGPON v3 – Program.cs
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SmartGPON.Core.Interfaces;
using SmartGPON.Infrastructure.Data;
using SmartGPON.Infrastructure.Services;

var builder = WebApplication.CreateBuilder(args);

// ── Database ──────────────────────────────────────────────────
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"),
        sql => sql.EnableRetryOnFailure(3)));

// ── Identity ─────────────────────────────────────────────────
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

// ── Auth cookie ───────────────────────────────────────────────
builder.Services.ConfigureApplicationCookie(opt =>
{
    opt.LoginPath        = "/Account/Login";
    opt.AccessDeniedPath = "/Account/AccessDenied";
    opt.SlidingExpiration = true;
    opt.ExpireTimeSpan   = TimeSpan.FromHours(8);
    opt.Cookie.HttpOnly  = true;
    opt.Cookie.SameSite  = Microsoft.AspNetCore.Http.SameSiteMode.Lax;
});

// ── Application services ──────────────────────────────────────
builder.Services.AddMemoryCache();
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<ISecurityService, SecurityService>();
builder.Services.AddScoped<ITreeService, TreeService>();
builder.Services.AddControllersWithViews();

var app = builder.Build();

// ── Pipeline ─────────────────────────────────────────────────
if (!app.Environment.IsDevelopment())
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

// ── Seed ─────────────────────────────────────────────────────
using (var scope = app.Services.CreateScope())
{
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    try
    {
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        // EnsureCreated : crée les tables depuis le modèle sans migrations EF Core
        db.Database.EnsureCreated();

        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        foreach (var role in new[] { "Admin", "Superviseur", "Technicien", "Readonly" })
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
                logger.LogInformation("Admin created: admin@smartgpon.local / Admin@12345");
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

app.Run();
