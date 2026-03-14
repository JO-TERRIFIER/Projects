// ============================================================
// SmartGPON — Controllers/UsersController.cs — FRESH START
// ============================================================
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartGPON.Core.Enums;
using SmartGPON.Core.Interfaces;
using SmartGPON.Infrastructure.Data;
using SmartGPON.Web.ViewModels;

namespace SmartGPON.Web.Controllers
{
    [Authorize(Roles = "Superviseur")]
    public class UsersController : RbacControllerBase
    {
        private readonly UserManager<ApplicationUser> _userMgr;
        private readonly RoleManager<IdentityRole> _roleMgr;

        public UsersController(ApplicationDbContext db, IUserProjectAssignmentService a, IAuditLogService au,
            UserManager<ApplicationUser> userMgr, RoleManager<IdentityRole> roleMgr)
            : base(db, a, au)
        { _userMgr = userMgr; _roleMgr = roleMgr; }

        public async Task<IActionResult> Index()
        {
            var users = await _userMgr.Users.ToListAsync();
            var list = new List<UserListItemViewModel>();
            foreach (var u in users)
            {
                var roles = await _userMgr.GetRolesAsync(u);
                list.Add(new UserListItemViewModel
                {
                    Id = u.Id, Email = u.Email ?? "", FirstName = u.FirstName, LastName = u.LastName,
                    Role = roles.FirstOrDefault() ?? "", Specialite = u.Specialite,
                    ClientId = u.ClientId, IsActive = u.IsActive
                });
            }
            return View(list);
        }

        [HttpGet]
        public IActionResult Create()
        {
            var vm = new UserCreateViewModel
            {
                AvailableRoles = new List<string> { UserRoles.Superviseur, UserRoles.Visiteur, UserRoles.Membre }
            };
            return View(vm);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(UserCreateViewModel vm)
        {
            vm.AvailableRoles = new List<string> { UserRoles.Superviseur, UserRoles.Visiteur, UserRoles.Membre };
            if (!ModelState.IsValid) return View(vm);

            var user = new ApplicationUser
            {
                UserName = vm.Email, Email = vm.Email, EmailConfirmed = true,
                FirstName = vm.FirstName, LastName = vm.LastName,
                Specialite = vm.Specialite, ClientId = vm.ClientId,
                IsActive = vm.IsActive, SecurityStamp = Guid.NewGuid().ToString()
            };
            var result = await _userMgr.CreateAsync(user, vm.Password);
            if (!result.Succeeded)
            {
                foreach (var e in result.Errors) ModelState.AddModelError("", e.Description);
                return View(vm);
            }
            await _userMgr.AddToRoleAsync(user, vm.Role);
            await LogAsync(null, "Create", "User", null, $"Utilisateur créé: {user.Email} ({vm.Role})");
            TempData["Success"] = "Utilisateur créé.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Edit(string id)
        {
            var user = await _userMgr.FindByIdAsync(id);
            if (user == null) return NotFound();
            var roles = await _userMgr.GetRolesAsync(user);
            return View(new UserEditViewModel
            {
                Id = user.Id, FirstName = user.FirstName, LastName = user.LastName,
                Email = user.Email ?? "", Role = roles.FirstOrDefault() ?? "",
                Specialite = user.Specialite, ClientId = user.ClientId, IsActive = user.IsActive,
                AvailableRoles = new List<string> { UserRoles.Superviseur, UserRoles.Visiteur, UserRoles.Membre }
            });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(UserEditViewModel vm)
        {
            vm.AvailableRoles = new List<string> { UserRoles.Superviseur, UserRoles.Visiteur, UserRoles.Membre };
            if (!ModelState.IsValid) return View(vm);
            var user = await _userMgr.FindByIdAsync(vm.Id);
            if (user == null) return NotFound();

            user.FirstName = vm.FirstName; user.LastName = vm.LastName;
            user.Email = vm.Email; user.UserName = vm.Email;
            user.Specialite = vm.Specialite; user.ClientId = vm.ClientId; user.IsActive = vm.IsActive;

            var result = await _userMgr.UpdateAsync(user);
            if (!result.Succeeded)
            {
                foreach (var e in result.Errors) ModelState.AddModelError("", e.Description);
                return View(vm);
            }

            // Update role
            var currentRoles = await _userMgr.GetRolesAsync(user);
            await _userMgr.RemoveFromRolesAsync(user, currentRoles);
            await _userMgr.AddToRoleAsync(user, vm.Role);

            // Password change
            if (!string.IsNullOrEmpty(vm.NewPassword))
            {
                var token = await _userMgr.GeneratePasswordResetTokenAsync(user);
                await _userMgr.ResetPasswordAsync(user, token, vm.NewPassword);
            }

            await LogAsync(null, "Update", "User", null, $"Utilisateur modifié: {user.Email}");
            TempData["Success"] = "Utilisateur modifié.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string id)
        {
            var user = await _userMgr.FindByIdAsync(id);
            if (user == null) return NotFound();
            await _userMgr.DeleteAsync(user);
            await LogAsync(null, "Delete", "User", null, $"Utilisateur supprimé: {user.Email}");
            TempData["Success"] = "Utilisateur supprimé.";
            return RedirectToAction(nameof(Index));
        }
    }
}
