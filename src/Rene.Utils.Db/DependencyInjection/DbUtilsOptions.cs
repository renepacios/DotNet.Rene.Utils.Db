// ReSharper disable once CheckNamespace

using Rene.Utils.Db;
using Rene.Utils.Db.Mapping;
using Rene.Utils.Db.Logging;

namespace Microsoft.Extensions.DependencyInjection
{
   
    using System;

    public class DbUtilsOptions
    {
        internal Type UnitOfWorkType { get; set; }

        internal Type DbContextType { get; set; }

        internal CommandLoggingOptions CommandLogging { get; private set; }

        /// <summary>
        /// Enables application-owned callbacks around MediatR request execution.
        /// Each callback receives the service provider of the executing scope.
        /// </summary>
        public DbUtilsOptions UseCommandLogging(Action<CommandLoggingOptions> configure)
        {
            if (configure == null) throw new ArgumentNullException(nameof(configure));
            var logging = new CommandLoggingOptions();
            configure(logging);
            if (logging.Before == null && logging.Completed == null)
                throw new ArgumentException("Configure at least one command logging callback.", nameof(configure));
            CommandLogging = new CommandLoggingOptions { Before = logging.Before, Completed = logging.Completed };
            return this;
        }

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
