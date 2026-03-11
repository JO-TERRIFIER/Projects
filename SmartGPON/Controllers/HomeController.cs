using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartGPON.Core.Entities;
using SmartGPON.Core.Enums;
using SmartGPON.Core.Interfaces;
using SmartGPON.Infrastructure.Data;

namespace SmartGPON.Web.Controllers
{
    [Authorize]
    public class HomeController : RbacControllerBase
    {
        private readonly IDashboardService _dashboard;

        public HomeController(IDashboardService dashboard, ApplicationDbContext db, IAuthorizationScopeService scope, IApprovalService approvals, IAuditService audit)
            : base(db, scope, approvals, audit)
        {
            _dashboard = dashboard;
        }

        public async Task<IActionResult> Index() => View(await _dashboard.GetDashboardAsync());
        public IActionResult GponBuilder() => View();
        public IActionResult ArchitectureGpon() => View();

        public async Task<IActionResult> Tree()
        {
            var clientsQuery = Db.Clients.AsNoTracking().AsQueryable();

            if (IsChefProjet && !IsSuperviseur)
            {
                var userId = CurrentUserId;
                var projIds = Db.Projets.Where(p => p.ProjectManagerId == userId).Select(p => p.Id).ToList();

                clientsQuery = clientsQuery
                    .Include(c => c.Projets.Where(p => projIds.Contains(p.Id))).ThenInclude(p => p.Zones)
                    .ThenInclude(z => z.Olts).ThenInclude(o => o.Fdts).ThenInclude(f => f.Fats)
                    .Include(c => c.Projets.Where(p => projIds.Contains(p.Id))).ThenInclude(p => p.Zones)
                    .ThenInclude(z => z.Olts).ThenInclude(o => o.Fdts).ThenInclude(f => f.Bpis);
            }
            else
            {
                clientsQuery = clientsQuery
                    .Include(c => c.Projets).ThenInclude(p => p.Zones)
                    .ThenInclude(z => z.Olts).ThenInclude(o => o.Fdts).ThenInclude(f => f.Fats)
                    .Include(c => c.Projets).ThenInclude(p => p.Zones)
                    .ThenInclude(z => z.Olts).ThenInclude(o => o.Fdts).ThenInclude(f => f.Bpis);
            }

            return View(await clientsQuery.OrderBy(c => c.Nom).ToListAsync());
        }

        [HttpGet] public async Task<IActionResult> GetClients() => Json(await Db.Clients.AsNoTracking().OrderBy(c => c.Nom).Select(c => new { c.Id, c.Nom }).ToListAsync());
        [HttpGet] public async Task<IActionResult> GetAllZones() => Json(await Db.Zones.AsNoTracking().Include(z => z.Projet).OrderBy(z => z.Nom).Select(z => new { z.Id, z.Nom, ProjetNom = z.Projet.Nom }).ToListAsync());
        [HttpGet] public async Task<IActionResult> GetOltsByZone(int zoneId) => Json(await Db.Olts.AsNoTracking().Where(o => o.ZoneId == zoneId).OrderBy(o => o.Nom).Select(o => new { o.Id, o.Nom, o.Marque, o.Modele }).ToListAsync());
        [HttpGet] public async Task<IActionResult> GetProjects()
        {
            var query = Db.Projets.AsNoTracking().AsQueryable();
            if (IsChefProjet && !IsSuperviseur)
            {
                var userId = CurrentUserId;
                query = query.Where(p => p.ProjectManagerId == userId);
            }
            return Json(await query.OrderBy(p => p.Nom).Select(p => new { p.Id, p.Nom, Statut = p.Statut.ToString() }).ToListAsync());
        }
        [HttpGet] public async Task<IActionResult> GetZonesByProject(int projectId)
        {
            if (IsChefProjet && !IsSuperviseur && !await CanChefProjectAsync(projectId)) return Forbid();
            return Json(await Db.Zones.AsNoTracking().Where(z => z.ProjetId == projectId).OrderBy(z => z.Nom).Select(z => new { z.Id, z.Nom }).ToListAsync());
        }

        [HttpGet] public async Task<IActionResult> GetOltsByProject(int projectId)
        {
            if (IsChefProjet && !IsSuperviseur && !await CanChefProjectAsync(projectId)) return Forbid();
            return Json(await Db.Olts.AsNoTracking().Include(o => o.Zone).Where(o => o.Zone.ProjetId == projectId).OrderBy(o => o.Nom).Select(o => new { o.Id, o.Nom, o.Marque, o.Modele, ZoneNom = o.Zone.Nom }).ToListAsync());
        }

        [HttpGet] public async Task<IActionResult> GetFdtsByOlt(int oltId)
        {
            var projetId = await ProjetIdFromOltAsync(oltId);
            if (IsChefProjet && !IsSuperviseur && !await CanChefProjectAsync(projetId)) return Forbid();
            return Json(await Db.Fdts.AsNoTracking().Where(f => f.OltId == oltId).OrderBy(f => f.Nom).Select(f => new { f.Id, f.Nom, f.Capacite }).ToListAsync());
        }

        [HttpGet]
        public async Task<IActionResult> GetItemsByFdt(int fdtId)
        {
            var projetId = await ProjetIdFromFdtAsync(fdtId);
            if (IsChefProjet && !IsSuperviseur && !await CanChefProjectAsync(projetId)) return Forbid();
            var fats = await Db.Fats.AsNoTracking().Where(f => f.FdtId == fdtId).OrderBy(f => f.Nom).Select(f => new { f.Id, f.Nom, Type = "FAT", f.Capacite }).ToListAsync();
            var bpis = await Db.Bpis.AsNoTracking().Where(b => b.FdtId == fdtId).OrderBy(b => b.Nom).Select(b => new { b.Id, b.Nom, Type = "BPI", b.Capacite }).ToListAsync();
            return Json(fats.Cast<object>().Concat(bpis.Cast<object>()));
        }

        [HttpGet]
        public async Task<IActionResult> GetProjectsByClient(int clientId)
        {
            var query = Db.Projets.AsNoTracking().Where(p => p.ClientId == clientId).AsQueryable();
            if (IsChefProjet && !IsSuperviseur)
            {
                var userId = CurrentUserId;
                query = query.Where(p => p.ProjectManagerId == userId);
            }
            return Json(await query.OrderBy(p => p.Nom).Select(p => new { p.Id, p.Nom, Statut = p.Statut.ToString() }).ToListAsync());
        }

        [HttpGet]
        public async Task<IActionResult> GetProjectArchitecture(int projectId)
        {
            if (IsChefProjet && !IsSuperviseur && !await CanChefProjectAsync(projectId)) return Forbid();

            var zones = await Db.Zones.AsNoTracking()
                .Where(z => z.ProjetId == projectId)
                .Include(z => z.Olts).ThenInclude(o => o.Fdts).ThenInclude(f => f.Fats)
                .Include(z => z.Olts).ThenInclude(o => o.Fdts).ThenInclude(f => f.Bpis)
                .OrderBy(z => z.Nom)
                .ToListAsync();

            var tree = zones.Select(z => new
            {
                z.Id,
                z.Nom,
                Type = "Zone",
                Children = z.Olts.OrderBy(o => o.Nom).Select(o => new
                {
                    o.Id,
                    o.Nom,
                    Type = "OLT",
                    Statut = o.Statut.ToString(),
                    Children = o.Fdts.OrderBy(f => f.Nom).Select(f => new
                    {
                        f.Id,
                        f.Nom,
                        Type = "FDT",
                        Children = f.Fats.OrderBy(fa => fa.Nom).Select(fa => new { fa.Id, fa.Nom, Type = "FAT", fa.Capacite }).Cast<object>()
                            .Concat(f.Bpis.OrderBy(b => b.Nom).Select(b => new { b.Id, b.Nom, Type = "BPI", b.Capacite }).Cast<object>())
                    })
                })
            });
            return Json(tree);
        }

        [HttpPost, Authorize(Roles = "Superviseur,ChefProjet")]
        public async Task<IActionResult> CreateProjectApi([FromBody] Projet m)
        {
            if (string.IsNullOrWhiteSpace(m.Nom)) return BadRequest(new { error = "Nom requis." });
            if (!await Db.Clients.AnyAsync(c => c.Id == m.ClientId)) return BadRequest(new { error = "Client invalide." });

            Db.Projets.Add(m);
            await Db.SaveChangesAsync();

            if (IsChefProjet && !IsSuperviseur)
            {
                m.ProjectManagerId = CurrentUserId;
                await Db.SaveChangesAsync();
            }

            await LogAsync(m.Id, "Create", nameof(Projet), m.Id, "Création projet via builder/API");
            return Json(new { m.Id, m.Nom });
        }

        [HttpPost, Authorize(Roles = "Superviseur,ChefProjet")]
        public async Task<IActionResult> CreateZoneApi([FromBody] Zone m)
        {
            if (string.IsNullOrWhiteSpace(m.Nom)) return BadRequest(new { error = "Nom requis." });
            var projetExists = await Db.Projets.AnyAsync(p => p.Id == m.ProjetId);
            if (!projetExists) return BadRequest(new { error = "Projet invalide." });
            if (!IsSuperviseur && !await CanChefProjectAsync(m.ProjetId)) return Forbid();

            Db.Zones.Add(m);
            await Db.SaveChangesAsync();
            await LogAsync(m.ProjetId, "Create", nameof(Zone), m.Id, "Création zone via builder/API");
            return Json(new { m.Id, m.Nom });
        }

        [HttpPost, Authorize(Roles = "Superviseur,ChefProjet,TechTerrain")]
        public async Task<IActionResult> CreateOltApi([FromBody] Olt m)
        {
            if (string.IsNullOrWhiteSpace(m.Nom)) return BadRequest(new { error = "Nom requis." });
            var projetId = await ProjetIdFromZoneAsync(m.ZoneId);
            if (projetId == 0) return BadRequest(new { error = "Zone invalide." });

            var allowed = IsSuperviseur || await CanChefProjectAsync(projetId) || await CanTechTerrainProjectAsync(projetId);
            if (!allowed) return Forbid();

            Db.Olts.Add(m);
            await Db.SaveChangesAsync();
            await LogAsync(projetId, "Create", nameof(Olt), m.Id, "Création OLT via builder/API");
            return Json(new { m.Id, m.Nom });
        }

        [HttpPost, Authorize(Roles = "Superviseur,ChefProjet,TechTerrain")]
        public async Task<IActionResult> CreateFdtApi([FromBody] Fdt m)
        {
            if (string.IsNullOrWhiteSpace(m.Nom)) return BadRequest(new { error = "Nom requis." });
            var projetId = await ProjetIdFromOltAsync(m.OltId);
            if (projetId == 0) return BadRequest(new { error = "OLT invalide." });

            var allowed = IsSuperviseur || await CanChefProjectAsync(projetId) || await CanTechTerrainProjectAsync(projetId);
            if (!allowed) return Forbid();

            Db.Fdts.Add(m);
            await Db.SaveChangesAsync();
            await LogAsync(projetId, "Create", nameof(Fdt), m.Id, "Création FDT via builder/API");
            return Json(new { m.Id, m.Nom });
        }

        [HttpPost, Authorize(Roles = "Superviseur,ChefProjet,TechTerrain")]
        public async Task<IActionResult> CreateFatApi([FromBody] Fat m)
        {
            var projetId = await ProjetIdFromFdtAsync(m.FdtId);
            if (projetId == 0) return BadRequest(new { error = "FDT invalide." });

            var allowed = IsSuperviseur || await CanChefProjectAsync(projetId) || await CanTechTerrainProjectAsync(projetId);
            if (!allowed) return Forbid();

            Db.Fats.Add(m);
            await Db.SaveChangesAsync();
            await LogAsync(projetId, "Create", nameof(Fat), m.Id, "Création FAT via builder/API");
            return Json(new { m.Id, m.Nom });
        }

        [HttpPost, Authorize(Roles = "Superviseur,ChefProjet,TechTerrain")]
        public async Task<IActionResult> CreateBpiApi([FromBody] Bpi m)
        {
            var projetId = await ProjetIdFromFdtAsync(m.FdtId);
            if (projetId == 0) return BadRequest(new { error = "FDT invalide." });

            var allowed = IsSuperviseur || await CanChefProjectAsync(projetId) || await CanTechTerrainProjectAsync(projetId);
            if (!allowed) return Forbid();

            Db.Bpis.Add(m);
            await Db.SaveChangesAsync();
            await LogAsync(projetId, "Create", nameof(Bpi), m.Id, "Création BPI via builder/API");
            return Json(new { m.Id, m.Nom });
        }

        [HttpGet]
        public async Task<IActionResult> GetProjectStats()
        {
            var now = DateTime.UtcNow;
            int year = now.Year;
            var result = new List<object>();

            for (int month = 1; month <= now.Month; month++)
            {
                var d = new DateTime(year, month, 1);
                var total = await Db.Projets.CountAsync(p => p.DateCreation <= d);
                var termines = await Db.Projets.CountAsync(p => p.DateCreation <= d && p.Statut == ProjetStatut.Termine);
                var enCours = await Db.Projets.CountAsync(p => p.DateCreation <= d && p.Statut == ProjetStatut.EnCours);
                var suspendus = await Db.Projets.CountAsync(p => p.DateCreation <= d && p.Statut == ProjetStatut.Suspendu);
                result.Add(new { Date = d.ToString("o"), Total = total, Termines = termines, EnCours = enCours, Suspendus = suspendus });
            }
            return Json(result);
        }

        [HttpPost]
        public IActionResult Shutdown([FromServices] Microsoft.Extensions.Hosting.IHostApplicationLifetime lifetime, [FromServices] KioskProcessContext ctx)
        {
            try
            {
                var searcher = new System.Diagnostics.ProcessStartInfo("powershell", "-NoProfile -Command \"Get-WmiObject Win32_Process -Filter \\\"Name='msedge.exe'\\\" | Where-Object { $_.CommandLine -match 'SmartGPON_Kiosk' } | ForEach-Object { Stop-Process -Id $_.ProcessId -Force }\"") { CreateNoWindow = true, UseShellExecute = false };
                System.Diagnostics.Process.Start(searcher)?.WaitForExit();
            }
            catch { }
            lifetime.StopApplication();
            return Ok(new { message = "Shutting down" });
        }
    }
}
