namespace Rene.Utils.Db.UnitTest
{
    using AutoMapper;
    using Builder;
    using Commands;
    using Fixtures;
    using FluentAssertions;
    using Microsoft.EntityFrameworkCore;
    using Models;
    using Moq;

    public class GenericAllTest : IClassFixture<InfrastructureMocksFixture>

    {
    private Mock<DbContext> MockDbContext { get; }
    private Mock<IDbUtilsUnitOfWork> MockUow { get; }
    private Mock<IMapper> MockMapper { get; }
    private Mock<DbSet<Sample>> MockDbSet { get; }

    public GenericAllTest(InfrastructureMocksFixture mocksFixture)
    {
            MockDbContext=mocksFixture.MockDbContext;
            MockUow=mocksFixture.MockUow;
            MockMapper=mocksFixture.MockMapper;
            MockDbSet=mocksFixture.MockDbSet;


    }

    }

}