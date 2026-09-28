using System.Linq;

namespace VgAuto.Core.Domain
{
    public class AddressComponent
    {
        protected AddressComponent() { }
        public AddressComponent(string street,string country,string region,string city,string postalCode) 
        {
            Country = country;
            Region = region;
            Street = street;
            City = city;
            PostalCode = postalCode;
        }
        public virtual string Country { get; protected set; }
        public virtual string Region { get; protected set; }
        public virtual string City { get; protected set; }
        public virtual string Street { get; protected set; }
        public virtual string PostalCode { get; protected set; }

        /// <summary>Address lines as written in North America: street / city, province postal code / country.</summary>
        public virtual string[] Lines()
        {
            static bool Has(string x) => !string.IsNullOrWhiteSpace(x);
            var cityLine = string.Join(", ", new[] { City, string.Join(" ", new[] { Region, PostalCode }.Where(Has).Select(x => x.Trim())) }.Where(Has).Select(x => x.Trim()));
            return new[] { Street, cityLine, Country }.Where(Has).Select(x => x.Trim()).ToArray();
        }

        /// <summary>"123 Main St, Vancouver, BC V6B 1A1, Canada"</summary>
        public override string ToString() => string.Join(", ", Lines());
    }
}