namespace Rene.Utils.Db.UnitTest
{
    using Microsoft.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore.ChangeTracking;
    using Microsoft.EntityFrameworkCore.ChangeTracking.Internal;
    using Microsoft.EntityFrameworkCore.Metadata;
    using Moq;

    public static class DbContextMockUtils
    {

        public static Mock<DbSet<T>> GetMockDbSet<T>(List<T> data) where T : class
        {
            var internalEntityEntry = new InternalEntityEntry(
                new Mock<IStateManager>().Object,
                new RuntimeEntityType("T", typeof(T), false, null!, null, null, ChangeTrackingStrategy.Snapshot, null, false,null),
                data);

            var mockEntityEntry=new Mock<EntityEntry<T>>(internalEntityEntry);
            

            var queryable = data.AsQueryable();
            var dbSet = new Mock<DbSet<T>>();
            dbSet.As<IQueryable<T>>().Setup(m => m.Provider).Returns(queryable.Provider);
            dbSet.As<IQueryable<T>>().Setup(m => m.Expression).Returns(queryable.Expression);
            dbSet.As<IQueryable<T>>().Setup(m => m.ElementType).Returns(queryable.ElementType);
            dbSet.As<IQueryable<T>>().Setup(m => m.GetEnumerator()).Returns(() => queryable.GetEnumerator());
            dbSet.Setup(d => d.Add(It.IsAny<T>())).Callback<T>(data.Add);
            dbSet.Setup(d => d.AddAsync(It.IsAny<T>(), It.IsAny<CancellationToken>()))
                .Callback<T,CancellationToken>((T s, CancellationToken _) =>
                {
                    data.Add(s);
                    mockEntityEntry.Setup(m => m.Entity).Returns(s);
                })
                .ReturnsAsync((T s, CancellationToken _) =>
                {
                    mockEntityEntry.Setup(m => m.Entity).Returns(s);
                    return mockEntityEntry.Object;
                });

            return dbSet;
        }
    }
}
