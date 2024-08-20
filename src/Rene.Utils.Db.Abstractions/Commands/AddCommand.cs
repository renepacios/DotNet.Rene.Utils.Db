using MediatR;

namespace Rene.Utils.Db.Commands
{
    public class AddCommand<TViewModel> : IRequest<TViewModel>
    {
        public TViewModel ViewModel { get; }

        public AddCommand(TViewModel viewModel)
        {
            ViewModel = viewModel;
        }
    }
}