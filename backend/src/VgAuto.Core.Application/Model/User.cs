
using VgAuto.Core.Application.Model;
using VgAuto.Core.Domain;
using NHibernate.Bytecode;
using System;
using System.Collections.Generic;

[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("VgAuto.Core.Persistence")]
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("VgAuto.Tests")]

namespace VgAuto.Core.Application.Model
{
}
namespace VgAuto.Core.Application
{
    public class User
    { 
        protected User() { }
        public User(string userName, string password, string email, bool validated, byte[] profileImage,  UserIdentifier id = null,
            bool mustChangePassword = false, int failedLoginCount = 0, DateTime? lockedUntil = null,
            string role = null, bool isOwner = false, bool disabled = false, Guid? companyId = null)
        {
            if (string.IsNullOrWhiteSpace(userName))
            {
                throw new ArgumentException($"'{nameof(userName)}' cannot be null or whitespace", nameof(userName));
            }

            if (string.IsNullOrWhiteSpace(password))
            {
                throw new ArgumentException($"'{nameof(password)}' cannot be null or whitespace", nameof(password));
            }
            Email = email;
            UserName = userName;
            Password = password;
            Validated = validated;
            ProfileImage = profileImage;
            Id = id;
            MustChangePassword = mustChangePassword;
            FailedLoginCount = failedLoginCount;
            LockedUntil = lockedUntil;
            Role = VgAuto.Core.Application.Authorization.UserRoles.Normalize(role);
            IsOwner = isOwner;
            Disabled = disabled;
            CompanyId = companyId ?? VgAuto.Core.Application.Database.ICompanyScope.FirstCompany;
        }

        /// <summary>Company the user works for; the user sees only its data.</summary>
        public virtual Guid CompanyId { get; protected set; }

        public virtual void MoveToCompany(Guid companyId) => CompanyId = companyId;

        public const int MaxFailedLogins = 10;
        public static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(15);

        public virtual bool MustChangePassword { get; protected set; }
        /// <summary>user, admin or superadmin (see <see cref="VgAuto.Core.Application.Authorization.UserRoles"/>).</summary>
        public virtual string Role { get; protected set; } = VgAuto.Core.Application.Authorization.UserRoles.User;
        /// <summary>The initial administrator. It cannot be disabled, demoted or changed by other administrators.</summary>
        public virtual bool IsOwner { get; protected set; }
        /// <summary>Disabled accounts cannot sign in and their sessions are rejected.</summary>
        public virtual bool Disabled { get; protected set; }

        public virtual bool IsAdmin => VgAuto.Core.Application.Authorization.UserRoles.IsAdmin(Role);

        public virtual void ChangeRole(string role)
        {
            if (!VgAuto.Core.Application.Authorization.UserRoles.IsValid(role)) throw new UserException("Unknown role.");
            if (IsOwner && role != VgAuto.Core.Application.Authorization.UserRoles.SuperAdmin) throw new UserException("The owner account must stay a super administrator.");
            Role = role;
        }

        public virtual void Disable()
        {
            if (IsOwner) throw new UserException("The owner account cannot be disabled.");
            Disabled = true;
        }

        public virtual void Enable() => Disabled = false;

        public virtual void Unlock()
        {
            FailedLoginCount = 0;
            LockedUntil = null;
        }

        /// <summary>Sets a temporary password chosen by an administrator; it must be replaced at the next sign in.</summary>
        public virtual void ResetPassword(string passwordHash)
        {
            Password = passwordHash;
            MustChangePassword = true;
            Unlock();
        }
        public virtual int FailedLoginCount { get; protected set; }
        public virtual DateTime? LockedUntil { get; protected set; }

        public virtual bool IsLockedOut(DateTime utcNow)
        {
            if (!LockedUntil.HasValue) return false;
            // values without a kind (e.g. MySQL DATETIME) are stored as UTC
            var until = LockedUntil.Value.Kind == DateTimeKind.Unspecified
                ? DateTime.SpecifyKind(LockedUntil.Value, DateTimeKind.Utc)
                : LockedUntil.Value.ToUniversalTime();
            return until > utcNow;
        }

        public virtual void LoginFailed(DateTime utcNow)
        {
            FailedLoginCount++;
            if (FailedLoginCount >= MaxFailedLogins)
            {
                LockedUntil = utcNow.Add(LockoutDuration);
                FailedLoginCount = 0;
            }
        }

        public virtual void LoginSucceeded()
        {
            FailedLoginCount = 0;
            LockedUntil = null;
        }

        public virtual void RequirePasswordChange()
        {
            MustChangePassword = true;
        }

        public virtual void MarkEmailValidated()
        {
            Validated = true;
        }

        public virtual byte[] ProfileImage { get; protected set; }
        public virtual string Email { get; protected set; }

        public virtual bool Validated { get;protected set; }
        public virtual string UserName { get; protected set; }
        public virtual string Password { get;protected set; }
        public virtual UserIdentifier Id { get; protected internal set; }

        public override bool Equals(object obj)
        {
            return obj is User user &&
                   Id == user.Id;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Id);
        }

        public virtual void ChangeEmail(string email)
        {
            if (!string.Equals(Email, email, StringComparison.OrdinalIgnoreCase))
            {
                // a new address has to be confirmed again (by a login code)
                Validated = false;
            }
            this.Email = email;
        }

        public virtual void ChangeProfileImage(byte[] profileImage)
        {

            const int fiveMb = 5 * 1024 * 1024; 
            var fileSize = profileImage == null?0: profileImage.Length;
            if (fileSize > fiveMb)
            {
                throw new UserException("Profile image is too big");
            }

            this.ProfileImage = profileImage;
        }

        public virtual void ChangePassword(string passwordHash)
        {
            this.Password = passwordHash;
            this.MustChangePassword = false;
            LoginSucceeded();
        }

        public virtual void ChangeUserName(string userName)
        {
            this.UserName = userName;
        }
    }
}