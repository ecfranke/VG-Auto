using System;

namespace VgAuto.Http.Api.Model
{
    public record IssuePricingDto(bool ShowVehicleOnPricing,bool SendClientEmail,string ClientEmail)
    {

    }
}
