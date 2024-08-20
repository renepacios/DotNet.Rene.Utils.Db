
// ReSharper disable once CheckNamespace



// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection
{

    using EntityFrameworkCore;
    using Rene.Utils.Db.DbInternal;
    using System;
    using System.Reflection;
    using Rene.Utils.Db.Builder;


    public static class ServiceCollectionExtensions
    {


        public static WebApplicationBuilder AddDbUtils<TDbContext>(this WebApplicationBuilder builder
            , Action<DbUtilsOptions> setup, params Assembly[] assemblies)
            where TDbContext : DbContext
        {
            _ = builder ?? throw new ArgumentNullException(nameof(builder));
            _ = setup ?? throw new ArgumentNullException(nameof(setup));



            var options = new DbUtilsOptions();

            setup?.Invoke(options);
            options.DbContextType = typeof(TDbContext);

            //TODO: Ver si se puede hacer algo con el UnitOfWork
            builder.RegisterFallBackUow<TDbContext>(options);

            return builder.AddDbUtils(options, assemblies);

        }

        public static WebApplicationBuilder AddDbUtils<TDbContext, TUnitOfWork>(this WebApplicationBuilder builder,
            Action<DbUtilsOptions> setup, params Assembly[] assemblies)
            where TDbContext : DbContext
        {

            _ = builder ?? throw new ArgumentNullException(nameof(builder));
            _ = setup ?? throw new ArgumentNullException(nameof(setup));

            var options = new DbUtilsOptions();
            setup?.Invoke(options);

            options.DbContextType = typeof(TDbContext);
            options.UnitOfWorkType = typeof(TUnitOfWork);


            return builder.AddDbUtils(options, assemblies);
        }



        public static WebApplicationBuilder AddDbUtils<TDbContext>(this WebApplicationBuilder builder, params Assembly[] assemblies)
            where TDbContext : DbContext

        {

            //_ = services ?? throw new ArgumentNullException(nameof(services));

            _ = builder ?? throw new ArgumentNullException(nameof(builder));

            var options = new DbUtilsOptions()
            {
                DbContextType = typeof(TDbContext)
            };


            //services.RegisterFallBackUOW(options);

            return builder.AddDbUtils(options, assemblies);


        }


        public static WebApplicationBuilder AddDbUtils<TDbContext, TUnitOfWork>(this WebApplicationBuilder builder, params Assembly[] assemblies)
            where TDbContext : DbContext
        {
            _ = builder ?? throw new ArgumentNullException(nameof(builder));
            //_ = services ?? throw new ArgumentNullException(nameof(services));
            var options = new DbUtilsOptions()
            {
                DbContextType = typeof(TDbContext),
                UnitOfWorkType = typeof(TUnitOfWork)
            };

            return builder.AddDbUtils(options, assemblies);

        }





        /// <summary>
        /// Registra un fake UOW para cuando generemos los genéricos decirle que no use esto y use el dbcontext directamente
        /// Es para evitar hacer 2 
        /// </summary>
        /// <param name="builder"></param>
        /// <returns></returns>
        private static WebApplicationBuilder RegisterFallBackUow<TDbContext>(this WebApplicationBuilder builder, DbUtilsOptions options)
            where TDbContext : DbContext
        {
            options.UnitOfWorkType = typeof(FakeUnitOfWork<TDbContext>);
            builder.Services.AddScoped<FakeUnitOfWork<TDbContext>>();
            //services.AddScoped<IDbUtilsUnitOfWork, FakeUnitOfWork>();
            return builder;
        }



        private static WebApplicationBuilder AddDbUtils(this WebApplicationBuilder builder,
                DbUtilsOptions options = null, Assembly[] assemblies = null)
        {

            // Type dbcontextType = typeof(TDbContext);
            var services = builder.Services;

            if (assemblies != null && assemblies.Length > 0)
            {
                foreach (Assembly assembly in assemblies)
                {
                    builder.AddMediatRGenericHandlers(options, assembly);
                }

                return builder;
            }

            return builder.AddMediatRGenericHandlers(options);

            return builder;
        }

    }
}
