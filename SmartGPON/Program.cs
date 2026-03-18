// ============================================================
// SmartGPON — Program.cs — FRESH START
// ============================================================
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SmartGPON.Core.Enums;
using SmartGPON.Core.Interfaces;
using SmartGPON.Infrastructure.Data;
using SmartGPON.Infrastructure.Services;

var builder = WebApplication.CreateBuilder(args);

// ── Database ────────────────────────────────────────────────
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"),
        sql =>
        {
            sql.EnableRetryOnFailure(3);
            sql.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
        }));

// ── Identity ────────────────────────────────────────────────
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

// ── Services DI ─────────────────────────────────────────────
builder.Services.AddMemoryCache();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<ISecurityService, SecurityService>();
builder.Services.AddScoped<ITreeService, TreeService>();
builder.Services.AddScoped<IUserProjectAssignmentService, UserProjectAssignmentService>();
builder.Services.AddScoped<IApprovalService, ApprovalService>();
builder.Services.AddScoped<IAuditLogService, AuditLogService>();
builder.Services.AddScoped<IFibreService, FibreService>();
builder.Services.AddScoped<IValidationService, ValidationService>();
builder.Services.AddScoped<ISplitterService, SplitterService>();
builder.Services.AddScoped<IAttackSimulationService, AttackSimulationService>();
builder.Services.AddControllersWithViews(options =>
{
    options.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true;
});

builder.Services.AddSingleton<SmartGPON.Web.Controllers.KioskProcessContext>();
builder.Services.AddHostedService<SmartGPON.Web.Services.TempUploadCleanupService>();

var app = builder.Build();

// ── Middleware ───────────────────────────────────────────────
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

// ── Initialisation DB (idempotent — ne recrée pas si déjà existante) ─
using (var scope = app.Services.CreateScope())
{
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    try
    {
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        // Crée la DB si elle n'existe pas — NE SUPPRIME RIEN si elle existe
        db.Database.SetCommandTimeout(300);
        db.Database.EnsureCreated();
        db.Database.SetCommandTimeout(60);

        // ── Rôles ────────────────────────────────────────────────────────
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        foreach (var role in new[] { UserRoles.Superviseur, UserRoles.Visiteur, UserRoles.Membre })
        {
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new IdentityRole(role));
        }

        // ── Utilisateurs de test ─────────────────────────────────────────
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        async Task<ApplicationUser?> CreateUser(string email, string pw, string firstName, string lastName, string role)
        {
            if (await userManager.FindByEmailAsync(email) != null) return null;
            var u = new ApplicationUser
            {
                UserName = email, Email = email, EmailConfirmed = true,
                FirstName = firstName, LastName = lastName,
                SecurityStamp = Guid.NewGuid().ToString()
            };
            var r = await userManager.CreateAsync(u, pw);
            if (r.Succeeded) await userManager.AddToRoleAsync(u, role);
            else logger.LogError("User seed failed [{Email}]: {E}", email, string.Join(";", r.Errors.Select(e => e.Description)));
            return r.Succeeded ? u : null;
        }

        var admin    = await CreateUser("admin@smartgpon.local",     "Admin@123456",    "Youssef",  "Admin",    UserRoles.Superviseur);
        var chef     = await CreateUser("chef@smartgpon.local",      "Chef@123456",     "Karim",    "Benali",   UserRoles.Membre);
        var dessin   = await CreateUser("dessin@smartgpon.local",    "Dessin@123456",   "Amina",    "Chaoui",   UserRoles.Membre);
        var visiteur = await CreateUser("visiteur@smartgpon.local",  "Visit@123456",    "Rachid",   "Messaoud", UserRoles.Visiteur);

        // ── Données de test (uniquement si Clients vide) ─────────────────
        if (!db.Clients.Any())
        {
            logger.LogInformation("Seeding test data…");

            // Clients
            var c1 = new SmartGPON.Core.Entities.Client { Nom = "Algérie Télécom",  Code = "AT",   IsActive = true };
            var c2 = new SmartGPON.Core.Entities.Client { Nom = "Mobilis",          Code = "MOB",  IsActive = true };
            var c3 = new SmartGPON.Core.Entities.Client { Nom = "Djezzy Fibre",     Code = "DJZ",  IsActive = true };
            db.Clients.AddRange(c1, c2, c3);
            await db.SaveChangesAsync();

            // Projets
            var p1 = new SmartGPON.Core.Entities.Projet { ClientId = c1.Id, Nom = "Déploiement FTTH Alger Centre",   Statut = SmartGPON.Core.Enums.ProjetStatut.EnCours };
            var p2 = new SmartGPON.Core.Entities.Projet { ClientId = c1.Id, Nom = "Extension FTTH Bab El Oued",      Statut = SmartGPON.Core.Enums.ProjetStatut.EnCours };
            var p3 = new SmartGPON.Core.Entities.Projet { ClientId = c2.Id, Nom = "Réseau GPON Oran Nord",           Statut = SmartGPON.Core.Enums.ProjetStatut.Suspendu };
            db.Projets.AddRange(p1, p2, p3);
            await db.SaveChangesAsync();

            // Zones
            var z1 = new SmartGPON.Core.Entities.Zone { ProjetId = p1.Id, Nom = "Zone A — Didouche Mourad",   Latitude = 36.7400m, Longitude = 3.0600m };
            var z2 = new SmartGPON.Core.Entities.Zone { ProjetId = p1.Id, Nom = "Zone B — Rue Larbi Ben M'hidi", Latitude = 36.7350m, Longitude = 3.0550m };
            var z3 = new SmartGPON.Core.Entities.Zone { ProjetId = p2.Id, Nom = "Zone BEO-1",                 Latitude = 36.7800m, Longitude = 3.0200m };
            var z4 = new SmartGPON.Core.Entities.Zone { ProjetId = p2.Id, Nom = "Zone BEO-2",                 Latitude = 36.7820m, Longitude = 3.0210m };
            var z5 = new SmartGPON.Core.Entities.Zone { ProjetId = p3.Id, Nom = "Zone Oran — Hai Seddikia",   Latitude = 35.6900m, Longitude = -0.6350m };
            db.Zones.AddRange(z1, z2, z3, z4, z5);
            await db.SaveChangesAsync();

            // OLTs
            var o1 = new SmartGPON.Core.Entities.Olt { ZoneId = z1.Id, Nom = "OLT-AC-01", IpAddress = "10.0.1.1",  NbrePorts = 16 };
            var o2 = new SmartGPON.Core.Entities.Olt { ZoneId = z2.Id, Nom = "OLT-AC-02", IpAddress = "10.0.1.2",  NbrePorts = 16 };
            var o3 = new SmartGPON.Core.Entities.Olt { ZoneId = z3.Id, Nom = "OLT-BE-01", IpAddress = "10.0.2.1",  NbrePorts = 8  };
            var o4 = new SmartGPON.Core.Entities.Olt { ZoneId = z5.Id, Nom = "OLT-OR-01", IpAddress = "10.10.1.1", NbrePorts = 32 };
            db.Olts.AddRange(o1, o2, o3, o4);
            await db.SaveChangesAsync();

            // FDTs
            var f1 = new SmartGPON.Core.Entities.Fdt { OltId = o1.Id, Nom = "FDT-AC-01-A", Latitude = 36.7410m,  Longitude = 3.0610m };
            var f2 = new SmartGPON.Core.Entities.Fdt { OltId = o1.Id, Nom = "FDT-AC-01-B", Latitude = 36.7415m,  Longitude = 3.0615m };
            var f3 = new SmartGPON.Core.Entities.Fdt { OltId = o2.Id, Nom = "FDT-AC-02-A", Latitude = 36.7360m,  Longitude = 3.0560m };
            var f4 = new SmartGPON.Core.Entities.Fdt { OltId = o3.Id, Nom = "FDT-BE-01-A", Latitude = 36.7810m,  Longitude = 3.0210m };
            var f5 = new SmartGPON.Core.Entities.Fdt { OltId = o4.Id, Nom = "FDT-OR-01-A", Latitude = 35.6910m,  Longitude = -0.6360m };
            db.Fdts.AddRange(f1, f2, f3, f4, f5);
            await db.SaveChangesAsync();

            // FATs
            var fa1 = new SmartGPON.Core.Entities.Fat { FdtId = f1.Id, Nom = "FAT-AC-01-A-1", Capacite = 8,  Latitude = 36.7420m, Longitude = 3.0620m };
            var fa2 = new SmartGPON.Core.Entities.Fat { FdtId = f1.Id, Nom = "FAT-AC-01-A-2", Capacite = 8,  Latitude = 36.7421m, Longitude = 3.0621m };
            var fa3 = new SmartGPON.Core.Entities.Fat { FdtId = f2.Id, Nom = "FAT-AC-01-B-1", Capacite = 16, Latitude = 36.7416m, Longitude = 3.0616m };
            var fa4 = new SmartGPON.Core.Entities.Fat { FdtId = f3.Id, Nom = "FAT-AC-02-A-1", Capacite = 8,  Latitude = 36.7365m, Longitude = 3.0565m };
            var fa5 = new SmartGPON.Core.Entities.Fat { FdtId = f4.Id, Nom = "FAT-BE-01-A-1", Capacite = 8,  Latitude = 36.7815m, Longitude = 3.0215m };
            db.Fats.AddRange(fa1, fa2, fa3, fa4, fa5);
            await db.SaveChangesAsync();

            // BPIs (accrochés aux FDTs directement, Capacite = nb ports)
            var b1  = new SmartGPON.Core.Entities.Bpi { FdtId = f1.Id, Nom = "BPI-AC-01-A-1", Capacite = 4, Latitude = 36.7425m, Longitude = 3.0625m };
            var b2  = new SmartGPON.Core.Entities.Bpi { FdtId = f1.Id, Nom = "BPI-AC-01-A-2", Capacite = 4, Latitude = 36.7426m, Longitude = 3.0626m };
            var b3  = new SmartGPON.Core.Entities.Bpi { FdtId = f2.Id, Nom = "BPI-AC-01-B-1", Capacite = 8, Latitude = 36.7418m, Longitude = 3.0618m };
            var b4  = new SmartGPON.Core.Entities.Bpi { FdtId = f3.Id, Nom = "BPI-AC-02-1",   Capacite = 4, Latitude = 36.7368m, Longitude = 3.0568m };
            var b5  = new SmartGPON.Core.Entities.Bpi { FdtId = f4.Id, Nom = "BPI-BE-01-1",   Capacite = 4, Latitude = 36.7818m, Longitude = 3.0218m };
            db.Bpis.AddRange(b1, b2, b3, b4, b5);
            await db.SaveChangesAsync();

            // Assignments (chef et dessin → p1 et p2)
            var chefUser   = await userManager.FindByEmailAsync("chef@smartgpon.local");
            var dessinUser = await userManager.FindByEmailAsync("dessin@smartgpon.local");
            if (chefUser != null)
            {
                db.UserProjectAssignments.AddRange(
                    new SmartGPON.Core.Entities.UserProjectAssignment { UserId = chefUser.Id,   ProjetId = p1.Id, IsActive = true, AssignmentType = SmartGPON.Core.Enums.AssignmentType.ChefProjet },
                    new SmartGPON.Core.Entities.UserProjectAssignment { UserId = chefUser.Id,   ProjetId = p2.Id, IsActive = true, AssignmentType = SmartGPON.Core.Enums.AssignmentType.ChefProjet }
                );
            }
            if (dessinUser != null)
            {
                db.UserProjectAssignments.AddRange(
                    new SmartGPON.Core.Entities.UserProjectAssignment { UserId = dessinUser.Id, ProjetId = p1.Id, IsActive = true, AssignmentType = SmartGPON.Core.Enums.AssignmentType.TechDessin }
                );
            }
            await db.SaveChangesAsync();

            logger.LogInformation("Seed terminé: 3 clients · 3 projets · 5 zones · 4 OLTs · 5 FDTs · 5 FATs · 5 BPIs · 4 utilisateurs.");
        }
        else
        {
            logger.LogInformation("DB déjà peuplée — seed ignoré.");
        }
    }
    catch (Exception ex)
    {
        scope.ServiceProvider.GetRequiredService<ILogger<Program>>().LogError(ex, "Seed error");
    }
}

// ── DatabaseInitializer — SQL DDL idempotent ─────────────────
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await SmartGPON.Infrastructure.Data.DatabaseInitializer.InitializeAsync(db);
}

// ── Kiosk launch logic (preserved as-is) ────────────────────
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
