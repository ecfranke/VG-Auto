using System;

namespace VgAuto.Core.Application.RateLimiting
{
    public class ClientStatistics
        {
            public DateTime LastSuccessfulResponseTime { get; set; }
            public int NumberOfRequestsCompletedSuccessfully { get; set; }
        } 
}
