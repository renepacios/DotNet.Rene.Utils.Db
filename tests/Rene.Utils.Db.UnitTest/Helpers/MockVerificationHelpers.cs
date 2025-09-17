namespace Rene.Utils.Db.UnitTest.Helpers
{
    using AutoMapper;
    using Microsoft.EntityFrameworkCore;
    using Models;
    using System.Linq.Expressions;

    internal static class MockVerificationHelpers
    {
        /// <summary>
        /// Verifies that .AddAsync(...) was called exactly once on a DbSet,
        /// with an entity matching the provided ViewModel's properties.
        /// </summary>
        internal static void VerifyAddAsyncCalledWith(this Mock<DbSet<Sample>> dbSetMock, SampleDetailsViewModel vm, Times? times = null)
        {
            dbSetMock.Verify(
                m => m.AddAsync(
                    It.Is<Sample>(s =>
                        s.Id == vm.Id &&
                        s.Name == vm.Name &&
                        s.Description == vm.Description
                    ),
                    It.IsAny<CancellationToken>()
                ),
                times ?? Times.Once()
            );
        }

        /// <summary>
        /// Verifies that .Update(...) was called exactly once on a DbSet,
        /// with an entity matching the provided ViewModel's properties.
        /// </summary>
        internal static void VerifyUpdateCalledWithId(this Mock<DbSet<Sample>> dbSetMock, SampleDetailsViewModel vm, int id, Times? times = null)
        {
            dbSetMock.Verify(
                m => m.Update(
                    It.Is<Sample>(s =>
                    s.Id == id &&
                    s.Name == vm.Name &&
                    s.Description == vm.Description)
                ),
                times ?? Times.Once()
            );
        }

        /// <summary>
        /// Verifies that .Remove was called once on the DbSet,
        /// </summary>
        /// <param name="dbSetMock"></param>
        /// <param name="id"></param>
        internal static void VerifyRemoveCalledWithId(this Mock<DbSet<Sample>> dbSetMock, int id, Times? times = null)
        {
            dbSetMock.Verify(m => m.Remove(It.Is<Sample>(s => s.Id == id)), times ?? Times.Once());
        }

        internal static void VerifyMappedToEntity(this Mock<IMapper> mapperMock, SampleDetailsViewModel vm, Times? times = null)
        {
            mapperMock.Verify(m =>
                    m.Map<Sample>(It.Is<SampleDetailsViewModel>(x => x == vm)),
                times ?? Times.Once());
        }

        internal static void VerifyMappedToEntity(this Mock<IMapper> mapperMock, SampleDetailsViewModel vm, int id, Times? times = null)
        {
            mapperMock.Verify(m =>
                m.Map<SampleViewModel, Sample>(
                    It.Is<SampleViewModel>(s => s.Equals(vm)),
                    It.Is<Sample>(s => s.Id == id)),
                times ?? Times.Once()
            );
        }

        /// <summary>
        /// Verifies that .Map<SampleDetailsViewModel> was called once
        /// with a Sample whose properties match the ViewModel.
        /// </summary>
        internal static void VerifyMappedBackToViewModel(this Mock<IMapper> mapperMock, SampleDetailsViewModel vm, Times? times = null)
        {
            mapperMock.Verify(
                m => m.Map<SampleDetailsViewModel>(
                    It.Is<Sample>(s =>
                        s.Id == vm.Id &&
                        s.Name == vm.Name &&
                        s.Description == vm.Description
                    )
                ),
                times ?? Times.Once()
            );
        }

        /// <summary>
        /// Verifies that .ProjectTo<SampleDetailsViewModel> was called once.
        /// </summary>
        internal static void VerifyProjectedToViewModel(this Mock<IMapper> mapperMock, IEnumerable<SampleDetailsViewModel> data, Times? times = null)
        {
            mapperMock.Verify(
                m => m.ProjectTo<SampleDetailsViewModel>(
                    It.Is<IQueryable<Sample>>(source =>
                        data.Any(e =>
                            source.Any(x => x.Id == e.Id &&
                                x.Name == e.Name &&
                                x.Description == e.Description
                            )
                        )
                    ),
                    It.IsAny<object?>(),
                    It.IsAny<Expression<Func<SampleDetailsViewModel, object>>[]>()
                ),
                times ?? Times.Once()
            );
        }

        /// <summary>
        /// Verifies that SaveChangesAsync was called once on the DbContext.
        /// </summary>
        /// <param name="dbContextMock"></param>
        internal static void VerifySaveChangesAsyncCalled(this Mock<DbContext> dbContextMock, Times? times = null)
        {
            dbContextMock.Verify(db => db.SaveChangesAsync(It.IsAny<CancellationToken>()), times ?? Times.Once());
        }

        /// <summary>
        /// Verifies that SaveChangesAsync was called once on the custom Unit of Work.
        /// </summary>
        /// <param name="uowMock"></param>
        internal static void VerifySaveChangesAsyncCalled(this Mock<IDbUtilsUnitOfWork> uowMock, Times? times = null)
        {
            uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), times ?? Times.Once());
        }
    }
}
