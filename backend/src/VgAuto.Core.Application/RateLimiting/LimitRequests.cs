using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc.Controllers;

namespace VgAuto.Core.Application.RateLimiting
{
    [AttributeUsage(AttributeTargets.Method)]
    public class LimitRequests : Attribute
    {
        public int TimeWindow { get; set; }
        public int MaxRequests { get; set; }
    }
}
