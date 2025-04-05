namespace Rene.Utils.Db.UnitTest.Setup.Factories
{
    using Microsoft.EntityFrameworkCore;

    internal static class UowMockFactory
    {
        /// <summary>
        /// Creates a Mock of <c>IDbUtilsUnitOfWork</c>, delegating SaveChanges* calls to the DbContext mock.
        /// </summary>
        /// <param name="dbContextMock">A mock of <c>DbContext</c> the UoW delegates to.</param>
        public static Mock<IDbUtilsUnitOfWork> Create(Mock<DbContext> dbContextMock)
        {
            var uowMock = new Mock<IDbUtilsUnitOfWork>();

            uowMock.Setup(u => u.SaveChanges())
                .Callback(() => dbContextMock.Object.SaveChanges())
                .Returns(1);

            uowMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .Callback(() => dbContextMock.Object.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            return uowMock;
        }
    }

}
