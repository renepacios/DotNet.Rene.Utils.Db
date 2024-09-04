namespace Rene.Utils.Db.UnitTest
{
    using AutoMapper;
    using Builder;
    using Commands;
    using DbInternal;
    using FluentAssertions;
    using Microsoft.EntityFrameworkCore;
    using Models;
    using Moq;

    public class GenericAddTest
    {
        private Mock<DbContext> MockDbContext { get; }
        private Mock<IDbUtilsUnitOfWork> MockUow { get; }
        private Mock<IMapper> MockMapper { get; }
        private Mock<DbSet<Sample>> MockDbSet { get; }

        public GenericAddTest()
        {
            MockDbSet = DbContextMockUtils.GetMockDbSet(SampleBuilder.CreateList(10));
            //MockDbSet.Setup(m => m.AddAsync(It.IsAny<Sample>(), It.IsAny<CancellationToken>()))
            //    .ReturnsAsync((Sample s, CancellationToken c) =>
            //    {
            //        s.Id = 99;
            //        s.Name = "NoBodySample";
            //        s.Description = "This is a fake description";

            //       return new EntityEntry<Sample>(new InternalEntityEntry(new StateManager(new StateManagerDependencies())))

            //        //return new EntityEntry<Sample>(new InternalEntityEntry(null!, null!, s));
            //    });
            MockDbContext = new Mock<DbContext>();
            MockDbContext.Setup(m => m.Set<Sample>())
                .Returns(MockDbSet.Object);

            MockUow = new Mock<IDbUtilsUnitOfWork>();
            MockUow.Setup(s=> s.SaveChanges())
                .Callback(()=>MockDbContext.Object.SaveChanges());

            MockUow.Setup(s=> s.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .Callback(()=>MockDbContext.Object.SaveChangesAsync(It.IsAny<CancellationToken>()));

            MockUow.Setup(s=> s.SaveChangesAsync(It.IsAny<bool>(), It.IsAny<CancellationToken>()))
                .Callback<bool, CancellationToken>((b,c)=>MockDbContext.Object.SaveChangesAsync(b,c));
            
            

            MockMapper = new Mock<IMapper>();
            MockMapper
                .Setup(m => m.Map<SampleDetailsViewModel, Sample>(It.IsAny<SampleDetailsViewModel>()))
                .Returns((SampleDetailsViewModel s) => Sample.Create(s));
            MockMapper.Setup(m => m.Map<Sample>(It.IsAny<SampleDetailsViewModel>()))
                .Returns((SampleDetailsViewModel s) => Sample.Create(s));


            MockMapper
                .Setup(m => m.Map<Sample, SampleDetailsViewModel>(It.IsAny<Sample>()))
                .Returns((Sample s) => SampleDetailsViewModel.Create(s));
            MockMapper
                .Setup(m => m.Map<SampleDetailsViewModel>(It.IsAny<Sample>()))
                .Returns((Sample s) => SampleDetailsViewModel.Create(s));


        }

        [Fact]
        public async Task add_command_work_as_expected()
        {
            var vm = SampleDetailsViewModel.DefaultSampleDetails;
            var addCommand = new AddCommand<SampleDetailsViewModel>(vm);
            var handler = new GenericCommandHandler<SampleDetailsViewModel, Sample, DbContext, IDbUtilsUnitOfWork>(MockMapper.Object, MockDbContext.Object, MockUow.Object);

            var result = await handler.Handle(addCommand, new CancellationToken());

            MockMapper.Verify(m => m.Map<Sample>(It.Is<SampleDetailsViewModel>(x => x == vm)), Times.Once);

            MockDbSet
                .Verify(m => m.AddAsync(It.Is<Sample>(s => s.Id == vm.Id && s.Name == vm.Name && s.Description == vm.Description), It.IsAny<CancellationToken>()), Times.Once);
            MockDbContext.Verify(m => m.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

            MockMapper.Verify(m => m.Map<SampleDetailsViewModel>(It.Is<Sample>(s => s.Id == vm.Id && s.Name == vm.Name && s.Description == vm.Description)), Times.Once);

            result.Should().BeOfType<SampleDetailsViewModel>();

            result.Id.Should().Be(99);
            result.Name.Should().Be("NoBodySample");
            result.Description.Should().Be("This is a fake description");
            }

    }

    public class SampleBuilder
    {
        public static List<Sample> CreateList(int i)
        {
            //implement this method
            var list = new List<Sample>();
            for (var j = 0; j < i; j++)
            {
                list.Add(Sample.Create(j, $"Name_{j}", $"Description_{j}"));
            }
            return list;
        }
    }
}