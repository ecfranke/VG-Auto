using System;
using System.Collections.Generic;
using System.Linq;
using VgAuto.Core;
using VgAuto.Core.Application;
using VgAuto.Core.Domain;
using VgAuto.Core.Persistence;
using VgAuto.Http.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace VgAuto.Http.Api.Controllers
{

    /// <summary>
    /// only httpget and httpost get and create
    /// </summary>
    /// <typeparam name="MODEL"></typeparam>
    /// <typeparam name="DOMAINOBJECT"></typeparam>
    public abstract class BaseController<MODEL, DOMAINOBJECT> : ControllerBase where DOMAINOBJECT : GuidIdentityEntity
    {

        protected readonly IRepository repository;

        protected BaseController(IRepository repository)
        {
            this.repository = repository;
        }
        [Authorize(Policy = "ServerSidePolicy")]
        [HttpGet("{id}")]
        public virtual ActionResult Get(Guid id)
        {
            var emp = repository.Get<DOMAINOBJECT>(id, false);
            var model = Map(emp);
            AfterGet(model, emp);
            return new JsonResult(model);
        }

        protected virtual MODEL Map(DOMAINOBJECT entity)
        {
            throw new NotSupportedException();
        }

        [Authorize(Policy = "ServerSidePolicy")]
        [HttpPost]
        public virtual ActionResult Post(MODEL model)
        {
            var domainObj = CreateFrom(model);
            repository.Add(domainObj);
            AfterSaved(model, domainObj);
            return new JsonResult(domainObj.Id);
        }
        protected virtual void AfterGet(MODEL model, DOMAINOBJECT domainObj)
        {
        }
        protected virtual void AfterSaved(MODEL model, DOMAINOBJECT domainObj)
        {
        }
        protected virtual void AfterUpdated(MODEL model, DOMAINOBJECT domainObj)
        {
        }
        [Authorize(Policy = "ServerSidePolicy")]
        [HttpPut("{id}")]
        public virtual ActionResult Put(Guid id, MODEL model)
        {
            var domainObj = repository.Get<DOMAINOBJECT>(id);
            Edit(domainObj, model);
            repository.Update(domainObj);
            AfterUpdated(model, domainObj);
            return new JsonResult(id);
        }

        [Authorize(Policy = "ServerSidePolicy")]
        [HttpDelete]
        public OkResult Delete([FromBody] Guid[] ids)
        {
            foreach (var id in ids)
            {
                var dObj = repository.Get<DOMAINOBJECT>(id);
                BeforeDelete(dObj);
                repository.Delete(dObj);
            }
            return Ok();
        }

        protected virtual void BeforeDelete(DOMAINOBJECT domainObj) { }

        protected virtual void Edit(DOMAINOBJECT entity, MODEL model)
        {
            throw new NotSupportedException();
        }
        protected virtual DOMAINOBJECT CreateFrom(MODEL model)
        {
            throw new NotSupportedException();
        }

    }
}
