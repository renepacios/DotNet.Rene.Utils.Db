namespace Rene.Utils.Db.UnitTest
{
    using AutoMapper;
    using Builder;
    using Commands;
    using DbInternal;
    using Microsoft.EntityFrameworkCore;
    using Mocks;
    using Models;
    using Moq;

    public class GenericDeleteTest

    {
        private Mock<DbContext> MockDbContext { get; }
        private Mock<FakeUnitOfWork<DbContext>> MockUow { get; }
        private Mock<IMapper> MockMapper { get; }
        private Mock<DbSet<Sample>> MockDbSet { get; }

        public GenericDeleteTest()
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
            MockUow.Setup(s => s.GetKeyNameFromEntityType<Sample>())
                .Returns(nameof(Sample.Id));
            MockUow.Setup(s => s.GetKeyNameFromEntityType(It.IsAny<Type>()))
                .Returns(nameof(Sample.Id));
            MockMapper = mocks.MockMapper;
            MockDbSet = mocks.MockDbSet;


        }

        [Fact]
        public async Task delete_command_work_as_expected()
        {

            var entityId = 2;

            var command = new DeleteCommand<SampleDetailsViewModel>(entityId);
            var handler =
                new GenericCommandHandler<SampleDetailsViewModel, Sample, DbContext, IDbUtilsUnitOfWork>(MockMapper.Object,
                    MockDbContext.Object, MockUow.Object);

            var result = await handler.Handle(command, new CancellationToken());


            MockDbSet.Verify(m => m.Remove(It.Is<Sample>(w => w.Id == entityId)), Times.Once);

            MockDbContext.Verify(m => m.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
            MockUow.Verify(m => m.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);


        }


        [Fact]
        public async Task delete_command_with_external_uow_work_as_expected()
        {

            var entityId = 2;
            var mockUow = new Mock<IDbUtilsUnitOfWork>();
            mockUow.Setup(s => s.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .Callback(() => MockDbContext.Object.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(() => 1);

            var command = new DeleteCommand<SampleDetailsViewModel>(entityId);
            var handler =
                new GenericCommandHandler<SampleDetailsViewModel, Sample, DbContext, IDbUtilsUnitOfWork>(MockMapper.Object,
                    MockDbContext.Object, mockUow.Object);

            var result = await handler.Handle(command, new CancellationToken());

            MockDbSet.Verify(m => m.Remove(It.Is<Sample>(w => w.Id == entityId)), Times.Once);

            MockDbContext.Verify(m => m.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
            mockUow.Verify(m => m.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

        }


        [Fact]
        public async Task delete_command_with_unexisted_id_work_as_expected()
        {

            var entityId = 2002;
            var mockUow = new Mock<IDbUtilsUnitOfWork>();
            mockUow.Setup(s => s.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .Callback(() => MockDbContext.Object.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(() => 1);

            var command = new DeleteCommand<SampleDetailsViewModel>(entityId);
            var handler =
                new GenericCommandHandler<SampleDetailsViewModel, Sample, DbContext, IDbUtilsUnitOfWork>(MockMapper.Object,
                    MockDbContext.Object, mockUow.Object);



            Func<Task<bool>> act = async () => await handler.Handle(command, new CancellationToken());

            var result = await act.Should()
                    .ThrowAsync<KeyNotFoundException>()
                    .WithMessage($"Entidad no encontrada {entityId}")
                ;

            MockDbSet.Verify(m => m.Remove(It.Is<Sample>(w => w.Id == entityId)), Times.Never);

            MockDbContext.Verify(m => m.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
            mockUow.Verify(m => m.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);

        }

        //TODO:
        /*
         * - Test validation commands
         * - Test events
         *
         */
    }

}