﻿using Microsoft.EntityFrameworkCore;

namespace Rene.Utils.Db.DbInternal
{

    //internal interface IFakeUnitOfWork : IDbUtilsUnitOfWork
    //{
    //    string GetKeyNameFromEntityType<TModel>();
    //    string GetKeyNameFromEntityType(Type type);
    //}

    internal class FakeUnitOfWork<TDbContext>(TDbContext dbContext) 
        : IDbUtilsUnitOfWork //, IFakeUnitOfWork
        where TDbContext : DbContext
    {
        public virtual int SaveChanges() => dbContext.SaveChanges();


        public virtual Task<int> SaveChangesAsync(CancellationToken cancellationToken = default(CancellationToken))
            => dbContext.SaveChangesAsync(cancellationToken);

        public virtual Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default(CancellationToken))
            => dbContext.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);


        internal virtual string GetKeyNameFromEntityType<TModel>() => dbContext.GetKeyNameFromEntityType<TModel>();

        internal virtual string GetKeyNameFromEntityType(Type type) => dbContext.GetKeyNameFromEntityType(type);
    }
}
