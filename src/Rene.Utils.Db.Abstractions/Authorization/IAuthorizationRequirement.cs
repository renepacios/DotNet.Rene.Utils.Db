using System.Threading;
using System.Threading.Tasks;

namespace Rene.Utils.Db.Authorization
{
    /// <summary>A reusable authorization condition, independent of the command that requires it.</summary>
    public interface IAuthorizationRequirement { }

    /// <summary>Validates one type of requirement. Register exactly one handler per requirement type.</summary>
    public interface IRequirementHandler<TRequirement> where TRequirement : IAuthorizationRequirement
    {
        Task<AuthorizationResult> Handle(TRequirement requirement, CancellationToken cancellationToken);
    }
}
