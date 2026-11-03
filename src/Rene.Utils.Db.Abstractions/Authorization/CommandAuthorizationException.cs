using System;

namespace Rene.Utils.Db.Authorization
{
    /// <summary>
    /// Raised when a command requirement is denied. Hosts decide how to translate this failure;
    /// do not expose internal failure reasons directly to untrusted clients.
    /// </summary>
    public sealed class CommandAuthorizationException : Exception
    {
        public CommandAuthorizationException(Type commandType, Type requirementType, string failureReason)
            : base($"Authorization denied for command '{commandType?.Name}'.")
        {
            CommandType = commandType ?? throw new ArgumentNullException(nameof(commandType));
            RequirementType = requirementType ?? throw new ArgumentNullException(nameof(requirementType));
            FailureReason = failureReason ?? throw new ArgumentNullException(nameof(failureReason));
        }

        public Type CommandType { get; }
        public Type RequirementType { get; }
        public string FailureReason { get; }
    }
}
