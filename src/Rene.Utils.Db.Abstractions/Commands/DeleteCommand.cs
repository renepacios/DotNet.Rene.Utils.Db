using MediatR;

namespace Rene.Utils.Db.Commands
{
    public class DeleteCommand<TViewModel> : IRequest<bool>
    {
        public object Id { get; }

        public DeleteCommand(object id)
        {
            Id = id;
        }
    }
}