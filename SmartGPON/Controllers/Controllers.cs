// SmartGPON v3 – Controllers.cs
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
            ViewBag.UnreadAlerts = await _db.NetworkAlerts.CountAsync(a => !a.IsRead);
            return View(vm);
        }

        public async Task<IActionResult> Tree()
        {
            var clients = await _db.Clients
                .Include(c => c.Projets)
                    .ThenInclude(p => p.Zones)
                        .ThenInclude(z => z.Olts)
                            .ThenInclude(o => o.Fdts)
                                .ThenInclude(f => f.Onts)
                .Include(c => c.Projets)
                    .ThenInclude(p => p.Zones)
                        .ThenInclude(z => z.Olts)
                            .ThenInclude(o => o.Fdts)
                                .ThenInclude(f => f.Fats)
                .Include(c => c.Projets)
                    .ThenInclude(p => p.Zones)
                        .ThenInclude(z => z.Olts)
                            .ThenInclude(o => o.Fdts)
                                .ThenInclude(f => f.Bpis)
                                    .ThenInclude(b => b.Onts)
                .AsNoTracking()
                .OrderBy(c => c.Nom)
                .ToListAsync();
            return View(clients);
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

        public IActionResult Create() => View(new Client());

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Client m)
        {
            if (!ModelState.IsValid) return View(m);
            _db.Clients.Add(m); await _db.SaveChangesAsync();
            TempData["Success"] = "Client créé.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int id)
        {
            var m = await _db.Clients.FindAsync(id);
            return m == null ? NotFound() : View(m);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Client m)
        {
            if (!ModelState.IsValid) return View(m);
            _db.Clients.Update(m); await _db.SaveChangesAsync();
            TempData["Success"] = "Client mis à jour.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var m = await _db.Clients.FindAsync(id);
            if (m != null) { _db.Clients.Remove(m); await _db.SaveChangesAsync(); }
            TempData["Success"] = "Client supprimé.";
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

        public async Task<IActionResult> Create()
        {
            ViewBag.Clients = await _db.Clients.AsNoTracking().OrderBy(c => c.Nom).ToListAsync();
            return View(new Projet());
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Projet m)
        {
            if (!ModelState.IsValid) { ViewBag.Clients = await _db.Clients.AsNoTracking().OrderBy(c => c.Nom).ToListAsync(); return View(m); }
            _db.Projets.Add(m); await _db.SaveChangesAsync();
            TempData["Success"] = "Projet créé."; return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int id)
        {
            var m = await _db.Projets.FindAsync(id);
            if (m == null) return NotFound();
            ViewBag.Clients = await _db.Clients.AsNoTracking().OrderBy(c => c.Nom).ToListAsync();
            ViewBag.Resources = await _db.Resources.AsNoTracking().Where(r => r.ProjetId == id).OrderByDescending(r => r.DateUpload).ToListAsync();
            return View(m);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Projet m)
        {
            if (!ModelState.IsValid) { ViewBag.Clients = await _db.Clients.AsNoTracking().OrderBy(c => c.Nom).ToListAsync(); return View(m); }
            _db.Projets.Update(m); await _db.SaveChangesAsync();
            TempData["Success"] = "Projet mis à jour."; return RedirectToAction(nameof(Index));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var m = await _db.Projets.FindAsync(id);
            if (m != null) { _db.Projets.Remove(m); await _db.SaveChangesAsync(); }
            TempData["Success"] = "Projet supprimé."; return RedirectToAction(nameof(Index));
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

        public async Task<IActionResult> Create()
        {
            ViewBag.Projets = await _db.Projets.AsNoTracking().OrderBy(p => p.Nom).ToListAsync();
            return View(new Zone());
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Zone m)
        {
            if (!ModelState.IsValid) { ViewBag.Projets = await _db.Projets.AsNoTracking().OrderBy(p => p.Nom).ToListAsync(); return View(m); }
            _db.Zones.Add(m); await _db.SaveChangesAsync();
            TempData["Success"] = "Zone créée."; return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int id)
        {
            var m = await _db.Zones.FindAsync(id);
            if (m == null) return NotFound();
            ViewBag.Projets = await _db.Projets.AsNoTracking().OrderBy(p => p.Nom).ToListAsync();
            ViewBag.Resources = await _db.Resources.AsNoTracking().Where(r => r.ZoneId == id).OrderByDescending(r => r.DateUpload).ToListAsync();
            return View(m);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Zone m)
        {
            if (!ModelState.IsValid) { ViewBag.Projets = await _db.Projets.AsNoTracking().OrderBy(p => p.Nom).ToListAsync(); return View(m); }
            _db.Zones.Update(m); await _db.SaveChangesAsync();
            TempData["Success"] = "Zone mise à jour."; return RedirectToAction(nameof(Index));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var m = await _db.Zones.FindAsync(id);
            if (m != null) { _db.Zones.Remove(m); await _db.SaveChangesAsync(); }
            TempData["Success"] = "Zone supprimée."; return RedirectToAction(nameof(Index));
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

        public async Task<IActionResult> Create()
        {
            ViewBag.Zones = await _db.Zones.AsNoTracking().OrderBy(z => z.Nom).ToListAsync();
            return View(new Olt());
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Olt m)
        {
            if (!ModelState.IsValid) { ViewBag.Zones = await _db.Zones.AsNoTracking().OrderBy(z => z.Nom).ToListAsync(); return View(m); }
            _db.Olts.Add(m); await _db.SaveChangesAsync();
            TempData["Success"] = "OLT créé."; return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int id)
        {
            var m = await _db.Olts.FindAsync(id);
            if (m == null) return NotFound();
            ViewBag.Zones = await _db.Zones.AsNoTracking().OrderBy(z => z.Nom).ToListAsync();
            return View(m);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Olt m)
        {
            if (!ModelState.IsValid) { ViewBag.Zones = await _db.Zones.AsNoTracking().OrderBy(z => z.Nom).ToListAsync(); return View(m); }
            _db.Olts.Update(m); await _db.SaveChangesAsync();
            TempData["Success"] = "OLT mis à jour."; return RedirectToAction(nameof(Index));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var m = await _db.Olts.FindAsync(id);
            if (m != null) { _db.Olts.Remove(m); await _db.SaveChangesAsync(); }
            TempData["Success"] = "OLT supprimé."; return RedirectToAction(nameof(Index));
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

        public async Task<IActionResult> Create()
        {
            ViewBag.Olts = await _db.Olts.AsNoTracking().OrderBy(o => o.Nom).ToListAsync();
            return View(new Fdt());
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Fdt m)
        {
            if (!ModelState.IsValid) { ViewBag.Olts = await _db.Olts.AsNoTracking().OrderBy(o => o.Nom).ToListAsync(); return View(m); }
            _db.Fdts.Add(m); await _db.SaveChangesAsync();
            TempData["Success"] = "FDT créé."; return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int id)
        {
            var m = await _db.Fdts.FindAsync(id);
            if (m == null) return NotFound();
            ViewBag.Olts = await _db.Olts.AsNoTracking().OrderBy(o => o.Nom).ToListAsync();
            return View(m);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Fdt m)
        {
            if (!ModelState.IsValid) { ViewBag.Olts = await _db.Olts.AsNoTracking().OrderBy(o => o.Nom).ToListAsync(); return View(m); }
            _db.Fdts.Update(m); await _db.SaveChangesAsync();
            TempData["Success"] = "FDT mis à jour."; return RedirectToAction(nameof(Index));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var m = await _db.Fdts.FindAsync(id);
            if (m != null) { _db.Fdts.Remove(m); await _db.SaveChangesAsync(); }
            TempData["Success"] = "FDT supprimé."; return RedirectToAction(nameof(Index));
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

        public async Task<IActionResult> Create()
        {
            ViewBag.Fdts = await _db.Fdts.AsNoTracking().OrderBy(f => f.Nom).ToListAsync();
            return View(new Fat());
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Fat m)
        {
            if (!ModelState.IsValid) { ViewBag.Fdts = await _db.Fdts.AsNoTracking().OrderBy(f => f.Nom).ToListAsync(); return View(m); }
            _db.Fats.Add(m); await _db.SaveChangesAsync();
            TempData["Success"] = "FAT créé."; return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int id)
        {
            var m = await _db.Fats.FindAsync(id);
            if (m == null) return NotFound();
            ViewBag.Fdts = await _db.Fdts.AsNoTracking().OrderBy(f => f.Nom).ToListAsync();
            return View(m);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Fat m)
        {
            if (!ModelState.IsValid) { ViewBag.Fdts = await _db.Fdts.AsNoTracking().OrderBy(f => f.Nom).ToListAsync(); return View(m); }
            _db.Fats.Update(m); await _db.SaveChangesAsync();
            TempData["Success"] = "FAT mis à jour."; return RedirectToAction(nameof(Index));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var m = await _db.Fats.FindAsync(id);
            if (m != null) { _db.Fats.Remove(m); await _db.SaveChangesAsync(); }
            TempData["Success"] = "FAT supprimé."; return RedirectToAction(nameof(Index));
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

        public async Task<IActionResult> Create()
        {
            ViewBag.Fdts = await _db.Fdts.AsNoTracking().OrderBy(f => f.Nom).ToListAsync();
            return View(new Bpi());
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Bpi m)
        {
            if (!ModelState.IsValid) { ViewBag.Fdts = await _db.Fdts.AsNoTracking().OrderBy(f => f.Nom).ToListAsync(); return View(m); }
            _db.Bpis.Add(m); await _db.SaveChangesAsync();
            TempData["Success"] = "BPI créé."; return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int id)
        {
            var m = await _db.Bpis.FindAsync(id);
            if (m == null) return NotFound();
            ViewBag.Fdts = await _db.Fdts.AsNoTracking().OrderBy(f => f.Nom).ToListAsync();
            return View(m);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Bpi m)
        {
            if (!ModelState.IsValid) { ViewBag.Fdts = await _db.Fdts.AsNoTracking().OrderBy(f => f.Nom).ToListAsync(); return View(m); }
            _db.Bpis.Update(m); await _db.SaveChangesAsync();
            TempData["Success"] = "BPI mis à jour."; return RedirectToAction(nameof(Index));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var m = await _db.Bpis.FindAsync(id);
            if (m != null) { _db.Bpis.Remove(m); await _db.SaveChangesAsync(); }
            TempData["Success"] = "BPI supprimé."; return RedirectToAction(nameof(Index));
        }
    }
}

namespace SmartGPON.Web.Controllers
{
    [Authorize]
    public class OntsController : Controller
    {
        private readonly ApplicationDbContext _db;
        public OntsController(ApplicationDbContext db) { _db = db; }

        public async Task<IActionResult> Index() =>
            View(await _db.Onts.AsNoTracking().Include(n => n.Fdt).Include(n => n.Bpi).OrderBy(n => n.SerialNumber).ToListAsync());

        private async Task PopulateOntViewBags()
        {
            ViewBag.Fdts = await _db.Fdts.AsNoTracking().OrderBy(f => f.Nom).ToListAsync();
            ViewBag.Bpis = await _db.Bpis.AsNoTracking().OrderBy(b => b.Nom).ToListAsync();
        }

        public async Task<IActionResult> Create()
        {
            await PopulateOntViewBags();
            return View(new Ont());
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Ont m)
        {
            if (!ModelState.IsValid) { await PopulateOntViewBags(); return View(m); }
            _db.Onts.Add(m); await _db.SaveChangesAsync();
            TempData["Success"] = "ONT créé."; return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int id)
        {
            var m = await _db.Onts.FindAsync(id);
            if (m == null) return NotFound();
            await PopulateOntViewBags();
            return View(m);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Ont m)
        {
            if (!ModelState.IsValid) { await PopulateOntViewBags(); return View(m); }
            _db.Onts.Update(m); await _db.SaveChangesAsync();
            TempData["Success"] = "ONT mis à jour."; return RedirectToAction(nameof(Index));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var m = await _db.Onts.FindAsync(id);
            if (m != null) { _db.Onts.Remove(m); await _db.SaveChangesAsync(); }
            TempData["Success"] = "ONT supprimé."; return RedirectToAction(nameof(Index));
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
        public async Task<IActionResult> Upload(int? zoneId, int? projetId, IFormFile file)
        {
            if (file == null || file.Length == 0) { TempData["Error"] = "Fichier vide."; return Back(zoneId, projetId); }

            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            var allowed = zoneId.HasValue ? ZoneExtensions : ProjetExtensions;
            if (!allowed.Contains(ext)) { TempData["Error"] = $"Type {ext} non autorisé."; return Back(zoneId, projetId); }

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
            TempData["Success"] = "Fichier uploadé.";
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
        public async Task<IActionResult> Delete(int id)
        {
            var r = await _db.Resources.FindAsync(id);
            if (r != null)
            {
                if (System.IO.File.Exists(r.CheminFichier)) System.IO.File.Delete(r.CheminFichier);
                _db.Resources.Remove(r); await _db.SaveChangesAsync();
            }
            TempData["Success"] = "Fichier supprimé.";
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
            View(await _db.Techniciens.AsNoTracking().Include(t => t.Projet).OrderBy(t => t.Nom).ToListAsync());

        public async Task<IActionResult> Create()
        {
            ViewBag.Projets = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(
                await _db.Projets.AsNoTracking().OrderBy(p => p.Nom).ToListAsync(), "Id", "Nom");
            return View(new Technicien());
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Technicien m)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Projets = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(
                    await _db.Projets.AsNoTracking().OrderBy(p => p.Nom).ToListAsync(), "Id", "Nom", m.ProjetId);
                return View(m);
            }
            _db.Techniciens.Add(m); await _db.SaveChangesAsync();
            TempData["Success"] = "Technicien créé."; return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int id)
        {
            var m = await _db.Techniciens.FindAsync(id);
            if (m == null) return NotFound();
            ViewBag.Projets = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(
                await _db.Projets.AsNoTracking().OrderBy(p => p.Nom).ToListAsync(), "Id", "Nom", m.ProjetId);
            return View(m);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Technicien m)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Projets = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(
                    await _db.Projets.AsNoTracking().OrderBy(p => p.Nom).ToListAsync(), "Id", "Nom", m.ProjetId);
                return View(m);
            }
            _db.Techniciens.Update(m); await _db.SaveChangesAsync();
            TempData["Success"] = "Technicien mis à jour."; return RedirectToAction(nameof(Index));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var m = await _db.Techniciens.FindAsync(id);
            if (m != null) { _db.Techniciens.Remove(m); await _db.SaveChangesAsync(); }
            TempData["Success"] = "Technicien supprimé."; return RedirectToAction(nameof(Index));
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
        public async Task<IActionResult> MarkRead(int id)
        { await _sec.MarkAlertReadAsync(id); return Ok(); }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAllRead()
        {
            var unread = await _db.NetworkAlerts.Where(a => !a.IsRead).ToListAsync();
            unread.ForEach(a => a.IsRead = true);
            await _db.SaveChangesAsync();
            return Ok();
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> ResolveRogue(int id)
        {
            var rogue = await _db.MaliciousOlts.FindAsync(id);
            if (rogue != null)
            {
                rogue.Statut = StatutMaliciousOlt.Resolu;
                rogue.DateResolution = DateTime.UtcNow;
                await _db.SaveChangesAsync();
                TempData["Success"] = "Rogue OLT marqué résolu.";
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

        public async Task<IActionResult> LancerSimulation()
        {
            ViewBag.Olts = await _db.Olts.AsNoTracking().OrderBy(o => o.Nom).ToListAsync();
            return View(new SimulationFormViewModel());
        }

        [HttpPost, ValidateAntiForgeryToken]
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
            TempData["Success"] = $"Simulation #{simId} lancée.";
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
                result.IsLockedOut  ? "Compte verrouillé. Réessayez dans 15 minutes." :
                result.IsNotAllowed ? "Connexion non autorisée pour ce compte." :
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
