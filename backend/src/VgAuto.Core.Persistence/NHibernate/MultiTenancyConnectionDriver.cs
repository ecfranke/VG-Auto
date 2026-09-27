using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Security.Claims;
using VgAuto.Core.Application;
using VgAuto.Core.Application.Configuration;
using VgAuto.Core.Application.Database;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NHibernate.Connection;

namespace VgAuto.Core.Persistence.Orm
{
     
    public class MultiTenancyConnectionDriver : ConnectionProvider, IMultiTenancyConnectionDriver
    {
        private readonly IConfiguration configuration;
        private readonly IHttpContextAccessor contextAccessor; 
        private readonly DbConnectionProvider connectionProvider;
        private readonly DbOptions options;

        public MultiTenancyConnectionDriver(IConfiguration configuration, IHttpContextAccessor contextAccessor, DbConnectionProvider serviceProvider) 
        {
            this.configuration = configuration;
            this.contextAccessor = contextAccessor; 
            this.connectionProvider = serviceProvider;
            options = new DbOptions(); configuration.GetSection("DbOptions").Bind(options);
        }
        public override void Configure(IDictionary<string, string> settings)
        {
            settings["connection.connection_string"] = BuildConnectionString();
            base.Configure(settings);
        }
         
        public override DbConnection GetConnection(string connectionString)
        { 
            var connection = connectionProvider.GetConnection(); //disposed by scope
            var principal = contextAccessor.HttpContext.User;
            if (principal == null) new Exception("Current ClaimsPrincipal is null");

            var tenantName = principal.Claims.FirstOrDefault(x => x.Type == ClaimTypes.Spn)?.Value;
            if (string.IsNullOrWhiteSpace(tenantName))
            {
                throw new Exception("Current principal is not valid, spn missing");
            }
            connection.ConnectionString = VgAuto.Core.Persistence.DbConnectionFactory.BuildConnectionString(options, new MultiTenancyDbName(options, tenantName));
            connection.Open();
            return connection;
        } 
        protected override void ConfigureDriver(IDictionary<string, string> settings)
        {
            base.ConfigureDriver(settings);
        }
        public string BuildConnectionString()
        {
            return VgAuto.Core.Persistence.DbConnectionFactory.BuildConnectionString(options, new MultiTenancyDbName(options, DbKind.Template));
        } 
    }
}
