using System;
using System.Collections.Generic;
using System.Linq;

namespace VgAuto.Core.Domain
{
    /// <summary>Subtotal (prices before tax), the taxes on it and the total.</summary>
    public class PriceSummary
    {
        public decimal TotalWithVat { get; protected set; }
        public decimal TotalWithoutVat { get; protected set; }
        public IReadOnlyList<TaxAmount> Taxes { get; protected set; } = Array.Empty<TaxAmount>();
        private PriceSummary() { }

        private PriceSummary(decimal price, decimal quantity, decimal discount, Taxes taxes)
        {
            var bargainMultiplier = (100 + discount) / 100m;
            TotalWithoutVat = quantity * price * bargainMultiplier;
            TotalWithVat = TotalWithoutVat * taxes.Multiplier;
        }

        public PriceSummary(Saleable saleable, Taxes taxes) : this(saleable.Price, saleable.Quantity.GetValueOrDefault(), saleable.Discount.GetValueOrDefault(), taxes)
        {
        }

        public static PriceSummary CalculatePriceSummary(Taxes taxes, IEnumerable<Saleable> saleables, int decimals = 2)
        {
            taxes ??= Domain.Taxes.None;
            var subtotal = saleables.Select(x => new PriceSummary(x, taxes)).Sum(x => x.TotalWithoutVat);
            var amounts = taxes.On(subtotal, decimals);
            return new PriceSummary
            {
                TotalWithoutVat = subtotal,
                Taxes = amounts,
                TotalWithVat = subtotal + amounts.Sum(t => t.Amount),
            };
        }
    }
}
