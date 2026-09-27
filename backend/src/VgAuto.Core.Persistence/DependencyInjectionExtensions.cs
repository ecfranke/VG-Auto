using VgAuto.Core.Application.Configuration;
using VgAuto.Core.Application.Database;
using VgAuto.Core.Application.Services;
using VgAuto.Core.Domain;
using VgAuto.Core.Persistence;
using VgAuto.Core.Persistence.Orm;
using VgAuto.Core.Persistence.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NHibernate;
using NHibernate.Mapping;
using System.Data.Common;
using System.Reflection;

namespace VgAuto.Core.Persistence
{
    public static class DependencyInjectionExtensions
    {
        static object lockObj = new object();
        public static IServiceCollection AddPersistanceServices(this IServiceCollection services, IConfiguration configuration)
        {
            Dapper.SqlMapper.AddTypeHandler(new VgAuto.Core.Application.Dapper.JsonNodeTypeHandler());
            var options = new DbOptions(); configuration.GetSection("DbOptions").Bind(options);
            SqlDialect.Use(options.Provider);
            var connectionFactory = new VgAuto.Core.Persistence.DbConnectionFactory(options);
            var multitenancyEnabled = options.MultiTenancy?.Enabled == true;
            var defaultFactory = default(ISessionFactory);
            var mappingAssemblies = new System.Collections.Generic.List<Assembly>() { typeof(UserDbMapping).Assembly };
            if (multitenancyEnabled)
            {
                defaultFactory = NNhibernateFactory.BuildSessionFactory(options.Provider, mappingAssemblies,
                    connectionFactory.ConnectionString(new MultiTenancyDbName(options, DbKind.Tenancy)));
            }
            else 
            {
                mappingAssemblies.Add(typeof(WorkMapping).Assembly);
                defaultFactory = NNhibernateFactory.BuildSessionFactory(options.Provider, mappingAssemblies, connectionFactory.ConnectionString(options.Name));
            }        

            var appFactory = default(ISessionFactory);
            
            services.AddSingleton<IDbConnectionFactory>(connectionFactory);
            services.AddScoped<IUserRepository, UserRepository>();
            services.AddScoped<VgAuto.Core.Application.Authentication.IAuthChallengeRepository, VgAuto.Core.Persistence.AuthChallengeRepository>();
            services.AddScoped<VgAuto.Core.Application.Authentication.IExternalLoginRepository, VgAuto.Core.Persistence.ExternalLoginRepository>();
            services.AddScoped<ISession>(x =>{

                if (!multitenancyEnabled) return defaultFactory.OpenSession();

                var user = x.GetRequiredService<Microsoft.AspNetCore.Http.IHttpContextAccessor>().HttpContext.User;
                if (user.Identity.IsAuthenticated) 
                {
                    if (appFactory == null)
                    {
                        lock (lockObj)
                        {
                            if (appFactory == null) //double if, if anyone was waiting it might have been initialized already
                            {
                                appFactory = NNhibernateFactory.BuildSessionFactory(options.Provider, new System.Collections.Generic.List<Assembly>() { typeof(WorkMapping).Assembly });
                            }
                        }
                    } 
                    return appFactory.OpenSession();
                }
                throw new System.Exception("Unable to open database session, user not authenticated.");
            });


            services.AddScoped<IRepository, GenericRepository>();
            services.AddScoped<VgAuto.Core.Application.Authorization.AuthTokenService>();
          
            services.AddScoped<ISequnceNumberProviderFactory, SequenceNumberProviderFactory>();
            services.AddScoped<InvoiceSequenceNumberProvider>();
            services.AddScoped<WorkSequenceNumberProvider>();
            services.AddScoped<EstimateSequenceNumberProvider>(); 
            services.AddScoped<UnitOfWorkAspect>();
            services.AddSingleton<DbConnectionProvider>();
            services.AddScoped<DbConnection>(x => connectionFactory.Create(null));
            services.AddSingleton<MultiTenancyConnectionDriver>();
            services.AddSingleton<DatabaseBackup>();
            services.AddScoped<ITenancyRepository, TenancyRepository>();
            return services;
        }
    }
     
}