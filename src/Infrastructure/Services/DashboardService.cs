using Behsazan.Application.DTOs;
using Behsazan.Application.Interfaces;
using Behsazan.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Behsazan.Infrastructure.Services;

public class DashboardService : IDashboardService
{
    private readonly IDbContextFactory<AppDbContext> _dbContextFactory;

    public DashboardService(IDbContextFactory<AppDbContext> dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    public async Task<DashboardStatsDto> GetStatsAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        var totalCustomers = await db.Customers.CountAsync(cancellationToken);
        var totalProjects = await db.Projects.CountAsync(cancellationToken);
        var totalInvoices = await db.Invoices.CountAsync(cancellationToken);
        var totalRevenue = await db.Invoices.SumAsync(i => (decimal?)i.TotalPrice, cancellationToken) ?? 0;
        var totalDeposits = await db.Deposits.SumAsync(d => (decimal?)d.Amount, cancellationToken) ?? 0;

        return new DashboardStatsDto
        {
            TotalCustomers = totalCustomers,
            TotalProjects = totalProjects,
            TotalInvoices = totalInvoices,
            TotalRevenue = totalRevenue,
            TotalDeposits = totalDeposits,
            OutstandingBalance = totalRevenue - totalDeposits
        };
    }

    public async Task<IReadOnlyList<RecentProjectDto>> GetRecentProjectsAsync(int count, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        return await dbContext.Projects
            .AsNoTracking()
            .OrderByDescending(p => p.ModifiedAt ?? p.CreatedAt)
            .Take(count)
            .Select(p => new RecentProjectDto
            {
                Id = p.Id,
                Name = p.Name,
                CustomerId = p.CustomerId,
                CustomerName = p.Customer.FirstName + " " + p.Customer.LastName,
                CreatedAt = p.CreatedAt,
                InvoicesCount = p.Invoices.Count()
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<RecentInvoiceDto>> GetRecentInvoicesAsync(int count, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        return await dbContext.Invoices
            .AsNoTracking()
            .OrderByDescending(i => i.InvoiceDate)
            .ThenByDescending(i => i.Id)
            .Take(count)
            .Select(i => new RecentInvoiceDto
            {
                Id = i.Id,
                InvoiceNumber = i.InvoiceNumber,
                InvoiceDate = i.InvoiceDate,
                ProjectId = i.ProjectId,
                ProjectName = i.Project.Name,
                CustomerId = i.Project.CustomerId,
                CustomerName = i.Project.Customer.FirstName + " " + i.Project.Customer.LastName,
                TotalPrice = i.TotalPrice
            })
            .ToListAsync(cancellationToken);
    }
}
