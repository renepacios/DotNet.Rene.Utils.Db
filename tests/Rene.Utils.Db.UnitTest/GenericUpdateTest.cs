namespace Rene.Utils.Db.UnitTest
{
    using AutoMapper;
    using Builder;
    using Commands;
    using DbInternal;
    using FluentAssertions;
    using Microsoft.EntityFrameworkCore;
    using Mocks;
    using Models;
    using Moq;

    public class GenericUpdateTest

    {
        private Mock<DbContext> MockDbContext { get; }
        private Mock<FakeUnitOfWork<DbContext>> MockUow { get; }
        private Mock<IMapper> MockMapper { get; }
        private Mock<DbSet<Sample>> MockDbSet { get; }

        public GenericUpdateTest()
        {
            var mocks = MockBuilders.Create();

            MockDbContext = mocks.MockDbContext;
            MockUow = new Mock<FakeUnitOfWork<DbContext>>(MockBehavior.Strict, MockDbContext.Object);
            MockUow.Setup(s => s.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .Callback(() => MockDbContext.Object.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(() => 1);
            MockUow.Setup(s => s.SaveChangesAsync(It.IsAny<bool>(), It.IsAny<CancellationToken>()))
                .Callback<bool, CancellationToken>((b, c) => MockDbContext.Object.SaveChangesAsync(b, c))
                .ReturnsAsync(() => 1);

            MockUow.Setup(s => s.SaveChanges())
                .Callback(() => MockDbContext.Object.SaveChanges())
                .Returns(() => 1);
            MockUow.Setup(s => s.GetKeyNameFromEntityType<Sample>()).Returns(nameof(Sample.Id));
            MockUow.Setup(s => s.GetKeyNameFromEntityType(It.IsAny<Type>()))
                .Returns(nameof(Sample.Id));
            MockMapper = mocks.MockMapper;
            MockDbSet = mocks.MockDbSet;


        }

        [Fact]
        public async Task update_command_work_as_expected()
        {

            var entityId = 2;
            const string newName = "New Name";
            const string newDescription = "New Description";

            var vmNewValues = SampleDetailsViewModel
                .DefaultSampleDetails
                .WithDescription(newDescription)
                .WithName(newName)
                .WithId(1003);


            var updateCommand = new UpdateCommand<SampleDetailsViewModel>(vmNewValues, entityId);
            var handler =
                new GenericCommandHandler<SampleDetailsViewModel, Sample, DbContext, IDbUtilsUnitOfWork>(MockMapper.Object,
                    MockDbContext.Object, MockUow.Object);

            var result = await handler.Handle(updateCommand, new CancellationToken());


            MockMapper.Verify(m => m.Map<SampleViewModel, Sample>(
                It.Is<SampleViewModel>(s => s.Equals(vmNewValues))
                , It.Is<Sample>(s => s.Id == entityId)));

            MockDbSet
                .Verify(
                    m => m.Update(It.Is<Sample>(w => w.Id == entityId && w.Name == vmNewValues.Name && w.Description == vmNewValues.Description)), Times.Once);

            MockDbContext.Verify(m => m.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
            MockUow.Verify(m => m.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);


            result.Should().BeOfType<SampleDetailsViewModel>();

            result.Id.Should().Be(entityId);
            result.Name.Should().Be(newName);
            result.Description.Should().Be(newDescription);
        }


        //TODO:
        /*
         * - Test validation commands
         * - Test events
         *
         */
    }

}