// ============================================================
// SmartGPON — Controllers/ProjetsController.cs — FRESH START
// ============================================================
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartGPON.Core.Interfaces;
using SmartGPON.Infrastructure.Data;
using SmartGPON.Web.ViewModels;

namespace SmartGPON.Web.Controllers
{
    [Authorize]
    public class ProjetsController : RbacControllerBase
    {
        public ProjetsController(ApplicationDbContext db, IUserProjectAssignmentService a, IAuditLogService au)
            : base(db, a, au) { }

        public async Task<IActionResult> Index()
        {
            var ids = await AccessibleProjetIdsAsync();
            var projets = await Db.Projets.Where(p => ids.Contains(p.Id))
                .Include(p => p.Client)
                .OrderBy(p => p.Nom)
                .Select(p => new ProjetDisplayVM
                {
                    Id = p.Id, Nom = p.Nom, Statut = p.Statut,
                    ClientNom = p.Client.Nom, ClientId = p.ClientId,
                    ZoneCount = p.Zones.Count
                }).ToListAsync();
            return View(projets);
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var d = DenyVisiteur(); if (d != null) return d;
            ViewBag.Clients = await Db.Clients.Where(c => c.IsActive).OrderBy(c => c.Nom).ToListAsync();
            return View(new ProjetCreateVM());
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ProjetCreateVM vm)
        {
            var d = DenyVisiteur(); if (d != null) return d;
            if (!ModelState.IsValid)
            {
                ViewBag.Clients = await Db.Clients.Where(c => c.IsActive).OrderBy(c => c.Nom).ToListAsync();
                return View(vm);
            }
            var entity = new SmartGPON.Core.Entities.Projet
            {
                ClientId = vm.ClientId, Nom = vm.Nom, Statut = vm.Statut
            };
            Db.Projets.Add(entity); await Db.SaveChangesAsync();
            await LogAsync(entity.Id, "Create", "Projet", entity.Id, $"Projet créé: {entity.Nom}");
            TempData["Success"] = "Projet créé.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var d = DenyVisiteur(); if (d != null) return d;
            var p = await Db.Projets.FindAsync(id);
            if (p == null) return NotFound();
            if (!await CanWriteAsync(id)) return Forbid();
            ViewBag.Clients = await Db.Clients.Where(c => c.IsActive).OrderBy(c => c.Nom).ToListAsync();
            return View(new ProjetUpdateVM { Id = p.Id, ClientId = p.ClientId, Nom = p.Nom, Statut = p.Statut });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(ProjetUpdateVM vm)
        {
            var d = DenyVisiteur(); if (d != null) return d;
            if (!await CanWriteAsync(vm.Id)) return Forbid();
            if (!ModelState.IsValid)
            {
                ViewBag.Clients = await Db.Clients.Where(c => c.IsActive).OrderBy(c => c.Nom).ToListAsync();
                return View(vm);
            }
            var p = await Db.Projets.FindAsync(vm.Id);
            if (p == null) return NotFound();
            p.ClientId = vm.ClientId; p.Nom = vm.Nom; p.Statut = vm.Statut;
            await Db.SaveChangesAsync();
            await LogAsync(p.Id, "Update", "Projet", p.Id, $"Projet modifié: {p.Nom}");
            TempData["Success"] = "Projet modifié.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var d = DenyVisiteur(); if (d != null) return d;
            if (!await CanWriteAsync(id)) return Forbid();
            var p = await Db.Projets.FindAsync(id);
            if (p == null) return NotFound();
            Db.Projets.Remove(p); await Db.SaveChangesAsync();
            await LogAsync(id, "Delete", "Projet", id, $"Projet supprimé: {p.Nom}");
            TempData["Success"] = "Projet supprimé.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var p = await Db.Projets.Include(p => p.Client).Include(p => p.Zones).FirstOrDefaultAsync(p => p.Id == id);
            if (p == null) return NotFound();
            return View(p);
        }
    }
}
