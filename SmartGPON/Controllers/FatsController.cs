using Microsoft.EntityFrameworkCore;
using SmartGPON.Core.Entities;
using SmartGPON.Core.Interfaces;
using SmartGPON.Infrastructure.Data;

namespace SmartGPON.Web.Controllers
{
    public class FatsController : ScopedEquipmentControllerBase<Fat>
    {
        public FatsController(ApplicationDbContext db, IAuthorizationScopeService scope, IApprovalService approvals, IAuditService audit)
            : base(db, scope, approvals, audit) { }

        protected override string EntityName => nameof(Fat);
        protected override IQueryable<Fat> QueryForIndex() => Db.Fats.Include(f => f.Fdt).ThenInclude(fd => fd.Olt);
        protected override Task<Fat?> FindAsync(int id) => Db.Fats.FirstOrDefaultAsync(f => f.Id == id)!;
        protected override int GetEntityId(Fat entity) => entity.Id;
        protected override Task<int> ResolveProjetIdAsync(Fat entity) => ProjetIdFromFdtAsync(entity.FdtId);
        protected override Task<int> ResolveProjetIdFromIdAsync(int id) => ProjetIdFromFatAsync(id);

        protected override async Task PopulateCreateEditBagsAsync(Fat? entity = null)
        {
            ViewBag.Fdts = await Db.Fdts.AsNoTracking().OrderBy(f => f.Nom).ToListAsync();
        }
    }
}
