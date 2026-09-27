using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Carmasters.Core.Application.Authentication;
using Carmasters.Core.Application.Database;
using Carmasters.Core.Application.Model;
using Dapper;

namespace Carmasters.Core.Persistence
{
    /// <summary>One time email codes, stored in the user list database.</summary>
    public class AuthChallengeRepository : IAuthChallengeRepository
    {
        private readonly IDbConnectionFactory connections;
        public AuthChallengeRepository(IDbConnectionFactory connections) { this.connections = connections; }

        private static string Sql(string sql) => SqlDialect.Current.Sql(sql);

        private const string Columns = "id, purpose, tenantname as TenantName, employeeid as EmployeeId, code_hash as CodeHash, payload, attempts, sends, created_at as CreatedAt, expires_at as ExpiresAt, consumed_at as ConsumedAt";

        public async Task AddAsync(AuthChallenge c)
        {
            await using var db = connections.Open(connections.UserListDatabase);
            await db.ExecuteAsync(Sql(@"INSERT INTO public.auth_challenge
                (id, purpose, tenantname, employeeid, code_hash, payload, attempts, sends, created_at, expires_at, consumed_at)
                VALUES (@Id, @Purpose, @TenantName, @EmployeeId, @CodeHash, @Payload, @Attempts, @Sends, @CreatedAt, @ExpiresAt, @ConsumedAt)"),
                Utc(c));
        }

        public async Task<AuthChallenge> GetAsync(Guid id)
        {
            await using var db = connections.Open(connections.UserListDatabase);
            var c = await db.QuerySingleOrDefaultAsync<AuthChallenge>(Sql($"SELECT {Columns} FROM public.auth_challenge WHERE id = @id"), new { id });
            if (c == null) return null;
            c.CreatedAt = AsUtc(c.CreatedAt);
            c.ExpiresAt = AsUtc(c.ExpiresAt);
            c.ConsumedAt = c.ConsumedAt.HasValue ? AsUtc(c.ConsumedAt.Value) : null;
            return c;
        }

        public async Task UpdateAsync(AuthChallenge c)
        {
            await using var db = connections.Open(connections.UserListDatabase);
            await db.ExecuteAsync(Sql(@"UPDATE public.auth_challenge SET code_hash = @CodeHash, attempts = @Attempts, sends = @Sends,
                expires_at = @ExpiresAt, consumed_at = @ConsumedAt WHERE id = @Id"), Utc(c));
        }

        public async Task DeleteExpiredAsync(DateTime utcNow)
        {
            await using var db = connections.Open(connections.UserListDatabase);
            await db.ExecuteAsync(Sql("DELETE FROM public.auth_challenge WHERE expires_at < @cutoff"), new { cutoff = utcNow.AddDays(-1) });
        }

        private static AuthChallenge Utc(AuthChallenge c) => new AuthChallenge
        {
            Id = c.Id, Purpose = c.Purpose, TenantName = c.TenantName, EmployeeId = c.EmployeeId, CodeHash = c.CodeHash,
            Payload = c.Payload, Attempts = c.Attempts, Sends = c.Sends,
            CreatedAt = AsUtc(c.CreatedAt), ExpiresAt = AsUtc(c.ExpiresAt),
            ConsumedAt = c.ConsumedAt.HasValue ? AsUtc(c.ConsumedAt.Value) : null,
        };

        internal static DateTime AsUtc(DateTime value) => value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value, DateTimeKind.Utc) // stored as UTC
        };
    }

    public class ExternalLoginRepository : IExternalLoginRepository
    {
        private readonly IDbConnectionFactory connections;
        public ExternalLoginRepository(IDbConnectionFactory connections) { this.connections = connections; }

        private static string Sql(string sql) => SqlDialect.Current.Sql(sql);

        private const string Select = "SELECT provider, subject, tenantname as TenantName, employeeid as EmployeeId, email, created_at as CreatedAt FROM public.user_external_login";

        private class Row
        {
            public string Provider { get; set; }
            public string Subject { get; set; }
            public string TenantName { get; set; }
            public Guid EmployeeId { get; set; }
            public string Email { get; set; }
            public DateTime CreatedAt { get; set; }
            public ExternalLogin ToLogin() => new(Provider, Subject, TenantName, EmployeeId, Email, AuthChallengeRepository.AsUtc(CreatedAt));
        }

        public async Task<ExternalLogin> FindAsync(string provider, string subject)
        {
            await using var db = connections.Open(connections.UserListDatabase);
            var row = await db.QuerySingleOrDefaultAsync<Row>(Sql($"{Select} WHERE provider = @provider AND subject = @subject"), new { provider, subject });
            return row?.ToLogin();
        }

        public async Task<IReadOnlyList<ExternalLogin>> GetForUserAsync(UserIdentifier user)
        {
            await using var db = connections.Open(connections.UserListDatabase);
            var rows = await db.QueryAsync<Row>(Sql($"{Select} WHERE tenantname = @TenantName AND employeeid = @EmployeeId"),
                new { user.TenantName, user.EmployeeId });
            return rows.Select(r => r.ToLogin()).ToList();
        }

        public async Task AddAsync(ExternalLogin login)
        {
            await using var db = connections.Open(connections.UserListDatabase);
            await db.ExecuteAsync(Sql(@"INSERT INTO public.user_external_login (provider, subject, tenantname, employeeid, email, created_at)
                VALUES (@Provider, @Subject, @TenantName, @EmployeeId, @Email, @CreatedAt)"),
                new { login.Provider, login.Subject, login.TenantName, login.EmployeeId, login.Email, CreatedAt = AuthChallengeRepository.AsUtc(login.CreatedAt) });
        }

        public async Task RemoveAsync(UserIdentifier user, string provider)
        {
            await using var db = connections.Open(connections.UserListDatabase);
            await db.ExecuteAsync(Sql("DELETE FROM public.user_external_login WHERE tenantname = @TenantName AND employeeid = @EmployeeId AND provider = @provider"),
                new { user.TenantName, user.EmployeeId, provider });
        }
    }
}
