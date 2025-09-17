namespace Rene.Utils.Db.UnitTest.Models.AsyncEnumerable
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    /// <summary>
    /// Emulates an <see cref="IAsyncEnumerable{T}"/> for testing purposes.
    /// Needed to test the <see cref="AutoMapper"/> ProjectTo methods, as our implementation uses ToListAsync.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class TestAsyncEnumerable<T>(IEnumerable<T> enumerable) : EnumerableQuery<T>(enumerable), IAsyncEnumerable<T>, IQueryable<T>
    {
        // Implementation for IAsyncEnumerable<T>
        public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
            => new TestAsyncEnumerator<T>(this.AsEnumerable().GetEnumerator());
    }

    public class TestAsyncEnumerator<T>(IEnumerator<T> inner) : IAsyncEnumerator<T>
    {
        public ValueTask<bool> MoveNextAsync() => new ValueTask<bool>(inner.MoveNext());

        public T Current => inner.Current;

        public ValueTask DisposeAsync()
        {
            inner.Dispose();
            return default;
        }
    }
}
