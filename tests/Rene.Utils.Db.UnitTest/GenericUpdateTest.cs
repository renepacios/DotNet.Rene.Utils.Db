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

    public class GenericUpdateTest : IClassFixture<InfrastructureMocksFixture>

    {
    private Mock<DbContext> MockDbContext { get; }
    private Mock<IDbUtilsUnitOfWork> MockUow { get; }
    private Mock<IMapper> MockMapper { get; }
    private Mock<DbSet<Sample>> MockDbSet { get; }

    public GenericUpdateTest(InfrastructureMocksFixture mocksFixture)
    {
            MockDbContext=mocksFixture.MockDbContext;
            MockUow=mocksFixture.MockUow;
            MockMapper=mocksFixture.MockMapper;
            MockDbSet=mocksFixture.MockDbSet;


    }

    [Fact]
    public async Task update_command_work_as_expected()
    {
        var entityId = 2;
        var vmNewValues = SampleDetailsViewModel
            .DefaultSampleDetails;
            

        var updateCommand = new UpdateCommand<SampleDetailsViewModel>(vmNewValues,entityId);
        var handler =
            new GenericCommandHandler<SampleDetailsViewModel, Sample, DbContext, IDbUtilsUnitOfWork>(MockMapper.Object,
                MockDbContext.Object, MockUow.Object);

        var result = await handler.Handle(updateCommand, new CancellationToken());

        MockMapper.Verify(m => m.Map<Sample>(It.Is<SampleDetailsViewModel>(x => x == vmNewValues)), Times.Once);

        MockDbSet
            .Verify(
                m => m.AddAsync(
                    It.Is<Sample>(s => s.Id == vmNewValues.Id && s.Name == vmNewValues.Name && s.Description == vmNewValues.Description),
                    It.IsAny<CancellationToken>()), Times.Once);
        MockDbContext.Verify(m => m.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

        MockMapper.Verify(
            m => m.Map<SampleDetailsViewModel>(It.Is<Sample>(s =>
                s.Id == vmNewValues.Id && s.Name == vmNewValues.Name && s.Description == vmNewValues.Description)), Times.Once);

        result.Should().BeOfType<SampleDetailsViewModel>();

        result.Id.Should().Be(99);
        result.Name.Should().Be("NoBodySample");
        result.Description.Should().Be("This is a fake description");
    }


    //TODO:
    /*
     * - Test validation commands
     * - Test events
     *
     */
    }

}