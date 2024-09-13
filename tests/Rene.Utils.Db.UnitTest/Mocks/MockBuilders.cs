namespace Rene.Utils.Db.UnitTest.Mocks
{
    using AutoMapper;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore.Metadata;
    using Models;



    public class MockBuilders
    {
        public Mock<DbContext> MockDbContext { get; private set; }
        internal Mock<IDbUtilsUnitOfWork> MockUow { get; private set; }
        public Mock<IMapper> MockMapper { get; private set; }
        public Mock<DbSet<Sample>> MockDbSet { get; private set; }

        private MockBuilders()
        {
            Rebuild();
        }

        public static MockBuilders Create()
        {
            return new MockBuilders();
        }

        void Rebuild()
        {
            MockDbSet = DbMocks.GetMockDbSet(SampleBuilder.CreateList(10));

            MockDbContext = new Mock<DbContext>();
            MockDbContext.Setup(m => m.Set<Sample>())
                .Returns(MockDbSet.Object);
            
         
            MockUow = new Mock<IDbUtilsUnitOfWork>();
            MockUow.Setup(s => s.SaveChanges())
                .Callback(() => MockDbContext.Object.SaveChanges());
            MockUow.Setup(s => s.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .Callback(() => MockDbContext.Object.SaveChangesAsync(It.IsAny<CancellationToken>()));
            MockUow.Setup(s => s.SaveChangesAsync(It.IsAny<bool>(), It.IsAny<CancellationToken>()))
                .Callback<bool, CancellationToken>((b, c) => MockDbContext.Object.SaveChangesAsync(b, c));

            MockMapper = new Mock<IMapper>();
            MockMapper
                .Setup(m => m.Map<SampleDetailsViewModel, Sample>(It.IsAny<SampleDetailsViewModel>()))
                .Returns((SampleDetailsViewModel s) => Sample.Create(s));

            MockMapper.Setup(m => m.Map<SampleDetailsViewModel,Sample>(It.IsAny<SampleDetailsViewModel>(),It.IsAny<Sample>()))
                .Callback((SampleDetailsViewModel s, Sample d) =>
                {
                    var a = Sample.Create(s);
                    d.Id = a.Id;
                    d.Description = a.Description;
                    d.Name = a.Name;
                });
                

            MockMapper.Setup(m => m.Map<Sample>(It.IsAny<SampleDetailsViewModel>()))
                .Returns((SampleDetailsViewModel s) => Sample.Create(s));

            MockMapper
                .Setup(m => m.Map<Sample, SampleDetailsViewModel>(It.IsAny<Sample>()))
                .Returns((Sample s) => SampleDetailsViewModel.Create(s));

            MockMapper
                .Setup(m => m.Map<SampleDetailsViewModel>(It.IsAny<Sample>()))
                .Returns((Sample s) => SampleDetailsViewModel.Create(s));

        }

    }
}
