using Rene.Utils.Db.CompositeSpecifications.Engine;
using System.Linq.Expressions;
using Rene.Utils.Db.CompositeSpecifications.OrderByEngine;

namespace Rene.Utils.Db.CompositeSpecifications
{
    public static class SpecificationCombinator
    {
        /// <summary>
        /// Returns a copy of the specification that uses split queries on supported EF Core providers.
        /// When combined with other specifications, this option applies to the entire combined query,
        /// not only to the specification on which this extension is called.
        /// </summary>
        public static IDbUtilsSpecification<T> AsSplitQuery<T>(this IDbUtilsSpecification<T> specification)
            => new CompositeSpecification<T>(specification) { AsSplitQuery = true };

        /// <summary>
        /// Returns a copy of the specification that ignores EF Core query filters.
        /// When combined with other specifications, this option applies to the entire combined query,
        /// not only to the specification on which this extension is called.
        /// </summary>
        public static IDbUtilsSpecification<T> IgnoreQueryFilters<T>(this IDbUtilsSpecification<T> specification)
            => new CompositeSpecification<T>(specification) { IgnoreQueryFilters = true };

        /// <summary>
        /// Returns a copy of the specification that disables EF Core change tracking.
        /// When combined with other specifications, this option applies to the entire combined query,
        /// not only to the specification on which this extension is called.
        /// </summary>
        public static IDbUtilsSpecification<T> AsNoTracking<T>(this IDbUtilsSpecification<T> specification)
            => new CompositeSpecification<T>(specification) { AsNoTracking = true };

        /// <summary>
        /// Combines two specifications using logical AND.
        /// </summary>
        /// <typeparam name="T">The entity type.</typeparam>
        /// <param name="left">The first specification.</param>
        /// <param name="right">The second specification.</param>
        /// <returns>A composite specification representing the logical AND of the two specifications.</returns>
        public static IDbUtilsSpecification<T> And<T>(this IDbUtilsSpecification<T> left, IDbUtilsSpecification<T> right)
        {
            var specificationEngine = new AndSpecificationEngine<T>(left, right);
            var s = specificationEngine.Build();
            return s;

        }

        /// <summary>
        /// Combines two specifications using logical OR.
        /// </summary>
        /// <typeparam name="T">The entity type.</typeparam>
        /// <param name="left">The first specification.</param>
        /// <param name="right">The second specification.</param>
        /// <returns>A composite specification representing the logical OR of the two specifications.</returns>
        public static IDbUtilsSpecification<T> Or<T>(this IDbUtilsSpecification<T> left, IDbUtilsSpecification<T> right)
        {
            var specificationEngine = new ORSpecificationEngine<T>(left, right);
            var s = specificationEngine.Build();
            return s;
        }

        /// <summary>
        /// Inverts the given specification using logical NOT.
        /// </summary>
        /// <typeparam name="T">The entity type.</typeparam>
        /// <param name="spe">The specification to invert.</param>
        /// <returns>A composite specification representing the logical NOT of the given specification.</returns>
        public static IDbUtilsSpecification<T> ToInvert<T>(this IDbUtilsSpecification<T> spe)
        {
            var specificationEngine = new NotSpecificationEngine<T>(spe);
            var s = specificationEngine.Build();
            return s;
        }

        /// <summary>
        /// Combines the current specification with an ordering specification based on a collection of sort expressions.
        /// </summary>
        /// <typeparam name="T">The entity type.</typeparam>
        /// <param name="left">The base specification to combine with ordering.</param>
        /// <param name="sortSequence">Collection of key-value pairs where the key is a boolean indicating ascending (true) or descending (false) and the value is the sort expression.</param>
        /// <returns>A composite specification that applies the given ordering to the base specification.</returns>
        public static IDbUtilsSpecification<T> OrderBy<T>(this IDbUtilsSpecification<T> left, ICollection<KeyValuePair<bool, Expression<Func<T, object>>>> sortSequence)
        {
            var orderSpecification = new OrderBySpecification<T>(sortSequence);
            var specificationEngine = new OrderBySpecificationEngine<T>(left, orderSpecification);
            var s = specificationEngine.Build();
            return s;
        }

        /// <summary>
        /// Combines the current specification with an ordering specification based on a collection of sort query strings.
        /// </summary>
        /// <typeparam name="T">The entity type.</typeparam>
        /// <param name="left">The base specification to combine with ordering.</param>
        /// <param name="sortQueryString">
        ///     Collection of tuples separated by ",", where the first position is the field name and the second is 'asc' or 'desc'.
        ///     Example: { "Name,asc", "Age,desc" }
        /// </param>
        /// <returns>
        ///     A composite specification that applies the given ordering to the base specification.
        /// </returns>
        public static IDbUtilsSpecification<T> OrderBy<T>(this IDbUtilsSpecification<T> left, ICollection<string> sortQueryString)
        {
            if (sortQueryString == null || !sortQueryString.Any())
                throw new ArgumentNullException(nameof(sortQueryString), "The collection of sort query strings cannot be null or empty.");

            var sortSequence = sortQueryString.ToSortSequenceExpression<T>();
            return left.OrderBy(sortSequence);
        }


        /// <summary>
        /// Adds an include expression to the specification for eager loading related entities.
        /// </summary>
        /// <typeparam name="T">The entity type.</typeparam>
        /// <param name="specification">The specification to which the include expression will be added.</param>
        /// <param name="includeExpression">The expression specifying the related entity to include.</param>
        /// <returns>
        /// The composite specification with the added include expression.
        /// </returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown if the specification does not support includes.
        /// </exception>
        public static IDbUtilsSpecification<T> AddIncludeSpecification<T>(this IDbUtilsSpecification<T> specification,
            Expression<Func<T, object>> includeExpression)
        {
            if (specification is IDbUtilsCompositeSpecification<T> compositeSpecification)
            {
                if (compositeSpecification?.Includes == null)
                {
                    ((CompositeSpecification<T>)compositeSpecification).Includes ??= new List<Expression<Func<T, object>>>();
                }
                compositeSpecification?.Includes?.Add(includeExpression);
                return compositeSpecification;
            }
            else
            {
                throw new InvalidOperationException("The specification does not support includes.");
            }
        }
    }

}
