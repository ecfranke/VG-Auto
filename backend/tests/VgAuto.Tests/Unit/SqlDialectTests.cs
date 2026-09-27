using VgAuto.Core.Application.Database;
using Xunit;

namespace VgAuto.Tests.Unit
{
    public class SqlDialectTests
    {
        [Fact]
        public void MySql_rewrites_schema_qualified_tables()
        {
            var d = SqlDialect.For(DatabaseProvider.MySql);
            var sql = d.Sql("select * from domain.Work w join tenant_config.pricing p on 1=1 join public.user u on 1=1");
            Assert.Equal("select * from work w join tenant_config_pricing p on 1=1 join app_user u on 1=1", sql);
        }

        [Fact]
        public void PostgreSql_keeps_sql_unchanged()
        {
            var d = SqlDialect.For(DatabaseProvider.PostgreSql);
            const string sql = "select * from domain.work";
            Assert.Equal(sql, d.Sql(sql));
        }

        [Theory]
        [InlineData("abc", "%abc%")]
        [InlineData("50%", "%50\\%%")]
        [InlineData("a_b", "%a\\_b%")]
        [InlineData("x'; drop table y; --", "%x'; drop table y; --%")]
        public void ContainsPattern_escapes_like_wildcards(string input, string expected)
        {
            Assert.Equal(expected, SqlDialect.ContainsPattern(input));
        }

        [Fact]
        public void Dialect_helpers_produce_vendor_sql()
        {
            var pg = SqlDialect.For(DatabaseProvider.PostgreSql);
            var my = SqlDialect.For(DatabaseProvider.MySql);
            Assert.Equal("x ILIKE @p", pg.ILike("x", "@p"));
            Assert.Equal("x LIKE @p", my.ILike("x", "@p"));
            Assert.Contains("string_agg", pg.StringAgg("a", "/ "));
            Assert.Contains("GROUP_CONCAT", my.StringAgg("a", "/ "));
            Assert.Equal("json_build_object('k', v)", pg.JsonObject(("k", "v")));
            Assert.Equal("JSON_OBJECT('k', v)", my.JsonObject(("k", "v")));
        }
    }
}
