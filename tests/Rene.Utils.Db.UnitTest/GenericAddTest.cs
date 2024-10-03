namespace Rene.Utils.Db.UnitTest
{
    using AutoMapper;
    using Builder;
    using Commands;
    using FluentAssertions;
    using Microsoft.EntityFrameworkCore;
    using Mocks;
    using Models;
    using Moq;

    public class GenericAddTest //: IClassFixture<InfrastructureMocksFixture>

    {
        private Mock<DbContext> MockDbContext { get; }
        private Mock<IDbUtilsUnitOfWork> MockUow { get; }
        private Mock<IMapper> MockMapper { get; }
        private Mock<DbSet<Sample>> MockDbSet { get; }

        public GenericAddTest()
        {
            var mocks = MockBuilders.Create();

            MockDbContext = mocks.MockDbContext;
            MockUow = mocks.MockUow;
            MockMapper = mocks.MockMapper;
            MockDbSet = mocks.MockDbSet;


        }

        [Fact]
        public async Task add_command_work_as_expected()
        {
            var vm = SampleDetailsViewModel.DefaultSampleDetails;
            var addCommand = new AddCommand<SampleDetailsViewModel>(vm);
            var handler =
                new GenericCommandHandler<SampleDetailsViewModel, Sample, DbContext, IDbUtilsUnitOfWork>(MockMapper.Object,
                    MockDbContext.Object, MockUow.Object);

            var result = await handler.Handle(addCommand, new CancellationToken());

            MockMapper.Verify(m => m.Map<Sample>(It.Is<SampleDetailsViewModel>(x => x == vm)), Times.Once);

            MockDbSet
                .Verify(
                    m => m.AddAsync(
                        It.Is<Sample>(s => s.Id == vm.Id && s.Name == vm.Name && s.Description == vm.Description),
                        It.IsAny<CancellationToken>()), Times.Once);

            MockDbContext.Verify(m => m.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
            MockUow.Verify(m => m.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

            MockMapper.Verify(
                m => m.Map<SampleDetailsViewModel>(It.Is<Sample>(s =>
                    s.Id == vm.Id && s.Name == vm.Name && s.Description == vm.Description)), Times.Once);

            result.Should().BeOfType<SampleDetailsViewModel>();

            result.Id.Should().Be(99);
            result.Name.Should().Be("NoBodySample");
            result.Description.Should().Be("This is a fake description");
        }

        [Fact]
        public async Task add_command_withoutUOW_work_as_expected()
        {
            var vm = SampleDetailsViewModel.DefaultSampleDetails;
            var addCommand = new AddCommand<SampleDetailsViewModel>(vm);
            var handler =
                new GenericCommandHandler<SampleDetailsViewModel, Sample, DbContext, IDbUtilsUnitOfWork>(MockMapper.Object,
                    MockDbContext.Object, null);

            var result = await handler.Handle(addCommand, new CancellationToken());

            MockMapper.Verify(m => m.Map<Sample>(It.Is<SampleDetailsViewModel>(x => x == vm)), Times.Once);

            MockDbSet
                .Verify(
                    m => m.AddAsync(
                        It.Is<Sample>(s => s.Id == vm.Id && s.Name == vm.Name && s.Description == vm.Description),
                        It.IsAny<CancellationToken>()), Times.Once);

            MockDbContext.Verify(m => m.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

            MockMapper.Verify(
                m => m.Map<SampleDetailsViewModel>(It.Is<Sample>(s =>
                    s.Id == vm.Id && s.Name == vm.Name && s.Description == vm.Description)), Times.Once);

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