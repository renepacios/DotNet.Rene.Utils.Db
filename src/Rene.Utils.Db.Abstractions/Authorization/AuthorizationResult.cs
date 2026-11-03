using System;

namespace Rene.Utils.Db.Authorization
{
    /// <summary>The outcome of evaluating an authorization requirement.</summary>
    public sealed class AuthorizationResult
    {
        private AuthorizationResult(bool succeeded, string failureReason)
        {
            Succeeded = succeeded;
            FailureReason = failureReason;
        }

        public bool Succeeded { get; }
        public string FailureReason { get; }

        public static AuthorizationResult Allow() => new AuthorizationResult(true, null);

        public static AuthorizationResult Deny(string reason)
        {
            if (string.IsNullOrWhiteSpace(reason))
                throw new ArgumentException("A denial reason is required.", nameof(reason));

            return new AuthorizationResult(false, reason);
        }
    }
}
