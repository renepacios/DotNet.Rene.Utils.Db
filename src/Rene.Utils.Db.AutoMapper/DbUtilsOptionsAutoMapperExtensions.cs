// ReSharper disable once CheckNamespace

namespace Microsoft.Extensions.DependencyInjection
{
    using global::AutoMapper;
    using Rene.Utils.Db.Mappers;

    public static class DbUtilsOptionsAutoMapperExtensions
    {
        /// <summary>
        /// Uses AutoMapper as mapper. The <see cref="IMapper"/> is resolved from the dependency injection container,
        /// so AutoMapper must be registered (e.g. <c>services.AddAutoMapper(...)</c>).
        /// </summary>
        public static DbUtilsOptions UseAutoMapper(this DbUtilsOptions options)
            => options.UseAutoMapper(sp => sp.GetRequiredService<IMapper>());

        /// <summary>
        /// Uses AutoMapper as mapper with the <see cref="IMapper"/> created by <paramref name="mapperFactory"/>.
        /// </summary>
        public static DbUtilsOptions UseAutoMapper(this DbUtilsOptions options, Func<IServiceProvider, IMapper> mapperFactory)
        {
            _ = options ?? throw new ArgumentNullException(nameof(options));
            _ = mapperFactory ?? throw new ArgumentNullException(nameof(mapperFactory));

            return options.UseMapper(sp => new AutoMapperDbUtilsMapper(mapperFactory(sp)));
        }
    }
}
