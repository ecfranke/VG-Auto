using System;
using System.Globalization;
using System.Threading.Tasks;
using VgAuto.Core.Domain;
using Xunit;

namespace VgAuto.Tests.Unit
{
    public class WorkCodeTests
    {
        private static readonly DateTime Started = new DateTime(2026, 9, 28, 12, 0, 0, DateTimeKind.Local);

        [Fact]
        public void Code_is_type_client_vehicle_date_and_number()
        {
            Assert.Equal("RP_TF_2019_HC_2026_09_28_15", WorkCode.Format(true, "Terry Fox", 2019, "Honda", "Civic", Started, "15"));
            Assert.Equal("OF_TF_2019_HC_2026_09_28_15", WorkCode.Format(false, "Terry Fox", 2019, "Honda", "Civic", Started, "15"));
        }

        [Fact]
        public void Missing_parts_are_X()
        {
            Assert.Equal("RP_TF_XXXX_HC_2026_09_28_3", WorkCode.Format(true, "Terry Fox", null, "Honda", "Civic", Started, "3"));
            Assert.Equal("RP_X_XXXX_XX_2026_09_28_3", WorkCode.Format(true, null, null, null, null, Started, "3"));
            Assert.Equal("OF_WC_XXXX_XX_2026_09_28_5", WorkCode.Format(false, "Wei Chen 陈伟", null, null, null, Started, "5"));
            Assert.Equal("OF_AML_XXXX_FX_2026_09_28_4", WorkCode.Format(false, "ABC Motors Ltd.", null, "Ford", "", Started, "4"));
        }

        [Fact]
        public void Pasted_code_gives_the_work_number()
        {
            Assert.True(WorkCode.TryParseNumber("RP_TF_2019_HC_2026_09_28_15", out var number));
            Assert.Equal(15, number);
            // an estimate of a later offer, lower case, spaces around it
            Assert.True(WorkCode.TryParseNumber(" of_陈_XXXX_XX_2026_09_28_7-2 ", out number));
            Assert.Equal(7, number);

            Assert.False(WorkCode.TryParseNumber("15", out _));
            Assert.False(WorkCode.TryParseNumber("Terry Fox", out _));
            Assert.False(WorkCode.TryParseNumber("RP_TF_2019_HC_2026_09_28_X", out _));
            Assert.False(WorkCode.TryParseNumber("RP_TF 15", out _));
            Assert.False(WorkCode.TryParseNumber(null, out _));
        }

        [Fact]
        public async Task Estimates_and_the_invoice_are_named_like_the_work()
        {
            var mechanic = new Employee("Mike", "Smith", DateTime.UtcNow);
            var vehicle = new Vehicle("ABC123", DateTime.UtcNow, "Honda", "Civic", year: 2019);
            var numbers = new Numbers();
            var work = Work.Start(numbers, mechanic, new PrivateClient(DateTime.UtcNow, "Terry", "Fox"), vehicle);
            var date = work.StartedOn.ToLocalTime().ToString("yyyy_MM_dd", CultureInfo.InvariantCulture);
            Assert.Equal($"OF_TF_2019_HC_{date}_15", work.Code);

            var offer = work.CreateOffer(mechanic);
            await work.Issue(offer, null, Taxes.None, mechanic, false, false, null);
            Assert.Equal($"OF_TF_2019_HC_{date}_15", offer.Estimate.Code);
            Assert.Equal("15-0", offer.Estimate.Number);
            Assert.Equal($"Estimate OF_TF_2019_HC_{date}_15", offer.Estimate.GetDisplayName());
            Assert.Equal($"estimate_OF_TF_2019_HC_{date}_15.pdf", offer.Estimate.GetFileName());

            // issuing it again makes a new offer, its estimate gets the order number
            var reissued = await work.Issue(offer, null, Taxes.None, mechanic, false, false, null);
            Assert.Equal($"OF_TF_2019_HC_{date}_15-1", reissued.Estimate.Code);
            Assert.Equal($"OF_TF_2019_HC_{date}_15", offer.Estimate.Code);

            work.StartRepairJob(mechanic);
            work.GenerateInvoice(numbers, Taxes.None, PaymentType.BankTransfer, 14, mechanic);
            Assert.Equal($"RP_TF_2019_HC_{date}_15", work.Invoice.Code);
            Assert.Equal(work.Code, work.Invoice.Code);
            Assert.Equal($"Invoice RP_TF_2019_HC_{date}_15", work.Invoice.GetDisplayName());
            Assert.Equal($"invoice_RP_TF_2019_HC_{date}_15.pdf", work.Invoice.GetFileName());
            Assert.Equal(7, work.Invoice.Number); // the invoice sequence stays, internally

            // the work follows its data, the issued documents keep their code
            work.IsFor(new PrivateClient(DateTime.UtcNow, "Wei", "Chen"));
            Assert.Equal($"RP_WC_2019_HC_{date}_15", work.Code);
            Assert.Equal($"RP_TF_2019_HC_{date}_15", work.Invoice.Code);
            Assert.Equal($"OF_TF_2019_HC_{date}_15", offer.Estimate.Code);
        }

        /// <summary>Work number 15, invoice number 7.</summary>
        private class Numbers : ISequnceNumberProviderFactory
        {
            public ISequencedNumberProvider GetNumberProvider<T>() => new FixedNumber(typeof(T) == typeof(Invoice) ? 7 : 15);

            private class FixedNumber : ISequencedNumberProvider
            {
                private readonly int number;
                public FixedNumber(int number) { this.number = number; }
                public int Next() => number;
            }
        }
    }
}
