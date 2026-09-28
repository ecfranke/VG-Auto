using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using VgAuto.Core.Application;
using VgAuto.Core.Application.Authorization;
using VgAuto.Core.Application.Database;
using VgAuto.Core.Application.Extensions;
using VgAuto.Core.Application.Model;
using VgAuto.Core.Application.RateLimiting;
using VgAuto.Core.Application.Services;
using VgAuto.Core.Domain;
using VgAuto.Core.Persistence;
using VgAuto.Http.Api.Models;
using Dapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NHibernate;
using NHibernate.Mapping;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace VgAuto.Http.Api.Controllers
{
    [TenantRateLimit]
    [Authorize(Policy = "ServerSidePolicy")]
    [Route("api/[controller]")]
    [ApiController]
    public class EmployeesController : BaseController<EmployeeDto, Employee>
    {
        private readonly IUserRepository userRepository;
        private readonly ISession session;

        public EmployeesController(IUserRepository userRepository, IRepository repository, ISession session) : base(repository)
        {
            this.userRepository = userRepository;
            this.session = session;
        }

        [HttpGet]
        public dynamic Get() 
        {
            return session.Query<Employee>().Select(x => new { 
               x.Id,
               x.Name
            }).ToList();
        }

        private static readonly System.Collections.Generic.Dictionary<string, string> SortColumns = new()
        {
            ["id"] = "id",
            ["firstname"] = "firstname",
            ["lastname"] = "lastname",
            ["email"] = "email",
        };

        [HttpGet("page")]
        public PagedResult<EmployeeDto> GetPage(string searchText, string orderby, int limit, int offset, bool desc)
        {
            var page =
                repository
                  .PageQuery<EmployeeDto>(orderby, limit, offset, desc)
                  .FilterBy(searchText)
                  .SearchFields("firstname", "lastname", "email", "phone")
                  .ForCompany("company_id", this.CompanyId())
                  .Sortable(SortColumns, "id")
                  .SelectSql(@"select employee.*,'' as username  from domain.employee ") //left join public.user u on u.employeeid = employee.id
                  .ToResult();

         
            var users =  userRepository.GetAllByTenant(this.TenantName());

            foreach (var entry in page.Items)
            {
                var user = users.Where(x => (Guid)x.Id.EmployeeId == entry.Id).SingleOrDefault();
                if (user != null)
                {
                    entry.UserName = user.UserName;
                }
            }

            return page;
        }

        protected override EmployeeDto Map(Employee entity) => VgAuto.Http.Api.Model.DtoMapper.ToDto(entity);

        protected override void AfterGet(EmployeeDto model, Employee domainObj)
        {
            base.AfterGet(model, domainObj);
            //check user 
            var userName = GetUser(domainObj)?.UserName;
            model.UserName = userName;
        }

        protected override void AfterSaved(EmployeeDto model, Employee domainObj)
        {
            base.AfterSaved(model, domainObj);
            if (!string.IsNullOrWhiteSpace(model.UserName))
            {
                // logins are created by administrators (normal users may only add mechanics without a login)
                var reason = AdminPermissions.Check(Actor(), new AdminTarget(false, UserRoles.User, false, false), AdminAction.CreateAccount, UserRoles.User);
                if (reason != null) throw new UserException(reason);
                if (string.IsNullOrWhiteSpace(model.Password))
                {
                    throw new UserException("Password is required when a username is set.");
                }
                var user = userRepository.GetBy(model.UserName);
                if (user != null) throw new UserException("This username is already taken.");

                var newUser = NewUser(model, domainObj);
                userRepository.Add(newUser);
            }
        }
         
        private User GetUser(Employee domainObj)
        {
            return userRepository.GetBy(new UserIdentifier(this.TenantName(), domainObj.Id));
        }
        private User NewUser(EmployeeDto model, Employee domainObj)
        {
            return new User(model.UserName, PasswordHasher.getHash(model.Password), model.Email, false, null, new UserIdentifier(this.TenantName(), domainObj.Id));
        }
        protected override Employee CreateFrom(EmployeeDto model)
        {
            var employee = new Employee(model.FirstName, model.LastName, DateTime.UtcNow, model.Phone, model.Email, model.Proffession, model.Description);

            return employee;
        }

        private AdminActor Actor()
        {
            var me = this.CurrentAccount();
            return new AdminActor(me?.Role, me?.IsOwner ?? false);
        }

        /// <summary>Employees with a login are managed in the administration (/admin).</summary>
        private void RequireAccountPermission(Employee employee, AdminAction action)
        {
            var account = GetUser(employee);
            if (account == null) return;
            var me = this.CurrentAccount();
            var reason = AdminPermissions.Check(Actor(), new AdminTarget(true, account.Role, account.IsOwner, me != null && me.Id.EmployeeId == employee.Id), action);
            if (reason != null) throw new UserException(reason);
        }

        protected override void BeforeDelete(Employee employee)
        {
            if (GetUser(employee) != null)
                throw new UserException("Employees with a login cannot be deleted. Disable the account in the administration instead.");
        }

        protected override void Edit(Employee employee, EmployeeDto model)
        {
            RequireAccountPermission(employee, AdminAction.EditProfile);
            employee.Change(model.FirstName, model.LastName, model.Phone, model.Email, model.Proffession, model.Description);
        }
    }
}
