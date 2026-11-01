namespace Rene.Utils.Db.Mapping
{
    using System.Linq.Expressions;

    internal sealed class DbUtilsMapperDefinitions
    {
        internal DbUtilsMapperDefinitions(
            IReadOnlyDictionary<(Type Source, Type Destination), Func<IServiceProvider, object, object>> maps,
            IReadOnlyDictionary<(Type Source, Type Destination), Action<IServiceProvider, object, object>> mapsInto,
            IReadOnlyDictionary<(Type Source, Type Destination), LambdaExpression> projections)
        {
            Maps = maps;
            MapsInto = mapsInto;
            Projections = projections;
        }

        internal IReadOnlyDictionary<(Type Source, Type Destination), Func<IServiceProvider, object, object>> Maps { get; }

        internal IReadOnlyDictionary<(Type Source, Type Destination), Action<IServiceProvider, object, object>> MapsInto { get; }

        internal IReadOnlyDictionary<(Type Source, Type Destination), LambdaExpression> Projections { get; }
    }

    /// <summary>
    /// <see cref="IDbUtilsMapper"/> implementation backed by the manual mappings defined with <see cref="DbUtilsMapperBuilder"/>.
    /// </summary>
    internal sealed class DelegateDbUtilsMapper : IDbUtilsMapper
    {
        private readonly DbUtilsMapperDefinitions _definitions;
        private readonly IServiceProvider _serviceProvider;

        public DelegateDbUtilsMapper(DbUtilsMapperDefinitions definitions, IServiceProvider serviceProvider)
        {
            _definitions = definitions ?? throw new ArgumentNullException(nameof(definitions));
            _serviceProvider = serviceProvider;
        }

        public TDestination Map<TDestination>(object source)
        {
            if (source == null) return default;

            var map = Find(_definitions.Maps, source.GetType(), typeof(TDestination))
                      ?? throw MappingNotFound("Map", source.GetType(), typeof(TDestination));

            return (TDestination)map(_serviceProvider, source);
        }

        public TDestination Map<TSource, TDestination>(TSource source, TDestination destination)
        {
            if (source == null) return destination;

            var sourceType = source.GetType();
            var destinationType = destination?.GetType() ?? typeof(TDestination);

            var mapInto = Find(_definitions.MapsInto, sourceType, destinationType)
                          ?? throw MappingNotFound("MapInto", sourceType, destinationType);

            mapInto(_serviceProvider, source, destination);
            return destination;
        }

        public IQueryable<TDestination> ProjectTo<TDestination>(IQueryable source)
        {
            _ = source ?? throw new ArgumentNullException(nameof(source));

            if (!_definitions.Projections.TryGetValue((source.ElementType, typeof(TDestination)), out var projection))
                throw MappingNotFound("Map (expression)", source.ElementType, typeof(TDestination));

            var call = Expression.Call(
                typeof(Queryable),
                nameof(Queryable.Select),
                new[] { source.ElementType, typeof(TDestination) },
                source.Expression,
                Expression.Quote(projection));

            return source.Provider.CreateQuery<TDestination>(call);
        }

        public bool CanProject(Type sourceType, Type destinationType)
            => _definitions.Projections.ContainsKey((sourceType, destinationType));

        private static T Find<T>(IReadOnlyDictionary<(Type Source, Type Destination), T> definitions, Type sourceType, Type destinationType)
            where T : class
        {
            // Walk the hierarchy so EF Core proxies (derived types) use the mapping of their entity
            for (var type = sourceType; type != null; type = type.BaseType)
            {
                if (definitions.TryGetValue((type, destinationType), out var value)) return value;
            }

            return null;
        }

        private static InvalidOperationException MappingNotFound(string method, Type sourceType, Type destinationType)
            => new($"No mapping registered from '{sourceType.FullName}' to '{destinationType.FullName}'. " +
                   $"Register it with DbUtilsMapperBuilder.{method}<{sourceType.Name}, {destinationType.Name}>(...).");
    }
}
