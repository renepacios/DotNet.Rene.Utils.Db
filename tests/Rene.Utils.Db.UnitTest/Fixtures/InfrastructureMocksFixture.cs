using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Rene.Utils.Db.UnitTest.Models;

namespace Rene.Utils.Db.UnitTest.Fixtures
{
    public class InfrastructureMocksFixture : IDisposable
    {

        public  Mock<DbContext> MockDbContext { get; }
        public  Mock<IDbUtilsUnitOfWork> MockUow { get; }
        public  Mock<IMapper> MockMapper { get; }
        public  Mock<DbSet<Sample>> MockDbSet { get; }


        public InfrastructureMocksFixture()
        {
            MockDbSet = DbContextMockUtils.GetMockDbSet(SampleBuilder.CreateList(10));

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
            MockMapper.Setup(m => m.Map<Sample>(It.IsAny<SampleDetailsViewModel>()))
                .Returns((SampleDetailsViewModel s) => Sample.Create(s));
            
            MockMapper
                .Setup(m => m.Map<Sample, SampleDetailsViewModel>(It.IsAny<Sample>()))
                .Returns((Sample s) => SampleDetailsViewModel.Create(s));

            MockMapper
                .Setup(m => m.Map<SampleDetailsViewModel>(It.IsAny<Sample>()))
                .Returns((Sample s) => SampleDetailsViewModel.Create(s));

        }

        public void Dispose()
        {
            // TODO release managed resources here
        }
    }
}
