using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using VgAuto.Core.Domain;

namespace VgAuto.Core.Application.Signing
{
    /// <summary>A link that lets a client sign one estimate without signing in. Only the hash of its token is stored.</summary>
    public record SignatureLink(string TokenHash, string TenantName, Guid CompanyId, Guid EstimateId, DateTime CreatedAt, DateTime ExpiresAt);

    /// <summary>Signature links, kept in the user list database (the link does not name its tenant).</summary>
    public interface ISignatureLinkRepository
    {
        Task AddAsync(SignatureLink link);
        Task<SignatureLink> FindAsync(string tokenHash);
        /// <summary>The links of deleted estimates.</summary>
        Task DeleteForEstimatesAsync(IReadOnlyCollection<Guid> estimateIds);
    }

    /// <summary>A client's signature of an estimate. Image: PNG as a data URL.</summary>
    public record EstimateSignature(Guid EstimateId, string SignerName, DateTime SignedAt, string Image);

    /// <summary>Signatures of estimates, in the company data (through the request's database session).</summary>
    public interface IEstimateSignatures
    {
        Task<EstimateSignature> GetAsync(Guid estimateId);
        /// <returns>false when the estimate was signed already</returns>
        Task<bool> AddAsync(EstimateSignature signature, Guid companyId, string ip);
        /// <summary>The signature of an estimate that is deleted.</summary>
        Task DeleteAsync(Guid estimateId);
    }

    /// <summary>Creates the links that go into estimate emails and checks the tokens the clients come back with.</summary>
    public class SignatureLinks
    {
        public const int DefaultValidDays = 30;
        private static readonly Regex TokenPattern = new("^[A-Za-z0-9_-]{43}$", RegexOptions.Compiled);

        private readonly ISignatureLinkRepository links;
        private readonly IConfiguration configuration;
        private readonly IHttpContextAccessor http;

        public SignatureLinks(ISignatureLinkRepository links, IConfiguration configuration, IHttpContextAccessor http)
        {
            this.links = links;
            this.configuration = configuration;
            this.http = http;
        }

        /// <summary>Header in which the Next.js server passes the address it is reached at.</summary>
        public const string AppOriginHeader = "X-App-Origin";

        /// <summary>
        /// The address people open the application at: App:Url, otherwise the first allowed CORS origin (the installers
        /// put the application's URL there), otherwise the address the Next.js server reports; null when none is known.
        /// </summary>
        public string AppUrl
        {
            get
            {
                var url = configuration["App:Url"];
                if (string.IsNullOrWhiteSpace(url)) url = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()?.FirstOrDefault(x => !string.IsNullOrWhiteSpace(x) && x != "*");
                if (string.IsNullOrWhiteSpace(url)) url = Origin(http.HttpContext?.Request.Headers[AppOriginHeader].ToString());
                return string.IsNullOrWhiteSpace(url) ? null : url.Trim().TrimEnd('/');
            }
        }

        /// <summary>"https://shop.example.com" of an absolute http(s) address; null otherwise.</summary>
        public static string Origin(string value) =>
            Uri.TryCreate(value?.Trim(), UriKind.Absolute, out var uri) && (uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeHttp)
                ? uri.GetLeftPart(UriPartial.Authority)
                : null;

        public int ValidDays => int.TryParse(configuration["Signing:LinkDays"], out var days) && days > 0 ? days : DefaultValidDays;

        /// <summary>A new link to sign the estimate; null when the application's URL is not known.</summary>
        public async Task<(string Url, DateTime ExpiresAt)?> CreateAsync(Estimate estimate)
        {
            var appUrl = AppUrl;
            if (appUrl == null) return null;
            var tenant = http.HttpContext?.User?.Claims.FirstOrDefault(x => x.Type == ClaimTypes.Spn)?.Value;
            if (string.IsNullOrWhiteSpace(tenant)) return null;

            var token = NewToken();
            var now = DateTime.UtcNow;
            var expires = now.AddDays(ValidDays);
            await links.AddAsync(new SignatureLink(Hash(token), tenant, estimate.CompanyId, estimate.Id, now, expires));
            return ($"{appUrl}/sign/{token}", expires);
        }

        /// <summary>The link of a token; null when the token is not one of ours. Expired links are returned (the page says so).</summary>
        public async Task<SignatureLink> FindAsync(string token)
        {
            if (string.IsNullOrEmpty(token) || !TokenPattern.IsMatch(token)) return null;
            return await links.FindAsync(Hash(token));
        }

        /// <summary>256 random bits, base64url (43 characters).</summary>
        public static string NewToken() =>
            Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)).TrimEnd('=').Replace('+', '-').Replace('/', '_');

        public static string Hash(string token) => Convert.ToHexString(SHA256.HashData(Encoding.ASCII.GetBytes(token))).ToLowerInvariant();
    }

    /// <summary>Checks the signature drawn on the signing page.</summary>
    public static class SignatureImage
    {
        public const string Prefix = "data:image/png;base64,";
        public const int MaxBytes = 300_000;
        private static readonly byte[] PngMagic = { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };

        /// <summary>A PNG data URL of at most <see cref="MaxBytes"/>; throws <see cref="UserException"/> otherwise.</summary>
        public static string Validate(string dataUrl)
        {
            if (string.IsNullOrWhiteSpace(dataUrl) || !dataUrl.StartsWith(Prefix, StringComparison.Ordinal))
                throw new UserException("Please sign in the box.");
            byte[] bytes;
            try
            {
                bytes = Convert.FromBase64String(dataUrl[Prefix.Length..]);
            }
            catch (FormatException)
            {
                throw new UserException("The signature could not be read. Please sign again.");
            }
            if (bytes.Length > MaxBytes) throw new UserException("The signature is too large. Please sign again.");
            if (bytes.Length < PngMagic.Length || !bytes.AsSpan(0, PngMagic.Length).SequenceEqual(PngMagic))
                throw new UserException("The signature could not be read. Please sign again.");
            // stored in a normalized form: the prefix and plain base64 only
            return Prefix + Convert.ToBase64String(bytes);
        }
    }
}
