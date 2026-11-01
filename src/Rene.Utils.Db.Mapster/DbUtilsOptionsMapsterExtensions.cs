// ReSharper disable once CheckNamespace

namespace Microsoft.Extensions.DependencyInjection
{
    using global::Mapster;
    using Rene.Utils.Db.Mappers;

    public static class DbUtilsOptionsMapsterExtensions
    {
        /// <summary>
        /// Uses Mapster as mapper. The <see cref="TypeAdapterConfig"/> is resolved from the dependency injection container
        /// and, if it isn't registered, <see cref="TypeAdapterConfig.GlobalSettings"/> is used.
        /// </summary>
        public static DbUtilsOptions UseMapster(this DbUtilsOptions options)
            => options.UseMapster(sp => sp.GetService<TypeAdapterConfig>() ?? TypeAdapterConfig.GlobalSettings);

        /// <summary>
        /// Uses Mapster as mapper with the given <paramref name="config"/>.
        /// </summary>
        public static DbUtilsOptions UseMapster(this DbUtilsOptions options, TypeAdapterConfig config)
        {
            _ = config ?? throw new ArgumentNullException(nameof(config));
            return options.UseMapster(_ => config);
        }

        /// <summary>
        /// Uses Mapster as mapper with the <see cref="TypeAdapterConfig"/> created by <paramref name="configFactory"/>.
        /// </summary>
        public static DbUtilsOptions UseMapster(this DbUtilsOptions options, Func<IServiceProvider, TypeAdapterConfig> configFactory)
        {
            _ = options ?? throw new ArgumentNullException(nameof(options));
            _ = configFactory ?? throw new ArgumentNullException(nameof(configFactory));

            return options.UseMapper(sp => new MapsterDbUtilsMapper(configFactory(sp)));
        }
    }
}
