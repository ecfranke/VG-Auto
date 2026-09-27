using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using NHibernate;
using VgAuto.Core.Application;
using VgAuto.Core.Application.Authentication;
using VgAuto.Core.Application.Authorization;
using VgAuto.Core.Application.Database;
using VgAuto.Core.Application.Extensions;
using VgAuto.Core.Application.Model;
using VgAuto.Core.Application.RateLimiting;
using VgAuto.Core.Domain;

namespace VgAuto.Http.Api.Controllers
{
    /// <summary>
    /// User administration used by the /admin pages. Administrators manage normal users; super
    /// administrators also manage administrators and roles; the owner account can only be changed
    /// by itself (see <see cref="AdminPermissions"/>). Every change is written to the audit log.
    /// </summary>
    [TenantRateLimit]
    [Authorize(Policy = "ServerSidePolicy")]
    [Route("api/admin")]
    [ApiController]
    public class AdminController : ControllerBase
    {
        private static readonly Regex UserNamePattern = new(@"^[A-Za-z0-9._@-]{3,64}$", RegexOptions.Compiled);

        private readonly IUserRepository users;
        private readonly ISession session;
        private readonly IAdminAuditLog audit;
        private readonly IExternalLoginRepository externalLogins;
        private readonly AuthenticationOptions authOptions;

        public AdminController(IUserRepository users, ISession session, IAdminAuditLog audit,
            IExternalLoginRepository externalLogins, IOptions<AuthenticationOptions> authOptions)
        {
            this.users = users;
            this.session = session;
            this.audit = audit;
            this.externalLogins = externalLogins;
            this.authOptions = authOptions.Value;
        }

        // ---------------------------------------------------------------- models

        public record MeDto(string UserName, string FullName, string Role, bool IsOwner, bool IsAdmin);

        public record AdminUserDto(
            Guid EmployeeId, string FirstName, string LastName, string Email, string Phone, string Profession, string Description,
            bool HasAccount, string UserName, string Role, bool IsOwner, bool Disabled, bool Locked, bool MustChangePassword,
            bool MicrosoftLinked, bool IsSelf, IReadOnlyList<string> AllowedActions);

        public record EmployeeInput(string FirstName, string LastName, string Email, string Phone, string Profession, string Description);

        public record CreateUserInput(string FirstName, string LastName, string Email, string Phone, string Profession, string Description,
            bool CreateAccount, string UserName, string Password, string Role);

        public record EditUserInput(string FirstName, string LastName, string Email, string Phone, string Profession, string Description, string UserName);

        public record AccountInput(string UserName, string Password, string Role);

        public record PasswordInput(string Password);

        public record RoleInput(string Role);

        public record CreatedDto(Guid EmployeeId, string TemporaryPassword);

        // ---------------------------------------------------------------- current account

        /// <summary>Role of the signed in account; available to everybody (the web app shows admin links with it).</summary>
        [HttpGet("me")]
        public ActionResult<MeDto> Me()
        {
            var me = this.CurrentAccount();
            if (me == null) return Unauthorized();
            var employee = session.Get<Employee>(me.Id.EmployeeId);
            return new MeDto(me.UserName, employee?.Name ?? me.UserName, me.Role, me.IsOwner, me.IsAdmin);
        }

        // ---------------------------------------------------------------- users

        [RequireAdmin]
        [HttpGet("users")]
        public async Task<ActionResult<IEnumerable<AdminUserDto>>> List()
        {
            var me = this.CurrentAccount();
            var accounts = users.GetAllByTenant(this.TenantName()).ToDictionary(u => u.Id.EmployeeId);
            var employees = session.Query<Employee>().ToList();
            var result = new List<AdminUserDto>();
            foreach (var employee in employees.OrderBy(e => e.FirstName).ThenBy(e => e.LastName))
            {
                accounts.TryGetValue(employee.Id, out var account);
                result.Add(await ToDto(me, employee, account));
            }
            return result;
        }

        [RequireAdmin]
        [HttpGet("users/{employeeId:guid}")]
        public async Task<ActionResult<AdminUserDto>> Get(Guid employeeId)
        {
            var (employee, account) = Load(employeeId);
            if (employee == null) return NotFound();
            return await ToDto(this.CurrentAccount(), employee, account);
        }

        [RequireAdmin]
        [HttpPost("users")]
        public async Task<ActionResult<CreatedDto>> Create([FromBody] CreateUserInput input)
        {
            var me = this.CurrentAccount();
            var employee = new Employee(Required(input.FirstName, "First name"), Required(input.LastName, "Last name"), DateTime.UtcNow,
                Clean(input.Phone), Clean(input.Email), Clean(input.Profession), Clean(input.Description));

            string temporaryPassword = null;
            if (input.CreateAccount)
            {
                var role = UserRoles.Normalize(input.Role);
                Allow(me, new AdminTarget(false, UserRoles.User, false, false), AdminAction.CreateAccount, role);
                ValidateNewAccount(input.UserName, input.Email);
                session.Save(employee);
                session.Flush();
                temporaryPassword = AddAccount(employee, input.UserName, input.Password, input.Email, role);
                await Log("user.create", input.UserName.Trim(), $"{employee.Name}, role {role}");
            }
            else
            {
                session.Save(employee);
                session.Flush();
                await Log("employee.create", null, employee.Name);
            }
            return new CreatedDto(employee.Id, temporaryPassword);
        }

        [RequireAdmin]
        [HttpPut("users/{employeeId:guid}")]
        public async Task<IActionResult> Edit(Guid employeeId, [FromBody] EditUserInput input)
        {
            var me = this.CurrentAccount();
            var (employee, account) = Load(employeeId);
            if (employee == null) return NotFound();
            Allow(me, TargetOf(me, employee, account), AdminAction.EditProfile);

            employee.Change(Required(input.FirstName, "First name"), Required(input.LastName, "Last name"),
                Clean(input.Phone), Clean(input.Email), Clean(input.Profession), Clean(input.Description));
            session.Update(employee);

            var changes = new List<string>();
            if (account != null)
            {
                var userName = Clean(input.UserName) ?? account.UserName;
                if (userName != account.UserName)
                {
                    ValidateUserName(userName);
                    changes.Add($"username {account.UserName} -> {userName}");
                    account.ChangeUserName(userName);
                }
                var email = Clean(input.Email);
                if (!string.Equals(email, account.Email, StringComparison.OrdinalIgnoreCase))
                {
                    RequireEmailForAccount(email);
                    changes.Add("email");
                    account.ChangeEmail(email);
                }
                users.Update(account);
            }
            await Log("user.edit", account?.UserName, string.Join(", ", changes.Prepend(employee.Name)));
            return Ok();
        }

        /// <summary>Creates a login for an employee that has none yet (for example a mechanic).</summary>
        [RequireAdmin]
        [HttpPost("users/{employeeId:guid}/account")]
        public async Task<ActionResult<CreatedDto>> CreateAccount(Guid employeeId, [FromBody] AccountInput input)
        {
            var me = this.CurrentAccount();
            var (employee, account) = Load(employeeId);
            if (employee == null) return NotFound();
            var role = UserRoles.Normalize(input.Role);
            Allow(me, TargetOf(me, employee, account), AdminAction.CreateAccount, role);
            ValidateNewAccount(input.UserName, employee.Email);
            var temporaryPassword = AddAccount(employee, input.UserName, input.Password, employee.Email, role);
            await Log("user.create", input.UserName.Trim(), $"{employee.Name}, role {role}");
            return new CreatedDto(employee.Id, temporaryPassword);
        }

        [RequireAdmin]
        [HttpPost("users/{employeeId:guid}/password")]
        public async Task<ActionResult<CreatedDto>> ResetPassword(Guid employeeId, [FromBody] PasswordInput input)
        {
            var (me, employee, account) = Authorize(employeeId, AdminAction.ResetPassword);
            var password = string.IsNullOrWhiteSpace(input?.Password) ? GeneratePassword() : input.Password;
            var policyError = PasswordPolicy.Validate(password, account.UserName);
            if (policyError != null) throw new UserException(policyError);
            account.ResetPassword(PasswordHasher.getHash(password));
            users.Update(account);
            await Log("user.password_reset", account.UserName, "temporary password set, must be changed at next sign in");
            return new CreatedDto(employee.Id, string.IsNullOrWhiteSpace(input?.Password) ? password : null);
        }

        [RequireAdmin]
        [HttpPost("users/{employeeId:guid}/unlock")]
        public async Task<IActionResult> Unlock(Guid employeeId)
        {
            var (_, _, account) = Authorize(employeeId, AdminAction.Unlock);
            account.Unlock();
            users.Update(account);
            await Log("user.unlock", account.UserName);
            return Ok();
        }

        [RequireAdmin]
        [HttpPost("users/{employeeId:guid}/disable")]
        public async Task<IActionResult> Disable(Guid employeeId)
        {
            var (_, _, account) = Authorize(employeeId, AdminAction.Disable);
            account.Disable();
            users.Update(account);
            await Log("user.disable", account.UserName);
            return Ok();
        }

        [RequireAdmin]
        [HttpPost("users/{employeeId:guid}/enable")]
        public async Task<IActionResult> Enable(Guid employeeId)
        {
            var (_, _, account) = Authorize(employeeId, AdminAction.Enable);
            account.Enable();
            users.Update(account);
            await Log("user.enable", account.UserName);
            return Ok();
        }

        [RequireAdmin]
        [HttpDelete("users/{employeeId:guid}/microsoft")]
        public async Task<IActionResult> UnlinkMicrosoft(Guid employeeId)
        {
            var (_, _, account) = Authorize(employeeId, AdminAction.UnlinkMicrosoft);
            await externalLogins.RemoveAsync(account.Id, AuthService.MicrosoftProvider);
            await Log("user.microsoft_unlink", account.UserName);
            return Ok();
        }

        [RequireAdmin]
        [HttpPut("users/{employeeId:guid}/role")]
        public async Task<IActionResult> ChangeRole(Guid employeeId, [FromBody] RoleInput input)
        {
            var role = input?.Role?.Trim().ToLowerInvariant();
            var (_, _, account) = Authorize(employeeId, AdminAction.ChangeRole, role);
            if (account.Role == role) return Ok();
            var previous = account.Role;
            account.ChangeRole(role);
            users.Update(account);
            await Log("user.role", account.UserName, $"{previous} -> {role}");
            return Ok();
        }

        // ---------------------------------------------------------------- audit log

        public record AuditPageDto(IReadOnlyList<AuditEntry> Items, int Total);

        [RequireAdmin]
        [HttpGet("audit")]
        public async Task<ActionResult<AuditPageDto>> Audit(int limit = 50, int offset = 0)
        {
            var (items, total) = await audit.PageAsync(this.TenantName(), limit, offset);
            return new AuditPageDto(items, total);
        }

        // ---------------------------------------------------------------- helpers

        private (Employee Employee, User Account) Load(Guid employeeId)
        {
            var employee = session.Get<Employee>(employeeId);
            if (employee == null) return (null, null);
            return (employee, users.GetBy(new UserIdentifier(this.TenantName(), employeeId)));
        }

        private (User Me, Employee Employee, User Account) Authorize(Guid employeeId, AdminAction action, string newRole = null)
        {
            var me = this.CurrentAccount();
            var (employee, account) = Load(employeeId);
            if (employee == null) throw new UserException("Employee not found.");
            Allow(me, TargetOf(me, employee, account), action, newRole);
            return (me, employee, account);
        }

        private static AdminTarget TargetOf(User me, Employee employee, User account) =>
            new(account != null, account?.Role ?? UserRoles.User, account?.IsOwner ?? false, me != null && me.Id.EmployeeId == employee.Id);

        private static void Allow(User me, AdminTarget target, AdminAction action, string newRole = null)
        {
            var reason = AdminPermissions.Check(new AdminActor(me?.Role, me?.IsOwner ?? false), target, action, newRole);
            if (reason != null) throw new UserException(reason);
        }

        private async Task<AdminUserDto> ToDto(User me, Employee employee, User account)
        {
            var target = TargetOf(me, employee, account);
            var actor = new AdminActor(me?.Role, me?.IsOwner ?? false);
            var allowed = Enum.GetValues<AdminAction>()
                .Where(a => AdminPermissions.Check(actor, target, a, a == AdminAction.ChangeRole ? UserRoles.User : null) == null)
                .Select(a => a.ToString())
                .ToList();
            var microsoft = account != null && (await externalLogins.GetForUserAsync(account.Id)).Any();
            return new AdminUserDto(employee.Id, employee.FirstName, employee.LastName, account?.Email ?? employee.Email, employee.Phone,
                employee.Proffession, employee.Description, account != null, account?.UserName, account?.Role, account?.IsOwner ?? false,
                account?.Disabled ?? false, account?.IsLockedOut(DateTime.UtcNow) ?? false, account?.MustChangePassword ?? false,
                microsoft, target.IsSelf, allowed);
        }

        private string AddAccount(Employee employee, string userName, string password, string email, string role)
        {
            var generated = string.IsNullOrWhiteSpace(password);
            var plain = generated ? GeneratePassword() : password;
            var policyError = PasswordPolicy.Validate(plain, userName.Trim());
            if (policyError != null) throw new UserException(policyError);
            users.Add(new User(userName.Trim(), PasswordHasher.getHash(plain), Clean(email), false, null,
                new UserIdentifier(this.TenantName(), employee.Id), mustChangePassword: true, role: role));
            return generated ? plain : null;
        }

        private void ValidateNewAccount(string userName, string email)
        {
            ValidateUserName(userName?.Trim());
            RequireEmailForAccount(Clean(email));
        }

        private void ValidateUserName(string userName)
        {
            if (string.IsNullOrWhiteSpace(userName) || !UserNamePattern.IsMatch(userName))
                throw new UserException("Username must be 3-64 characters: letters, digits, . _ @ -");
            if (users.GetBy(userName) != null) throw new UserException($"Username '{userName}' is already taken.");
        }

        private void RequireEmailForAccount(string email)
        {
            if (string.IsNullOrWhiteSpace(email) && authOptions.EmailCode.RequireForPasswordLogin && !authOptions.EmailCode.AllowUsersWithoutEmail)
                throw new UserException("An email address is required: sign in codes are sent to it.");
        }

        private Task Log(string action, string target, string details = null) =>
            audit.WriteAsync(this.TenantName(), this.CurrentAccount()?.UserName ?? this.UserName(), action, target, details);

        private static string Required(string value, string field) =>
            string.IsNullOrWhiteSpace(value) ? throw new UserException($"{field} is required.") : value.Trim();

        private static string Clean(string value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

        /// <summary>A random temporary password that satisfies the password policy.</summary>
        internal static string GeneratePassword()
        {
            const string alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz23456789";
            return string.Create(14, 0, (span, _) =>
            {
                for (int i = 0; i < span.Length; i++) span[i] = alphabet[RandomNumberGenerator.GetInt32(alphabet.Length)];
            });
        }
    }
}
