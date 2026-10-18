using System;
using System.Collections.Generic;

namespace VgAuto.Core.Domain
{
    /// <summary>Which vehicle details are printed on an estimate or invoice (set per company in the administration).</summary>
    [Flags]
    public enum VehicleFields
    {
        None = 0,
        /// <summary>year, make and model ("2019 Honda Civic")</summary>
        Title = 1,
        LicensePlate = 2,
        Odometer = 4,
        Vin = 8,
        All = Title | LicensePlate | Odometer | Vin,
    }
}
