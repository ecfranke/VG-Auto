using System;
using System.Collections.Generic;
using System.Linq;

namespace VgAuto.Core.Domain
{
    /// <summary>One sales tax of a document, for example "GST" 5 or "QST" 9.975.</summary>
    public record TaxRate(string Name, decimal Rate);

    /// <summary>A tax with its amount on a document.</summary>
    public record TaxAmount(string Name, decimal Rate, decimal Amount);

    /// <summary>
    /// The sales taxes a company charges: up to two (GST + PST, GST + QST), or one (GST, HST, VAT).
    /// Prices are before tax; each tax is calculated on the subtotal.
    /// </summary>
    public class Taxes
    {
        public IReadOnlyList<TaxRate> Rates { get; }

        public Taxes(params TaxRate[] rates)
        {
            Rates = (rates ?? Array.Empty<TaxRate>())
                .Where(r => r != null && !string.IsNullOrWhiteSpace(r.Name))
                .Select(r =>
                {
                    if (r.Rate < 0 || r.Rate > 100) throw new UserException("Tax rate must be between 0 and 100.");
                    return new TaxRate(r.Name.Trim(), r.Rate);
                })
                .Take(2)
                .ToList();
        }

        public static Taxes Of(string name1, decimal rate1, string name2 = null, decimal rate2 = 0) =>
            new(new TaxRate(name1, rate1), new TaxRate(name2, rate2));

        public static readonly Taxes None = new();

        /// <summary>Sum of the rates, for example 12 for GST 5 + PST 7.</summary>
        public decimal TotalRate => Rates.Sum(r => r.Rate);

        public decimal Multiplier => 1 + TotalRate / 100m;

        public TaxRate First => Rates.Count > 0 ? Rates[0] : null;
        public TaxRate Second => Rates.Count > 1 ? Rates[1] : null;

        /// <summary>Each tax on the subtotal, rounded to cents (the way it is printed and paid).</summary>
        public IReadOnlyList<TaxAmount> On(decimal subtotal, int decimals = 2) =>
            Rates.Select(r => new TaxAmount(r.Name, r.Rate, Math.Round(subtotal * r.Rate / 100m, decimals, MidpointRounding.AwayFromZero))).ToList();
    }
}
