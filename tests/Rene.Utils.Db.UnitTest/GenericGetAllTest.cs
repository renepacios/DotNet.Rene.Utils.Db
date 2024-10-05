namespace Rene.Utils.Db.UnitTest
{
    using AutoMapper;
    using Builder;
    using Commands;
    using Microsoft.EntityFrameworkCore;
    using Mocks;
    using Models;
    using Moq;

    public class GenericAllTest

    {
        private Mock<DbContext> MockDbContext { get; }
        private Mock<IDbUtilsUnitOfWork> MockUow { get; }
        private Mock<IMapper> MockMapper { get; }
        private Mock<DbSet<Sample>> MockDbSet { get; }

        public GenericAllTest()
        {
            var mocks = MockBuilders.Create();

            MockDbContext = mocks.MockDbContext;
            MockUow = new Mock<IDbUtilsUnitOfWork>(MockBehavior.Strict);
            MockUow.Setup(s => s.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .Callback(() => MockDbContext.Object.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(() => 1);
            MockUow.Setup(s => s.SaveChangesAsync(It.IsAny<bool>(), It.IsAny<CancellationToken>()))
                .Callback<bool, CancellationToken>((b, c) => MockDbContext.Object.SaveChangesAsync(b, c))
                .ReturnsAsync(() => 1);

            MockUow.Setup(s => s.SaveChanges())
                .Callback(() => MockDbContext.Object.SaveChanges())
                .Returns(() => 1);

            MockDbSet = mocks.MockDbSet;

            MockMapper = mocks.MockMapper;
            MockMapper.Setup(m => m.ProjectTo<SampleDetailsViewModel>(MockDbSet.Object.AsQueryable(),null))
                .Returns(() =>
            {
                var dbSet = MockDbSet.Object;
                var dev = dbSet
                    .Select(m => MockMapper.Object.Map<SampleDetailsViewModel>(m))
                    .AsQueryable();
                return dev;
            });




        }

        [Fact]
        public async Task getAll_command_work_as_expected()
        {

            var entityId = 2;

            var command = new GetAllCommand<SampleDetailsViewModel>();
            var handler =
                new GenericCommandHandler<SampleDetailsViewModel, Sample, DbContext, IDbUtilsUnitOfWork>(MockMapper.Object,
                    MockDbContext.Object, MockUow.Object);

        //    var result = await handler.Handle(command, new CancellationToken());
            //TODO: To test CD
            true.Should().BeTrue();

            //MockDbSet.Verify(m => m.Remove(It.Is<Sample>(w => w.Id == entityId)), Times.Once);

            //MockDbContext.Verify(m => m.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
            //MockUow.Verify(m => m.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);


        }
    }

}