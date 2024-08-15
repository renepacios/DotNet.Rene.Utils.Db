
using System.Threading;
using System.Threading.Tasks;

namespace Rene.Utils.Db
{
    public interface IDbUtilsUnitOfWork
    {
        int SaveChanges();
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default(CancellationToken));
        Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default(CancellationToken));
    }
}
