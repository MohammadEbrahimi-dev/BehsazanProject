using Behsazan.Application.DTOs;
using Behsazan.Application.Interfaces;
using Behsazan.Application.Validation;
using Behsazan.Domain.Entities;
using Behsazan.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace Behsazan.Infrastructure.Services;

public class InvoiceService : IInvoiceService
{
    private readonly IDbContextFactory<AppDbContext> _dbContextFactory;

    public InvoiceService(IDbContextFactory<AppDbContext> dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    #region Read
    public async Task<PagedResultDto<InvoiceListItemDto>> GetPagedAsync(
        InvoiceQueryDto query,
        CancellationToken cancellationToken = default)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        var invoices = dbContext.Invoices.AsNoTracking();

        #region Filters
        if (!string.IsNullOrWhiteSpace(query.SearchTerm))
        {
            var term = query.SearchTerm.Trim();
            var digits = CustomerValidationRules.NormalizeDigits(term);
            int? numberSearch = int.TryParse(digits, out var number) ? number : null;

            invoices = invoices.Where(i =>
                (numberSearch != null && i.InvoiceNumber == numberSearch) ||
                (i.Title != null && i.Title.Contains(term)) ||
                i.Project.Name.Contains(term) ||
                (i.Project.Customer.FirstName + " " + i.Project.Customer.LastName).Contains(term) ||
                i.Project.Customer.FirstName.Contains(term) ||
                i.Project.Customer.LastName.Contains(term));
        }

        if (query.CustomerId is > 0)
            invoices = invoices.Where(i => i.Project.CustomerId == query.CustomerId);

        if (query.ProjectId is > 0)
            invoices = invoices.Where(i => i.ProjectId == query.ProjectId);

        if (query.InvoiceDateFrom.HasValue)
        {
            var from = query.InvoiceDateFrom.Value.Date;
            invoices = invoices.Where(i => i.InvoiceDate >= from);
        }

        if (query.InvoiceDateTo.HasValue)
        {
            var toExclusive = query.InvoiceDateTo.Value.Date.AddDays(1);
            invoices = invoices.Where(i => i.InvoiceDate < toExclusive);
        }
        #endregion

        var totalCount = await invoices.CountAsync(cancellationToken);

        if (totalCount == 0)
            return PagedResultDto<InvoiceListItemDto>.Empty(query.PageNumber, query.PageSize);

        var ordered = ApplySort(invoices, query);

        var items = await ordered
            .Skip((query.PageNumber - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(i => new InvoiceListItemDto
            {
                Id = i.Id,
                InvoiceNumber = i.InvoiceNumber,
                InvoiceDate = i.InvoiceDate,
                Title = i.Title,
                TotalAmount = i.TotalAmount,
                TotalPrice = i.TotalPrice,
                ShippingCost = i.ShippingCost,
                ItemsCount = i.InvoiceItems.Count(),
                ProjectId = i.ProjectId,
                ProjectName = i.Project.Name,
                CustomerId = i.Project.CustomerId,
                CustomerFullName = i.Project.Customer.FirstName + " " + i.Project.Customer.LastName,
                CreatedAt = i.CreatedAt,
                ModifiedAt = i.ModifiedAt,
                LastActivityAt = i.ModifiedAt ?? i.CreatedAt
            })
            .ToListAsync(cancellationToken);

        return new PagedResultDto<InvoiceListItemDto>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = query.PageNumber,
            PageSize = query.PageSize
        };
    }

    private static IQueryable<Invoice> ApplySort(IQueryable<Invoice> invoices, InvoiceQueryDto query)
    {
        var descending = query.SortDescending;

        return query.SortBy switch
        {
            InvoiceSortBy.InvoiceNumber => descending
                ? invoices.OrderByDescending(i => i.InvoiceNumber).ThenByDescending(i => i.Id)
                : invoices.OrderBy(i => i.InvoiceNumber).ThenBy(i => i.Id),

            InvoiceSortBy.InvoiceDate => descending
                ? invoices.OrderByDescending(i => i.InvoiceDate).ThenByDescending(i => i.Id)
                : invoices.OrderBy(i => i.InvoiceDate).ThenBy(i => i.Id),

            InvoiceSortBy.CustomerName => descending
                ? invoices.OrderByDescending(i => i.Project.Customer.LastName)
                    .ThenByDescending(i => i.Project.Customer.FirstName)
                    .ThenByDescending(i => i.Id)
                : invoices.OrderBy(i => i.Project.Customer.LastName)
                    .ThenBy(i => i.Project.Customer.FirstName)
                    .ThenBy(i => i.Id),

            InvoiceSortBy.ProjectName => descending
                ? invoices.OrderByDescending(i => i.Project.Name).ThenByDescending(i => i.Id)
                : invoices.OrderBy(i => i.Project.Name).ThenBy(i => i.Id),

            InvoiceSortBy.TotalPrice => descending
                ? invoices.OrderByDescending(i => i.TotalPrice).ThenByDescending(i => i.Id)
                : invoices.OrderBy(i => i.TotalPrice).ThenBy(i => i.Id),

            InvoiceSortBy.ItemsCount => descending
                ? invoices.OrderByDescending(i => i.InvoiceItems.Count()).ThenByDescending(i => i.Id)
                : invoices.OrderBy(i => i.InvoiceItems.Count()).ThenBy(i => i.Id),

            InvoiceSortBy.CreatedAt => descending
                ? invoices.OrderByDescending(i => i.CreatedAt).ThenByDescending(i => i.Id)
                : invoices.OrderBy(i => i.CreatedAt).ThenBy(i => i.Id),

            _ => descending
                ? invoices.OrderByDescending(i => i.ModifiedAt ?? i.CreatedAt).ThenByDescending(i => i.Id)
                : invoices.OrderBy(i => i.ModifiedAt ?? i.CreatedAt).ThenBy(i => i.Id)
        };
    }

    public async Task<InvoiceDetailsDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        var header = await dbContext.Invoices
            .AsNoTracking()
            .Where(i => i.Id == id)
            .Select(i => new InvoiceDetailsDto
            {
                Id = i.Id,
                InvoiceNumber = i.InvoiceNumber,
                InvoiceDate = i.InvoiceDate,
                Title = i.Title,
                TotalAmount = i.TotalAmount,
                TotalPrice = i.TotalPrice,
                ShippingCost = i.ShippingCost,
                ProjectId = i.ProjectId,
                ProjectName = i.Project.Name,
                ProjectAddress = i.Project.Address,
                ProjectGeneralLedgerNumber = i.Project.GeneralLedgerNumber,
                ProjectJoistType = i.Project.JoistType,
                CustomerId = i.Project.CustomerId,
                CustomerFullName = i.Project.Customer.FirstName + " " + i.Project.Customer.LastName,
                CustomerNationalCode = i.Project.Customer.NationalCode ?? "",
                CreatedAt = i.CreatedAt,
                ModifiedAt = i.ModifiedAt
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (header is null)
            return null;

        var items = await dbContext.InvoiceItems
            .AsNoTracking()
            .Where(item => item.InvoiceId == id)
            .OrderBy(item => item.Id)
            .Select(item => new InvoiceItemPreviewDto
            {
                Id = item.Id,
                Length = item.Length,
                Count = item.Count,
                BottomRebar = item.BottomRebar,
                TopRebar = item.TopRebar,
                ReinforcementBar = item.ReinforcementBar,
                ReinforcementPercent = item.ReinforcementPercent,
                Zigzag = item.Zigzag,
                UnitPrice = item.UnitPrice,
                TotalAmount = item.TotalAmount,
                TotalPrice = item.TotalPrice
            })
            .ToListAsync(cancellationToken);

        header.Items = items;
        return header;
    }

    public async Task<IReadOnlyList<InvoiceListItemDto>> GetByProjectAsync(
        int projectId,
        CancellationToken cancellationToken = default)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        return await dbContext.Invoices
            .AsNoTracking()
            .Where(i => i.ProjectId == projectId)
            .OrderByDescending(i => i.InvoiceDate)
            .ThenByDescending(i => i.Id)
            .Select(i => new InvoiceListItemDto
            {
                Id = i.Id,
                InvoiceNumber = i.InvoiceNumber,
                InvoiceDate = i.InvoiceDate,
                Title = i.Title,
                TotalAmount = i.TotalAmount,
                TotalPrice = i.TotalPrice,
                ShippingCost = i.ShippingCost,
                ItemsCount = i.InvoiceItems.Count(),
                ProjectId = i.ProjectId,
                ProjectName = i.Project.Name,
                CustomerId = i.Project.CustomerId,
                CustomerFullName = i.Project.Customer.FirstName + " " + i.Project.Customer.LastName,
                CreatedAt = i.CreatedAt,
                ModifiedAt = i.ModifiedAt,
                LastActivityAt = i.ModifiedAt ?? i.CreatedAt
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<CustomerLookupDto>> SearchCustomersAsync(
        string? searchTerm,
        int take = 20,
        CancellationToken cancellationToken = default)
    {
        take = take is < 1 or > 50 ? 20 : take;

        await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        var customers = dbContext.Customers.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.Trim();
            var digits = CustomerValidationRules.NormalizeDigits(term);

            customers = customers.Where(c =>
                c.FirstName.Contains(term) ||
                c.LastName.Contains(term) ||
                (c.FirstName + " " + c.LastName).Contains(term) ||
                c.NationalCode != null && c.NationalCode.Contains(term) ||
                (digits != string.Empty && c.NationalCode != null && c.NationalCode.Contains(digits)));
        }

        return await customers
            .OrderBy(c => c.LastName)
            .ThenBy(c => c.FirstName)
            .Take(take)
            .Select(c => new CustomerLookupDto
            {
                Id = c.Id,
                FullName = c.FirstName == "" ? c.LastName : c.FirstName + " " + c.LastName,
                NationalCode = c.NationalCode ?? ""
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<CustomerLookupDto?> GetCustomerLookupAsync(
        int customerId,
        CancellationToken cancellationToken = default)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        return await dbContext.Customers
            .AsNoTracking()
            .Where(c => c.Id == customerId)
            .Select(c => new CustomerLookupDto
            {
                Id = c.Id,
                FullName = c.FirstName == "" ? c.LastName : c.FirstName + " " + c.LastName,
                NationalCode = c.NationalCode ?? ""
            })
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ProjectLookupDto>> SearchProjectsAsync(
        string? searchTerm,
        int? customerId = null,
        int take = 20,
        CancellationToken cancellationToken = default)
    {
        take = take is < 1 or > 50 ? 20 : take;

        await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        var projects = dbContext.Projects.AsNoTracking();

        if (customerId is > 0)
            projects = projects.Where(p => p.CustomerId == customerId);

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.Trim();

            projects = projects.Where(p =>
                p.Name.Contains(term) ||
                p.Address.Contains(term) ||
                (p.Customer.FirstName + " " + p.Customer.LastName).Contains(term) ||
                p.Customer.FirstName.Contains(term) ||
                p.Customer.LastName.Contains(term));
        }

        return await projects
            .OrderBy(p => p.Name)
            .Take(take)
            .Select(p => new ProjectLookupDto
            {
                Id = p.Id,
                Name = p.Name,
                CustomerId = p.CustomerId,
                CustomerFullName = p.Customer.FirstName + " " + p.Customer.LastName
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<ProjectLookupDto?> GetProjectLookupAsync(
        int projectId,
        CancellationToken cancellationToken = default)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        return await dbContext.Projects
            .AsNoTracking()
            .Where(p => p.Id == projectId)
            .Select(p => new ProjectLookupDto
            {
                Id = p.Id,
                Name = p.Name,
                CustomerId = p.CustomerId,
                CustomerFullName = p.Customer.FirstName + " " + p.Customer.LastName
            })
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<InvoiceFormDto?> GetForEditAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        var header = await dbContext.Invoices
            .AsNoTracking()
            .Where(i => i.Id == id)
            .Select(i => new InvoiceFormDto
            {
                Id = i.Id,
                ProjectId = i.ProjectId,
                ProjectName = i.Project.Name,
                CustomerId = i.Project.CustomerId,
                CustomerFullName = i.Project.Customer.FirstName + " " + i.Project.Customer.LastName,
                InvoiceNumber = i.InvoiceNumber,
                InvoiceDate = i.InvoiceDate,
                Title = i.Title,
                ShippingCost = i.ShippingCost,
                TotalAmount = i.TotalAmount,
                TotalPrice = i.TotalPrice
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (header is null)
            return null;

        header.Items = await dbContext.InvoiceItems
            .AsNoTracking()
            .Where(item => item.InvoiceId == id)
            .OrderBy(item => item.Id)
            .Select(item => new InvoiceItemFormDto
            {
                Id = item.Id,
                Length = item.Length,
                Count = item.Count,
                BottomRebar = item.BottomRebar,
                TopRebar = item.TopRebar,
                ReinforcementBar = item.ReinforcementBar,
                ReinforcementPercent = item.ReinforcementPercent,
                Zigzag = item.Zigzag,
                UnitPrice = item.UnitPrice,
                TotalAmount = item.TotalAmount,
                TotalPrice = item.TotalPrice
            })
            .ToListAsync(cancellationToken);

        return header;
    }
    #endregion

    #region Write
    public async Task<OperationResultDto> CreateAsync(
        InvoiceFormDto form,
        int currentUserId,
        CancellationToken cancellationToken = default)
    {
        var sanitized = Sanitize(form);
        sanitized.RecalculateAll();

        var errors = InvoiceValidationRules.ValidateForm(sanitized);
        if (errors.Count > 0)
            return OperationResultDto.Invalid(errors);

        await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        var projectExists = await dbContext.Projects
            .AnyAsync(p => p.Id == sanitized.ProjectId, cancellationToken);

        if (!projectExists)
            return OperationResultDto.Fail("پروژه انتخاب‌شده یافت نشد");

        var now = DateTime.UtcNow;
        var invoiceNumber = await GenerateInvoiceNumberAsync(dbContext, sanitized.InvoiceDate, cancellationToken);

        var invoice = new Invoice
        {
            ProjectId = sanitized.ProjectId,
            InvoiceNumber = invoiceNumber,
            InvoiceDate = sanitized.InvoiceDate.Date,
            Title = sanitized.Title,
            TotalAmount = sanitized.TotalAmount,
            TotalPrice = sanitized.TotalPrice,
            ShippingCost = sanitized.ShippingCost,
            CreatedAt = now,
            CreatedBy = currentUserId
        };

        foreach (var item in sanitized.Items)
        {
            invoice.InvoiceItems.Add(MapNewItem(item, currentUserId, now));
        }

        await dbContext.Invoices.AddAsync(invoice, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        return OperationResultDto.Ok(
            $"فاکتور شماره {invoiceNumber} با موفقیت ثبت شد",
            invoice.Id);
    }

    public async Task<OperationResultDto> UpdateAsync(
        InvoiceFormDto form,
        int currentUserId,
        CancellationToken cancellationToken = default)
    {
        if (form.Id <= 0)
            return OperationResultDto.Fail("شناسه فاکتور نامعتبر است");

        var sanitized = Sanitize(form);
        sanitized.RecalculateAll();

        var errors = InvoiceValidationRules.ValidateForm(sanitized);
        if (errors.Count > 0)
            return OperationResultDto.Invalid(errors);

        await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        var invoice = await dbContext.Invoices
            .Include(i => i.InvoiceItems)
            .FirstOrDefaultAsync(i => i.Id == form.Id, cancellationToken);

        if (invoice is null)
            return OperationResultDto.Fail("فاکتور مورد نظر یافت نشد");

        var projectExists = await dbContext.Projects
            .AnyAsync(p => p.Id == sanitized.ProjectId, cancellationToken);

        if (!projectExists)
            return OperationResultDto.Fail("پروژه انتخاب‌شده یافت نشد");

        var now = DateTime.UtcNow;

        invoice.ProjectId = sanitized.ProjectId;
        invoice.InvoiceDate = sanitized.InvoiceDate.Date;
        invoice.Title = sanitized.Title;
        invoice.ShippingCost = sanitized.ShippingCost;
        invoice.TotalAmount = sanitized.TotalAmount;
        invoice.TotalPrice = sanitized.TotalPrice;
        invoice.ModifiedAt = now;
        invoice.ModifiedBy = currentUserId;

        SyncItems(invoice, sanitized.Items, currentUserId, now);

        await dbContext.SaveChangesAsync(cancellationToken);

        return OperationResultDto.Ok("فاکتور با موفقیت به‌روزرسانی شد", invoice.Id);
    }

    public async Task<OperationResultDto> DeleteAsync(
        int id,
        int currentUserId,
        CancellationToken cancellationToken = default)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        var invoice = await dbContext.Invoices
            .Include(i => i.InvoiceItems)
            .FirstOrDefaultAsync(i => i.Id == id, cancellationToken);

        if (invoice is null)
            return OperationResultDto.Fail("فاکتور مورد نظر یافت نشد");

        var now = DateTime.UtcNow;

        invoice.IsDeleted = true;
        invoice.DeletedAt = now;
        invoice.DeletedBy = currentUserId;
        invoice.ModifiedAt = now;
        invoice.ModifiedBy = currentUserId;

        foreach (var item in invoice.InvoiceItems.Where(i => !i.IsDeleted))
        {
            item.IsDeleted = true;
            item.DeletedAt = now;
            item.DeletedBy = currentUserId;
            item.ModifiedAt = now;
            item.ModifiedBy = currentUserId;
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return OperationResultDto.Ok($"فاکتور شماره {invoice.InvoiceNumber} با موفقیت حذف شد", id);
    }
    #endregion

    #region Helpers
    private static void SyncItems(
        Invoice invoice,
        IReadOnlyList<InvoiceItemFormDto> incoming,
        int currentUserId,
        DateTime now)
    {
        var existing = invoice.InvoiceItems
            .Where(i => !i.IsDeleted)
            .ToDictionary(i => i.Id);

        var keptIds = new HashSet<int>();

        foreach (var item in incoming)
        {
            if (item.Id != 0 && existing.TryGetValue(item.Id, out var entity))
            {
                keptIds.Add(item.Id);

                entity.Length = item.Length;
                entity.Count = item.Count;
                entity.BottomRebar = item.BottomRebar;
                entity.TopRebar = item.TopRebar;
                entity.ReinforcementBar = item.ReinforcementBar;
                entity.ReinforcementPercent = item.ReinforcementPercent;
                entity.Zigzag = item.Zigzag;
                entity.UnitPrice = item.UnitPrice;
                entity.TotalAmount = item.TotalAmount;
                entity.TotalPrice = item.TotalPrice;
                entity.ModifiedAt = now;
                entity.ModifiedBy = currentUserId;
                continue;
            }

            invoice.InvoiceItems.Add(MapNewItem(item, currentUserId, now));
        }

        foreach (var entity in existing.Values)
        {
            if (keptIds.Contains(entity.Id))
                continue;

            entity.IsDeleted = true;
            entity.DeletedAt = now;
            entity.DeletedBy = currentUserId;
            entity.ModifiedAt = now;
            entity.ModifiedBy = currentUserId;
        }
    }

    private static InvoiceItem MapNewItem(InvoiceItemFormDto item, int currentUserId, DateTime now) => new()
    {
        Length = item.Length,
        Count = item.Count,
        BottomRebar = item.BottomRebar,
        TopRebar = item.TopRebar,
        ReinforcementBar = item.ReinforcementBar,
        ReinforcementPercent = item.ReinforcementPercent,
        Zigzag = item.Zigzag,
        UnitPrice = item.UnitPrice,
        TotalAmount = item.TotalAmount,
        TotalPrice = item.TotalPrice,
        CreatedAt = now,
        CreatedBy = currentUserId
    };

    private static async Task<int> GenerateInvoiceNumberAsync(
        AppDbContext dbContext,
        DateTime invoiceDate,
        CancellationToken cancellationToken)
    {
        var calendar = new PersianCalendar();
        var local = invoiceDate.Kind == DateTimeKind.Utc ? invoiceDate.ToLocalTime() : invoiceDate;
        var persianYear = calendar.GetYear(local);
        var persianMonth = calendar.GetMonth(local);
        var monthPrefix = persianYear * 100_000 + persianMonth * 1_000;
        var monthCeiling = monthPrefix + 999;

        var maxNumber = await dbContext.Invoices
            .IgnoreQueryFilters()
            .Where(i => i.InvoiceNumber >= monthPrefix && i.InvoiceNumber <= monthCeiling)
            .Select(i => (int?)i.InvoiceNumber)
            .MaxAsync(cancellationToken) ?? monthPrefix;

        if (maxNumber >= monthCeiling)
            throw new InvalidOperationException(
                $"ظرفیت شماره فاکتور برای {persianYear}/{persianMonth:00} پر شده است.");

        return maxNumber + 1;
    }

    private static InvoiceFormDto Sanitize(InvoiceFormDto form)
    {
        var sanitized = new InvoiceFormDto
        {
            Id = form.Id,
            ProjectId = form.ProjectId,
            ProjectName = form.ProjectName?.Trim() ?? string.Empty,
            CustomerFullName = form.CustomerFullName?.Trim() ?? string.Empty,
            CustomerId = form.CustomerId,
            InvoiceNumber = form.InvoiceNumber,
            InvoiceDate = form.InvoiceDate == default ? DateTime.Today : form.InvoiceDate.Date,
            Title = string.IsNullOrWhiteSpace(form.Title) ? null : form.Title.Trim(),
            ShippingCost = form.ShippingCost,
            Items = form.Items?
                .Select(i => new InvoiceItemFormDto
                {
                    Id = i.Id,
                    Length = i.Length,
                    Count = i.Count,
                    BottomRebar = i.BottomRebar,
                    TopRebar = i.TopRebar,
                    ReinforcementBar = i.ReinforcementBar,
                    ReinforcementPercent = i.ReinforcementPercent,
                    Zigzag = i.Zigzag,
                    UnitPrice = i.UnitPrice
                })
                .ToList() ?? []
        };

        sanitized.RecalculateAll();
        return sanitized;
    }
    #endregion
}
