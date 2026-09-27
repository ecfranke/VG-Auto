using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace Carmasters.Core.Application.Database
{
    public enum DatabaseProvider
    {
        PostgreSql,
        MySql
    }

    /// <summary>
    /// Small SQL dialect layer so hand written (Dapper) SQL can run on PostgreSQL and MySQL.
    /// All values coming from users must be passed as parameters, never through these helpers.
    /// </summary>
    public abstract class SqlDialect
    {
        private static SqlDialect current = new PostgreSqlDialect();

        /// <summary>Dialect of the configured database. Set once at startup.</summary>
        public static SqlDialect Current => current;

        public static SqlDialect For(DatabaseProvider provider) => provider switch
        {
            DatabaseProvider.PostgreSql => new PostgreSqlDialect(),
            DatabaseProvider.MySql => new MySqlDialect(),
            _ => throw new NotSupportedException(provider.ToString())
        };

        public static void Use(DatabaseProvider provider)
        {
            current = For(provider);
        }

        public abstract DatabaseProvider Provider { get; }

        /// <summary>
        /// Rewrites schema qualified table names (domain.x, tenant_config.x, public.user) for the target database.
        /// </summary>
        public abstract string Sql(string sql);

        /// <summary>Physical table name for a schema/table pair.</summary>
        public abstract string TableName(string schema, string table);

        /// <summary>Schema to use in ORM mappings, null when the database has no schemas.</summary>
        public abstract string Schema(string schema);

        /// <summary>Case insensitive LIKE. <paramref name="parameterName"/> must be a bound parameter.</summary>
        public abstract string ILike(string expression, string parameterName);

        /// <summary>String concatenation of SQL expressions.</summary>
        public abstract string Concat(params string[] expressions);

        public abstract string CastToText(string expression);

        /// <summary>Aggregates text values, separated by a constant separator.</summary>
        public abstract string StringAgg(string expression, string separator);

        /// <summary>Builds a JSON object from (key, sql expression) pairs.</summary>
        public abstract string JsonObject(params (string Key, string Expression)[] properties);

        /// <summary>timestamp + n days.</summary>
        public abstract string AddDays(string timestampExpression, string daysExpression);

        /// <summary>Formats a date column as MM-YYYY.</summary>
        public abstract string FormatMonthYear(string dateExpression);

        public abstract string CurrentTimestamp { get; }

        /// <summary>A timestamp inside a JSON object, formatted as ISO 8601 UTC.</summary>
        public virtual string JsonTimestamp(string expression) => expression;

        /// <summary>A boolean column inside a JSON object (true/false, not 1/0).</summary>
        public virtual string JsonBool(string expression) => expression;

        public virtual string Paging(string limitParameter, string offsetParameter) => $"LIMIT {limitParameter} OFFSET {offsetParameter}";

        /// <summary>Escapes LIKE wildcards in user supplied text and wraps it with %.</summary>
        public static string ContainsPattern(string text)
        {
            if (text == null) return "%";
            var escaped = text.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_");
            return $"%{escaped}%";
        }

        protected static string Literal(string text) => "'" + text.Replace("'", "''") + "'";
    }

    public class PostgreSqlDialect : SqlDialect
    {
        public override DatabaseProvider Provider => DatabaseProvider.PostgreSql;
        public override string Sql(string sql) => sql;
        public override string TableName(string schema, string table) => $"{schema}.{table}";
        public override string Schema(string schema) => schema;
        public override string ILike(string expression, string parameterName) => $"{expression} ILIKE {parameterName}";
        public override string Concat(params string[] expressions) => "(" + string.Join(" || ", expressions) + ")";
        public override string CastToText(string expression) => $"CAST({expression} AS text)";
        public override string StringAgg(string expression, string separator) => $"string_agg({expression}, {Literal(separator)})";
        public override string JsonObject(params (string Key, string Expression)[] properties) =>
            "json_build_object(" + string.Join(", ", properties.Select(p => $"{Literal(p.Key)}, {p.Expression}")) + ")";
        public override string AddDays(string timestampExpression, string daysExpression) => $"({timestampExpression} + {daysExpression} * interval '1 day')";
        public override string FormatMonthYear(string dateExpression) => $"TO_CHAR({dateExpression}, 'MM-YYYY')";
        public override string CurrentTimestamp => "CURRENT_TIMESTAMP";
    }

    public class MySqlDialect : SqlDialect
    {
        // domain.work -> work, tenant_config.pricing -> tenant_config_pricing, public.user -> app_user
        private static readonly Regex SchemaQualified = new Regex(@"\b(domain|tenant_config|public)\.(\w+)\b", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public override DatabaseProvider Provider => DatabaseProvider.MySql;

        public override string Sql(string sql) =>
            SchemaQualified.Replace(sql, m => TableName(m.Groups[1].Value, m.Groups[2].Value));

        public override string TableName(string schema, string table)
        {
            schema = schema.ToLowerInvariant();
            table = table.ToLowerInvariant();
            return schema switch
            {
                "domain" => table,
                "public" when table == "user" => "app_user",
                "public" => table,
                _ => $"{schema}_{table}"
            };
        }

        public override string Schema(string schema) => null;
        // utf8mb4_0900_ai_ci collation used by the schema is case insensitive
        public override string ILike(string expression, string parameterName) => $"{expression} LIKE {parameterName}";
        public override string Concat(params string[] expressions) => "CONCAT(" + string.Join(", ", expressions) + ")";
        public override string CastToText(string expression) => $"CAST({expression} AS CHAR)";
        public override string StringAgg(string expression, string separator) => $"GROUP_CONCAT({expression} SEPARATOR {Literal(separator)})";
        public override string JsonObject(params (string Key, string Expression)[] properties) =>
            "JSON_OBJECT(" + string.Join(", ", properties.Select(p => $"{Literal(p.Key)}, {p.Expression}")) + ")";
        public override string AddDays(string timestampExpression, string daysExpression) => $"DATE_ADD({timestampExpression}, INTERVAL {daysExpression} DAY)";
        public override string FormatMonthYear(string dateExpression) => $"DATE_FORMAT({dateExpression}, '%m-%Y')";
        public override string CurrentTimestamp => "CURRENT_TIMESTAMP(6)";
        public override string JsonTimestamp(string expression) => $"DATE_FORMAT({expression}, '%Y-%m-%dT%H:%i:%s.%fZ')";
        public override string JsonBool(string expression) => $"IF({expression}, CAST('true' AS JSON), CAST('false' AS JSON))";
    }
}
