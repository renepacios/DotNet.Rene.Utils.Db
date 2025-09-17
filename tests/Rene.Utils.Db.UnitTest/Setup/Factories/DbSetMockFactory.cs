namespace Rene.Utils.Db.UnitTest.Setup.Factories
{
    using Microsoft.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore.ChangeTracking;
    using Microsoft.EntityFrameworkCore.ChangeTracking.Internal;
    using Microsoft.EntityFrameworkCore.Metadata;
    using Models;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    internal static class DbSetMockFactory
    {
        /// <summary>
        /// Creates a Mock of DbSet, backed by the provided in-memory list 'data'.
        /// This simulates adding/removing/finding on an EF set.
        /// </summary>
        internal static Mock<DbSet<T>> Create<T>(List<T> data) where T : class, IEntity
        {
            var queryable = data.AsQueryable();

            // The main DbSet mock
            var dbSetMock = new Mock<DbSet<T>>();

            // Hook up IQueryable members so we can do .Where(), .Select(), etc.
            dbSetMock.As<IQueryable<T>>().Setup(m => m.Provider).Returns(queryable.Provider);
            dbSetMock.As<IQueryable<T>>().Setup(m => m.Expression).Returns(queryable.Expression);
            dbSetMock.As<IQueryable<T>>().Setup(m => m.ElementType).Returns(queryable.ElementType);
            dbSetMock.As<IQueryable<T>>().Setup(m => m.GetEnumerator()).Returns(() => queryable.GetEnumerator());

            // Add(...) => modifies the data list and returns an EntityEntry mock
            dbSetMock.Setup(d => d.Add(It.IsAny<T>()))
                .Callback((T entity) => data.Add(entity))
                .Returns((T entity) => CreateEntityEntry(entity));

            // AddAsync(...) => same, but async
            dbSetMock.Setup(d => d.AddAsync(It.IsAny<T>(), It.IsAny<CancellationToken>()))
                .Callback((T entity, CancellationToken _) => data.Add(entity))
                .ReturnsAsync((T entity, CancellationToken _) => CreateEntityEntry(entity));

            // Update(...)
            dbSetMock.Setup(d => d.Update(It.IsAny<T>()))
                .Callback((T entity) =>
                {
                    var existing = data.FirstOrDefault(x => x.Id == entity.Id);
                    if (existing != null) data.Remove(existing);
                    data.Add(entity);
                })
                .Returns((T entity) => CreateEntityEntry(entity));

            // Remove(...)
            dbSetMock.Setup(d => d.Remove(It.IsAny<T>()))
                .Callback((T entity) => data.Remove(entity))
                .Returns((T entity) => CreateEntityEntry(entity));

            // FindAsync(...)
            dbSetMock.Setup(d => d.FindAsync(It.IsAny<object[]>()))
                .ReturnsAsync((object[] keyValues) =>
                {
                    var id = Convert.ToInt32(keyValues[0]);
                    return data.FirstOrDefault(x => x.Id == id);
                });

            // Overload with cancellation token
            dbSetMock.Setup(d => d.FindAsync(It.IsAny<object[]>(), It.IsAny<CancellationToken>()))
                .Returns((object[] keyValues, CancellationToken _) =>
                {
                    var id = Convert.ToInt32(keyValues[0]);
                    var entity = data.FirstOrDefault(x => x.Id == id);
                    return new ValueTask<T>(entity!);
                });

            return dbSetMock;
        }

        /// <summary>
        /// Helper method to mock returning an EntityEntry from EF calls.
        /// </summary>
        private static EntityEntry<T> CreateEntityEntry<T>(T entity) where T : class, IEntity => CreateEntityMock(entity).Object;

        private static Mock<EntityEntry<T>> CreateEntityMock<T>(T entity) where T : class, IEntity
        {
            var internalEntityEntry = new InternalEntityEntry(
                new Mock<IStateManager>().Object,
                new RuntimeEntityType("T", typeof(T), false, null!, null, null, ChangeTrackingStrategy.Snapshot, null, false, null),
                entity);

            var entryMock = new Mock<EntityEntry<T>>(internalEntityEntry);
            entryMock.Setup(e => e.Entity).Returns(entity);
            return entryMock;
        }
    }

}
