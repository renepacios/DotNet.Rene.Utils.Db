namespace Rene.Utils.Db.UnitTest
{
    using Microsoft.EntityFrameworkCore;
    using Moq;

    public static class DbContextMockUtils
    {

        public static Mock<DbSet<T>> GetMockDbSet<T>(List<T> data) where T : class
        {
            var queryable = data.AsQueryable();
            var dbSet = new Mock<DbSet<T>>();
            dbSet.As<IQueryable<T>>().Setup(m => m.Provider).Returns(queryable.Provider);
            dbSet.As<IQueryable<T>>().Setup(m => m.Expression).Returns(queryable.Expression);
            dbSet.As<IQueryable<T>>().Setup(m => m.ElementType).Returns(queryable.ElementType);
            dbSet.As<IQueryable<T>>().Setup(m => m.GetEnumerator()).Returns(() => queryable.GetEnumerator());
            //dbSet.Setup(d => d.Add(It.IsAny<T>())).Callback<T>(data.Add);
            //dbSet.Setup(d => d.AddAsync(It.IsAny<T>(), It.IsAny<CancellationToken>())).Callback<T>(data.Add);
            return dbSet;
        }
    }
}
