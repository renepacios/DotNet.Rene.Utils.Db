// ReSharper disable once CheckNamespace

using Rene.Utils.Db;
using Rene.Utils.Db.Mapping;

namespace Microsoft.Extensions.DependencyInjection
{
   
    using System;

    public class DbUtilsOptions
    {
        internal Type UnitOfWorkType { get; set; }

        internal Type DbContextType { get; set; }

        internal Func<IServiceProvider, IDbUtilsMapper> MapperFactory { get; private set; }

        public void UseOwnUnitOfWork<T>() where T : IDbUtilsUnitOfWork => UnitOfWorkType = typeof(T);

        /// <summary>
        /// Uses <typeparamref name="TMapper"/> as mapper. Its dependencies are resolved from the dependency injection container.
        /// </summary>
        public DbUtilsOptions UseMapper<TMapper>() where TMapper : class, IDbUtilsMapper
            => UseMapper(sp => ActivatorUtilities.GetServiceOrCreateInstance<TMapper>(sp));

        /// <summary>
        /// Uses the mapper created by <paramref name="factory"/>. It is invoked once per scope.
        /// Mapping library integrations (UseAutoMapper, UseMapster...) are built on top of this method.
        /// </summary>
        public DbUtilsOptions UseMapper(Func<IServiceProvider, IDbUtilsMapper> factory)
        {
            MapperFactory = factory ?? throw new ArgumentNullException(nameof(factory));
            return this;
        }

        /// <summary>
        /// Uses manual mappings defined with <see cref="DbUtilsMapperBuilder"/>, without any mapping library.
        /// </summary>
        public DbUtilsOptions UseMapper(Action<DbUtilsMapperBuilder> configure)
        {
            _ = configure ?? throw new ArgumentNullException(nameof(configure));

            var builder = new DbUtilsMapperBuilder();
            configure(builder);
            var definitions = builder.Build();

            return UseMapper(sp => new DelegateDbUtilsMapper(definitions, sp));
        }
    }
}
