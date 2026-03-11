using Microsoft.EntityFrameworkCore;
using SmartGPON.Core.Entities;
using SmartGPON.Core.Interfaces;
using SmartGPON.Infrastructure.Data;

namespace SmartGPON.Web.Controllers
{
    public class FdtsController : ScopedEquipmentControllerBase<Fdt>
    {
        public FdtsController(ApplicationDbContext db, IAuthorizationScopeService scope, IApprovalService approvals, IAuditService audit)
            : base(db, scope, approvals, audit) { }

        protected override string EntityName => nameof(Fdt);
        protected override IQueryable<Fdt> QueryForIndex() => Db.Fdts.Include(f => f.Olt);
        protected override Task<Fdt?> FindAsync(int id) => Db.Fdts.FirstOrDefaultAsync(f => f.Id == id)!;
        protected override int GetEntityId(Fdt entity) => entity.Id;
        protected override Task<int> ResolveProjetIdAsync(Fdt entity) => ProjetIdFromOltAsync(entity.OltId);
        protected override Task<int> ResolveProjetIdFromIdAsync(int id) => ProjetIdFromFdtAsync(id);

        protected override async Task PopulateCreateEditBagsAsync(Fdt? entity = null)
        {
            ViewBag.Olts = await Db.Olts.AsNoTracking().OrderBy(o => o.Nom).ToListAsync();
        }
    }
}
