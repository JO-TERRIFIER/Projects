// ============================================================
// SmartGPON — Controllers/SecurityController.cs — FRESH START
// ============================================================
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
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
    public class SecurityController : RbacControllerBase
    {
        private readonly ISecurityService _sec;
        private readonly IAttackSimulationService _sim;

        public SecurityController(ApplicationDbContext db, IUserProjectAssignmentService a, IAuditLogService au,
            ISecurityService sec, IAttackSimulationService sim)
            : base(db, a, au)
        { _sec = sec; _sim = sim; }

        public async Task<IActionResult> Index()
        {
            var data = await _sec.GetSecurityDashboardAsync();
            return View(data);
        }

        public async Task<IActionResult> Simulations()
        {
            var sims = await _sim.GetAllAsync();
            var list = sims.Select(s => new AttackSimulationDisplayVM
            {
                Id = s.Id, OltId = s.OltId, OltNom = s.Olt?.Nom,
                LaunchedByUserId = s.LaunchedByUserId,
                Statut = s.Statut, NiveauRisque = s.NiveauRisque,
                Description = s.Description, ResultatDetails = s.ResultatDetails,
                DateLancement = s.DateLancement
            }).ToList();
            return View(list);
        }

        [HttpGet]
        public async Task<IActionResult> LancerSimulation()
        {
            var d = DenyVisiteur(); if (d != null) return d;
            ViewBag.Olts = await Db.Olts.OrderBy(o => o.Nom).ToListAsync();
            return View(new AttackSimulationCreateVM());
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> LancerSimulation(AttackSimulationCreateVM vm)
        {
            var d = DenyVisiteur(); if (d != null) return d;
            if (!ModelState.IsValid)
            {
                ViewBag.Olts = await Db.Olts.OrderBy(o => o.Nom).ToListAsync();
                return View(vm);
            }
            // A1 — injecter les champs auto (non modifiables par l'utilisateur)
            var entity = new AttackSimulation
            {
                OltId            = vm.OltId,
                Description      = vm.Description,
                NiveauRisque     = vm.NiveauRisque,
                ResultatDetails  = vm.ResultatDetails,
                LaunchedByUserId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty,
                DateLancement    = DateTime.UtcNow,
                Statut           = SimulationStatut.EnAttente
            };
            await _sim.LaunchAsync(entity);
            TempData["Success"] = "Simulation lancée.";
            return RedirectToAction(nameof(Simulations));
        }

        public async Task<IActionResult> RogueOlts()
        {
            var list = await _sec.GetRogueOltsAsync();
            return View(list.Select(m => new MaliciousOltDisplayVM
            {
                Id = m.Id, OltId = m.OltId, OltNom = m.Olt?.Nom,
                Reason = m.Reason, DetectedAt = m.DetectedAt
            }).ToList());
        }

        public async Task<IActionResult> Alerts()
        {
            var list = await _sec.GetAlertsAsync();
            return View(list.Select(a => new NetworkAlertDisplayVM
            {
                Id = a.Id, OltId = a.OltId, OltNom = a.Olt?.Nom,
                Message = a.Message, Severite = a.Severite, OccurredAt = a.OccurredAt
            }).ToList());
        }
    }
}
