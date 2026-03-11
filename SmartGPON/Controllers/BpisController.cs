using Microsoft.EntityFrameworkCore;
using SmartGPON.Core.Entities;
using SmartGPON.Core.Interfaces;
using SmartGPON.Infrastructure.Data;

namespace SmartGPON.Web.Controllers
{
    public class BpisController : ScopedEquipmentControllerBase<Bpi>
    {
        public BpisController(ApplicationDbContext db, IAuthorizationScopeService scope, IApprovalService approvals, IAuditService audit)
            : base(db, scope, approvals, audit) { }

        protected override string EntityName => nameof(Bpi);
        protected override IQueryable<Bpi> QueryForIndex() => Db.Bpis.Include(b => b.Fdt).ThenInclude(fd => fd.Olt);
        protected override Task<Bpi?> FindAsync(int id) => Db.Bpis.FirstOrDefaultAsync(b => b.Id == id)!;
        protected override int GetEntityId(Bpi entity) => entity.Id;
        protected override Task<int> ResolveProjetIdAsync(Bpi entity) => ProjetIdFromFdtAsync(entity.FdtId);
        protected override Task<int> ResolveProjetIdFromIdAsync(int id) => ProjetIdFromBpiAsync(id);

        protected override async Task PopulateCreateEditBagsAsync(Bpi? entity = null)
        {
            ViewBag.Fdts = await Db.Fdts.AsNoTracking().OrderBy(f => f.Nom).ToListAsync();
        }
    }
}
