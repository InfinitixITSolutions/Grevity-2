using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;
using Grevity.Services.Interfaces;

namespace Grevity.Services.Implementations
{
    public class CompanyContext : ICompanyContext
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private const string SessionKey = "ActiveCompanyId";

        public CompanyContext(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public int? CurrentCompanyId
        {
            get
            {
                var session = _httpContextAccessor.HttpContext?.Session;
                if (session != null)
                {
                    var val = session.GetInt32(SessionKey);
                    return val;
                }
                return null;
            }
        }

        public Task SetCompanyAsync(int companyId)
        {
            _httpContextAccessor.HttpContext?.Session.SetInt32(SessionKey, companyId);
            return Task.CompletedTask;
        }
    }
}
