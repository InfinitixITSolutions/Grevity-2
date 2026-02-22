using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Grevity.Data;
using Microsoft.EntityFrameworkCore;
using Grevity.Services.Interfaces;
using System.Linq;
using System.Threading.Tasks;

namespace Grevity.Controllers
{
    [Authorize]
    public class AuditLogsController : Controller
    {
        private readonly AppDbContext _context;
        private readonly ICompanyContext _companyContext;

        public AuditLogsController(AppDbContext context, ICompanyContext companyContext)
        {
            _context = context;
            _companyContext = companyContext;
        }

        public async Task<IActionResult> Index()
        {
            var logs = await _context.AuditLogs
                .Where(l => l.CompanyId == _companyContext.CurrentCompanyId)
                .OrderByDescending(l => l.Timestamp)
                .Take(100)
                .ToListAsync();

            return View(logs);
        }
    }
}
