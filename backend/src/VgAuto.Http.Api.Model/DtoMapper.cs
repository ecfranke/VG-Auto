using System.Linq;
using VgAuto.Core.Domain;
using VgAuto.Http.Api.Models;

namespace VgAuto.Http.Api.Model
{
    /// <summary>Explicit entity to DTO mapping (replaces AutoMapper).</summary>
    public static class DtoMapper
    {
        public static EmployeeDto ToDto(this Employee e) => e == null ? null :
            new EmployeeDto(e.FirstName, e.LastName, e.Phone, e.Email, e.Proffession, e.Description, e.IntroducedAt, null, null, e.Id);

        public static StorageDto ToDto(this Storage s) => s == null ? null :
            new StorageDto(s.Name, s.Address, s.Id, s.Description, s.IntroducedAt);

        public static SparePartDto ToDto(this SparePart s) => s == null ? null :
            new SparePartDto(s.Code, s.Name, s.Price, s.Quantity, s.Discount, s.Storage?.Id, s.Storage?.Name, s.Id, s.Description, s.IntroducedAt);

        public static AddressDto ToDto(this AddressComponent a) => a == null ? null :
            new AddressDto(a.Country, a.Region, a.City, a.Street, a.PostalCode);

        public static PrivateClientDto ToDto(this PrivateClient c) => c == null ? null :
            new PrivateClientDto(c.Id, c.FirstName, c.LastName, c.Address.ToDto(), c.Phone,
                c.EmailAddresses.Select(x => x.Address).ToArray(), c.CurrentEmail, c.IsAsshole, c.Description, c.PersonalCode, c.IntroducedAt);

        public static LegalClientDto ToDto(this LegalClient c) => c == null ? null :
            new LegalClientDto(c.Id, c.Name, c.RegNr, c.Address.ToDto(), c.Phone,
                c.EmailAddresses.Select(x => x.Address).ToArray(), c.CurrentEmail, c.IsAsshole, c.Description, c.IntroducedAt);
    }
}
