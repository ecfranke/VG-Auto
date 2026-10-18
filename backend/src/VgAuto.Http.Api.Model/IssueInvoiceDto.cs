using VgAuto.Core.Domain;

namespace VgAuto.Http.Api.Model
{
    /// <param name="ShowVehicleOnPricing">print the vehicle details chosen for invoices; null: the company default</param>
    public record  IssueInvoiceDto(PaymentType PaymentType, short DueDays, bool SendClientEmail, string ClientEmail, bool? ShowVehicleOnPricing = null);
}
