using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.Cookies;
using Grevity.Data;
using Grevity.Repositories.Interfaces;
using Grevity.Repositories.Implementations;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Database Configuration
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Repository Configuration
builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));

// Service Configuration
builder.Services.AddScoped<Grevity.Services.Interfaces.IAuthService, Grevity.Services.Implementations.AuthService>();
builder.Services.AddScoped<Grevity.Services.Interfaces.IBusinessSettingService, Grevity.Services.Implementations.BusinessSettingService>();
builder.Services.AddScoped<Grevity.Services.Interfaces.IProductService, Grevity.Services.Implementations.ProductService>();
builder.Services.AddScoped<Grevity.Services.Interfaces.ICustomerService, Grevity.Services.Implementations.CustomerService>();
builder.Services.AddScoped<Grevity.Services.Interfaces.ISupplierService, Grevity.Services.Implementations.SupplierService>();
builder.Services.AddScoped<Grevity.Services.Interfaces.IInvoiceService, Grevity.Services.Implementations.InvoiceService>();
builder.Services.AddScoped<Grevity.Services.Interfaces.IEmailService, Grevity.Services.Implementations.EmailService>();
builder.Services.AddScoped<Grevity.Services.Interfaces.ICompanyContext, Grevity.Services.Implementations.CompanyContext>();
builder.Services.AddScoped<Grevity.Services.Interfaces.ISubProductService, Grevity.Services.Implementations.SubProductService>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});


// Authentication Configuration
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
    });

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();
app.UseSession(); // Added Session Middleware

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
