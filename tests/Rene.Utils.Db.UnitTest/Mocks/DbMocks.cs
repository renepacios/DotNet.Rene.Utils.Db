namespace Rene.Utils.Db.UnitTest.Mocks
{
    using Microsoft.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore.ChangeTracking;
    using Microsoft.EntityFrameworkCore.ChangeTracking.Internal;
    using Microsoft.EntityFrameworkCore.Metadata;
    using Models;
    using Moq;

    public static class DbMocks
    {
        private static Mock<EntityEntry<T>> GetEntityEntry<T>(T entity) where T : class, IEntity
        {
            var internalEntityEntry = new InternalEntityEntry(
                new Mock<IStateManager>().Object,
                new RuntimeEntityType("T", typeof(T), false, null!, null, null, ChangeTrackingStrategy.Snapshot, null, false, null),
                entity);

            var mockEntityEntry = new Mock<EntityEntry<T>>(internalEntityEntry);
            mockEntityEntry.Setup(m => m.Entity).Returns(entity);
            return mockEntityEntry;
        }


        public static Mock<DbSet<T>> GetMockDbSet<T>(List<T> data) where T : class, IEntity
        {
            var internalEntityEntry = new InternalEntityEntry(
                new Mock<IStateManager>().Object,
                new RuntimeEntityType("T", typeof(T), false, null!, null, null, ChangeTrackingStrategy.Snapshot, null, false, null),
                data);

            var mockEntityEntry = new Mock<EntityEntry<T>>(internalEntityEntry);


            var queryable = data.AsQueryable();
            var dbSet = new Mock<DbSet<T>>();
            dbSet.As<IQueryable<T>>().Setup(m => m.Provider).Returns(queryable.Provider);
            dbSet.As<IQueryable<T>>().Setup(m => m.Expression).Returns(queryable.Expression);
            dbSet.As<IQueryable<T>>().Setup(m => m.ElementType).Returns(queryable.ElementType);
            dbSet.As<IQueryable<T>>().Setup(m => m.GetEnumerator()).Returns(() => queryable.GetEnumerator());
            dbSet.Setup(d => d.Add(It.IsAny<T>())).Callback<T>(data.Add);
            dbSet.Setup(d => d.AddAsync(It.IsAny<T>(), It.IsAny<CancellationToken>()))
                .Callback((T s, CancellationToken _) =>
                {
                    data.Add(s);
                    mockEntityEntry.Setup(m => m.Entity).Returns(s);
                })
                .ReturnsAsync((T s, CancellationToken _) =>
                {
                    mockEntityEntry.Setup(m => m.Entity).Returns(s);
                    return mockEntityEntry.Object;
                });

            dbSet.Setup(d => d.Update(It.IsAny<T>()))
                .Callback<T>(d =>
                {
                    data = data.Where(s => s.Id != d.Id).ToList();
                    data.Add(d);

                })
                .Returns((T s) =>
                {
                    var mEntry = GetEntityEntry(s);
                    mEntry.Setup(m => m.Entity).Returns(s);
                    return mEntry.Object;
                });

            dbSet.Setup(d => d.Remove(It.IsAny<T>()))
                .Callback<T>(entity => data.Remove(entity));



            dbSet.Setup(d => d.FindAsync(It.IsAny<object[]>()))
                .ReturnsAsync((object[] ids) =>
                {
                    int.TryParse(ids[0].ToString(), out var id);
                    var dev = data.Find(s => s.Id == id);
                    return dev;
                });

            dbSet.Setup(d => d.FindAsync(It.IsAny<object[]>(), It.IsAny<CancellationToken>()))
                .Returns((object[] ids, CancellationToken _) =>
                {
                    var id = (int)ids[0];
                    var dev = data.Find(s => s.Id == id);
                    //return dev;
                    return new ValueTask<T>(dev);
                });


            return dbSet;
        }
    }
}
