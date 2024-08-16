using MediatR;

namespace Rene.Utils.Db.Commands
{
    public class UpdateCommand<TViewModel> : IRequest<TViewModel>
    {
        public TViewModel ViewModel { get; }
        public object Id { get; }

        public UpdateCommand(TViewModel viewModel, object id)
        {
            ViewModel = viewModel;
            Id = id;
        }
    }
}