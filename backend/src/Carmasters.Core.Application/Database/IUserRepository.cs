using Carmasters.Core.Application.Model;
using Carmasters.Core.Domain;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data.Common;
using System.Net.Mail;

namespace Carmasters.Core.Application.Database
{
    public interface IUserRepository
    {
        public User GetBy(string userName);
        public User GetByEmail(string email);
        /// <summary>All accounts with this email address (case insensitive).</summary>
        IReadOnlyList<User> GetAllByEmail(string email);
        public User GetBy(UserIdentifier id); 
        void Update(User user);
        void Add(User user);
        string GetFullName(string userName);
        IEnumerable<User> GetAllByTenant(string tenantName);
    }
}