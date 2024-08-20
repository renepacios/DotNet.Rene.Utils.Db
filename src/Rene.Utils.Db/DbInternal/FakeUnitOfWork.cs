using Microsoft.EntityFrameworkCore;

namespace Rene.Utils.Db.DbInternal
{
    internal class FakeUnitOfWork<TDbContext>(TDbContext dbContext) : IDbUtilsUnitOfWork
        where TDbContext : DbContext
    {
        public int SaveChanges() => dbContext.SaveChanges();


        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default(CancellationToken)) 
            => dbContext.SaveChangesAsync(cancellationToken);

        public Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default(CancellationToken)) 
            => dbContext.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }
}
