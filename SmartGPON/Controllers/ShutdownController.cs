// ============================================================
// SmartGPON — Controllers/ShutdownController.cs
// Contrôleur dédié à l'extinction de l'application kiosk.
// Aucun [Authorize] : l'authentification serait un obstacle
// si la session expire ou si le cookie n'est pas envoyé par fetch.
// Protégé par un token secret partagé en mémoire.
// ============================================================
using Microsoft.AspNetCore.Mvc;

namespace SmartGPON.Web.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ShutdownController : ControllerBase
    {
        // Token statique généré au démarrage de l'app — valide pour toute la durée de vie du process
        public static readonly string Token = Guid.NewGuid().ToString("N");

        private readonly IHostApplicationLifetime _lifetime;
        private readonly KioskProcessContext _kioskCtx;
        private readonly ILogger<ShutdownController> _logger;

        public ShutdownController(
            IHostApplicationLifetime lifetime,
            KioskProcessContext kioskCtx,
            ILogger<ShutdownController> logger)
        {
            _lifetime = lifetime;
            _kioskCtx = kioskCtx;
            _logger = logger;
        }

        [HttpPost]
        public IActionResult Execute([FromQuery] string token)
        {
            if (token != Token)
            {
                _logger.LogWarning("Shutdown refusé : token invalide.");
                return Unauthorized();
            }

            _logger.LogInformation("Arrêt demandé via bouton kiosk.");

            // 1. Tuer le processus Edge kiosk si connu
            if (_kioskCtx.BrowserProcess != null)
            {
                try { _kioskCtx.BrowserProcess.Kill(entireProcessTree: true); }
                catch (Exception ex) { _logger.LogWarning(ex, "Impossible de tuer BrowserProcess."); }
            }

            // 2. Tuer tous les processus msedge liés au profil SmartGPON_Kiosk via PowerShell (filet de sécurité)
            try
            {
                var ps = new System.Diagnostics.ProcessStartInfo("powershell",
                    "-NoProfile -NonInteractive -Command \"" +
                    "Get-WmiObject Win32_Process -Filter \\\"Name='msedge.exe'\\\" " +
                    "| Where-Object { $_.CommandLine -match 'SmartGPON_Kiosk' } " +
                    "| ForEach-Object { Stop-Process -Id $_.ProcessId -Force }\"")
                {
                    CreateNoWindow = true,
                    UseShellExecute = false
                };
                System.Diagnostics.Process.Start(ps)?.WaitForExit(3000);
            }
            catch (Exception ex) { _logger.LogWarning(ex, "PowerShell Kill Edge: erreur ignorée."); }

            // 3. Arrêt propre du serveur ASP.NET via IHostApplicationLifetime
            _ = Task.Run(async () =>
            {
                await Task.Delay(300); // laisse le temps de renvoyer la réponse HTTP
                _lifetime.StopApplication();
            });

            return Ok(new { message = "Arrêt en cours…" });
        }
    }
}
