using System;

namespace VgAuto.Core.Application.Database
{
    /// <summary>
    /// The company whose data the current request works with (the company of the signed in user).
    /// Queries through the ORM are limited to it automatically; raw SQL must filter on company_id.
    /// </summary>
    public interface ICompanyScope
    {
        public static readonly Guid FirstCompany = new("00000000-0000-0000-0000-000000000001");

        Guid CompanyId { get; }

        /// <summary>Work with another company for the rest of the request (administration).</summary>
        void SwitchTo(Guid companyId);

        /// <summary>See the data of all companies for the rest of the request (administration).</summary>
        void AllCompanies();
    }
}
