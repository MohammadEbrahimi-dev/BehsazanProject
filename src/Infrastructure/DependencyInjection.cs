using System.Text;
using Behsazan.Application.Interfaces;
using Behsazan.Infrastructure.Persistence;
using Behsazan.Infrastructure.Seeding;
using Behsazan.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace Behsazan.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        #region DbContext
        services.AddDbContextFactory<AppDbContext>(options =>
            options.UseSqlServer(
                configuration.GetConnectionString("DefaultConnection"),
                sqlOptions =>
                {
                    sqlOptions.MigrationsAssembly(typeof(DependencyInjection).Assembly.FullName);
                    sqlOptions.EnableRetryOnFailure(
                        maxRetryCount: 2,
                        maxRetryDelay: TimeSpan.FromSeconds(3),
                        errorNumbersToAdd: null);
                }));

        services.AddScoped<AppDbContext>(sp =>
            sp.GetRequiredService<IDbContextFactory<AppDbContext>>().CreateDbContext());
        #endregion

        #region Repository & Unit of Work
        services.AddScoped(typeof(Application.Interfaces.IRepository<>), typeof(Repositories.Repository<>));
        services.AddScoped<Application.Interfaces.IUnitOfWork, UnitOfWork>();
        #endregion

        #region Authentication Services
        services.AddScoped<IAuthService, AuthService>();
        services.AddSingleton<IJwtTokenService, JwtTokenService>();
        services.AddSingleton<IPasswordHasher, PasswordHasherService>();
        #endregion

        #region Dashboard Services
        services.AddScoped<IDashboardService, DashboardService>();
        #endregion

        #region Financial Report Services
        services.AddScoped<IFinancialReportService, FinancialReportService>();
        #endregion

        #region Customer Services
        services.AddScoped<ICustomerService, CustomerService>();
        services.AddScoped<ICustomerPhoneNumberService, CustomerPhoneNumberService>();
        #endregion

        #region Project Services
        services.AddScoped<IProjectService, ProjectService>();
        services.AddScoped<IProjectLedgerService, ProjectLedgerService>();
        services.AddScoped<IProjectFinancialReportService, ProjectFinancialReportService>();
        #endregion

        #region Deposit Services
        services.AddScoped<IDepositService, DepositService>();
        #endregion

        #region Invoice Services
        services.AddScoped<IInvoiceService, InvoiceService>();
        services.AddScoped<IInvoiceExcelService, InvoiceExcelService>();
        services.AddScoped<IInvoicePdfService, InvoicePdfService>();
        #endregion

        #region Seeding
        services.AddScoped<DefaultDataSeeder>();
        #endregion

        #region JWT Authentication
        var key = configuration["Jwt:Key"]
            ?? throw new InvalidOperationException("JWT Key is not configured in appsettings.json.");
        var issuer = configuration["Jwt:Issuer"] ?? "Behsazan";
        var audience = configuration["Jwt:Audience"] ?? "Behsazan";

        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = issuer,
                ValidAudience = audience,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
                ClockSkew = TimeSpan.Zero,
            };

            options.Events = new JwtBearerEvents
            {
                OnChallenge = context =>
                {
                    context.HandleResponse();
                    return Task.CompletedTask;
                }
            };
        });
        #endregion

        return services;
    }
}
