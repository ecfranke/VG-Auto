using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using VgAuto.Core.Application.Model;

namespace VgAuto.Core.Application.Authentication
{
    public static class ChallengePurpose
    {
        public const string Login = "login";
        public const string PasswordReset = "reset";
        public const string LinkExternal = "link";
    }

    public class AuthChallenge
    {
        public Guid Id { get; set; }
        public string Purpose { get; set; }
        public string TenantName { get; set; }
        public Guid EmployeeId { get; set; }
        public string CodeHash { get; set; }
        public string Payload { get; set; }
        public int Attempts { get; set; }
        public int Sends { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime ExpiresAt { get; set; }
        public DateTime? ConsumedAt { get; set; }

        public UserIdentifier User => new UserIdentifier(TenantName, EmployeeId);
    }

    public interface IAuthChallengeRepository
    {
        Task AddAsync(AuthChallenge challenge);
        Task<AuthChallenge> GetAsync(Guid id);
        Task UpdateAsync(AuthChallenge challenge);
        Task DeleteExpiredAsync(DateTime utcNow);
    }

    public record ExternalLogin(string Provider, string Subject, string TenantName, Guid EmployeeId, string Email, DateTime CreatedAt);

    public interface IExternalLoginRepository
    {
        Task<ExternalLogin> FindAsync(string provider, string subject);
        Task<IReadOnlyList<ExternalLogin>> GetForUserAsync(UserIdentifier user);
        Task AddAsync(ExternalLogin login);
        Task RemoveAsync(UserIdentifier user, string provider);
    }

    /// <summary>Verified identity returned by an external provider.</summary>
    public record ExternalIdentity(string Provider, string Subject, string Email, string Name);

    public interface IMicrosoftIdentityClient
    {
        /// <summary>Exchanges an authorization code (PKCE) and returns the validated identity.</summary>
        Task<ExternalIdentity> RedeemCodeAsync(string code, string codeVerifier, string redirectUri, string nonce);
    }
}
