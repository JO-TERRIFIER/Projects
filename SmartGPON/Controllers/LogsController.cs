using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartGPON.Infrastructure.Data;

namespace SmartGPON.Web.Controllers
{
    [Authorize]
    public class LogsController : Controller
    {
        private readonly ApplicationDbContext _db;
        public LogsController(ApplicationDbContext db) { _db = db; }

        public async Task<IActionResult> Index() =>
            View(await _db.SecurityEvents.OrderByDescending(e => e.DateEvenement).Take(200).AsNoTracking().ToListAsync());
    }
}
