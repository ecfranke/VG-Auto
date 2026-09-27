using System.Collections.Generic;
using System.Linq;
using VgAuto.Core.Application.Database;
using VgAuto.Core.Application.Services;
using Xunit;

namespace VgAuto.Tests.Unit
{
    public class PageResultQueryTests
    {
        private static PageResultQuery<object> Query(DatabaseProvider provider = DatabaseProvider.PostgreSql) =>
            new PageResultQuery<object>(null, SqlDialect.For(provider));

        [Fact]
        public void Search_text_is_never_part_of_the_sql()
        {
            const string attack = "x'/**/union/**/select/**/version()--";
            var q = Query().PageIs(null, 10, 0, false)
                .FilterBy(attack)
                .SearchFields("name", "code")
                .SelectSql("select * from domain.sparepart");

            var sql = q.BuildSql();

            Assert.DoesNotContain("union", sql);
            Assert.DoesNotContain("version()", sql);
            Assert.Contains("@p0", sql);
            Assert.Contains(q.Parameters.ParameterNames, n => n == "p0");
        }

        [Fact]
        public void Every_word_must_match_one_of_the_fields()
        {
            var sql = Query().PageIs(null, 10, 0, false)
                .FilterBy("john  doe")
                .SearchFields("firstname", "lastname")
                .SelectSql("select * from t")
                .BuildSql();

            Assert.Contains("@p0", sql);
            Assert.Contains("@p1", sql);
            Assert.Contains(" AND ", sql);
        }

        [Theory]
        [InlineData("name", "ORDER BY c.name ASC")]
        [InlineData("NAME", "ORDER BY c.name ASC")]
        [InlineData("(select 1 from pg_sleep(3))", "ORDER BY c.id ASC")]
        [InlineData(null, "ORDER BY c.id ASC")]
        public void Order_by_uses_whitelist(string requested, string expected)
        {
            var q = Query().PageIs(requested, 10, 0, false)
                .Sortable(new Dictionary<string, string> { ["name"] = "c.name" }, "c.id");
            Assert.Equal(expected, q.GetOrderBy());
        }

        [Theory]
        [InlineData(0, 30)]
        [InlineData(-5, 30)]
        [InlineData(100000, PageResultQuery<object>.MaxLimit)]
        [InlineData(20, 20)]
        public void Limit_is_clamped(int requested, int expectedLimit)
        {
            var q = Query().PageIs(null, requested, -1, false);
            q.GetPagingRestriction();
            var limit = q.Parameters.Get<int>("page_limit");
            var offset = q.Parameters.Get<int>("page_offset");
            Assert.Equal(expectedLimit + 1, limit);
            Assert.Equal(0, offset);
        }

        [Fact]
        public void MySql_query_has_no_schema_prefix()
        {
            var sql = Query(DatabaseProvider.MySql).PageIs(null, 10, 0, false)
                .SelectSql("select * from domain.vehicle v")
                .BuildSql();
            Assert.Contains("from vehicle v", sql);
            Assert.DoesNotContain("domain.", sql);
        }
    }
}
