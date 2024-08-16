// ReSharper disable once CheckNamespace

using Rene.Utils.Db;

namespace Microsoft.Extensions.DependencyInjection
{
   
    using System;

    public class DbUtilsOptions
    {
        internal Type UnitOfWorkType { get; set; }

        internal Type DbContextType { get; set; }

        public void UseOwnUnitOfWork<T>() where T : IDbUtilsUnitOfWork => UnitOfWorkType = typeof(T);


    }
}
