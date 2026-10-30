namespace Rene.Utils.Db.Mapping
{
    using System.Linq.Expressions;

    /// <summary>
    /// Builder to define manual mappings without depending on any mapping library.
    /// </summary>
    /// <example>
    /// <code>
    /// options.UseMapper(m => m
    ///     .Map&lt;Product, ProductDto&gt;(p => new ProductDto { Id = p.Id, Name = p.Name })
    ///     .Map&lt;ProductDto, Product&gt;(dto => new Product { Name = dto.Name })
    ///     .MapInto&lt;ProductDto, Product&gt;((dto, entity) => entity.Name = dto.Name));
    /// </code>
    /// </example>
    public sealed class DbUtilsMapperBuilder
    {
        private readonly Dictionary<(Type Source, Type Destination), Func<IServiceProvider, object, object>> _maps = new();
        private readonly Dictionary<(Type Source, Type Destination), Action<IServiceProvider, object, object>> _mapsInto = new();
        private readonly Dictionary<(Type Source, Type Destination), LambdaExpression> _projections = new();

        /// <summary>
        /// Registers a mapping expression. It is used both for in-memory mapping and for database projections (<c>ProjectTo</c>),
        /// so it must be translatable by the EF Core provider.
        /// </summary>
        public DbUtilsMapperBuilder Map<TSource, TDestination>(Expression<Func<TSource, TDestination>> mapping)
        {
            _ = mapping ?? throw new ArgumentNullException(nameof(mapping));

            var key = (typeof(TSource), typeof(TDestination));
            var compiled = mapping.Compile();
            _maps[key] = (_, source) => compiled((TSource)source);
            _projections[key] = mapping;
            return this;
        }

        /// <summary>
        /// Registers an in-memory mapping function that can resolve services from the dependency injection container.
        /// Queries are materialized before being mapped unless a projection expression is also registered for the same types.
        /// </summary>
        public DbUtilsMapperBuilder Map<TSource, TDestination>(Func<IServiceProvider, TSource, TDestination> mapping)
        {
            _ = mapping ?? throw new ArgumentNullException(nameof(mapping));

            _maps[(typeof(TSource), typeof(TDestination))] = (sp, source) => mapping(sp, (TSource)source);
            return this;
        }

        /// <summary>
        /// Registers a mapping that updates an existing destination instance (used by the update command).
        /// </summary>
        public DbUtilsMapperBuilder MapInto<TSource, TDestination>(Action<TSource, TDestination> mapping)
        {
            _ = mapping ?? throw new ArgumentNullException(nameof(mapping));

            return MapInto<TSource, TDestination>((_, source, destination) => mapping(source, destination));
        }

        /// <summary>
        /// Registers a mapping that updates an existing destination instance and can resolve services from the dependency injection container.
        /// </summary>
        public DbUtilsMapperBuilder MapInto<TSource, TDestination>(Action<IServiceProvider, TSource, TDestination> mapping)
        {
            _ = mapping ?? throw new ArgumentNullException(nameof(mapping));

            _mapsInto[(typeof(TSource), typeof(TDestination))] = (sp, source, destination) => mapping(sp, (TSource)source, (TDestination)destination);
            return this;
        }

        internal DbUtilsMapperDefinitions Build() => new(_maps.ToDictionary(k => k.Key, v => v.Value),
            _mapsInto.ToDictionary(k => k.Key, v => v.Value),
            _projections.ToDictionary(k => k.Key, v => v.Value));
    }
}
