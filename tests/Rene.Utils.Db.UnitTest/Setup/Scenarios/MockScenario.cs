namespace Rene.Utils.Db.UnitTest.Setup.Scenarios
{
    using AutoMapper;
    using Factories;
    using Microsoft.EntityFrameworkCore;
    using Models;
    using Rene.Utils.Db.DbInternal;
    using System.Collections.Generic;

    /// <summary>
    /// Bundles all relevant mocks (DbContext, UoW, Mapper, DbSet, and data) 
    /// so tests can easily verify behaviors.
    /// </summary>
    public class MockScenario<T>
        where T : class, IEntity
    {
        internal Mock<DbContext> DbContextMock { get; }
        internal Mock<IDbUtilsUnitOfWork> UowMock { get; }
        internal Mock<FakeUnitOfWork<DbContext>> FakeUowMock { get; }
        internal Mock<IMapper> MapperMock { get; }
        internal Mock<DbSet<T>> DbSetMock { get; }
        internal List<T> Data { get; }

        internal MockScenario(
            Mock<DbContext> dbContextMock,
            Mock<IDbUtilsUnitOfWork> uowMock,
            Mock<FakeUnitOfWork<DbContext>> fakeUowMock,
            Mock<IMapper> mapperMock,
            Mock<DbSet<T>> dbSetMock,
            List<T> data)
        {
            DbContextMock = dbContextMock;
            UowMock = uowMock;
            FakeUowMock = fakeUowMock;
            MapperMock = mapperMock;
            DbSetMock = dbSetMock;
            Data = data;
        }
    }

    /// <summary>
    /// Factory for creating a full 'scenario' of mocks 
    /// (DbContext, UoW, Mapper, DbSet) pre-wired together.
    /// </summary>
    internal static class MockScenarioFactory
    {
        internal static MockScenario<Sample> CreateSampleScenario(int howMany = 10)
        {
            var data = SampleBuilder.CreateList(howMany);
            return CreateScenario<Sample>(data, MapperMockFactory.CreateSampleMapper);
        }

        /// <summary>
        /// Creates a scenario for arbitrary T, with the ability to 
        /// pass a custom mapper creation method if needed.
        /// </summary>
        /// <typeparam name="T">The entity type.</typeparam>
        /// <param name="seedData">In-memory list of seed data for the mocked DbSet.</param>
        /// <param name="mapperFactory">Function that creates a mapper mock. </param>
        internal static MockScenario<T> CreateScenario<T>(List<T> seedData, Func<Mock<IMapper>> mapperFactory)
            where T : class, IEntity
        {
            // 1) Create DbSet mock
            var dbSetMock = DbSetMockFactory.Create(seedData);

            // 2) Create DbContext mock
            var dbContextMock = DbContextMockFactory.Create(dbSetMock);

            // 3) Create UoW mock
            var uowMock = UowMockFactory.Create(dbContextMock);

            // 4) Create Fake UoW mock
            var fakeUowMock = FakeUowMockFactory.Create(dbContextMock);

            // 5) Create or pass in the mapper mock
            var mapperMock = mapperFactory();

            // 6) Return scenario
            return new MockScenario<T>(
                dbContextMock,
                uowMock,
                fakeUowMock,
                mapperMock,
                dbSetMock,
                seedData
            );
        }
    }

}
