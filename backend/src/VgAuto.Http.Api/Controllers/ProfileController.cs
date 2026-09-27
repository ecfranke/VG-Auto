using System;
using VgAuto.Core.Application.Authorization;
using VgAuto.Core.Application.Database;
using VgAuto.Core.Application.Extensions;
using VgAuto.Core.Application.Model;
using VgAuto.Core.Application.RateLimiting;
using VgAuto.Core.Domain;
using VgAuto.Http.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using System.Linq;
using System.Threading.Tasks;
namespace VgAuto.Http.Api.Controllers
{
    [TenantRateLimit]
    [Authorize(Policy = "ServerSidePolicy")]
    [Route("api/[controller]")]
    [ApiController]
    public class ProfileController : ControllerBase
    {
        private readonly IUserRepository repository;
        private readonly NHibernate.ISession session;

        private readonly AuthTokenService tokens;

        public ProfileController(IUserRepository repository, NHibernate.ISession session, AuthTokenService tokens)
        {
            this.repository = repository;
            this.session = session;
            this.tokens = tokens;
        }
         
        [HttpGet()]
        public IActionResult Get()
        {
            if (this.EmployeeId() == null) return NotFound();
            var employee = session.Get<Employee>(this.EmployeeId().GetValueOrDefault());
            if (employee == null) return NotFound();
            var user = repository.GetBy(new UserIdentifier(this.TenantName(), employee.Id));
            return Ok(new UserProfileDto(employee.FirstName, employee.LastName,user.Email, user.UserName, user.ProfileImage == null? null: Convert.ToBase64String(user.ProfileImage)));
        }

        [HttpPut]
        public IActionResult Put([FromBody] UserProfileDto profile)
        {
             
            if (this.EmployeeId() == null) return NotFound();
            var employee = session.Get<Employee>(this.EmployeeId().GetValueOrDefault());
            if (employee == null) return NotFound();
            var user = repository.GetBy(new UserIdentifier(this.TenantName(), employee.Id));

            if (profile.UserName != user.UserName)
            {
                if (string.IsNullOrWhiteSpace(user.UserName))
                {
                    throw new UserException($"Username cannot be empty.");
                }
                if (repository.GetBy(profile.UserName) != null)
                {
                    throw new UserException($"Username '{profile.UserName}' is taken.");
                }
                user.ChangeUserName(profile.UserName);
            }

            user.ChangeEmail(profile.Email);
             
            var profileImage = string.IsNullOrEmpty(profile.ProfileImageBase64) ? user.ProfileImage : Convert.FromBase64String(profile.ProfileImageBase64);
            user.ChangeProfileImage(profileImage);
            repository.Update(user);

            employee.ChangeName(profile.FirstName, profile.LastName);
            session.Update(employee);

            return Ok(); 
        }

        [HttpPut("changepassword")]
        public IActionResult ChangePassword([FromBody] PasswordChangeDto model)
        {
            if (this.EmployeeId() == null) return NotFound();
            var user = repository.GetBy(new UserIdentifier(this.TenantName(), this.EmployeeId().GetValueOrDefault()));
            if (user == null) return NotFound();

            if (model.NewPassword != model.ConfirmPassword)
            {
                throw new UserException("New password does not match with confirmed password");
            }

            var policyError = PasswordPolicy.Validate(model.NewPassword, user.UserName);
            if (policyError != null) throw new UserException(policyError);

            if (!PasswordHasher.verifyHash(model.CurrentPassword ?? string.Empty, user.Password))
            {
                throw new UserException("Current password does not match.");
            }

            if (PasswordHasher.verifyHash(model.NewPassword, user.Password))
            {
                throw new UserException("New password must be different from the current one.");
            }

            user.ChangePassword(PasswordHasher.getHash(model.NewPassword));
            repository.Update(user);

            // fresh tokens without the "password change required" restriction
            return Ok(tokens.Issue(user, "pwd"));
        }

        /// <summary>External accounts (Microsoft) linked to the current user.</summary>
        [HttpGet("externallogins")]
        public async Task<IActionResult> ExternalLogins([FromServices] VgAuto.Core.Application.Authentication.IExternalLoginRepository externalLogins)
        {
            if (this.EmployeeId() == null) return NotFound();
            var logins = await externalLogins.GetForUserAsync(new UserIdentifier(this.TenantName(), this.EmployeeId().Value));
            return Ok(logins.Select(l => new { l.Provider, l.Email, l.CreatedAt }));
        }

        [HttpDelete("externallogins/{provider}")]
        public async Task<IActionResult> RemoveExternalLogin(string provider, [FromServices] VgAuto.Core.Application.Authentication.IExternalLoginRepository externalLogins)
        {
            if (this.EmployeeId() == null) return NotFound();
            await externalLogins.RemoveAsync(new UserIdentifier(this.TenantName(), this.EmployeeId().Value), provider);
            return Ok();
        }

        [HttpDelete()]
        public  IActionResult DeleteAccount()
        {
            return Ok();
        }
    }

}
