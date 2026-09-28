using System.Collections.Generic;
using System.Linq;

namespace VgAuto.Core.Application.Configuration
{
    /// <summary>
    /// Places of registration and the sales taxes charged there. Choosing a region fills in the tax names
    /// and rates; they can still be changed afterwards (rates change, special cases).
    /// Canadian rates as of 2026: GST 5 %, HST 13 % (ON), 14 % (NS), 15 % (NB, NL, PE),
    /// PST 7 % (BC), RST 7 % (MB), PST 6 % (SK), QST 9.975 % (QC).
    /// </summary>
    public static class TaxRegions
    {
        public record Tax(string Name, decimal Rate);

        /// <param name="TaxIdLabel">How the tax registration number is called on documents.</param>
        public record Region(string Country, string Code, string Name, string TaxIdLabel, IReadOnlyList<Tax> Taxes);

        public record Country(string Code, string Name, IReadOnlyList<Region> Regions, string TaxIdLabel, IReadOnlyList<Tax> DefaultTaxes);

        private static Region Ca(string code, string name, params Tax[] taxes) => new("CA", code, name, "GST/HST No.", taxes);
        private static Tax Gst => new("GST", 5m);
        private static Tax Hst(decimal rate) => new("HST", rate);

        public static readonly IReadOnlyList<Country> All = new[]
        {
            new Country("CA", "Canada", new[]
            {
                Ca("AB", "Alberta", Gst),
                Ca("BC", "British Columbia", Gst, new Tax("PST", 7m)),
                Ca("MB", "Manitoba", Gst, new Tax("RST", 7m)),
                Ca("NB", "New Brunswick", Hst(15m)),
                Ca("NL", "Newfoundland and Labrador", Hst(15m)),
                Ca("NS", "Nova Scotia", Hst(14m)),
                Ca("NT", "Northwest Territories", Gst),
                Ca("NU", "Nunavut", Gst),
                Ca("ON", "Ontario", Hst(13m)),
                Ca("PE", "Prince Edward Island", Hst(15m)),
                Ca("QC", "Quebec", Gst, new Tax("QST", 9.975m)),
                Ca("SK", "Saskatchewan", Gst, new Tax("PST", 6m)),
                Ca("YT", "Yukon", Gst),
            }, "GST/HST No.", new[] { Gst }),
            // sales tax in the US depends on state, county and city: the rate is entered by hand
            new Country("US", "United States", new Region[0], "Tax ID", new[] { new Tax("Sales tax", 0m) }),
            new Country("OTHER", "Other", new Region[0], "Tax ID", new[] { new Tax("VAT", 0m) }),
        };

        public static Country FindCountry(string code) => All.FirstOrDefault(c => c.Code == code?.Trim().ToUpperInvariant());

        public static Region Find(string country, string region) =>
            FindCountry(country)?.Regions.FirstOrDefault(r => r.Code == region?.Trim().ToUpperInvariant());

        /// <summary>Label of the tax registration number on documents ("GST/HST No." in Canada, otherwise "Tax ID").</summary>
        public static string TaxIdLabel(string country, string region) =>
            Find(country, region)?.TaxIdLabel ?? FindCountry(country)?.TaxIdLabel ?? "Tax ID";
    }
}
