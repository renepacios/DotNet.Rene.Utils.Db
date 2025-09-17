using System.Linq.Expressions;

namespace Rene.Utils.Db.CompositeSpecifications.OrderByEngine
{
    internal class OrderBySpecification<T> : CompositeSpecification<T>, IDbUtilsCompositeSpecification<T>
    {
        public OrderBySpecification(ICollection<KeyValuePair<bool, Expression<Func<T, object>>>> sortSequence)
        {
            if (!sortSequence.AnyNotNull())
                throw new ArgumentNullException(nameof(sortSequence), "sortSequence cannot be null or empty");

            if (!OrderBy.AnyNotNull())
            {
                OrderBy = sortSequence.ToList();
            }
            else
            {
                OrderBy.AddRange(sortSequence);
            }

        }
    }
}
