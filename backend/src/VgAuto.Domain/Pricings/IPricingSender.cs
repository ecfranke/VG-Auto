using System.Net.Http;
using System.Threading.Tasks;

namespace VgAuto.Core.Domain
{
    public interface IPricingSender
    {
        Task Send(Pricing pricing );
    }
}