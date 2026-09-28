using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using VgAuto.Core.Application.Database;
using VgAuto.Core.Domain;
using VgAuto.Http.Api.Models;
using Dapper;

namespace VgAuto.Core.Application.Services
{
    /// <summary>
    /// Builds paged list queries. Every user supplied value is sent as a query parameter;
    /// ordering only accepts keys from a whitelist.
    /// </summary>
    public class PageResultQuery<DTO>
    {
        public const int MaxLimit = 200;

        private string searchText;
        private int limit;
        private int offset;
        private bool desc;
        private string[] searchFields = Array.Empty<string>();
        private string selectSql;
        private string orderByKey;
        private IReadOnlyDictionary<string, string> sortableColumns = new Dictionary<string, string>();
        private string defaultOrderBy;
        private bool usePagingRestriction = true;
        private bool useWhereRestriction = true;
        private readonly List<string> whereExpressions = new List<string>();
        private readonly DynamicParameters parameters = new DynamicParameters();
        private readonly IDbConnection connection;
        private readonly SqlDialect dialect;

        public PageResultQuery(IDbConnection connection, SqlDialect dialect = null)
        {
            this.connection = connection;
            this.dialect = dialect ?? SqlDialect.Current;
        }

        public SqlDialect Dialect => dialect;

        public DynamicParameters Parameters => parameters;

        public PageResultQuery<DTO> FilterBy(string searchText)
        {
            this.searchText = searchText;
            return this;
        }

        public PageResultQuery<DTO> SelectSql(string selectSql)
        {
            this.selectSql = selectSql;
            return this;
        }

        /// <summary>
        /// Columns (trusted SQL expressions) searched with the free text. Each word must match at least one column.
        /// </summary>
        public PageResultQuery<DTO> SearchFields(params string[] fields)
        {
            this.searchFields = fields ?? Array.Empty<string>();
            return this;
        }

        /// <summary>
        /// Allowed sort keys mapped to trusted SQL expressions. Unknown keys fall back to <paramref name="defaultExpression"/>.
        /// </summary>
        public PageResultQuery<DTO> Sortable(IReadOnlyDictionary<string, string> columns, string defaultExpression)
        {
            this.sortableColumns = columns ?? new Dictionary<string, string>();
            this.defaultOrderBy = defaultExpression;
            return this;
        }

        public PageResultQuery<DTO> UsePagingRestriction(bool use)
        {
            this.usePagingRestriction = use;
            return this;
        }

        public PageResultQuery<DTO> UseWhereRestriction(bool use)
        {
            this.useWhereRestriction = use;
            return this;
        }

        public PageResultQuery<DTO> PageIs(string orderby, int limit, int offset, bool desc)
        {
            this.orderByKey = orderby;
            this.limit = Math.Clamp(limit <= 0 ? 30 : limit, 1, MaxLimit);
            this.offset = Math.Max(0, offset);
            this.desc = desc;
            return this;
        }

        /// <summary>Limits the rows to one company (<paramref name="column"/> is the company_id column of the main table).</summary>
        public PageResultQuery<DTO> ForCompany(string column, Guid companyId) => Where($"{column} = {Parameter(companyId)}");

        /// <summary>Adds a trusted SQL condition. Use <see cref="Parameter"/> for values.</summary>
        public PageResultQuery<DTO> Where(string expression)
        {
            whereExpressions.Add(expression);
            return this;
        }

        /// <summary>Registers a parameter value and returns its placeholder (e.g. @p0).</summary>
        public string Parameter(object value)
        {
            var name = "p" + parameters.ParameterNames.Count();
            parameters.Add(name, value);
            return "@" + name;
        }

        public string GetOrderBy()
        {
            string expression = null;
            if (!string.IsNullOrWhiteSpace(orderByKey) && sortableColumns.TryGetValue(orderByKey.Trim().ToLowerInvariant(), out var column))
            {
                expression = column;
            }
            expression ??= defaultOrderBy;
            if (string.IsNullOrWhiteSpace(expression)) return string.Empty;
            return $"ORDER BY {expression} {(desc ? "DESC" : "ASC")}";
        }

        public string GetPagingRestriction()
        {
            parameters.Add("page_offset", offset);
            parameters.Add("page_limit", limit + 1); // one extra row tells whether there are more
            return $"{GetOrderBy()} {dialect.Paging("@page_limit", "@page_offset")}";
        }

        public string GetWhereRestriction()
        {
            var expressions = new List<string>(whereExpressions);
            if (!string.IsNullOrWhiteSpace(searchText) && searchFields.Length > 0)
            {
                foreach (var word in new WildcardTokens(searchText).AllTokens())
                {
                    var p = Parameter(SqlDialect.ContainsPattern(word));
                    var anyField = string.Join(" OR ", searchFields.Select(f => dialect.ILike($"COALESCE({dialect.CastToText(f)}, '')", p)));
                    expressions.Add($"({anyField})");
                }
            }
            return expressions.Any() ? " WHERE " + string.Join(Environment.NewLine + " AND ", expressions) : string.Empty;
        }

        public string BuildSql()
        {
            return dialect.Sql($@"{selectSql}
                {(useWhereRestriction ? GetWhereRestriction() : string.Empty)}
                {(usePagingRestriction ? GetPagingRestriction() : string.Empty)}");
        }

        public PagedResult<DTO> ToResult()
        {
            var sql = BuildSql();
            var results = connection.Query<DTO>(sql, parameters).ToList();
            bool hasMore = false;
            if (results.Count > limit)
            {
                hasMore = true;
                results.RemoveAt(results.Count - 1);
            }
            return new PagedResult<DTO> { Items = results.ToArray(), HasMore = hasMore };
        }
    }

    public static class PageResultQueryExtensions
    {
        public static PageResultQuery<DTO> PageQuery<DTO>(this IRepository repository, string orderby, int limit, int offset, bool desc)
        {
            return new PageResultQuery<DTO>(repository.GetConnection())
                .PageIs(orderby, limit, offset, desc);
        }
    }
}
