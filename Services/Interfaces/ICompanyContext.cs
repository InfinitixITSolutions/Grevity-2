using System.Threading.Tasks;

namespace Grevity.Services.Interfaces
{
    public interface ICompanyContext
    {
        int? CurrentCompanyId { get; }
        Task SetCompanyAsync(int companyId);
    }
}
