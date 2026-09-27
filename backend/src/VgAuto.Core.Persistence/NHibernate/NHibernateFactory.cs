using System.Collections.Generic;
using System.Reflection;
using VgAuto.Core.Application.Database;
using FluentNHibernate;
using FluentNHibernate.Cfg;
using NHibernate;
using NHibernate.Cfg;

namespace VgAuto.Core.Persistence.Orm
{
    public class NNhibernateFactory
    {
        /// <param name="connectionString">null = multi-tenancy, the connection is chosen per request</param>
        public static ISessionFactory BuildSessionFactory(DatabaseProvider provider, IEnumerable<Assembly> mappingAssemblies, string connectionString = null)
        {
            var cfg = new Configuration();
            cfg.DataBaseIntegration(i =>
            {
                if (provider == DatabaseProvider.MySql)
                {
                    i.Dialect<global::NHibernate.Dialect.MySQL8InnoDBDialect>();
                    i.Driver<global::NHibernate.Driver.MySqlConnector.MySqlConnectorDriver>();
                }
                else
                {
                    i.Dialect<global::NHibernate.Dialect.PostgreSQL83Dialect>();
                }

                if (connectionString == null)
                {
                    i.ConnectionProvider<MultiTenancyConnectionDriver>();
                }
                else
                {
                    i.ConnectionString = connectionString;
                }
            });

            foreach (var assembly in mappingAssemblies)
            {
                cfg.AddMappingsFromAssembly(assembly);
            }

            ApplyTableNaming(cfg, SqlDialect.For(provider));
            return cfg.BuildSessionFactory();
        }

        /// <summary>
        /// Mappings use PostgreSQL schemas (domain, tenant_config, public). Databases without schemas
        /// get flat table names instead (see <see cref="SqlDialect.TableName"/>).
        /// </summary>
        public static void ApplyTableNaming(Configuration cfg, SqlDialect dialect)
        {
            foreach (var persistentClass in cfg.ClassMappings)
            {
                var table = persistentClass.Table;
                if (table == null || string.IsNullOrEmpty(table.Schema)) continue;
                var schema = table.Schema;
                var mappedSchema = dialect.Schema(schema);
                if (mappedSchema == schema) continue;
                table.Name = dialect.TableName(schema, table.Name);
                table.Schema = mappedSchema;
            }
            foreach (var collection in cfg.CollectionMappings)
            {
                var table = collection.CollectionTable;
                if (table == null || string.IsNullOrEmpty(table.Schema)) continue;
                var schema = table.Schema;
                var mappedSchema = dialect.Schema(schema);
                if (mappedSchema == schema) continue;
                table.Name = dialect.TableName(schema, table.Name);
                table.Schema = mappedSchema;
            }
        }
    }
}
