using MediatR;

namespace Rene.Utils.Db.Events
{
    /// <summary>Published after a Delete handler saves changes. No view model is generated for deletion.</summary>
    public sealed class EntityDeleted<TModel, TViewModel> : INotification where TModel : class
    {
        public EntityDeleted(TModel entity, object id)
        {
            Entity = entity ?? throw new System.ArgumentNullException(nameof(entity));
            Id = id;
        }

        /// <summary>The removed entity. This is a live reference, not a snapshot.</summary>
        public TModel Entity { get; }

        /// <summary>The identifier supplied to the command.</summary>
        public object Id { get; }
    }
}
