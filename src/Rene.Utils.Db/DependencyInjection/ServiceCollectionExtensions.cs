// ReSharper disable once CheckNamespace

using Microsoft.Extensions.Hosting;

namespace Microsoft.Extensions.DependencyInjection
{
    using EntityFrameworkCore;
    using Extensions;
    using Rene.Utils.Db;
    using Rene.Utils.Db.Builder;
    using Rene.Utils.Db.DbInternal;
    using System.Reflection;

    public static class ServiceCollectionExtensions
    {
        public static IHostApplicationBuilder AddDbUtils<TDbContext>(this IHostApplicationBuilder builder,
            Action<DbUtilsOptions> setup, params Assembly[] assemblies)
            where TDbContext : DbContext
        {
            _ = builder ?? throw new ArgumentNullException(nameof(builder));

            builder.Services.AddDbUtils<TDbContext>(setup, assemblies);
            return builder;
        }

        public static IHostApplicationBuilder AddDbUtils<TDbContext, TUnitOfWork>(this IHostApplicationBuilder builder,
            Action<DbUtilsOptions> setup, params Assembly[] assemblies)
            where TDbContext : DbContext
            where TUnitOfWork : IDbUtilsUnitOfWork
        {
            _ = builder ?? throw new ArgumentNullException(nameof(builder));

            builder.Services.AddDbUtils<TDbContext, TUnitOfWork>(setup, assemblies);
            return builder;
        }

        public static IHostApplicationBuilder AddDbUtils<TDbContext>(this IHostApplicationBuilder builder, params Assembly[] assemblies)
            where TDbContext : DbContext
        {
            _ = builder ?? throw new ArgumentNullException(nameof(builder));

            builder.Services.AddDbUtils<TDbContext>(assemblies);
            return builder;
        }

        public static IHostApplicationBuilder AddDbUtils<TDbContext, TUnitOfWork>(this IHostApplicationBuilder builder, params Assembly[] assemblies)
            where TDbContext : DbContext
            where TUnitOfWork : IDbUtilsUnitOfWork
        {
            _ = builder ?? throw new ArgumentNullException(nameof(builder));

            builder.Services.AddDbUtils<TDbContext, TUnitOfWork>(assemblies);
            return builder;
        }

        public static IServiceCollection AddDbUtils<TDbContext>(this IServiceCollection services,
            Action<DbUtilsOptions> setup, params Assembly[] assemblies)
            where TDbContext : DbContext
        {
            _ = services ?? throw new ArgumentNullException(nameof(services));
            _ = setup ?? throw new ArgumentNullException(nameof(setup));

            var options = new DbUtilsOptions();
            setup(options);
            options.DbContextType = typeof(TDbContext);

            return services.AddDbUtils<TDbContext>(options, assemblies);
        }

        public static IServiceCollection AddDbUtils<TDbContext, TUnitOfWork>(this IServiceCollection services,
            Action<DbUtilsOptions> setup, params Assembly[] assemblies)
            where TDbContext : DbContext
            where TUnitOfWork : IDbUtilsUnitOfWork
        {
            _ = services ?? throw new ArgumentNullException(nameof(services));
            _ = setup ?? throw new ArgumentNullException(nameof(setup));

            var options = new DbUtilsOptions();
            setup(options);
            options.DbContextType = typeof(TDbContext);
            options.UnitOfWorkType = typeof(TUnitOfWork);

            return services.AddDbUtils<TDbContext>(options, assemblies);
        }

        public static IServiceCollection AddDbUtils<TDbContext>(this IServiceCollection services, params Assembly[] assemblies)
            where TDbContext : DbContext
        {
            _ = services ?? throw new ArgumentNullException(nameof(services));

            var options = new DbUtilsOptions { DbContextType = typeof(TDbContext) };

            return services.AddDbUtils<TDbContext>(options, assemblies);
        }

        public static IServiceCollection AddDbUtils<TDbContext, TUnitOfWork>(this IServiceCollection services, params Assembly[] assemblies)
            where TDbContext : DbContext
            where TUnitOfWork : IDbUtilsUnitOfWork
        {
            _ = services ?? throw new ArgumentNullException(nameof(services));

            var options = new DbUtilsOptions
            {
                DbContextType = typeof(TDbContext),
                UnitOfWorkType = typeof(TUnitOfWork)
            };

            return services.AddDbUtils<TDbContext>(options, assemblies);
        }

        private static IServiceCollection AddDbUtils<TDbContext>(this IServiceCollection services,
            DbUtilsOptions options, Assembly[] assemblies)
            where TDbContext : DbContext
        {
            // Only when no unit of work has been configured (UseOwnUnitOfWork or TUnitOfWork), so it is never overwritten.
            if (options.UnitOfWorkType == null) services.RegisterFallBackUow<TDbContext>(options);

            services.RegisterMapper(options);

            if (assemblies == null || assemblies.Length <= 0) return services.AddMediatRGenericHandlers(options);

            foreach (var assembly in assemblies) services.AddMediatRGenericHandlers(options, assembly);

            return services;
        }

        /// <summary>
        /// Registers a fake unit of work that uses the DbContext directly, for when no unit of work has been configured.
        /// </summary>
        private static void RegisterFallBackUow<TDbContext>(this IServiceCollection services, DbUtilsOptions options)
            where TDbContext : DbContext
        {
            options.UnitOfWorkType = typeof(FakeUnitOfWork<TDbContext>);
            services.TryAddScoped<FakeUnitOfWork<TDbContext>>();
            services.TryAddScoped<IDbUtilsUnitOfWork, FakeUnitOfWork<TDbContext>>();
        }

        /// <summary>
        /// Registers the mapper configured in <see cref="DbUtilsOptions"/>.
        /// When no mapper is configured, an <see cref="IDbUtilsMapper"/> must be registered manually in the container.
        /// </summary>
        private static void RegisterMapper(this IServiceCollection services, DbUtilsOptions options)
        {
            if (options.MapperFactory == null) return;

            services.Replace(ServiceDescriptor.Scoped(options.MapperFactory));
        }
    }
}