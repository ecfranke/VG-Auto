using System;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using NHibernate;
using VgAuto.Core.Application.Database;
using VgAuto.Core.Application.Signing;

namespace VgAuto.Core.Persistence
{
    /// <summary>Signature links in the user list database.</summary>
    public class SignatureLinkRepository : ISignatureLinkRepository
    {
        private readonly IDbConnectionFactory connections;
        public SignatureLinkRepository(IDbConnectionFactory connections) { this.connections = connections; }

        private static string Sql(string sql) => SqlDialect.Current.Sql(sql);

        private class Row
        {
            public string TokenHash { get; set; }
            public string TenantName { get; set; }
            public Guid CompanyId { get; set; }
            public Guid EstimateId { get; set; }
            public DateTime CreatedAt { get; set; }
            public DateTime ExpiresAt { get; set; }
        }

        public async Task AddAsync(SignatureLink link)
        {
            await using var db = connections.Open(connections.UserListDatabase);
            await db.ExecuteAsync(Sql(@"INSERT INTO public.signature_link (token_hash, tenantname, company_id, estimate_id, created_at, expires_at)
                                        VALUES (@TokenHash, @TenantName, @CompanyId, @EstimateId, @CreatedAt, @ExpiresAt)"),
                new
                {
                    link.TokenHash,
                    link.TenantName,
                    link.CompanyId,
                    link.EstimateId,
                    CreatedAt = AuthChallengeRepository.AsUtc(link.CreatedAt),
                    ExpiresAt = AuthChallengeRepository.AsUtc(link.ExpiresAt),
                });
        }

        public async Task<SignatureLink> FindAsync(string tokenHash)
        {
            await using var db = connections.Open(connections.UserListDatabase);
            var row = await db.QuerySingleOrDefaultAsync<Row>(Sql(@"SELECT token_hash as TokenHash, tenantname as TenantName, company_id as CompanyId,
                       estimate_id as EstimateId, created_at as CreatedAt, expires_at as ExpiresAt
                  FROM public.signature_link WHERE token_hash = @tokenHash"), new { tokenHash });
            return row == null ? null : new SignatureLink(row.TokenHash, row.TenantName, row.CompanyId, row.EstimateId,
                AuthChallengeRepository.AsUtc(row.CreatedAt), AuthChallengeRepository.AsUtc(row.ExpiresAt));
        }
    }

    /// <summary>
    /// Signatures of estimates, through the request's session: also inside its transaction (MySQL does not allow another
    /// command on the connection of an open NHibernate transaction).
    /// </summary>
    public class EstimateSignatureRepository : IEstimateSignatures
    {
        private readonly ISession session;
        public EstimateSignatureRepository(ISession session) { this.session = session; }

        private static string Sql(string sql) => SqlDialect.Current.Sql(sql);

        public Task<EstimateSignature> GetAsync(Guid estimateId)
        {
            var row = session.CreateSQLQuery(Sql("select signer_name, signed_at, image from domain.estimate_signature where estimate_id = :id"))
                .SetParameter("id", estimateId)
                .List<object[]>()
                .FirstOrDefault();
            if (row == null) return Task.FromResult<EstimateSignature>(null);
            var signedAt = row[1] is DateTimeOffset offset ? offset.UtcDateTime : Convert.ToDateTime(row[1]);
            return Task.FromResult(new EstimateSignature(estimateId, row[0]?.ToString(), AuthChallengeRepository.AsUtc(signedAt), row[2]?.ToString()));
        }

        public async Task<bool> AddAsync(EstimateSignature signature, Guid companyId, string ip)
        {
            if (await GetAsync(signature.EstimateId) != null) return false;
            session.CreateSQLQuery(Sql(@"insert into domain.estimate_signature (estimate_id, company_id, signer_name, signed_at, image, ip)
                                         values (:estimateId, :companyId, :signerName, :signedAt, :image, :ip)"))
                .SetParameter("estimateId", signature.EstimateId)
                .SetParameter("companyId", companyId)
                .SetParameter("signerName", signature.SignerName)
                .SetParameter("signedAt", AuthChallengeRepository.AsUtc(signature.SignedAt))
                .SetParameter("image", signature.Image)
                .SetParameter("ip", ip, NHibernateUtil.String)
                .ExecuteUpdate();
            return true;
        }
    }
}
