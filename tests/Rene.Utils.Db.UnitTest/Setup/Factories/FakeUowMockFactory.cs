namespace Rene.Utils.Db.UnitTest.Setup.Factories
{
    using DbInternal;
    using Microsoft.EntityFrameworkCore;
    using Models;
    using System;

    internal static class FakeUowMockFactory
    {
        internal static Mock<FakeUnitOfWork<DbContext>> Create(Mock<DbContext> dbContextMock)
        {
            // 2) We need a 'FakeUnitOfWork<DbContext>' mock.
            //    We'll do that here or in a small helper method.
            var mockFakeUow = new Mock<FakeUnitOfWork<DbContext>>(MockBehavior.Strict, dbContextMock.Object);

            // Delegate SaveChanges calls
            mockFakeUow.Setup(s => s.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .Callback(() => dbContextMock.Object.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);
            mockFakeUow.Setup(s => s.SaveChanges())
                .Callback(() => dbContextMock.Object.SaveChanges())
                .Returns(1);

            // If your code uses this method:
            mockFakeUow.Setup(s => s.GetKeyNameFromEntityType<Sample>())
                .Returns(nameof(Sample.Id));
            mockFakeUow.Setup(s => s.GetKeyNameFromEntityType(It.IsAny<Type>()))
                .Returns(nameof(Sample.Id));

            return mockFakeUow;
        }
    }
}
