using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartGPON.Core.Entities;
using SmartGPON.Core.Enums;
using SmartGPON.Infrastructure.Data;
using SmartGPON.Web.ViewModels;
using System.Security.Claims;

namespace SmartGPON.Web.Controllers
{
    [Authorize(Roles = UserRoles.Superviseur)]
    public class UsersController : Controller
    {
        private readonly UserManager<ApplicationUser> _users;
        private readonly ApplicationDbContext _db;

        public UsersController(UserManager<ApplicationUser> users, ApplicationDbContext db)
        {
            _users = users;
            _db = db;
        }

        private static readonly string[] AllowedRoles = { UserRoles.Superviseur, UserRoles.ChefProjet, UserRoles.TechTerrain, UserRoles.TechDessin, UserRoles.Visiteur };
        private static bool IsUserActive(ApplicationUser user) => user.LockoutEnd == null || user.LockoutEnd <= DateTimeOffset.UtcNow;
        private static List<string> GetAllowedRoles() => AllowedRoles.ToList();

        private async Task AuditAsync(string actionType, string entityType, string description)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
            var email  = User.FindFirstValue(ClaimTypes.Email) ?? User.FindFirstValue(ClaimTypes.Name) ?? userId;
            var ip     = HttpContext.Connection.RemoteIpAddress?.ToString();
            _db.AuditLogs.Add(new AuditLog
            {
                UserId = userId, UserEmail = email, IpAddress = ip,
                ProjetId = null, ActionType = actionType,
                EntityType = entityType, EntityId = null,
                Description = description, OccurredAt = DateTime.UtcNow
            });
            await _db.SaveChangesAsync();
        }

        public async Task<IActionResult> Index()
        {
            var users = await _users.Users.OrderBy(u => u.LastName).ThenBy(u => u.FirstName).ToListAsync();
            var vm = new List<UserListItemViewModel>();
            foreach (var user in users)
            {
                var roles = await _users.GetRolesAsync(user);
                vm.Add(new UserListItemViewModel
                {
                    Id = user.Id,
                    Email = user.Email ?? user.UserName ?? string.Empty,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
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
            if (!AllowedRoles.Contains(vm.Role)) ModelState.AddModelError(nameof(vm.Role), "Rôle invalide.");
            if (!ModelState.IsValid) return View(vm);

            var user = new ApplicationUser
            {
                UserName = vm.Email.Trim(),
                Email = vm.Email.Trim(),
                EmailConfirmed = true,
                FirstName = vm.FirstName.Trim(),
                LastName = vm.LastName.Trim()
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

            await AuditAsync("Create", "Utilisateur", $"Création du compte {vm.Email} avec rôle {vm.Role}");
            TempData["Success"] = "Compte utilisateur créé.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Details(string id)
        {
            var user = await _users.FindByIdAsync(id);
            if (user == null) return NotFound();
            var roles = await _users.GetRolesAsync(user);
            ViewBag.Assignments = await _db.UserProjectAssignments.AsNoTracking().Include(a => a.Projet)
                .Where(a => a.UserId == user.Id && a.IsActive).OrderBy(a => a.Projet.Nom).ToListAsync();
            return View(new UserDetailsViewModel
            {
                Id = user.Id, Email = user.Email ?? user.UserName ?? string.Empty,
                FirstName = user.FirstName, LastName = user.LastName,
                Role = roles.FirstOrDefault() ?? "-",
                IsActive = IsUserActive(user), EmailConfirmed = user.EmailConfirmed
            });
        }

        public async Task<IActionResult> Edit(string id)
        {
            var user = await _users.FindByIdAsync(id);
            if (user == null) return NotFound();
            var roles = await _users.GetRolesAsync(user);
            ViewBag.Projets = await _db.Projets.AsNoTracking().OrderBy(p => p.Nom).ToListAsync();
            ViewBag.Assignments = await _db.UserProjectAssignments.AsNoTracking().Where(a => a.UserId == user.Id && a.IsActive).ToListAsync();
            return View(new UserEditViewModel
            {
                Id = user.Id, Email = user.Email ?? user.UserName ?? string.Empty,
                FirstName = user.FirstName, LastName = user.LastName,
                Role = roles.FirstOrDefault() ?? UserRoles.Visiteur,
                IsActive = IsUserActive(user), AvailableRoles = GetAllowedRoles()
            });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(UserEditViewModel vm)
        {
            vm.AvailableRoles = GetAllowedRoles();
            if (!AllowedRoles.Contains(vm.Role)) ModelState.AddModelError(nameof(vm.Role), "Rôle invalide.");
            if (!ModelState.IsValid) return View(vm);

            var user = await _users.FindByIdAsync(vm.Id);
            if (user == null) return NotFound();

            var oldRole = (await _users.GetRolesAsync(user)).FirstOrDefault() ?? "-";
            user.Email = vm.Email.Trim();
            user.UserName = vm.Email.Trim();
            user.FirstName = vm.FirstName.Trim();
            user.LastName = vm.LastName.Trim();
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

            await AuditAsync("Edit", "Utilisateur", $"Modification du compte {vm.Email} ({oldRole} → {vm.Role})");
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
            if (roles.Contains(UserRoles.Superviseur))
            {
                var count = 0;
                foreach (var u in _users.Users) if ((await _users.GetRolesAsync(u)).Contains(UserRoles.Superviseur)) count++;
                if (count <= 1) { TempData["Error"] = "Impossible de supprimer le dernier compte Superviseur."; return RedirectToAction(nameof(Index)); }
            }

            var email = user.Email ?? user.UserName ?? id;
            var del = await _users.DeleteAsync(user);
            if (!del.Succeeded)
            {
                TempData["Error"] = string.Join("; ", del.Errors.Select(e => e.Description));
                return RedirectToAction(nameof(Index));
            }

            await AuditAsync("Delete", "Utilisateur", $"Suppression du compte {email}");
            TempData["Success"] = "Compte utilisateur supprimé.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> AddAssignment(string userId, int projetId, AssignmentType assignmentType)
        {
            if (assignmentType == AssignmentType.ChefProjet)
            {
                var projet = await _db.Projets.FindAsync(projetId);
                if (projet != null) { projet.ProjectManagerId = userId; await _db.SaveChangesAsync(); }
            }
            else if (!await _db.UserProjectAssignments.AnyAsync(a => a.UserId == userId && a.ProjetId == projetId && a.AssignmentType == assignmentType && a.IsActive))
            {
                _db.UserProjectAssignments.Add(new UserProjectAssignment
                {
                    UserId = userId, ProjetId = projetId, AssignmentType = assignmentType,
                    IsActive = true, AssignedAt = DateTime.UtcNow, AssignedByUserId = _users.GetUserId(User)
                });
                await _db.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Edit), new { id = userId });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveAssignment(int id, string userId)
        {
            var m = await _db.UserProjectAssignments.FindAsync(id);
            if (m != null) { m.IsActive = false; await _db.SaveChangesAsync(); }
            return RedirectToAction(nameof(Edit), new { id = userId });
        }
    }
}
