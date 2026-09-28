using VgAuto.Core.Application;
using VgAuto.Core.Application.Configuration;
using VgAuto.Core.Application.Database;
using VgAuto.Core.Application.Model;
using VgAuto.Core.Domain;
using VgAuto.Http.Api.Models;
using Dapper;
using Microsoft.Extensions.Options;
using NHibernate;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Data.Common;
using System.Net.Mail;
using System.Reflection;
using System.Threading.Tasks;

namespace VgAuto.Core.Persistence
{
    /// <summary>
    /// TODO, transaction handling and separate connection handling .. kinda special case but without multitenancy it should work as normal .. needs to implemented better
    /// </summary>
    public class UserRepository :  IUserRepository
    {
        private readonly IDbConnectionFactory connections;
        private const string UserSelectQuery =
            "SELECT profile_image as ProfileImage, UserName, Password, TenantName, Email, Validated, EmployeeId, must_change_password as MustChangePassword, failed_login_count as FailedLoginCount, locked_until as LockedUntil, role as Role, is_owner as IsOwner, disabled as Disabled, company_id as CompanyId FROM public.user";

        public UserRepository(IDbConnectionFactory connections)
        {
            this.connections = connections;
        }

        public User GetBy(string userName)
        {
            return QuerySingleUser($"{UserSelectQuery} WHERE UserName = @UserName", new { UserName = userName });
        }

        public User GetByEmail(string email)
        {
            return QuerySingleUser($"{UserSelectQuery} WHERE Email = @Email", new { Email = email });
        }

        public IReadOnlyList<User> GetAllByEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email)) return Array.Empty<User>();
            using var connection = CreateConnection(GetUserListDatabase());
            return connection.Query<UserDto>(Sql($"{UserSelectQuery} WHERE LOWER(Email) = LOWER(@Email)"), new { Email = email.Trim() })
                .Select(ToUser)
                .ToList();
        }

        public User GetBy(UserIdentifier id)
        {
            return QuerySingleUser(
                $"{UserSelectQuery} WHERE EmployeeId = @EmployeeId AND TenantName = @TenantName",
                new { EmployeeId = id.EmployeeId, TenantName = id.TenantName });
        }

        /// <summary>
        /// Gets the full name of a user by their username
        /// </summary>
        public string GetFullName(string userName)
        {
            if (string.IsNullOrEmpty(userName))
                throw new ArgumentNullException(nameof(userName));

            // Get the user to find the tenant and employee ID
            var user = GetBy(userName);
            if (user == null)
                return null;

            return GetFullName(user.Id);
        }

        /// <summary>
        /// Gets the full name of a user by their user ID
        /// </summary>
        private string GetFullName(UserIdentifier id)
        {
            if (id == null)
                throw new ArgumentNullException(nameof(id));

            // Create connection to the appropriate database
            using (var connection = CreateConnection(GetUserDatabase(id.TenantName)))
            {
                // Query the employee record to get the name
                var fullName = connection.QuerySingleOrDefault<string>(
                    Sql("SELECT CONCAT(FirstName, ' ', LastName) FROM domain.employee WHERE Id = @EmployeeId"),
                    new { EmployeeId = id.EmployeeId });

                return fullName;
            }
        }

        /// <summary>
        /// Helper method to execute a query for a single user
        /// </summary>
        private User QuerySingleUser(string query, object parameters)
        {
            using var connection = CreateConnection(GetUserListDatabase());

            var user = connection.QuerySingleOrDefault<UserDto>(Sql(query), parameters);

            if (user == null)
                return null;

            return ToUser(user);
        }

        public void Update(User user)
        {
            if (user == null)
                throw new ArgumentNullException(nameof(user));

            if (user.Id == null)
                throw new ArgumentException("Cannot update user without a valid identifier");

            using (var connection = CreateConnection(GetUserListDatabase()))
            {
                // Begin a transaction to ensure data consistency
                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {
                        // Update the user record
                        int rowsAffected = connection.Execute(
                            Sql(@"UPDATE public.user 
                              SET UserName = @UserName, 
                                  Password = @Password, 
                                  Email = @Email, 
                                  Validated = @Validated, 
                                  Profile_Image = @ProfileImage,
                                  must_change_password = @MustChangePassword,
                                  failed_login_count = @FailedLoginCount,
                                  locked_until = @LockedUntil,
                                  role = @Role,
                                  is_owner = @IsOwner,
                                  disabled = @Disabled,
                                  company_id = @CompanyId
                              WHERE TenantName = @TenantName AND EmployeeId = @EmployeeId"),
                            new
                            {
                                UserName = user.UserName,
                                Password = user.Password,
                                Email = user.Email,
                                Validated = user.Validated,
                                ProfileImage = user.ProfileImage,
                                MustChangePassword = user.MustChangePassword,
                                FailedLoginCount = user.FailedLoginCount,
                                LockedUntil = user.LockedUntil,
                                Role = user.Role,
                                IsOwner = user.IsOwner,
                                Disabled = user.Disabled,
                                CompanyId = user.CompanyId,
                                TenantName = user.Id.TenantName,
                                EmployeeId = user.Id.EmployeeId
                            }, transaction);

                        if (rowsAffected == 0)
                        {
                            throw new EntityNotFoundException($"User with ID {user.Id.TenantName}/{user.Id.EmployeeId} not found");
                        }

                        transaction.Commit();
                    }
                    catch (Exception)
                    {
                        transaction.Rollback();
                        throw;
                    }
                }
            }
        }

        public void Add(User user)
        {
            if (user == null) throw new ArgumentNullException(nameof(user));
            if (user.Id == null) throw new ArgumentException("Cannot add user without a valid identifier");

            using var connection = CreateConnection(GetUserListDatabase());
            connection.Execute(
                Sql(@"INSERT INTO public.user (username, password, tenantname, email, validated, profile_image, employeeid, must_change_password, role, is_owner, disabled, company_id)
                  VALUES (@UserName, @Password, @TenantName, @Email, @Validated, @ProfileImage, @EmployeeId, @MustChangePassword, @Role, @IsOwner, @Disabled, @CompanyId)"),
                new
                {
                    user.UserName,
                    user.Password,
                    TenantName = user.Id.TenantName,
                    user.Email,
                    user.Validated,
                    user.ProfileImage,
                    EmployeeId = user.Id.EmployeeId,
                    user.MustChangePassword,
                    user.Role,
                    user.IsOwner,
                    user.Disabled,
                    user.CompanyId
                });
        }

        public IEnumerable<User> GetAllByTenant(string tenantName)
        {
            using (var connection = CreateConnection(GetUserListDatabase()))
            {
                var users = connection.Query<UserDto>(
                    Sql($"{UserSelectQuery} WHERE TenantName = @TenantName"),
                    new { TenantName = tenantName });

                foreach (var user in users)
                {
                    yield return ToUser(user);
                }
            }
        }

        private string GetUserListDatabase() => connections.UserListDatabase;

        private string GetUserDatabase(string tenantName) => connections.TenantDatabase(tenantName);

        private DbConnection CreateConnection(string databaseName) => connections.Open(databaseName);

        private static string Sql(string sql) => SqlDialect.Current.Sql(sql);

        private static User ToUser(UserDto user) => new User(
            user.UserName,
            user.Password,
            user.Email,
            user.Validated,
            user.ProfileImage,
            new UserIdentifier(user.TenantName, user.EmployeeId),
            user.MustChangePassword,
            user.FailedLoginCount,
            user.LockedUntil,
            user.Role,
            user.IsOwner,
            user.Disabled,
            user.CompanyId == Guid.Empty ? null : user.CompanyId);
    }
}