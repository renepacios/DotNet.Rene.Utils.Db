using MediatR;

namespace Rene.Utils.Db.Events
{
    /// <summary>Published after an Add handler saves changes, before it returns to the caller.</summary>
    public sealed class EntityAdded<TModel, TViewModel> : INotification where TModel : class
    {
        public EntityAdded(TModel entity, TViewModel viewModel, TViewModel response)
        {
            Entity = entity ?? throw new System.ArgumentNullException(nameof(entity));
            ViewModel = viewModel;
            Response = response;
        }

        /// <summary>The saved entity, including generated values. This is a live reference, not a snapshot.</summary>
        public TModel Entity { get; }

        /// <summary>The view model supplied to the command.</summary>
        public TViewModel ViewModel { get; }

        /// <summary>The mapped response returned by the command.</summary>
        public TViewModel Response { get; }
    }
}
