using MediatR;

namespace Rene.Utils.Db.Events
{
    /// <summary>Published after an Update handler saves changes, before it returns to the caller.</summary>
    public sealed class EntityUpdated<TModel, TViewModel> : INotification where TModel : class
    {
        public EntityUpdated(TModel entity, TViewModel viewModel, object id, TViewModel response)
        {
            Entity = entity ?? throw new System.ArgumentNullException(nameof(entity));
            ViewModel = viewModel;
            Id = id;
            Response = response;
        }

        /// <summary>The saved entity after the update. This is a live reference, not a snapshot.</summary>
        public TModel Entity { get; }

        /// <summary>The view model supplied to the command.</summary>
        public TViewModel ViewModel { get; }

        /// <summary>The identifier supplied to the command.</summary>
        public object Id { get; }

        /// <summary>The mapped response returned by the command.</summary>
        public TViewModel Response { get; }
    }
}
