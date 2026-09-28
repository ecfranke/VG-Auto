using System.Linq;
using VgAuto.Core.Domain;
using Xunit;

namespace VgAuto.Tests.Unit
{
    public class TaxCalculationTests
    {
        [Fact]
        public void Taxes_are_added_to_the_subtotal_and_rounded_per_tax()
        {
            var taxes = Taxes.Of("GST", 5m, "QST", 9.975m);
            Assert.Equal(14.975m, taxes.TotalRate);
            var amounts = taxes.On(123.45m);
            Assert.Equal(new[] { 6.17m, 12.31m }, amounts.Select(a => a.Amount));
        }

        [Fact]
        public void Empty_second_tax_is_ignored_and_rates_are_checked()
        {
            Assert.Single(Taxes.Of("HST", 13m, "", 0m).Rates);
            Assert.Throws<UserException>(() => Taxes.Of("GST", 120m));
        }
    }
}
