using Microsoft.EntityFrameworkCore;
using SmartGPON.Core.Entities;
using SmartGPON.Core.Interfaces;
using SmartGPON.Infrastructure.Data;

namespace SmartGPON.Web.Controllers
{
    public class OltsController : ScopedEquipmentControllerBase<Olt>
    {
        public OltsController(ApplicationDbContext db, IAuthorizationScopeService scope, IApprovalService approvals, IAuditService audit)
            : base(db, scope, approvals, audit) { }

        protected override string EntityName => nameof(Olt);
        protected override IQueryable<Olt> QueryForIndex() => Db.Olts.Include(o => o.Zone).ThenInclude(z => z.Projet);
        protected override Task<Olt?> FindAsync(int id) => Db.Olts.FirstOrDefaultAsync(o => o.Id == id)!;
        protected override int GetEntityId(Olt entity) => entity.Id;
        protected override Task<int> ResolveProjetIdAsync(Olt entity) => ProjetIdFromZoneAsync(entity.ZoneId);
        protected override Task<int> ResolveProjetIdFromIdAsync(int id) => ProjetIdFromOltAsync(id);

        protected override async Task PopulateCreateEditBagsAsync(Olt? entity = null)
        {
            ViewBag.Zones = await Db.Zones.AsNoTracking().OrderBy(z => z.Nom).ToListAsync();
        }
    }
}
