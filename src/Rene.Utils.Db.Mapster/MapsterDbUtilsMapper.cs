namespace Rene.Utils.Db.Mappers
{
    using global::Mapster;

    /// <summary>
    /// <see cref="IDbUtilsMapper"/> adapter for Mapster.
    /// </summary>
    public sealed class MapsterDbUtilsMapper : IDbUtilsMapper
    {
        private readonly TypeAdapterConfig _config;

        public MapsterDbUtilsMapper(TypeAdapterConfig config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
        }

        public TDestination Map<TDestination>(object source) => source == null ? default : source.Adapt<TDestination>(_config);

        public TDestination Map<TSource, TDestination>(TSource source, TDestination destination) => source.Adapt(destination, _config);

        public IQueryable<TDestination> ProjectTo<TDestination>(IQueryable source) => source.ProjectToType<TDestination>(_config);
    }
}
