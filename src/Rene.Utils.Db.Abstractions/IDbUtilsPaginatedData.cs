

using System.Collections.Generic;

namespace Rene.Utils.Db
{
    public interface IDbUtilsPaginatedDataWithOutCount<TEntity>
    {
        public int Size { get; }
        public int Number { get; }
        public int NumberOfElements { get; }
        public bool Empty { get; }
        public IList<TEntity> Content { get; }
    }

    public interface IDbUtilsPaginatedData<TEntity> : IDbUtilsPaginatedDataWithOutCount<TEntity>
    {
        public int TotalPages { get; }
        public int TotalElements { get; }
        public bool First { get; }
        public bool Last { get; }


    }
}
