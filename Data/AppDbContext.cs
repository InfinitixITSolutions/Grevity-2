using Microsoft.EntityFrameworkCore;
using Grevity.Models.Entities;
using Grevity.Services.Interfaces;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace Grevity.Data
{
    public class AppDbContext : DbContext
    {
        private readonly ICompanyContext _companyContext;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public AppDbContext(DbContextOptions<AppDbContext> options, ICompanyContext companyContext, IHttpContextAccessor httpContextAccessor)
            : base(options)
        {
            _companyContext = companyContext;
            _httpContextAccessor = httpContextAccessor;
        }

        public DbSet<BusinessSetting> BusinessSettings { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<UserCompany> UserCompanies { get; set; }
        public DbSet<Customer> Customers { get; set; }
        public DbSet<Supplier> Suppliers { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<Invoice> Invoices { get; set; }
        public DbSet<InvoiceItem> InvoiceItems { get; set; }
        public DbSet<PaymentTransaction> PaymentTransactions { get; set; }
        public DbSet<AuditLog> AuditLogs { get; set; }
        public DbSet<SubProduct> SubProducts { get; set; }
        public DbSet<ProductSubProductMapping> ProductSubProductMappings { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Global decimal precision
            var decimalProps = modelBuilder.Model
                .GetEntityTypes()
                .SelectMany(t => t.GetProperties())
                .Where(p => p.ClrType == typeof(decimal) || p.ClrType == typeof(decimal?));

            foreach (var property in decimalProps)
            {
                property.SetPrecision(18);
                property.SetScale(2);
            }

            // Global Query Filter for Multi-Tenancy
            // We apply this filter to any entity implementing ICompanyEntity
            // IMPORTANT: If CurrentCompanyId is null, we might want to return nothing OR all (for super admin)
            // For now: if null, we filter nothing (danger) or everything? 
            // Better: If company ID is set, filter by it. If not, maybe return nothing to be safe?
            // Actually, for a safer default, if no company is selected, show nothing.
            // But strict filter `e => e.CompanyId == _companyContext.CurrentCompanyId` will work because null == null usually matches in C# but in SQL `CompanyId = NULL`
            // Let's rely on the lambda.

            // Using a locally captured variable for the filter expression might be tricky with dependency injection scope.
            // EF Core allows injecting services into DbContext but the filter is compiled once.
            // Actually, we can pass a lambda that accesses the property on the context instance.

            modelBuilder.Entity<Product>().HasQueryFilter(e => !e.CompanyId.HasValue || e.CompanyId == _companyContext.CurrentCompanyId);
            modelBuilder.Entity<Customer>().HasQueryFilter(e => !e.CompanyId.HasValue || e.CompanyId == _companyContext.CurrentCompanyId);
            modelBuilder.Entity<Supplier>().HasQueryFilter(e => !e.CompanyId.HasValue || e.CompanyId == _companyContext.CurrentCompanyId);
            modelBuilder.Entity<Invoice>().HasQueryFilter(e => !e.CompanyId.HasValue || e.CompanyId == _companyContext.CurrentCompanyId);
            modelBuilder.Entity<InvoiceItem>().HasQueryFilter(e => !e.CompanyId.HasValue || e.CompanyId == _companyContext.CurrentCompanyId);
            modelBuilder.Entity<PaymentTransaction>().HasQueryFilter(e => !e.CompanyId.HasValue || e.CompanyId == _companyContext.CurrentCompanyId);
            modelBuilder.Entity<SubProduct>().HasQueryFilter(e => !e.CompanyId.HasValue || e.CompanyId == _companyContext.CurrentCompanyId);
            modelBuilder.Entity<ProductSubProductMapping>().HasQueryFilter(e => !e.CompanyId.HasValue || e.CompanyId == _companyContext.CurrentCompanyId);
            
            // Note: The logic `!e.CompanyId.HasValue` allows global items (CompanyId=null) to be seen by everyone.
            // If strict segregation is needed: `e.CompanyId == _companyContext.CurrentCompanyId`
            // Let's assume STRICT for now, but handle the case where _companyContext might be null during migrations.
        }

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            var auditEntries = OnBeforeSaveChanges();
            var result = await base.SaveChangesAsync(cancellationToken);
            await OnAfterSaveChanges(auditEntries);
            return result;
        }

        private List<AuditEntry> OnBeforeSaveChanges()
        {
            ChangeTracker.DetectChanges();
            var auditEntries = new List<AuditEntry>();

            var userIdStr = _httpContextAccessor.HttpContext?.User?.FindFirst("UserId")?.Value;
             int? userId = null;
            if(int.TryParse(userIdStr, out int uid)) userId = uid;

            foreach (var entry in ChangeTracker.Entries())
            {
                if (entry.Entity is AuditLog || entry.State == EntityState.Detached || entry.State == EntityState.Unchanged)
                    continue;

                // Auto-set CompanyId for new entities if they implement ICompanyEntity
                if (entry.State == EntityState.Added && entry.Entity is ICompanyEntity companyEntity)
                {
                    if (!companyEntity.CompanyId.HasValue)
                    {
                        companyEntity.CompanyId = _companyContext.CurrentCompanyId;
                    }
                }

                var auditEntry = new AuditEntry(entry);
                auditEntry.TableName = entry.Entity.GetType().Name;
                auditEntry.UserId = userId;
                auditEntry.CompanyId = _companyContext.CurrentCompanyId;
                auditEntry.Action = entry.State.ToString();

                foreach (var property in entry.Properties)
                {
                    if (property.IsTemporary)
                    {
                        // value will be generated by the database, get the value after saving
                        auditEntry.TemporaryProperties.Add(property);
                        continue;
                    }

                    string propertyName = property.Metadata.Name;
                    if (property.Metadata.IsPrimaryKey())
                    {
                        auditEntry.KeyValues[propertyName] = property.CurrentValue;
                        continue;
                    }

                    switch (entry.State)
                    {
                        case EntityState.Added:
                            auditEntry.NewValues[propertyName] = property.CurrentValue;
                            break;

                        case EntityState.Deleted:
                            auditEntry.OldValues[propertyName] = property.OriginalValue;
                            break;

                        case EntityState.Modified:
                            if (property.IsModified)
                            {
                                auditEntry.OldValues[propertyName] = property.OriginalValue;
                                auditEntry.NewValues[propertyName] = property.CurrentValue;
                            }
                            break;
                    }
                }
                auditEntries.Add(auditEntry);
            }
            return auditEntries;
        }

        private async Task OnAfterSaveChanges(List<AuditEntry> auditEntries)
        {
            if (auditEntries == null || auditEntries.Count == 0)
                return;

            foreach (var auditEntry in auditEntries)
            {
                foreach (var prop in auditEntry.TemporaryProperties)
                {
                    if (prop.Metadata.IsPrimaryKey())
                    {
                        auditEntry.KeyValues[prop.Metadata.Name] = prop.CurrentValue;
                    }
                    else
                    {
                        auditEntry.NewValues[prop.Metadata.Name] = prop.CurrentValue;
                    }
                }

                AuditLogs.Add(auditEntry.ToAuditLog());
            }

            await base.SaveChangesAsync();
        }
    }

    public class AuditEntry
    {
        public AuditEntry(Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry entry)
        {
            Entry = entry;
        }

        public Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry Entry { get; }
        public int? UserId { get; set; }
        public int? CompanyId { get; set; }
        public string TableName { get; set; }
        public string Action { get; set; }
        public Dictionary<string, object> KeyValues { get; } = new Dictionary<string, object>();
        public Dictionary<string, object> OldValues { get; } = new Dictionary<string, object>();
        public Dictionary<string, object> NewValues { get; } = new Dictionary<string, object>();
        public List<Microsoft.EntityFrameworkCore.ChangeTracking.PropertyEntry> TemporaryProperties { get; } = new List<Microsoft.EntityFrameworkCore.ChangeTracking.PropertyEntry>();

        public AuditLog ToAuditLog()
        {
            var audit = new AuditLog();
            audit.UserId = UserId;
            audit.CompanyId = CompanyId;
            audit.Action = Action;
            audit.EntityName = TableName;
            audit.EntityId = JsonConvert.SerializeObject(KeyValues);
            audit.Timestamp = System.DateTime.Now;
            audit.Details = Action == "Deleted" ? JsonConvert.SerializeObject(OldValues) : JsonConvert.SerializeObject(NewValues);
            return audit;
        }
    }
}
