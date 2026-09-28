using System;
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
    }
}
