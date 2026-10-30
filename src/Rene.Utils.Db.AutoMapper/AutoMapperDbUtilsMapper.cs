namespace Rene.Utils.Db.Mappers
{
    using global::AutoMapper;

    /// <summary>
    /// <see cref="IDbUtilsMapper"/> adapter for AutoMapper.
    /// </summary>
    public sealed class AutoMapperDbUtilsMapper : IDbUtilsMapper
    {
        private readonly IMapper _mapper;

        public AutoMapperDbUtilsMapper(IMapper mapper)
        {
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        }

        public TDestination Map<TDestination>(object source) => _mapper.Map<TDestination>(source);

        public TDestination Map<TSource, TDestination>(TSource source, TDestination destination) => _mapper.Map(source, destination);

        public IQueryable<TDestination> ProjectTo<TDestination>(IQueryable source) => _mapper.ProjectTo<TDestination>(source);
    }
}
