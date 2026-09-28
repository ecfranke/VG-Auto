using VgAuto.Core.Application.Email;
using Xunit;

namespace VgAuto.Tests.Unit
{
    public class EmailAddressTests
    {
        [Fact]
        public void Several_recipients_are_split_and_checked()
        {
            Assert.Equal(new[] { "a@x.com", "b@y.org" }, EmailAddresses.Parse(" a@x.com; b@y.org, "));
            Assert.Throws<EmailDeliveryException>(() => EmailAddresses.Parse("not-an-email"));
            Assert.Throws<EmailDeliveryException>(() => EmailAddresses.Parse("a@x.com b@y.org"));
            Assert.Throws<EmailDeliveryException>(() => EmailAddresses.Parse(" ; "));
            Assert.False(EmailAddresses.IsValid(""));
        }
    }
}
