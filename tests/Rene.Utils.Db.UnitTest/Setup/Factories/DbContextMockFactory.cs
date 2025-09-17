namespace Rene.Utils.Db.UnitTest.Setup.Factories
{
    using Microsoft.EntityFrameworkCore;

    internal static class DbContextMockFactory
    {
        /// <summary>
        /// Creates a Mock of DbContext, sets up <c>.Set&lt;T&gt;()</c> to return the provided DbSet mock,
        /// and configures <c>SaveChanges</c>/<c>SaveChangesAsync</c> to return <c>1</c>.
        /// </summary>
        /// <typeparam name="T">The entity type for <c>.Set&lt;T&gt;()</c></typeparam>
        /// <param name="dbSetMock">The mocked DbSet to return</param>
        /// <returns>The configured Mock of DbContext</returns>
        public static Mock<DbContext> Create<T>(Mock<DbSet<T>> dbSetMock) where T : class
        {
            var dbContextMock = new Mock<DbContext>();

            // Hook up .Set<T>() to return our mocked DbSet
            dbContextMock
                .Setup(ctx => ctx.Set<T>())
                .Returns(dbSetMock.Object);

            // Make SaveChanges and SaveChangesAsync always return 1
            dbContextMock
                .Setup(ctx => ctx.SaveChanges())
                .Returns(1);

            dbContextMock
                .Setup(ctx => ctx.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            return dbContextMock;
        }
    }

}
