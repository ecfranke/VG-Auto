using Carmasters.Core.Application.Configuration;
using Carmasters.Core.Application.Database;
using Carmasters.Core.Application.Services;
using Carmasters.Core.Domain;
using Carmasters.Core.Persistence.Postgres;
using Carmasters.Core.Persistence.Postgres.NHibernate;
using Carmasters.Core.Persistence.Postgres.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NHibernate;
using NHibernate.Mapping;
using System.Data.Common;
using System.Reflection;

namespace Carmasters.Core.Repository.Postgres
{
    public static class DependencyInjectionExtensions
    {
        static object lockObj = new object();
        public static IServiceCollection AddPersistanceServices(this IServiceCollection services, IConfiguration configuration)
        {
            Dapper.SqlMapper.AddTypeHandler(new Carmasters.Core.Application.Dapper.JsonNodeTypeHandler());
            var options = new DbOptions(); configuration.GetSection("DbOptions").Bind(options);
            SqlDialect.Use(options.Provider);
            var connectionFactory = new Carmasters.Core.Persistence.DbConnectionFactory(options);
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
            services.AddScoped<Carmasters.Core.Application.Authentication.IAuthChallengeRepository, Carmasters.Core.Persistence.AuthChallengeRepository>();
            services.AddScoped<Carmasters.Core.Application.Authentication.IExternalLoginRepository, Carmasters.Core.Persistence.ExternalLoginRepository>();
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
            services.AddScoped<Carmasters.Core.Application.Authorization.AuthTokenService>();
          
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