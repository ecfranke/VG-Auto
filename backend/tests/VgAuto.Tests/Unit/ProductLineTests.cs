using VgAuto.Core.Domain;
using Xunit;

namespace VgAuto.Tests.Unit
{
    public class ProductLineTests
    {
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void A_line_without_a_code_is_a_custom_line(string code)
        {
            var line = new ProductOffered(null, 1, code, "Diagnostics", 1, null, 95m);
            Assert.Equal(Product.CustomCode, line.Code);
            Assert.Equal("", line.Unit);
        }

        [Fact]
        public void A_code_is_kept_without_spaces()
        {
            Assert.Equal("BP-F", new ProductInstalled(null, 1, " BP-F ", "Front brake pads", 1, "pcs", 89.5m).Code);
        }
    }
}
