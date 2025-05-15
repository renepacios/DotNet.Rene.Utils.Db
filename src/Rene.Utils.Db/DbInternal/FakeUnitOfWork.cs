namespace Rene.Utils.Db.DbInternal
{
    using Microsoft.EntityFrameworkCore;

    //internal interface IFakeUnitOfWork : IDbUtilsUnitOfWork
    //{
    //    string GetKeyNameFromEntityType<TModel>();
    //    string GetKeyNameFromEntityType(Type type);
    //}

    public class FakeUnitOfWork<TDbContext>(TDbContext dbContext)
        : IDbUtilsUnitOfWork //, IFakeUnitOfWork
        where TDbContext : DbContext
    {
        public virtual int SaveChanges() => dbContext.SaveChanges();


        public virtual Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
            => dbContext.SaveChangesAsync(cancellationToken);

        public virtual Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
            => dbContext.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);


        internal virtual string GetKeyNameFromEntityType<TModel>() => dbContext.GetKeyNameFromEntityType<TModel>();

        internal virtual string GetKeyNameFromEntityType(Type type) => dbContext.GetKeyNameFromEntityType(type);
    }
}