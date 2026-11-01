using System;
using System.Linq;

namespace Rene.Utils.Db
{
    /// <summary>
    /// Mapping abstraction used by the generic command handlers.
    /// Implement it to plug any mapping library (AutoMapper, Mapster, manual mappings...).
    /// </summary>
    public interface IDbUtilsMapper
    {
        /// <summary>
        /// Maps <paramref name="source"/> to a new instance of <typeparamref name="TDestination"/>.
        /// </summary>
        TDestination Map<TDestination>(object source);

        /// <summary>
        /// Maps <paramref name="source"/> onto an existing <paramref name="destination"/> instance.
        /// </summary>
        TDestination Map<TSource, TDestination>(TSource source, TDestination destination);

        /// <summary>
        /// Projects a query to <typeparamref name="TDestination"/> so the projection can be translated to the database.
        /// </summary>
        IQueryable<TDestination> ProjectTo<TDestination>(IQueryable source);

        /// <summary>
        /// Indicates whether <see cref="ProjectTo{TDestination}"/> is supported for the given types.
        /// When <c>false</c>, the entities are materialized first and mapped in memory with <see cref="Map{TDestination}(object)"/>.
        /// </summary>
        bool CanProject(Type sourceType, Type destinationType) => true;
    }
}
