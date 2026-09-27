using System;
using VgAuto.Core.Application;
using VgAuto.Core.Application.Authorization;
using VgAuto.Core.Application.Configuration;
using VgAuto.Core.Application.Model;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.FileProviders;
using Xunit;

namespace VgAuto.Tests.Unit
{
    public class AccountSecurityTests
    {
        [Theory]
        [InlineData("short", false)]
        [InlineData("carcare", false)]
        [InlineData("aaaaaaaaaaaa", false)]
        [InlineData("admin-password-1", false)] // contains user name
        [InlineData("Werkstatt-2026!x", true)]
        public void Password_policy(string password, bool ok)
        {
            Assert.Equal(ok, PasswordPolicy.Validate(password, "admin") == null);
        }

        [Fact]
        public void Account_is_locked_after_too_many_failures_and_unlocked_after_success()
        {
            var user = new User("u", "hash", null, false, null, new UserIdentifier("t", Guid.NewGuid()));
            var now = DateTime.UtcNow;
            for (int i = 0; i < User.MaxFailedLogins; i++) user.LoginFailed(now);

            Assert.True(user.IsLockedOut(now));
            Assert.False(user.IsLockedOut(now.Add(User.LockoutDuration).AddSeconds(1)));

            user.LoginSucceeded();
            Assert.False(user.IsLockedOut(now));
            Assert.Equal(0, user.FailedLoginCount);
        }

        [Fact]
        public void Changing_password_clears_forced_change()
        {
            var user = new User("u", "hash", null, false, null, new UserIdentifier("t", Guid.NewGuid()), mustChangePassword: true);
            user.ChangePassword("newhash");
            Assert.False(user.MustChangePassword);
        }

        [Fact]
        public void Changing_email_requires_new_verification()
        {
            var user = new User("u", "hash", "a@example.com", true, null, new UserIdentifier("t", Guid.NewGuid()));
            user.ChangeEmail("A@example.com");
            Assert.True(user.Validated);
            user.ChangeEmail("b@example.com");
            Assert.False(user.Validated);
        }

        [Fact]
        public void Secrets_are_compared_exactly()
        {
            Assert.True(AppJwtToken.SecretsEqual("abc", "abc"));
            Assert.False(AppJwtToken.SecretsEqual("abc", "abd"));
            Assert.False(AppJwtToken.SecretsEqual("abc", null));
            Assert.False(AppJwtToken.SecretsEqual("", ""));
        }

        [Fact]
        public void Startup_rejects_weak_or_placeholder_secrets()
        {
            var config = new ConfigurationBuilder().AddInMemoryCollection(new System.Collections.Generic.Dictionary<string, string>
            {
                ["JwtOptions:Secret"] = "too-short",
                ["JwtOptions:ConsumerSecret"] = "[your-server-secret]",
                ["Cors:Mode"] = "open",
            }).Build();

            var ex = Assert.Throws<InvalidOperationException>(() => StartupValidation.Validate(config, new Env("Production")));
            Assert.Contains("JwtOptions:Secret", ex.Message);
            Assert.Contains("placeholder", ex.Message);
            Assert.Contains("Cors:Mode", ex.Message);
        }

        private class Env : IHostEnvironment
        {
            public Env(string name) { EnvironmentName = name; }
            public string EnvironmentName { get; set; }
            public string ApplicationName { get; set; } = "test";
            public string ContentRootPath { get; set; } = "/";
            public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
        }
    }
}
