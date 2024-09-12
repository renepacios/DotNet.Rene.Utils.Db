using Microsoft.EntityFrameworkCore;

namespace Rene.Utils.Db.DbInternal
{

    //internal interface IFakeUnitOfWork : IDbUtilsUnitOfWork
    //{
    //    string GetKeyNameFromEntityType<TModel>();
    //    string GetKeyNameFromEntityType(Type type);
    //}

    internal class FakeUnitOfWork<TDbContext>(TDbContext dbContext) :  IDbUtilsUnitOfWork
        where TDbContext : DbContext
    {
        public int SaveChanges() => dbContext.SaveChanges();


        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default(CancellationToken))
            => dbContext.SaveChangesAsync(cancellationToken);

        public Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default(CancellationToken))
            => dbContext.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);


        internal string GetKeyNameFromEntityType<TModel>() => dbContext.GetKeyNameFromEntityType<TModel>();

        internal string GetKeyNameFromEntityType(Type type) => dbContext.GetKeyNameFromEntityType(type);
    }
}
