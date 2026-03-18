// ============================================================
// SmartGPON — ViewComponents/DeletionPendingBadgeViewComponent.cs
// P9 · R1 · Badge notification demandes de suppression EnAttente
// Visible: Superviseur + ChefProjet dans leurs projets
// ============================================================
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartGPON.Core.Enums;
using SmartGPON.Core.Interfaces;
using SmartGPON.Infrastructure.Data;

namespace SmartGPON.Web.ViewComponents
{
    public class DeletionPendingBadgeViewComponent : ViewComponent
    {
        private readonly ApplicationDbContext _db;
        private readonly IUserProjectAssignmentService _assignments;

        public DeletionPendingBadgeViewComponent(
            ApplicationDbContext db,
            IUserProjectAssignmentService assignments)
        {
            _db          = db;
            _assignments = assignments;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            if (!UserClaimsPrincipal.Identity?.IsAuthenticated == true)
                return View(0);

            // Visible uniquement si Superviseur ou Membre (ChefProjet potentiel)
            if (!UserClaimsPrincipal.IsInRole(UserRoles.Superviseur) &&
                !UserClaimsPrincipal.IsInRole(UserRoles.Membre))
                return View(0);

            var ids = await _assignments.GetAccessibleProjetIdsAsync(UserClaimsPrincipal);

            var count = await _db.DeletionRequests.CountAsync(r =>
                r.Statut == DeletionStatut.EnAttente &&
                ids.Contains(r.ProjetId));

            return View(count);
        }
    }
}
