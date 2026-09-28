using System;
using FluentNHibernate.Mapping;
using NHibernate;
using NHibernate.Type;
using VgAuto.Core.Application.Database;

namespace VgAuto.Core.Persistence
{
    /// <summary>NHibernate filter that limits queries to one company (company_id column).</summary>
    public class CompanyFilter : FilterDefinition
    {
        public const string Name = "company";
        public const string Parameter = "companyId";
        public const string Condition = "company_id = :companyId";

        public CompanyFilter()
        {
            WithName(Name).AddParameter(Parameter, NHibernateUtil.Guid);
        }
    }

    /// <summary>
    /// Keeps the data of companies apart:
    /// - new rows get the company of the current session (unless it was set explicitly),
    /// - loading a row of another company by its id fails (queries are limited by <see cref="CompanyFilter"/>).
    /// Administration code can switch the company or turn the check off for the request.
    /// </summary>
    public class CompanyInterceptor : EmptyInterceptor
    {
        public Guid CompanyId { get; set; }
        public bool Enforce { get; set; } = true;

        public CompanyInterceptor(Guid companyId)
        {
            CompanyId = companyId;
        }

        private static bool IsCompanyProperty(string name) => name == "CompanyId" || name == "NumberCompanyId";

        public override bool OnSave(object entity, object id, object[] state, string[] propertyNames, IType[] types)
        {
            var modified = false;
            var owner = Guid.Empty;
            for (var i = 0; i < propertyNames.Length; i++)
            {
                if (propertyNames[i] == "CompanyId" && state[i] is Guid g && g != Guid.Empty) owner = g;
            }
            for (var i = 0; i < propertyNames.Length; i++)
            {
                if (!IsCompanyProperty(propertyNames[i])) continue;
                if (state[i] is Guid current && current != Guid.Empty) continue;
                state[i] = owner != Guid.Empty ? owner : CompanyId;
                modified = true;
            }
            return modified;
        }

        public override bool OnLoad(object entity, object id, object[] state, string[] propertyNames, IType[] types)
        {
            if (!Enforce) return false;
            for (var i = 0; i < propertyNames.Length; i++)
            {
                if (propertyNames[i] == "CompanyId" && state[i] is Guid owner && owner != Guid.Empty && owner != CompanyId)
                {
                    throw new VgAuto.Core.Domain.UserException("Not found.");
                }
            }
            return false;
        }
    }

    /// <summary>Company of the current request, and the switches used by the administration.</summary>
    public class CompanyScope : ICompanyScope
    {
        private readonly Lazy<ISession> session;
        private readonly CompanyInterceptor interceptor;

        public CompanyScope(Guid companyId, CompanyInterceptor interceptor, Lazy<ISession> session)
        {
            CompanyId = companyId;
            this.interceptor = interceptor;
            this.session = session;
        }

        public Guid CompanyId { get; private set; }

        public void SwitchTo(Guid companyId)
        {
            CompanyId = companyId;
            interceptor.CompanyId = companyId;
            interceptor.Enforce = true;
            session.Value.EnableFilter(CompanyFilter.Name).SetParameter(CompanyFilter.Parameter, companyId);
        }

        public void AllCompanies()
        {
            interceptor.Enforce = false;
            session.Value.DisableFilter(CompanyFilter.Name);
        }
    }
}
