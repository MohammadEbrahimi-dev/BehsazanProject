using Behsazan.Application.DTOs;
using Behsazan.Application.Enums;
using Behsazan.Application.Interfaces;
using Behsazan.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Behsazan.Infrastructure.Services;

public class ProjectLedgerService : IProjectLedgerService
{
    private readonly IDbContextFactory<AppDbContext> _dbContextFactory;

    public ProjectLedgerService(IDbContextFactory<AppDbContext> dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    public async Task<ProjectLedgerDto?> GetByProjectIdAsync(
        int projectId,
        CancellationToken cancellationToken = default)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        var project = await dbContext.Projects
            .AsNoTracking()
            .Where(p => p.Id == projectId)
            .Select(p => new
            {
                p.Id,
                p.Name,
                p.Address,
                p.GeneralLedgerNumber,
                p.CustomerId,
                CustomerFullName = p.Customer.FirstName == ""
                    ? p.Customer.LastName
                    : p.Customer.FirstName + " " + p.Customer.LastName
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (project is null)
            return null;

        var invoices = await dbContext.Invoices
            .AsNoTracking()
            .Where(i => i.ProjectId == projectId)
            .Select(i => new
            {
                i.Id,
                i.InvoiceNumber,
                i.InvoiceDate,
                i.Title,
                i.TotalPrice
            })
            .ToListAsync(cancellationToken);

        var deposits = await dbContext.Deposits
            .AsNoTracking()
            .Where(d => d.ProjectId == projectId)
            .Select(d => new
            {
                d.Id,
                d.DepositDate,
                d.Amount,
                d.Description,
                d.TrackingNumber,
                d.ReferenceNumber
            })
            .ToListAsync(cancellationToken);

        var rawEntries = invoices
            .Select(i => new
            {
                EntryType = ProjectLedgerEntryType.Invoice,
                RelatedId = i.Id,
                Date = i.InvoiceDate,
                Description = BuildInvoiceDescription(i.InvoiceNumber, i.Title),
                Debit = i.TotalPrice,
                Credit = 0m
            })
            .Concat(deposits.Select(d => new
            {
                EntryType = ProjectLedgerEntryType.Deposit,
                RelatedId = d.Id,
                Date = d.DepositDate,
                Description = BuildDepositDescription(d.Description, d.TrackingNumber, d.ReferenceNumber),
                Debit = 0m,
                Credit = d.Amount
            }))
            .OrderBy(e => e.Date.Date)
            .ThenBy(e => e.EntryType)
            .ThenBy(e => e.RelatedId)
            .ToList();

        var running = 0m;
        var entries = new List<ProjectLedgerEntryDto>(rawEntries.Count);

        foreach (var row in rawEntries)
        {
            running += row.Debit - row.Credit;
            entries.Add(new ProjectLedgerEntryDto
            {
                EntryType = row.EntryType,
                RelatedId = row.RelatedId,
                Date = row.Date,
                Description = row.Description,
                Debit = row.Debit,
                Credit = row.Credit,
                RunningBalance = running
            });
        }

        var invoiceTotal = invoices.Sum(i => i.TotalPrice);
        var depositTotal = deposits.Sum(d => d.Amount);

        return new ProjectLedgerDto
        {
            ProjectId = project.Id,
            ProjectName = project.Name,
            ProjectAddress = project.Address,
            GeneralLedgerNumber = project.GeneralLedgerNumber,
            CustomerId = project.CustomerId,
            CustomerFullName = project.CustomerFullName,
            InvoiceCount = invoices.Count,
            InvoiceTotal = invoiceTotal,
            DepositCount = deposits.Count,
            DepositTotal = depositTotal,
            OutstandingBalance = invoiceTotal - depositTotal,
            Entries = entries
        };
    }

    private static string BuildInvoiceDescription(int invoiceNumber, string? title)
    {
        if (string.IsNullOrWhiteSpace(title))
            return $"فاکتور #{invoiceNumber}";

        return $"فاکتور #{invoiceNumber} — {title.Trim()}";
    }

    private static string BuildDepositDescription(
        string? description,
        string? trackingNumber,
        string? referenceNumber)
    {
        if (!string.IsNullOrWhiteSpace(description))
            return description.Trim();

        if (!string.IsNullOrWhiteSpace(trackingNumber))
            return $"واریزی — پیگیری {trackingNumber.Trim()}";

        if (!string.IsNullOrWhiteSpace(referenceNumber))
            return $"واریزی — مرجع {referenceNumber.Trim()}";

        return "واریزی";
    }
}
