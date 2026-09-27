using VgAuto.Core.Application.Authorization;
using Xunit;

namespace VgAuto.Tests.Unit
{
    public class AdminPermissionsTests
    {
        private static readonly AdminActor Owner = new(UserRoles.SuperAdmin, true);
        private static readonly AdminActor Super = new(UserRoles.SuperAdmin, false);
        private static readonly AdminActor Admin = new(UserRoles.Admin, false);
        private static readonly AdminActor Staff = new(UserRoles.User, false);

        private static AdminTarget User(bool self = false) => new(true, UserRoles.User, false, self);
        private static AdminTarget AdminTarget() => new(true, UserRoles.Admin, false, false);
        private static AdminTarget SuperTarget() => new(true, UserRoles.SuperAdmin, false, false);
        private static AdminTarget OwnerTarget(bool self = false) => new(true, UserRoles.SuperAdmin, true, self);
        private static readonly AdminTarget NoLogin = new(false, UserRoles.User, false, false);

        private static bool Can(AdminActor a, AdminTarget t, AdminAction action, string role = null) => AdminPermissions.Check(a, t, action, role) == null;

        [Fact]
        public void Normal_users_have_no_administration_rights()
        {
            Assert.False(Can(Staff, User(), AdminAction.EditProfile));
            Assert.False(Can(Staff, NoLogin, AdminAction.CreateAccount));
        }

        [Theory]
        [InlineData(AdminAction.EditProfile)]
        [InlineData(AdminAction.ResetPassword)]
        [InlineData(AdminAction.Unlock)]
        [InlineData(AdminAction.Disable)]
        [InlineData(AdminAction.Enable)]
        [InlineData(AdminAction.UnlinkMicrosoft)]
        public void Admin_manages_normal_users_but_not_administrators(AdminAction action)
        {
            Assert.True(Can(Admin, User(), action));
            Assert.False(Can(Admin, AdminTarget(), action));
            Assert.False(Can(Admin, SuperTarget(), action));
            Assert.False(Can(Admin, OwnerTarget(), action));
        }

        [Fact]
        public void Admin_creates_only_normal_accounts_and_cannot_change_roles()
        {
            Assert.True(Can(Admin, NoLogin, AdminAction.CreateAccount, UserRoles.User));
            Assert.False(Can(Admin, NoLogin, AdminAction.CreateAccount, UserRoles.Admin));
            Assert.False(Can(Admin, NoLogin, AdminAction.CreateAccount, UserRoles.SuperAdmin));
            Assert.False(Can(Admin, User(), AdminAction.ChangeRole, UserRoles.Admin));
        }

        [Fact]
        public void Super_admin_manages_administrators_and_roles()
        {
            Assert.True(Can(Super, NoLogin, AdminAction.CreateAccount, UserRoles.SuperAdmin));
            Assert.True(Can(Super, User(), AdminAction.ChangeRole, UserRoles.Admin));
            Assert.True(Can(Super, AdminTarget(), AdminAction.Disable));
            Assert.True(Can(Super, SuperTarget(), AdminAction.ChangeRole, UserRoles.User));
            Assert.False(Can(Super, User(), AdminAction.ChangeRole, "root"));
        }

        [Theory]
        [InlineData(AdminAction.EditProfile)]
        [InlineData(AdminAction.ResetPassword)]
        [InlineData(AdminAction.Disable)]
        [InlineData(AdminAction.ChangeRole)]
        public void Owner_cannot_be_changed_by_other_administrators(AdminAction action)
        {
            Assert.False(Can(Super, OwnerTarget(), action, UserRoles.User));
        }

        [Fact]
        public void Nobody_changes_the_own_role_or_status_here()
        {
            Assert.True(Can(Owner, OwnerTarget(self: true), AdminAction.EditProfile));
            Assert.False(Can(Owner, OwnerTarget(self: true), AdminAction.ChangeRole, UserRoles.User));
            Assert.False(Can(Owner, OwnerTarget(self: true), AdminAction.Disable));
            Assert.False(Can(Admin, new AdminTarget(true, UserRoles.Admin, false, true), AdminAction.ResetPassword));
            Assert.True(Can(Admin, new AdminTarget(true, UserRoles.Admin, false, true), AdminAction.EditProfile));
        }

        [Fact]
        public void Owner_cannot_be_disabled_or_demoted()
        {
            var owner = new VgAuto.Core.Application.User("admin", "hash", null, false, null,
                new VgAuto.Core.Application.Model.UserIdentifier("t", System.Guid.NewGuid()), role: UserRoles.SuperAdmin, isOwner: true);
            Assert.Throws<VgAuto.Core.Domain.UserException>(() => owner.Disable());
            Assert.Throws<VgAuto.Core.Domain.UserException>(() => owner.ChangeRole(UserRoles.Admin));
        }
    }
}

namespace VgAuto.Tests.Unit
{
    public class CurrencyTests
    {
        [Theory]
        [InlineData(1234.5, "CAD", "$1,234.50")]
        [InlineData(1234.5, "USD", "$1,234.50")]
        [InlineData(1234.5, "CNY", "¥1,234.50")]
        [InlineData(1234.5, "JPY", "￥1,235")]
        public void Amounts_are_formatted_like_in_the_home_country(double amount, string code, string expected)
        {
            Assert.Equal(expected, VgAuto.Core.Domain.Currencies.Format((decimal)amount, code));
        }

        [Fact]
        public void Euro_uses_comma_decimals_and_unknown_codes_fall_back_to_the_default()
        {
            Assert.Contains("1.234,50", VgAuto.Core.Domain.Currencies.Format(1234.5m, "EUR"));
            Assert.Equal("CAD", VgAuto.Core.Domain.Currencies.Normalize("xyz"));
            Assert.Equal("USD", VgAuto.Core.Domain.Currencies.Normalize(" usd "));
        }
    }
}
