// SmartGPON v3 â€“ Controllers.cs
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartGPON.Core.Entities;
using SmartGPON.Core.Enums;
using SmartGPON.Core.Interfaces;
using SmartGPON.Infrastructure.Data;
using SmartGPON.Web.ViewModels;

namespace SmartGPON.Web.Controllers
{
    public class KioskProcessContext
    {
        public System.Diagnostics.Process? BrowserProcess { get; set; }
    }

    [Authorize]
    public class HomeController : Controller
    {
        private readonly IDashboardService _dashboard;
        private readonly ApplicationDbContext _db;
        public HomeController(IDashboardService dashboard, ApplicationDbContext db)
        { _dashboard = dashboard; _db = db; }

        public async Task<IActionResult> Index()
        {
            var vm = await _dashboard.GetDashboardAsync();
            return View(vm);
        }

        public IActionResult GponBuilder() => View();
        public IActionResult ArchitectureGpon() => View();

        [HttpGet]
        public async Task<IActionResult> GetClients()
        {
            var list = await _db.Clients.AsNoTracking().OrderBy(c => c.Nom)
                .Select(c => new { c.Id, c.Nom }).ToListAsync();
            return Json(list);
        }

        [HttpGet]
        public async Task<IActionResult> GetAllZones()
        {
            var list = await _db.Zones.AsNoTracking().Include(z => z.Projet).OrderBy(z => z.Nom)
                .Select(z => new { z.Id, z.Nom, ProjetNom = z.Projet.Nom }).ToListAsync();
            return Json(list);
        }

        [HttpGet]
        public async Task<IActionResult> GetOltsByZone(int zoneId)
        {
            var list = await _db.Olts.AsNoTracking().Where(o => o.ZoneId == zoneId)
                .OrderBy(o => o.Nom).Select(o => new { o.Id, o.Nom, o.Marque, o.Modele }).ToListAsync();
            return Json(list);
        }

        [HttpPost, Authorize(Roles = "Admin,Technicien")]
        public async Task<IActionResult> CreateZoneApi([FromBody] Zone m)
        {
            if (string.IsNullOrWhiteSpace(m.Nom)) return BadRequest(new { error = "Nom requis." });
            var projet = await _db.Projets.FindAsync(m.ProjetId);
            if (projet == null) return BadRequest(new { error = "Projet invalide." });
            _db.Zones.Add(m); await _db.SaveChangesAsync();
            return Json(new { m.Id, m.Nom });
        }

        // ── JSON APIs for Dashboard architecture tree ──
        [HttpGet]
        public async Task<IActionResult> GetProjectArchitecture(int projectId)
        {
            var zones = await _db.Zones.AsNoTracking()
                .Where(z => z.ProjetId == projectId)
                .Include(z => z.Olts).ThenInclude(o => o.Fdts).ThenInclude(f => f.Fats)
                .Include(z => z.Olts).ThenInclude(o => o.Fdts).ThenInclude(f => f.Bpis)
                .OrderBy(z => z.Nom)
                .ToListAsync();

            var tree = zones.Select(z => new
            {
                z.Id, z.Nom, Type = "Zone",
                Children = z.Olts.OrderBy(o => o.Nom).Select(o => new
                {
                    o.Id, o.Nom, Type = "OLT", Statut = o.Statut.ToString(), o.Marque, o.Modele,
                    Children = o.Fdts.OrderBy(f => f.Nom).Select(f => new
                    {
                        f.Id, f.Nom, Type = "FDT", f.NbSplitters1x8, f.NbSplitters1x64,
                        Children = f.Fats.OrderBy(fa => fa.Nom).Select(fa => new
                            { fa.Id, fa.Nom, Type = "FAT", fa.Capacite })
                            .Cast<object>()
                            .Concat(f.Bpis.OrderBy(b => b.Nom).Select(b => new
                            { b.Id, b.Nom, Type = "BPI", b.Capacite, b.NbSplitters1x8 })
                            .Cast<object>())
                    })
                })
            });
            return Json(tree);
        }

        // ── JSON APIs for GPON Builder ──
        [HttpGet]
        public async Task<IActionResult> GetProjects()
        {
            var list = await _db.Projets.AsNoTracking().OrderBy(p => p.Nom)
                .Select(p => new { p.Id, p.Nom, Statut = p.Statut.ToString() }).ToListAsync();
            return Json(list);
        }

        [HttpGet]
        public async Task<IActionResult> GetZonesByProject(int projectId)
        {
            var list = await _db.Zones.AsNoTracking().Where(z => z.ProjetId == projectId)
                .OrderBy(z => z.Nom).Select(z => new { z.Id, z.Nom }).ToListAsync();
            return Json(list);
        }

        [HttpGet]
        public async Task<IActionResult> GetOltsByProject(int projectId)
        {
            var list = await _db.Olts.AsNoTracking()
                .Include(o => o.Zone)
                .Where(o => o.Zone.ProjetId == projectId)
                .OrderBy(o => o.Nom)
                .Select(o => new { o.Id, o.Nom, o.Marque, o.Modele, ZoneNom = o.Zone.Nom })
                .ToListAsync();
            return Json(list);
        }

        [HttpGet]
        public async Task<IActionResult> GetFdtsByOlt(int oltId)
        {
            var list = await _db.Fdts.AsNoTracking().Where(f => f.OltId == oltId)
                .OrderBy(f => f.Nom).Select(f => new { f.Id, f.Nom, f.Capacite }).ToListAsync();
            return Json(list);
        }

        [HttpGet]
        public async Task<IActionResult> GetItemsByFdt(int fdtId)
        {
            var fats = await _db.Fats.AsNoTracking().Where(f => f.FdtId == fdtId)
                .OrderBy(f => f.Nom).Select(f => new { f.Id, f.Nom, Type = "FAT", f.Capacite }).ToListAsync();
            var bpis = await _db.Bpis.AsNoTracking().Where(b => b.FdtId == fdtId)
                .OrderBy(b => b.Nom).Select(b => new { b.Id, b.Nom, Type = "BPI", b.Capacite }).ToListAsync();
            var items = fats.Cast<object>().Concat(bpis.Cast<object>()).ToList();
            return Json(items);
        }

        // ── Create APIs ──
        [HttpPost, Authorize(Roles = "Admin,Technicien")]
        public async Task<IActionResult> CreateProjectApi([FromBody] Projet m)
        {
            if (string.IsNullOrWhiteSpace(m.Nom)) return BadRequest(new { error = "Nom requis." });
            var client = await _db.Clients.FindAsync(m.ClientId);
            if (client == null) return BadRequest(new { error = "Client invalide." });
            _db.Projets.Add(m); await _db.SaveChangesAsync();
            return Json(new { m.Id, m.Nom });
        }

        [HttpPost, Authorize(Roles = "Admin,Technicien")]
        public async Task<IActionResult> CreateOltApi([FromBody] Olt m)
        {
            if (string.IsNullOrWhiteSpace(m.Nom)) return BadRequest(new { error = "Nom requis." });
            var zone = await _db.Zones.FindAsync(m.ZoneId);
            if (zone == null) return BadRequest(new { error = "Zone invalide." });
            _db.Olts.Add(m); await _db.SaveChangesAsync();
            return Json(new { m.Id, m.Nom });
        }

        [HttpPost, Authorize(Roles = "Admin,Technicien")]
        public async Task<IActionResult> CreateFdtApi([FromBody] Fdt m)
        {
            if (string.IsNullOrWhiteSpace(m.Nom)) return BadRequest(new { error = "Nom requis." });
            var olt = await _db.Olts.FindAsync(m.OltId);
            if (olt == null) return BadRequest(new { error = "OLT invalide." });
            _db.Fdts.Add(m); await _db.SaveChangesAsync();
            return Json(new { m.Id, m.Nom });
        }

        [HttpPost, Authorize(Roles = "Admin,Technicien")]
        public async Task<IActionResult> CreateFatApi([FromBody] Fat m)
        {
            if (string.IsNullOrWhiteSpace(m.Nom)) return BadRequest(new { error = "Nom requis." });
            var fdt = await _db.Fdts.FindAsync(m.FdtId);
            if (fdt == null) return BadRequest(new { error = "FDT invalide." });
            _db.Fats.Add(m); await _db.SaveChangesAsync();
            return Json(new { m.Id, m.Nom });
        }

        [HttpPost, Authorize(Roles = "Admin,Technicien")]
        public async Task<IActionResult> CreateBpiApi([FromBody] Bpi m)
        {
            if (string.IsNullOrWhiteSpace(m.Nom)) return BadRequest(new { error = "Nom requis." });
            var fdt = await _db.Fdts.FindAsync(m.FdtId);
            if (fdt == null) return BadRequest(new { error = "FDT invalide." });
            _db.Bpis.Add(m); await _db.SaveChangesAsync();
            return Json(new { m.Id, m.Nom });
        }

        [HttpGet]
        public async Task<IActionResult> GetProjectsByClient(int clientId)
        {
            var list = await _db.Projets.AsNoTracking()
                .Where(p => p.ClientId == clientId)
                .OrderBy(p => p.Nom)
                .Select(p => new { p.Id, p.Nom, Statut = p.Statut.ToString() })
                .ToListAsync();
            return Json(list);
        }

        [HttpGet]
        public async Task<IActionResult> GetProjectStats()
        {
            var now = DateTime.UtcNow;
            int year = now.Year;
            var result = new List<object>();

            // Points on the 1st of each month up to the current month
            for (int month = 1; month <= now.Month; month++)
            {
                var d = new DateTime(year, month, 1);
                var total = await _db.Projets.CountAsync(p => p.DateCreation <= d);
                var termines = await _db.Projets.CountAsync(p => p.DateCreation <= d && p.Statut == ProjetStatut.Termine);
                var enCours = await _db.Projets.CountAsync(p => p.DateCreation <= d && p.Statut == ProjetStatut.EnCours);
                var suspendus = await _db.Projets.CountAsync(p => p.DateCreation <= d && p.Statut == ProjetStatut.Suspendu);
                
                result.Add(new { Date = d.ToString("o"), Total = total, Termines = termines, EnCours = enCours, Suspendus = suspendus });
            }

            // Final point for today, to end the line exactly on the current day
            if (now.Day > 1 || now.Hour > 0)
            {
                var total = await _db.Projets.CountAsync(p => p.DateCreation <= now);
                var termines = await _db.Projets.CountAsync(p => p.DateCreation <= now && p.Statut == ProjetStatut.Termine);
                var enCours = await _db.Projets.CountAsync(p => p.DateCreation <= now && p.Statut == ProjetStatut.EnCours);
                var suspendus = await _db.Projets.CountAsync(p => p.DateCreation <= now && p.Statut == ProjetStatut.Suspendu);

                result.Add(new { Date = now.ToString("o"), Total = total, Termines = termines, EnCours = enCours, Suspendus = suspendus });
            }

            return Json(result);
        }

        [HttpPost]
        public IActionResult Shutdown([FromServices] Microsoft.Extensions.Hosting.IHostApplicationLifetime lifetime, [FromServices] KioskProcessContext ctx)
        {
            try {
                if (ctx.BrowserProcess != null && !ctx.BrowserProcess.HasExited) {
                    ctx.BrowserProcess.Kill(true); // Tue le processus Edge Kiosk complet
                }
            } 
            catch { /* Ignorer les erreurs si le processus est déjà mort */ }

            lifetime.StopApplication();
            return Ok(new { message = "Shutting down" });
        }
    }
}

namespace SmartGPON.Web.Controllers
{
    [Authorize]
    public class ClientsController : Controller
    {
        private readonly ApplicationDbContext _db;
        public ClientsController(ApplicationDbContext db) { _db = db; }

        public async Task<IActionResult> Index() =>
            View(await _db.Clients.AsNoTracking().OrderBy(c => c.Nom).ToListAsync());

        [Authorize(Roles = "Admin,Technicien")]
        public IActionResult Create() => View(new Client());

        [HttpPost, ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Technicien")]
        public async Task<IActionResult> Create(Client m)
        {
            if (!ModelState.IsValid) return View(m);
            _db.Clients.Add(m); await _db.SaveChangesAsync();
            TempData["Success"] = "Client crÃ©Ã©.";
            return RedirectToAction(nameof(Index));
        }

        [Authorize(Roles = "Admin,Technicien")]
        public async Task<IActionResult> Edit(int id)
        {
            var m = await _db.Clients.FindAsync(id);
            return m == null ? NotFound() : View(m);
        }

        [HttpPost, ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Technicien")]
        public async Task<IActionResult> Edit(Client m)
        {
            if (!ModelState.IsValid) return View(m);
            _db.Clients.Update(m); await _db.SaveChangesAsync();
            TempData["Success"] = "Client mis Ã  jour.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost, ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var m = await _db.Clients.FindAsync(id);
            if (m != null) { _db.Clients.Remove(m); await _db.SaveChangesAsync(); }
            TempData["Success"] = "Client supprimÃ©.";
            return RedirectToAction(nameof(Index));
        }
    }
}

namespace SmartGPON.Web.Controllers
{
    [Authorize]
    public class ProjetsController : Controller
    {
        private readonly ApplicationDbContext _db;
        public ProjetsController(ApplicationDbContext db) { _db = db; }

        public async Task<IActionResult> Index() =>
            View(await _db.Projets.AsNoTracking().Include(p => p.Client).OrderBy(p => p.Nom).ToListAsync());

        [Authorize(Roles = "Admin,Technicien")]
        public async Task<IActionResult> Create()
        {
            ViewBag.Clients = await _db.Clients.AsNoTracking().OrderBy(c => c.Nom).ToListAsync();
            return View(new Projet());
        }

        [HttpPost, ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Technicien")]
        public async Task<IActionResult> Create(Projet m)
        {
            if (!ModelState.IsValid) { ViewBag.Clients = await _db.Clients.AsNoTracking().OrderBy(c => c.Nom).ToListAsync(); return View(m); }
            _db.Projets.Add(m); await _db.SaveChangesAsync();
            TempData["Success"] = "Projet crÃ©Ã©."; return RedirectToAction(nameof(Index));
        }

        [Authorize(Roles = "Admin,Technicien")]
        public async Task<IActionResult> Edit(int id)
        {
            var m = await _db.Projets.FindAsync(id);
            if (m == null) return NotFound();
            ViewBag.Clients = await _db.Clients.AsNoTracking().OrderBy(c => c.Nom).ToListAsync();
            ViewBag.Resources = await _db.Resources.AsNoTracking().Where(r => r.ProjetId == id).OrderByDescending(r => r.DateUpload).ToListAsync();
            return View(m);
        }

        [HttpPost, ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Technicien")]
        public async Task<IActionResult> Edit(Projet m)
        {
            if (!ModelState.IsValid) { ViewBag.Clients = await _db.Clients.AsNoTracking().OrderBy(c => c.Nom).ToListAsync(); return View(m); }
            _db.Projets.Update(m); await _db.SaveChangesAsync();
            TempData["Success"] = "Projet mis Ã  jour."; return RedirectToAction(nameof(Index));
        }

        [HttpPost, ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var m = await _db.Projets.FindAsync(id);
            if (m != null) { _db.Projets.Remove(m); await _db.SaveChangesAsync(); }
            TempData["Success"] = "Projet supprimÃ©."; return RedirectToAction(nameof(Index));
        }
    }
}

namespace SmartGPON.Web.Controllers
{
    [Authorize]
    public class ZonesController : Controller
    {
        private readonly ApplicationDbContext _db;
        public ZonesController(ApplicationDbContext db) { _db = db; }

        public async Task<IActionResult> Index() =>
            View(await _db.Zones.AsNoTracking().Include(z => z.Projet).OrderBy(z => z.Nom).ToListAsync());

        [Authorize(Roles = "Admin,Technicien")]
        public async Task<IActionResult> Create()
        {
            ViewBag.Projets = await _db.Projets.AsNoTracking().OrderBy(p => p.Nom).ToListAsync();
            return View(new Zone());
        }

        [HttpPost, ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Technicien")]
        public async Task<IActionResult> Create(Zone m)
        {
            if (!ModelState.IsValid) { ViewBag.Projets = await _db.Projets.AsNoTracking().OrderBy(p => p.Nom).ToListAsync(); return View(m); }
            _db.Zones.Add(m); await _db.SaveChangesAsync();
            TempData["Success"] = "Zone crÃ©Ã©e."; return RedirectToAction(nameof(Index));
        }

        [Authorize(Roles = "Admin,Technicien")]
        public async Task<IActionResult> Edit(int id)
        {
            var m = await _db.Zones.FindAsync(id);
            if (m == null) return NotFound();
            ViewBag.Projets = await _db.Projets.AsNoTracking().OrderBy(p => p.Nom).ToListAsync();
            ViewBag.Resources = await _db.Resources.AsNoTracking().Where(r => r.ZoneId == id).OrderByDescending(r => r.DateUpload).ToListAsync();
            return View(m);
        }

        [HttpPost, ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Technicien")]
        public async Task<IActionResult> Edit(Zone m)
        {
            if (!ModelState.IsValid) { ViewBag.Projets = await _db.Projets.AsNoTracking().OrderBy(p => p.Nom).ToListAsync(); return View(m); }
            _db.Zones.Update(m); await _db.SaveChangesAsync();
            TempData["Success"] = "Zone mise Ã  jour."; return RedirectToAction(nameof(Index));
        }

        [HttpPost, ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var m = await _db.Zones.FindAsync(id);
            if (m != null) { _db.Zones.Remove(m); await _db.SaveChangesAsync(); }
            TempData["Success"] = "Zone supprimÃ©e."; return RedirectToAction(nameof(Index));
        }
    }
}

namespace SmartGPON.Web.Controllers
{
    [Authorize]
    public class OltsController : Controller
    {
        private readonly ApplicationDbContext _db;
        public OltsController(ApplicationDbContext db) { _db = db; }

        public async Task<IActionResult> Index() =>
            View(await _db.Olts.AsNoTracking()
                .Include(o => o.Zone).ThenInclude(z => z.Projet)
                .OrderBy(o => o.Nom).ToListAsync());

        [Authorize(Roles = "Admin,Technicien")]
        public async Task<IActionResult> Create()
        {
            ViewBag.Zones = await _db.Zones.AsNoTracking().OrderBy(z => z.Nom).ToListAsync();
            return View(new Olt());
        }

        [HttpPost, ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Technicien")]
        public async Task<IActionResult> Create(Olt m)
        {
            if (!ModelState.IsValid) { ViewBag.Zones = await _db.Zones.AsNoTracking().OrderBy(z => z.Nom).ToListAsync(); return View(m); }
            _db.Olts.Add(m); await _db.SaveChangesAsync();
            TempData["Success"] = "OLT crÃ©Ã©."; return RedirectToAction(nameof(Index));
        }

        [Authorize(Roles = "Admin,Technicien")]
        public async Task<IActionResult> Edit(int id)
        {
            var m = await _db.Olts.FindAsync(id);
            if (m == null) return NotFound();
            ViewBag.Zones = await _db.Zones.AsNoTracking().OrderBy(z => z.Nom).ToListAsync();
            return View(m);
        }

        [HttpPost, ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Technicien")]
        public async Task<IActionResult> Edit(Olt m)
        {
            if (!ModelState.IsValid) { ViewBag.Zones = await _db.Zones.AsNoTracking().OrderBy(z => z.Nom).ToListAsync(); return View(m); }
            _db.Olts.Update(m); await _db.SaveChangesAsync();
            TempData["Success"] = "OLT mis Ã  jour."; return RedirectToAction(nameof(Index));
        }

        [HttpPost, ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var m = await _db.Olts.FindAsync(id);
            if (m != null) { _db.Olts.Remove(m); await _db.SaveChangesAsync(); }
            TempData["Success"] = "OLT supprimÃ©."; return RedirectToAction(nameof(Index));
        }
    }
}

namespace SmartGPON.Web.Controllers
{
    [Authorize]
    public class FdtsController : Controller
    {
        private readonly ApplicationDbContext _db;
        public FdtsController(ApplicationDbContext db) { _db = db; }

        public async Task<IActionResult> Index() =>
            View(await _db.Fdts.AsNoTracking().Include(f => f.Olt).OrderBy(f => f.Nom).ToListAsync());

        [Authorize(Roles = "Admin,Technicien")]
        public async Task<IActionResult> Create()
        {
            ViewBag.Olts = await _db.Olts.AsNoTracking().OrderBy(o => o.Nom).ToListAsync();
            return View(new Fdt());
        }

        [HttpPost, ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Technicien")]
        public async Task<IActionResult> Create(Fdt m)
        {
            if (!ModelState.IsValid) { ViewBag.Olts = await _db.Olts.AsNoTracking().OrderBy(o => o.Nom).ToListAsync(); return View(m); }
            _db.Fdts.Add(m); await _db.SaveChangesAsync();
            TempData["Success"] = "FDT crÃ©Ã©."; return RedirectToAction(nameof(Index));
        }

        [Authorize(Roles = "Admin,Technicien")]
        public async Task<IActionResult> Edit(int id)
        {
            var m = await _db.Fdts.FindAsync(id);
            if (m == null) return NotFound();
            ViewBag.Olts = await _db.Olts.AsNoTracking().OrderBy(o => o.Nom).ToListAsync();
            return View(m);
        }

        [HttpPost, ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Technicien")]
        public async Task<IActionResult> Edit(Fdt m)
        {
            if (!ModelState.IsValid) { ViewBag.Olts = await _db.Olts.AsNoTracking().OrderBy(o => o.Nom).ToListAsync(); return View(m); }
            _db.Fdts.Update(m); await _db.SaveChangesAsync();
            TempData["Success"] = "FDT mis Ã  jour."; return RedirectToAction(nameof(Index));
        }

        [HttpPost, ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var m = await _db.Fdts.FindAsync(id);
            if (m != null) { _db.Fdts.Remove(m); await _db.SaveChangesAsync(); }
            TempData["Success"] = "FDT supprimÃ©."; return RedirectToAction(nameof(Index));
        }
    }
}

namespace SmartGPON.Web.Controllers
{
    [Authorize]
    public class FatsController : Controller
    {
        private readonly ApplicationDbContext _db;
        public FatsController(ApplicationDbContext db) { _db = db; }

        public async Task<IActionResult> Index() =>
            View(await _db.Fats.AsNoTracking()
                .Include(f => f.Fdt).ThenInclude(fd => fd.Olt)
                .OrderBy(f => f.Nom)
                .ToListAsync());

        [Authorize(Roles = "Admin,Technicien")]
        public async Task<IActionResult> Create()
        {
            ViewBag.Fdts = await _db.Fdts.AsNoTracking().OrderBy(f => f.Nom).ToListAsync();
            return View(new Fat());
        }

        [HttpPost, ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Technicien")]
        public async Task<IActionResult> Create(Fat m)
        {
            if (!ModelState.IsValid) { ViewBag.Fdts = await _db.Fdts.AsNoTracking().OrderBy(f => f.Nom).ToListAsync(); return View(m); }
            _db.Fats.Add(m); await _db.SaveChangesAsync();
            TempData["Success"] = "FAT crÃ©Ã©."; return RedirectToAction(nameof(Index));
        }

        [Authorize(Roles = "Admin,Technicien")]
        public async Task<IActionResult> Edit(int id)
        {
            var m = await _db.Fats.FindAsync(id);
            if (m == null) return NotFound();
            ViewBag.Fdts = await _db.Fdts.AsNoTracking().OrderBy(f => f.Nom).ToListAsync();
            return View(m);
        }

        [HttpPost, ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Technicien")]
        public async Task<IActionResult> Edit(Fat m)
        {
            if (!ModelState.IsValid) { ViewBag.Fdts = await _db.Fdts.AsNoTracking().OrderBy(f => f.Nom).ToListAsync(); return View(m); }
            _db.Fats.Update(m); await _db.SaveChangesAsync();
            TempData["Success"] = "FAT mis Ã  jour."; return RedirectToAction(nameof(Index));
        }

        [HttpPost, ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var m = await _db.Fats.FindAsync(id);
            if (m != null) { _db.Fats.Remove(m); await _db.SaveChangesAsync(); }
            TempData["Success"] = "FAT supprimÃ©."; return RedirectToAction(nameof(Index));
        }
    }
}

namespace SmartGPON.Web.Controllers
{
    [Authorize]
    public class BpisController : Controller
    {
        private readonly ApplicationDbContext _db;
        public BpisController(ApplicationDbContext db) { _db = db; }

        public async Task<IActionResult> Index() =>
            View(await _db.Bpis.AsNoTracking()
                .Include(b => b.Fdt)
                .OrderBy(b => b.Nom).ToListAsync());

        [Authorize(Roles = "Admin,Technicien")]
        public async Task<IActionResult> Create()
        {
            ViewBag.Fdts = await _db.Fdts.AsNoTracking().OrderBy(f => f.Nom).ToListAsync();
            return View(new Bpi());
        }

        [HttpPost, ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Technicien")]
        public async Task<IActionResult> Create(Bpi m)
        {
            if (!ModelState.IsValid) { ViewBag.Fdts = await _db.Fdts.AsNoTracking().OrderBy(f => f.Nom).ToListAsync(); return View(m); }
            _db.Bpis.Add(m); await _db.SaveChangesAsync();
            TempData["Success"] = "BPI crÃ©Ã©."; return RedirectToAction(nameof(Index));
        }

        [Authorize(Roles = "Admin,Technicien")]
        public async Task<IActionResult> Edit(int id)
        {
            var m = await _db.Bpis.FindAsync(id);
            if (m == null) return NotFound();
            ViewBag.Fdts = await _db.Fdts.AsNoTracking().OrderBy(f => f.Nom).ToListAsync();
            return View(m);
        }

        [HttpPost, ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Technicien")]
        public async Task<IActionResult> Edit(Bpi m)
        {
            if (!ModelState.IsValid) { ViewBag.Fdts = await _db.Fdts.AsNoTracking().OrderBy(f => f.Nom).ToListAsync(); return View(m); }
            _db.Bpis.Update(m); await _db.SaveChangesAsync();
            TempData["Success"] = "BPI mis Ã  jour."; return RedirectToAction(nameof(Index));
        }

        [HttpPost, ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var m = await _db.Bpis.FindAsync(id);
            if (m != null) { _db.Bpis.Remove(m); await _db.SaveChangesAsync(); }
            TempData["Success"] = "BPI supprimÃ©."; return RedirectToAction(nameof(Index));
        }
    }
}

namespace SmartGPON.Web.Controllers
{
    [Authorize]
    public class ResourcesController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly IWebHostEnvironment _env;
        private static readonly string[] ZoneExtensions = { ".dwg", ".pdf", ".png", ".jpg", ".jpeg", ".xlsx" };
        private static readonly string[] ProjetExtensions = { ".pdf", ".png", ".jpg", ".jpeg", ".xlsx" };

        public ResourcesController(ApplicationDbContext db, IWebHostEnvironment env) { _db = db; _env = env; }

        [HttpPost, ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Technicien")]
        public async Task<IActionResult> Upload(int? zoneId, int? projetId, IFormFile file)
        {
            if (file == null || file.Length == 0) { TempData["Error"] = "Fichier vide."; return Back(zoneId, projetId); }

            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            var allowed = zoneId.HasValue ? ZoneExtensions : ProjetExtensions;
            if (!allowed.Contains(ext)) { TempData["Error"] = $"Type {ext} non autorisÃ©."; return Back(zoneId, projetId); }

            var folder = zoneId.HasValue
                ? Path.Combine(_env.WebRootPath, "resources", "zones", zoneId.ToString()!)
                : Path.Combine(_env.WebRootPath, "resources", "projets", projetId.ToString()!);
            Directory.CreateDirectory(folder);

            var fileName = $"{Guid.NewGuid():N}{ext}";
            var filePath = Path.Combine(folder, fileName);
            using (var stream = new FileStream(filePath, FileMode.Create))
                await file.CopyToAsync(stream);

            _db.Resources.Add(new Resource
            {
                ZoneId = zoneId, ProjetId = projetId,
                NomFichier = file.FileName,
                CheminFichier = filePath,
                TypeFichier = ext,
                TailleFichier = file.Length
            });
            await _db.SaveChangesAsync();
            TempData["Success"] = "Fichier uploadÃ©.";
            return Back(zoneId, projetId);
        }

        public async Task<IActionResult> Download(int id)
        {
            var r = await _db.Resources.FindAsync(id);
            if (r == null || !System.IO.File.Exists(r.CheminFichier)) return NotFound();
            var ct = r.TypeFichier switch
            {
                ".pdf" => "application/pdf",
                ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                ".dwg" => "application/acad",
                ".png" => "image/png",
                ".jpg" or ".jpeg" => "image/jpeg",
                _ => "application/octet-stream"
            };
            return PhysicalFile(r.CheminFichier, ct, r.NomFichier);
        }

        [HttpPost, ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var r = await _db.Resources.FindAsync(id);
            if (r != null)
            {
                if (System.IO.File.Exists(r.CheminFichier)) System.IO.File.Delete(r.CheminFichier);
                _db.Resources.Remove(r); await _db.SaveChangesAsync();
            }
            TempData["Success"] = "Fichier supprimÃ©.";
            return Back(r?.ZoneId, r?.ProjetId);
        }

        private IActionResult Back(int? zoneId, int? projetId)
        {
            if (zoneId.HasValue) return RedirectToAction("Edit", "Zones", new { id = zoneId });
            if (projetId.HasValue) return RedirectToAction("Edit", "Projets", new { id = projetId });
            return RedirectToAction("Index", "Home");
        }
    }
}

namespace SmartGPON.Web.Controllers
{
    [Authorize]
    public class TechniciensController : Controller
    {
        private readonly ApplicationDbContext _db;
        public TechniciensController(ApplicationDbContext db) { _db = db; }

        public async Task<IActionResult> Index() =>
            View(await _db.Techniciens.AsNoTracking().Include(t => t.Projet).Include(t => t.Zone).OrderBy(t => t.Nom).ToListAsync());

        [Authorize(Roles = "Admin,Technicien")]
        public async Task<IActionResult> Create()
        {
            ViewBag.Projets = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(
                await _db.Projets.AsNoTracking().OrderBy(p => p.Nom).ToListAsync(), "Id", "Nom");
            ViewBag.Zones = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(
                await _db.Zones.AsNoTracking().OrderBy(z => z.Nom).ToListAsync(), "Id", "Nom");
            return View(new Technicien());
        }

        [HttpPost, ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Technicien")]
        public async Task<IActionResult> Create(Technicien m)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Projets = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(
                    await _db.Projets.AsNoTracking().OrderBy(p => p.Nom).ToListAsync(), "Id", "Nom", m.ProjetId);
                ViewBag.Zones = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(
                    await _db.Zones.AsNoTracking().OrderBy(z => z.Nom).ToListAsync(), "Id", "Nom", m.ZoneId);
                return View(m);
            }
            _db.Techniciens.Add(m); await _db.SaveChangesAsync();
            TempData["Success"] = "Technicien crÃ©Ã©."; return RedirectToAction(nameof(Index));
        }

        [Authorize(Roles = "Admin,Technicien")]
        public async Task<IActionResult> Edit(int id)
        {
            var m = await _db.Techniciens.FindAsync(id);
            if (m == null) return NotFound();
            ViewBag.Projets = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(
                await _db.Projets.AsNoTracking().OrderBy(p => p.Nom).ToListAsync(), "Id", "Nom", m.ProjetId);
            ViewBag.Zones = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(
                await _db.Zones.AsNoTracking().OrderBy(z => z.Nom).ToListAsync(), "Id", "Nom", m.ZoneId);
            return View(m);
        }

        [HttpPost, ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Technicien")]
        public async Task<IActionResult> Edit(Technicien m)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Projets = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(
                    await _db.Projets.AsNoTracking().OrderBy(p => p.Nom).ToListAsync(), "Id", "Nom", m.ProjetId);
                ViewBag.Zones = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(
                    await _db.Zones.AsNoTracking().OrderBy(z => z.Nom).ToListAsync(), "Id", "Nom", m.ZoneId);
                return View(m);
            }
            _db.Techniciens.Update(m); await _db.SaveChangesAsync();
            TempData["Success"] = "Technicien mis Ã  jour."; return RedirectToAction(nameof(Index));
        }

        [HttpPost, ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var m = await _db.Techniciens.FindAsync(id);
            if (m != null) { _db.Techniciens.Remove(m); await _db.SaveChangesAsync(); }
            TempData["Success"] = "Technicien supprimÃ©."; return RedirectToAction(nameof(Index));
        }
    }
}

namespace SmartGPON.Web.Controllers
{
    [Authorize]
    public class SecurityController : Controller
    {
        private readonly ISecurityService _sec;
        private readonly ApplicationDbContext _db;
        public SecurityController(ISecurityService sec, ApplicationDbContext db) { _sec = sec; _db = db; }

        public async Task<IActionResult> Index() =>
            View(await _sec.GetSecurityDashboardAsync());

        // FIX-3: retourne PagedResult<NetworkAlert> pour correspondre au @model de la vue Alertes.cshtml
        public async Task<IActionResult> Alertes(int page = 1, string filter = "All")
        {
            ViewBag.Filter = filter;
            const int pageSize = 20;

            var query = _db.NetworkAlerts.Include(a => a.Olt).AsNoTracking();

            query = filter switch
            {
                "Critical" => query.Where(a => a.Severite == AlertSeverite.Critical),
                "Warning"  => query.Where(a => a.Severite == AlertSeverite.Warning),
                "Info"     => query.Where(a => a.Severite == AlertSeverite.Info),
                "Unread"   => query.Where(a => !a.IsRead),
                _          => query
            };

            var total = await query.CountAsync();
            var items = await query
                .OrderByDescending(a => a.DateAlerte)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return View(new PagedResult<NetworkAlert>
            {
                Items = items, TotalCount = total, Page = page, PageSize = pageSize
            });
        }

        [HttpPost]
        [Authorize(Roles = "Admin,Technicien")]
        public async Task<IActionResult> MarkRead(int id)
        { await _sec.MarkAlertReadAsync(id); return Ok(); }

        [HttpPost, ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Technicien")]
        public async Task<IActionResult> MarkAllRead()
        {
            var unread = await _db.NetworkAlerts.Where(a => !a.IsRead).ToListAsync();
            unread.ForEach(a => a.IsRead = true);
            await _db.SaveChangesAsync();
            return Ok();
        }

        [HttpPost, ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ResolveRogue(int id)
        {
            var rogue = await _db.MaliciousOlts.FindAsync(id);
            if (rogue != null)
            {
                rogue.Statut = StatutMaliciousOlt.Resolu;
                rogue.DateResolution = DateTime.UtcNow;
                await _db.SaveChangesAsync();
                TempData["Success"] = "Rogue OLT marquÃ© rÃ©solu.";
            }
            return RedirectToAction(nameof(RogueOlts));
        }

        public async Task<IActionResult> RogueOlts() =>
            View(await _sec.GetRogueOltsAsync());

        public async Task<IActionResult> TrafficCaptures(int? oltId = null, int page = 1) =>
            View(await _sec.GetTrafficCapturesAsync(oltId, page));

        public async Task<IActionResult> Simulations() =>
            View(await _db.AttackSimulations.AsNoTracking().Include(s => s.Olt)
                .OrderByDescending(s => s.DateLancement).Take(50).ToListAsync());

        [Authorize(Roles = "Admin,Technicien")]
        public async Task<IActionResult> LancerSimulation()
        {
            ViewBag.Olts = await _db.Olts.AsNoTracking().OrderBy(o => o.Nom).ToListAsync();
            return View(new SimulationFormViewModel());
        }

        [HttpPost, ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Technicien")]
        public async Task<IActionResult> LancerSimulation(SimulationFormViewModel vm)
        {
            if (!ModelState.IsValid)
            { ViewBag.Olts = await _db.Olts.AsNoTracking().OrderBy(o => o.Nom).ToListAsync(); return View(vm); }
            var sim = new AttackSimulation
            {
                OltId = vm.OltId, TypeAttaque = vm.TypeAttaque,
                NiveauRisque = vm.NiveauRisque, Parametres = vm.Parametres,
                LancePar = User.Identity?.Name
            };
            var simId = await _sec.LancerSimulationAsync(sim);
            TempData["Success"] = $"Simulation #{simId} lancÃ©e.";
            return RedirectToAction(nameof(Simulations));
        }

        public async Task<IActionResult> SimulationDetails(int id) =>
            View(await _db.AttackSimulations.AsNoTracking().Include(s => s.Olt)
                .FirstOrDefaultAsync(s => s.Id == id));
    }
}

namespace SmartGPON.Web.Controllers
{
    public class AccountController : Controller
    {
        private readonly SignInManager<ApplicationUser> _signIn;
        private readonly UserManager<ApplicationUser> _users;
        public AccountController(SignInManager<ApplicationUser> signIn, UserManager<ApplicationUser> users)
        { _signIn = signIn; _users = users; }

        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        { ViewBag.ReturnUrl = returnUrl; return View(new LoginViewModel()); }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel vm, string? returnUrl = null)
        {
            ViewBag.ReturnUrl = returnUrl;
            if (string.IsNullOrWhiteSpace(vm.Email) || string.IsNullOrWhiteSpace(vm.Password))
            {
                ModelState.AddModelError("", "Email et mot de passe obligatoires.");
                return View(vm);
            }
            var result = await _signIn.PasswordSignInAsync(
                vm.Email.Trim(), vm.Password, vm.RememberMe, lockoutOnFailure: false);
            if (result.Succeeded)
            {
                var safe = !string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl) ? returnUrl : "/";
                return Redirect(safe);
            }
            ModelState.AddModelError("",
                result.IsLockedOut  ? "Compte verrouillÃ©. RÃ©essayez dans 15 minutes." :
                result.IsNotAllowed ? "Connexion non autorisÃ©e pour ce compte." :
                                      "Identifiants incorrects.");
            return View(vm);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        { await _signIn.SignOutAsync(); return RedirectToAction("Login"); }

        [HttpGet]
        public IActionResult AccessDenied() => View();
    }
}

namespace SmartGPON.Web.Controllers
{
    [Authorize(Roles = UserRoles.Admin)]
    public class UsersController : Controller
    {
        private readonly UserManager<ApplicationUser> _users;

        public UsersController(UserManager<ApplicationUser> users)
        {
            _users = users;
        }

        private static readonly string[] AllowedRoles = { UserRoles.Admin, UserRoles.Technicien, UserRoles.Lecteur };

        private static bool IsUserActive(ApplicationUser user)
            => user.LockoutEnd == null || user.LockoutEnd <= DateTimeOffset.UtcNow;

        private static List<string> GetAllowedRoles() => AllowedRoles.ToList();

        public async Task<IActionResult> Index()
        {
            var users = await _users.Users.OrderBy(u => u.Email).ToListAsync();
            var vm = new List<UserListItemViewModel>();
            foreach (var user in users)
            {
                var roles = await _users.GetRolesAsync(user);
                vm.Add(new UserListItemViewModel
                {
                    Id = user.Id,
                    Email = user.Email ?? user.UserName ?? string.Empty,
                    Role = roles.FirstOrDefault() ?? "-",
                    IsActive = IsUserActive(user)
                });
            }
            return View(vm);
        }

        public IActionResult Create() => View(new UserCreateViewModel { AvailableRoles = GetAllowedRoles(), IsActive = true });

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(UserCreateViewModel vm)
        {
            vm.AvailableRoles = GetAllowedRoles();
            if (!AllowedRoles.Contains(vm.Role))
            {
                ModelState.AddModelError(nameof(vm.Role), "Rôle invalide.");
            }
            if (!ModelState.IsValid) return View(vm);

            var user = new ApplicationUser
            {
                UserName = vm.Email.Trim(),
                Email = vm.Email.Trim(),
                EmailConfirmed = true
            };

            var create = await _users.CreateAsync(user, vm.Password);
            if (!create.Succeeded)
            {
                foreach (var e in create.Errors) ModelState.AddModelError(string.Empty, e.Description);
                return View(vm);
            }

            await _users.AddToRoleAsync(user, vm.Role);

            if (!vm.IsActive)
            {
                user.LockoutEnabled = true;
                user.LockoutEnd = DateTimeOffset.UtcNow.AddYears(50);
                await _users.UpdateAsync(user);
            }

            TempData["Success"] = "Compte utilisateur créé.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Details(string id)
        {
            var user = await _users.FindByIdAsync(id);
            if (user == null) return NotFound();

            var roles = await _users.GetRolesAsync(user);
            return View(new UserDetailsViewModel
            {
                Id = user.Id,
                Email = user.Email ?? user.UserName ?? string.Empty,
                Role = roles.FirstOrDefault() ?? "-",
                IsActive = IsUserActive(user),
                EmailConfirmed = user.EmailConfirmed
            });
        }

        public async Task<IActionResult> Edit(string id)
        {
            var user = await _users.FindByIdAsync(id);
            if (user == null) return NotFound();
            var roles = await _users.GetRolesAsync(user);

            return View(new UserEditViewModel
            {
                Id = user.Id,
                Email = user.Email ?? user.UserName ?? string.Empty,
                Role = roles.FirstOrDefault() ?? UserRoles.Lecteur,
                IsActive = IsUserActive(user),
                AvailableRoles = GetAllowedRoles()
            });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(UserEditViewModel vm)
        {
            vm.AvailableRoles = GetAllowedRoles();
            if (!AllowedRoles.Contains(vm.Role))
            {
                ModelState.AddModelError(nameof(vm.Role), "Rôle invalide.");
            }
            if (!ModelState.IsValid) return View(vm);

            var user = await _users.FindByIdAsync(vm.Id);
            if (user == null) return NotFound();

            user.Email = vm.Email.Trim();
            user.UserName = vm.Email.Trim();
            user.LockoutEnabled = true;
            user.LockoutEnd = vm.IsActive ? null : DateTimeOffset.UtcNow.AddYears(50);

            var update = await _users.UpdateAsync(user);
            if (!update.Succeeded)
            {
                foreach (var e in update.Errors) ModelState.AddModelError(string.Empty, e.Description);
                return View(vm);
            }

            var existingRoles = await _users.GetRolesAsync(user);
            if (existingRoles.Any()) await _users.RemoveFromRolesAsync(user, existingRoles);
            await _users.AddToRoleAsync(user, vm.Role);

            if (!string.IsNullOrWhiteSpace(vm.NewPassword))
            {
                var token = await _users.GeneratePasswordResetTokenAsync(user);
                var reset = await _users.ResetPasswordAsync(user, token, vm.NewPassword);
                if (!reset.Succeeded)
                {
                    foreach (var e in reset.Errors) ModelState.AddModelError(string.Empty, e.Description);
                    return View(vm);
                }
            }

            TempData["Success"] = "Compte utilisateur mis à jour.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string id)
        {
            var currentId = _users.GetUserId(User);
            if (string.Equals(currentId, id, StringComparison.Ordinal))
            {
                TempData["Error"] = "Impossible de supprimer votre propre compte.";
                return RedirectToAction(nameof(Index));
            }

            var user = await _users.FindByIdAsync(id);
            if (user == null) return NotFound();

            var roles = await _users.GetRolesAsync(user);
            if (roles.Contains(UserRoles.Admin))
            {
                var adminCount = 0;
                foreach (var u in _users.Users)
                {
                    if ((await _users.GetRolesAsync(u)).Contains(UserRoles.Admin)) adminCount++;
                }
                if (adminCount <= 1)
                {
                    TempData["Error"] = "Impossible de supprimer le dernier compte Admin.";
                    return RedirectToAction(nameof(Index));
                }
            }

            var del = await _users.DeleteAsync(user);
            if (!del.Succeeded)
            {
                TempData["Error"] = string.Join("; ", del.Errors.Select(e => e.Description));
                return RedirectToAction(nameof(Index));
            }

            TempData["Success"] = "Compte utilisateur supprimé.";
            return RedirectToAction(nameof(Index));
        }
    }
}
namespace SmartGPON.Web.Controllers
{
    [Authorize]
    public class LogsController : Controller
    {
        private readonly ApplicationDbContext _db;
        public LogsController(ApplicationDbContext db) { _db = db; }

        public async Task<IActionResult> Index() =>
            View(await _db.SecurityEvents
                .OrderByDescending(e => e.DateEvenement)
                .Take(200)
                .AsNoTracking()
                .ToListAsync());
    }
}







