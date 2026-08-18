using Behsazan.Application.DTOs;
using Behsazan.Application.Enums;
using Behsazan.Application.Interfaces;
using Behsazan.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Behsazan.Infrastructure.Services;

public class FinancialReportService : IFinancialReportService
{
    private const int TopProjectsCount = 8;

    private static readonly DayOfWeek WeekStart = DayOfWeek.Saturday;

    private readonly IDbContextFactory<AppDbContext> _dbContextFactory;

    public FinancialReportService(IDbContextFactory<AppDbContext> dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    public async Task<FinancialDashboardDto> GetDashboardAsync(
        DateTime? from = null,
        DateTime? to = null,
        FinancialTrendGranularity granularity = FinancialTrendGranularity.Month,
        CancellationToken cancellationToken = default)
    {
        var (rangeFrom, rangeToExclusive) = ResolveRange(from, to);

        var invoiceTask = LoadInvoiceSideAsync(rangeFrom, rangeToExclusive, granularity, cancellationToken);
        var depositTask = LoadDepositSideAsync(rangeFrom, rangeToExclusive, granularity, cancellationToken);
        await Task.WhenAll(invoiceTask, depositTask);

        var invoices = await invoiceTask;
        var deposits = await depositTask;

        var trends = BuildTrends(rangeFrom, rangeToExclusive, granularity, invoices.Buckets, deposits.Buckets);
        var summary = BuildSummary(
            invoices.Buckets,
            deposits.Buckets,
            invoices.AllTimeByProject.Sum(p => p.Total),
            deposits.AllTimeByProject.Sum(p => p.Total));

        var revenueByProject = await BuildRevenueByProjectAsync(
            invoices.RangeByProject,
            deposits.RangeByProject,
            cancellationToken);

        var receivablesStatus = BuildReceivablesStatus(invoices.AllTimeByProject, deposits.AllTimeByProject);

        return new FinancialDashboardDto
        {
            Summary = summary,
            Trends = trends,
            RevenueByProject = revenueByProject,
            ReceivablesStatus = receivablesStatus,
            MonthlyInvoices = trends
                .Select(t => new MonthlyAmountDto { Year = t.Year, Month = t.Month, Amount = t.Revenue })
                .ToList(),
            MonthlyCollections = trends
                .Select(t => new MonthlyAmountDto { Year = t.Year, Month = t.Month, Amount = t.Collections })
                .ToList()
        };
    }

    private async Task<SideAggregates> LoadInvoiceSideAsync(
        DateTime from,
        DateTime toExclusive,
        FinancialTrendGranularity granularity,
        CancellationToken cancellationToken)
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        var buckets = await GetInvoiceBucketsAsync(db, from, toExclusive, granularity, cancellationToken);
        var rangeByProject = await GetProjectInvoiceTotalsAsync(db, from, toExclusive, cancellationToken);
        var allTimeByProject = await GetProjectInvoiceTotalsAsync(db, null, null, cancellationToken);

        return new SideAggregates(buckets, rangeByProject, allTimeByProject);
    }

    private async Task<SideAggregates> LoadDepositSideAsync(
        DateTime from,
        DateTime toExclusive,
        FinancialTrendGranularity granularity,
        CancellationToken cancellationToken)
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        var buckets = await GetDepositBucketsAsync(db, from, toExclusive, granularity, cancellationToken);
        var rangeByProject = await GetProjectDepositTotalsAsync(db, from, toExclusive, cancellationToken);
        var allTimeByProject = await GetProjectDepositTotalsAsync(db, null, null, cancellationToken);

        return new SideAggregates(buckets, rangeByProject, allTimeByProject);
    }

    private static (DateTime From, DateTime ToExclusive) ResolveRange(DateTime? from, DateTime? to)
    {
        if (from is null && to is null)
        {
            var today = DateTime.Today;
            var defaultFrom = new DateTime(today.Year, today.Month, 1).AddMonths(-11);
            return (defaultFrom, today.AddDays(1));
        }

        var rangeFrom = (from ?? DateTime.MinValue).Date;
        var rangeTo = (to ?? DateTime.Today).Date;
        return (rangeFrom, rangeTo.AddDays(1));
    }

    private static async Task<List<BucketGroup>> GetInvoiceBucketsAsync(
        AppDbContext db,
        DateTime from,
        DateTime toExclusive,
        FinancialTrendGranularity granularity,
        CancellationToken cancellationToken)
    {
        var query = db.Invoices
            .AsNoTracking()
            .Where(i => i.InvoiceDate >= from && i.InvoiceDate < toExclusive);

        return granularity switch
        {
            FinancialTrendGranularity.Day or FinancialTrendGranularity.Week =>
                await query
                    .GroupBy(i => new { i.InvoiceDate.Year, i.InvoiceDate.Month, i.InvoiceDate.Day })
                    .Select(g => new BucketGroup(
                        g.Key.Year, g.Key.Month, g.Key.Day,
                        g.Sum(x => x.TotalPrice), g.Count()))
                    .ToListAsync(cancellationToken),

            _ => await query
                .GroupBy(i => new { i.InvoiceDate.Year, i.InvoiceDate.Month })
                .Select(g => new BucketGroup(
                    g.Key.Year, g.Key.Month, 0,
                    g.Sum(x => x.TotalPrice), g.Count()))
                .ToListAsync(cancellationToken)
        };
    }

    private static async Task<List<BucketGroup>> GetDepositBucketsAsync(
        AppDbContext db,
        DateTime from,
        DateTime toExclusive,
        FinancialTrendGranularity granularity,
        CancellationToken cancellationToken)
    {
        var query = db.Deposits
            .AsNoTracking()
            .Where(d => d.DepositDate >= from && d.DepositDate < toExclusive);

        return granularity switch
        {
            FinancialTrendGranularity.Day or FinancialTrendGranularity.Week =>
                await query
                    .GroupBy(d => new { d.DepositDate.Year, d.DepositDate.Month, d.DepositDate.Day })
                    .Select(g => new BucketGroup(
                        g.Key.Year, g.Key.Month, g.Key.Day,
                        g.Sum(x => x.Amount), g.Count()))
                    .ToListAsync(cancellationToken),

            _ => await query
                .GroupBy(d => new { d.DepositDate.Year, d.DepositDate.Month })
                .Select(g => new BucketGroup(
                    g.Key.Year, g.Key.Month, 0,
                    g.Sum(x => x.Amount), g.Count()))
                .ToListAsync(cancellationToken)
        };
    }

    private static async Task<List<ProjectTotal>> GetProjectInvoiceTotalsAsync(
        AppDbContext db, DateTime? from, DateTime? toExclusive, CancellationToken cancellationToken)
    {
        var query = db.Invoices.AsNoTracking();
        if (from.HasValue && toExclusive.HasValue)
            query = query.Where(i => i.InvoiceDate >= from.Value && i.InvoiceDate < toExclusive.Value);

        return await query
            .GroupBy(i => new { i.ProjectId, i.Project.Name })
            .Select(g => new ProjectTotal(g.Key.ProjectId, g.Sum(x => x.TotalPrice), g.Key.Name))
            .ToListAsync(cancellationToken);
    }

    private static async Task<List<ProjectTotal>> GetProjectDepositTotalsAsync(
        AppDbContext db, DateTime? from, DateTime? toExclusive, CancellationToken cancellationToken)
    {
        var query = db.Deposits.AsNoTracking();
        if (from.HasValue && toExclusive.HasValue)
            query = query.Where(d => d.DepositDate >= from.Value && d.DepositDate < toExclusive.Value);

        return await query
            .GroupBy(d => d.ProjectId)
            .Select(g => new ProjectTotal(g.Key, g.Sum(x => x.Amount), null))
            .ToListAsync(cancellationToken);
    }

    private static List<FinancialTrendPointDto> BuildTrends(
        DateTime from,
        DateTime toExclusive,
        FinancialTrendGranularity granularity,
        List<BucketGroup> invoiceGroups,
        List<BucketGroup> depositGroups)
    {
        return granularity switch
        {
            FinancialTrendGranularity.Day => BuildDailyTrends(from, toExclusive, invoiceGroups, depositGroups),
            FinancialTrendGranularity.Week => BuildWeeklyTrends(from, toExclusive, invoiceGroups, depositGroups),
            _ => BuildMonthlyTrends(from, toExclusive, invoiceGroups, depositGroups)
        };
    }

    private static List<FinancialTrendPointDto> BuildMonthlyTrends(
        DateTime from, DateTime toExclusive, List<BucketGroup> invoiceGroups, List<BucketGroup> depositGroups)
    {
        var revenueByKey = invoiceGroups.ToDictionary(g => (g.Year, g.Month), g => g.Total);
        var collectionsByKey = depositGroups.ToDictionary(g => (g.Year, g.Month), g => g.Total);

        var points = new List<FinancialTrendPointDto>();
        var cursor = new DateTime(from.Year, from.Month, 1);
        var lastIncludedDay = toExclusive.AddDays(-1);
        var end = new DateTime(lastIncludedDay.Year, lastIncludedDay.Month, 1);

        while (cursor <= end)
        {
            var key = (cursor.Year, cursor.Month);
            points.Add(new FinancialTrendPointDto
            {
                Year = cursor.Year,
                Month = cursor.Month,
                Day = 0,
                PeriodStart = cursor,
                Label = $"{cursor.Year:0000}/{cursor.Month:00}",
                Revenue = revenueByKey.GetValueOrDefault(key),
                Collections = collectionsByKey.GetValueOrDefault(key)
            });
            cursor = cursor.AddMonths(1);
        }

        return points;
    }

    private static List<FinancialTrendPointDto> BuildDailyTrends(
        DateTime from, DateTime toExclusive, List<BucketGroup> invoiceGroups, List<BucketGroup> depositGroups)
    {
        var revenueByKey = invoiceGroups.ToDictionary(g => (g.Year, g.Month, g.Day), g => g.Total);
        var collectionsByKey = depositGroups.ToDictionary(g => (g.Year, g.Month, g.Day), g => g.Total);

        var points = new List<FinancialTrendPointDto>();
        for (var cursor = from.Date; cursor < toExclusive; cursor = cursor.AddDays(1))
        {
            var key = (cursor.Year, cursor.Month, cursor.Day);
            points.Add(new FinancialTrendPointDto
            {
                Year = cursor.Year,
                Month = cursor.Month,
                Day = cursor.Day,
                PeriodStart = cursor,
                Label = $"{cursor:yyyy/MM/dd}",
                Revenue = revenueByKey.GetValueOrDefault(key),
                Collections = collectionsByKey.GetValueOrDefault(key)
            });
        }

        return points;
    }

    private static List<FinancialTrendPointDto> BuildWeeklyTrends(
        DateTime from, DateTime toExclusive, List<BucketGroup> invoiceGroups, List<BucketGroup> depositGroups)
    {
        var revenueByDay = invoiceGroups.ToDictionary(
            g => new DateTime(g.Year, g.Month, g.Day),
            g => g.Total);
        var collectionsByDay = depositGroups.ToDictionary(
            g => new DateTime(g.Year, g.Month, g.Day),
            g => g.Total);

        var weekStart = StartOfWeek(from.Date);
        var lastDay = toExclusive.AddDays(-1).Date;
        var points = new List<FinancialTrendPointDto>();

        for (var cursor = weekStart; cursor <= lastDay; cursor = cursor.AddDays(7))
        {
            var weekEndExclusive = cursor.AddDays(7);
            decimal revenue = 0;
            decimal collections = 0;
            var countHint = 0;

            for (var d = cursor; d < weekEndExclusive; d = d.AddDays(1))
            {
                if (d < from.Date || d > lastDay)
                    continue;

                if (revenueByDay.TryGetValue(d, out var r))
                {
                    revenue += r;
                    countHint++;
                }

                if (collectionsByDay.TryGetValue(d, out var c))
                    collections += c;
            }

            _ = countHint;
            points.Add(new FinancialTrendPointDto
            {
                Year = cursor.Year,
                Month = cursor.Month,
                Day = cursor.Day,
                PeriodStart = cursor,
                Label = $"{cursor:yyyy/MM/dd}",
                Revenue = revenue,
                Collections = collections
            });
        }

        return points;
    }

    private static DateTime StartOfWeek(DateTime date)
    {
        var diff = (7 + (date.DayOfWeek - WeekStart)) % 7;
        return date.AddDays(-diff).Date;
    }

    private static FinancialSummaryDto BuildSummary(
        List<BucketGroup> invoiceGroups,
        List<BucketGroup> depositGroups,
        decimal allTimeRevenue,
        decimal allTimeCollections)
    {
        var totalRevenue = invoiceGroups.Sum(g => g.Total);
        var totalCollections = depositGroups.Sum(g => g.Total);

        return new FinancialSummaryDto
        {
            TotalRevenue = totalRevenue,
            TotalCollections = totalCollections,
            NetCashFlow = totalCollections,
            OutstandingReceivables = allTimeRevenue - allTimeCollections,
            InvoiceCount = invoiceGroups.Sum(g => g.Count),
            DepositCount = depositGroups.Sum(g => g.Count)
        };
    }

    private async Task<List<ProjectRevenueDto>> BuildRevenueByProjectAsync(
        List<ProjectTotal> projectInvoiceTotals,
        List<ProjectTotal> projectDepositTotals,
        CancellationToken cancellationToken)
    {
        var topProjects = projectInvoiceTotals
            .OrderByDescending(p => p.Total)
            .Take(TopProjectsCount)
            .ToList();

        if (topProjects.Count == 0)
            return [];

        var depositByProject = projectDepositTotals.ToDictionary(p => p.ProjectId, p => p.Total);
        var projectIds = topProjects.Select(p => p.ProjectId).ToList();

        var names = topProjects
            .Where(p => !string.IsNullOrEmpty(p.Name))
            .ToDictionary(p => p.ProjectId, p => p.Name!);

        if (names.Count < topProjects.Count)
        {
            await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
            var missingIds = projectIds.Where(id => !names.ContainsKey(id)).ToList();
            var fetched = await db.Projects
                .AsNoTracking()
                .Where(p => missingIds.Contains(p.Id))
                .Select(p => new { p.Id, p.Name })
                .ToDictionaryAsync(p => p.Id, p => p.Name, cancellationToken);

            foreach (var (id, name) in fetched)
                names[id] = name;
        }

        return topProjects
            .Select(p =>
            {
                var collections = depositByProject.GetValueOrDefault(p.ProjectId);
                return new ProjectRevenueDto
                {
                    ProjectId = p.ProjectId,
                    ProjectName = names.GetValueOrDefault(p.ProjectId, string.Empty),
                    Revenue = p.Total,
                    Collections = collections,
                    Outstanding = p.Total - collections
                };
            })
            .ToList();
    }

    private static ReceivablesStatusDto BuildReceivablesStatus(
        List<ProjectTotal> allTimeInvoiceTotals, List<ProjectTotal> allTimeDepositTotals)
    {
        var depositByProject = allTimeDepositTotals.ToDictionary(p => p.ProjectId, p => p.Total);
        var status = new ReceivablesStatusDto();

        foreach (var invoiceTotal in allTimeInvoiceTotals)
        {
            if (invoiceTotal.Total <= 0)
                continue;

            var deposits = depositByProject.GetValueOrDefault(invoiceTotal.ProjectId);

            if (deposits <= 0)
                status.UnpaidCount++;
            else if (deposits < invoiceTotal.Total)
                status.PartiallyPaidCount++;
            else if (deposits == invoiceTotal.Total)
                status.FullyPaidCount++;
            else
                status.OverpaidCount++;
        }

        return status;
    }

    private sealed record SideAggregates(
        List<BucketGroup> Buckets,
        List<ProjectTotal> RangeByProject,
        List<ProjectTotal> AllTimeByProject);

    private sealed record BucketGroup(int Year, int Month, int Day, decimal Total, int Count);

    private sealed record ProjectTotal(int ProjectId, decimal Total, string? Name);
}
