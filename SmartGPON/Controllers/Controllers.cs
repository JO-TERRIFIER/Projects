// SmartGPON v3 – Controllers.cs
// FIXES APPLIED:
//   FIX-1: All using directives at file top (no CS1529)
//   FIX-3: SecurityController.Alertes returns PagedResult<NetworkAlert>
//   FIX-4: FatsController.Index includes Splitters -> Onts
//   FIX-5: SplitersController.Index includes Onts
//   FIX-6: OntsController ViewBag key = "Spliters" (matches view)
using System;
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
                                .ThenInclude(f => f.Fats)
                                    .ThenInclude(fat => fat.Splitters)
                                        .ThenInclude(s => s.Onts)
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

        // FIX-4: Include Splitters->Onts pour affichage capacité en vue
        public async Task<IActionResult> Index() =>
            View(await _db.Fats.AsNoTracking()
                .Include(f => f.Fdt).ThenInclude(fd => fd.Olt)
                .Include(f => f.Splitters).ThenInclude(s => s.Onts)
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
    public class SplitersController : Controller
    {
        private readonly ApplicationDbContext _db;
        public SplitersController(ApplicationDbContext db) { _db = db; }

        // FIX-5: Include Onts pour affichage occupation en vue
        public async Task<IActionResult> Index() =>
            View(await _db.Splitters.AsNoTracking()
                .Include(s => s.Fat)
                .Include(s => s.Onts)
                .OrderBy(s => s.Nom)
                .ToListAsync());

        public async Task<IActionResult> Create()
        {
            ViewBag.Fats = await _db.Fats.AsNoTracking().OrderBy(f => f.Nom).ToListAsync();
            return View(new Splitter());
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Splitter m)
        {
            if (!ModelState.IsValid) { ViewBag.Fats = await _db.Fats.AsNoTracking().OrderBy(f => f.Nom).ToListAsync(); return View(m); }
            _db.Splitters.Add(m); await _db.SaveChangesAsync();
            TempData["Success"] = "Splitter créé."; return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int id)
        {
            var m = await _db.Splitters.FindAsync(id);
            if (m == null) return NotFound();
            ViewBag.Fats = await _db.Fats.AsNoTracking().OrderBy(f => f.Nom).ToListAsync();
            return View(m);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Splitter m)
        {
            if (!ModelState.IsValid) { ViewBag.Fats = await _db.Fats.AsNoTracking().OrderBy(f => f.Nom).ToListAsync(); return View(m); }
            _db.Splitters.Update(m); await _db.SaveChangesAsync();
            TempData["Success"] = "Splitter mis à jour."; return RedirectToAction(nameof(Index));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var m = await _db.Splitters.FindAsync(id);
            if (m != null) { _db.Splitters.Remove(m); await _db.SaveChangesAsync(); }
            TempData["Success"] = "Splitter supprimé."; return RedirectToAction(nameof(Index));
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
            View(await _db.Onts.AsNoTracking().Include(n => n.Splitter).OrderBy(n => n.SerialNumber).ToListAsync());

        public async Task<IActionResult> Create()
        {
            // FIX-6: ViewBag.Spliters — correspond exactement au nom utilisé dans Onts/Create.cshtml
            ViewBag.Spliters = await _db.Splitters.AsNoTracking().OrderBy(s => s.Nom).ToListAsync();
            return View(new Ont());
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Ont m)
        {
            if (!ModelState.IsValid) { ViewBag.Spliters = await _db.Splitters.AsNoTracking().OrderBy(s => s.Nom).ToListAsync(); return View(m); }
            _db.Onts.Add(m); await _db.SaveChangesAsync();
            TempData["Success"] = "ONT créé."; return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int id)
        {
            var m = await _db.Onts.FindAsync(id);
            if (m == null) return NotFound();
            ViewBag.Spliters = await _db.Splitters.AsNoTracking().OrderBy(s => s.Nom).ToListAsync();
            return View(m);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Ont m)
        {
            if (!ModelState.IsValid) { ViewBag.Spliters = await _db.Splitters.AsNoTracking().OrderBy(s => s.Nom).ToListAsync(); return View(m); }
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
    public class TechniciensController : Controller
    {
        private readonly ApplicationDbContext _db;
        public TechniciensController(ApplicationDbContext db) { _db = db; }

        public async Task<IActionResult> Index() =>
            View(await _db.Techniciens.AsNoTracking().Include(t => t.Client).OrderBy(t => t.Nom).ToListAsync());

        public async Task<IActionResult> Create()
        {
            ViewBag.Clients = await _db.Clients.AsNoTracking().OrderBy(c => c.Nom).ToListAsync();
            return View(new Technicien());
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Technicien m)
        {
            if (!ModelState.IsValid) { ViewBag.Clients = await _db.Clients.AsNoTracking().OrderBy(c => c.Nom).ToListAsync(); return View(m); }
            _db.Techniciens.Add(m); await _db.SaveChangesAsync();
            TempData["Success"] = "Technicien créé."; return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int id)
        {
            var m = await _db.Techniciens.FindAsync(id);
            if (m == null) return NotFound();
            ViewBag.Clients = await _db.Clients.AsNoTracking().OrderBy(c => c.Nom).ToListAsync();
            return View(m);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Technicien m)
        {
            if (!ModelState.IsValid) { ViewBag.Clients = await _db.Clients.AsNoTracking().OrderBy(c => c.Nom).ToListAsync(); return View(m); }
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
