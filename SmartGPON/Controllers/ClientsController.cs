// ============================================================
// SmartGPON — Controllers/ClientsController.cs — FRESH START
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
    public class ClientsController : RbacControllerBase
    {
        public ClientsController(ApplicationDbContext db, IUserProjectAssignmentService a, IAuditLogService au)
            : base(db, a, au) { }

        public async Task<IActionResult> Index()
        {
            var clients = await Db.Clients.OrderBy(c => c.Nom)
                .Select(c => new ClientDisplayVM
                {
                    Id = c.Id, Nom = c.Nom, Code = c.Code, IsActive = c.IsActive,
                    ProjetCount = c.Projets.Count
                }).ToListAsync();
            return View(clients);
        }

        [HttpGet]
        public IActionResult Create()
        {
            var d = DenyVisiteur(); if (d != null) return d;
            return View(new ClientCreateVM());
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ClientCreateVM vm)
        {
            var d = DenyVisiteur(); if (d != null) return d;
            if (!ModelState.IsValid) return View(vm);
            var entity = new SmartGPON.Core.Entities.Client { Nom = vm.Nom, Code = vm.Code, IsActive = vm.IsActive };
            Db.Clients.Add(entity); await Db.SaveChangesAsync();
            await LogAsync(null, "Create", "Client", entity.Id, $"Client créé: {entity.Nom}");
            TempData["Success"] = "Client créé.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var d = DenyVisiteur(); if (d != null) return d;
            var c = await Db.Clients.FindAsync(id);
            if (c == null) return NotFound();
            return View(new ClientUpdateVM { Id = c.Id, Nom = c.Nom, Code = c.Code, IsActive = c.IsActive });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(ClientUpdateVM vm)
        {
            var d = DenyVisiteur(); if (d != null) return d;
            if (!ModelState.IsValid) return View(vm);
            var c = await Db.Clients.FindAsync(vm.Id);
            if (c == null) return NotFound();
            c.Nom = vm.Nom; c.Code = vm.Code; c.IsActive = vm.IsActive;
            await Db.SaveChangesAsync();
            await LogAsync(null, "Update", "Client", c.Id, $"Client modifié: {c.Nom}");
            TempData["Success"] = "Client modifié.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var d = DenyVisiteur(); if (d != null) return d;
            var c = await Db.Clients.FindAsync(id);
            if (c == null) return NotFound();
            Db.Clients.Remove(c); await Db.SaveChangesAsync();
            await LogAsync(null, "Delete", "Client", id, $"Client supprimé: {c.Nom}");
            TempData["Success"] = "Client supprimé.";
            return RedirectToAction(nameof(Index));
        }
    }
}
