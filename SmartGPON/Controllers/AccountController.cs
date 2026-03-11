using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using SmartGPON.Infrastructure.Data;
using SmartGPON.Web.ViewModels;

namespace SmartGPON.Web.Controllers
{
    public class AccountController : Controller
    {
        private readonly SignInManager<ApplicationUser> _signIn;

        public AccountController(SignInManager<ApplicationUser> signIn)
        {
            _signIn = signIn;
        }

        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            ViewBag.ReturnUrl = returnUrl;
            return View(new LoginViewModel());
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel vm, string? returnUrl = null)
        {
            ViewBag.ReturnUrl = returnUrl;
            if (string.IsNullOrWhiteSpace(vm.Email) || string.IsNullOrWhiteSpace(vm.Password))
            {
                ModelState.AddModelError("", "Email et mot de passe obligatoires.");
                return View(vm);
            }

            var result = await _signIn.PasswordSignInAsync(vm.Email.Trim(), vm.Password, vm.RememberMe, lockoutOnFailure: false);
            if (result.Succeeded)
            {
                var safe = !string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl) ? returnUrl : "/";
                return Redirect(safe);
            }

            ModelState.AddModelError("", "Identifiants incorrects.");
            return View(vm);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signIn.SignOutAsync();
            return RedirectToAction(nameof(Login));
        }

        [HttpGet]
        public IActionResult AccessDenied() => View();
    }
}
