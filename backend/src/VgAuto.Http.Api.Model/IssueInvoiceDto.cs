using VgAuto.Core.Domain;

namespace VgAuto.Http.Api.Model
{
    public record  IssueInvoiceDto(PaymentType PaymentType, short DueDays, bool SendClientEmail, string ClientEmail);
}
