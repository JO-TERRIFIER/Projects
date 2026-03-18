// ============================================================
// SmartGPON — Services/TempUploadCleanupService.cs
// P2b · IHostedService · Nettoyage /uploads/temp/ orphelins > 2h
// ============================================================
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace SmartGPON.Web.Services
{
    public class TempUploadCleanupService : BackgroundService
    {
        private readonly IWebHostEnvironment _env;
        private readonly IConfiguration     _config;
        private readonly ILogger<TempUploadCleanupService> _logger;

        private readonly TimeSpan _interval   = TimeSpan.FromMinutes(30);
        private readonly TimeSpan _maxAge     = TimeSpan.FromHours(2);

        public TempUploadCleanupService(
            IWebHostEnvironment env,
            IConfiguration config,
            ILogger<TempUploadCleanupService> logger)
        {
            _env    = env;
            _config = config;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try { Cleanup(); }
                catch (Exception ex) { _logger.LogError(ex, "TempUploadCleanupService: erreur nettoyage."); }

                await Task.Delay(_interval, stoppingToken);
            }
        }

        private void Cleanup()
        {
            var uploadPath = _config.GetValue<string>("FileUpload:UploadPath") ?? "uploads";
            var tempRoot   = Path.Combine(_env.ContentRootPath, uploadPath, "temp");
            if (!Directory.Exists(tempRoot)) return;

            foreach (var dir in Directory.GetDirectories(tempRoot))
            {
                var info = new DirectoryInfo(dir);
                // Supprimer si vieux de plus de 2h (basé sur date création du dossier)
                if (DateTime.UtcNow - info.CreationTimeUtc > _maxAge)
                {
                    try
                    {
                        Directory.Delete(dir, recursive: true);
                        _logger.LogInformation("TempUploadCleanupService: supprimé {Dir}", dir);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "TempUploadCleanupService: échec suppression {Dir}", dir);
                    }
                }
            }
        }
    }
}
