using Behsazan.Application.DTOs;
using Behsazan.Application.Interfaces;
using Behsazan.Application.Validation;
using Behsazan.Domain.Entities;
using Behsazan.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Behsazan.Infrastructure.Services;

public class DepositService : IDepositService
{
    private readonly IDbContextFactory<AppDbContext> _dbContextFactory;

    public DepositService(IDbContextFactory<AppDbContext> dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    #region Read
    public async Task<PagedResultDto<DepositListItemDto>> GetPagedAsync(
        DepositQueryDto query,
        CancellationToken cancellationToken = default)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        var deposits = dbContext.Deposits.AsNoTracking();

        #region Filters
        if (!string.IsNullOrWhiteSpace(query.SearchTerm))
        {
            var term = query.SearchTerm.Trim();
            var digits = CustomerValidationRules.NormalizeDigits(term);

            deposits = deposits.Where(d =>
                (d.TrackingNumber != null && d.TrackingNumber.Contains(term)) ||
                (d.ReferenceNumber != null && d.ReferenceNumber.Contains(term)) ||
                (d.Description != null && d.Description.Contains(term)) ||
                d.FromAccountNo.Contains(term) ||
                d.ToAccountNo.Contains(term) ||
                d.Project.Name.Contains(term) ||
                (d.Project.Customer.FirstName + " " + d.Project.Customer.LastName).Contains(term) ||
                d.Project.Customer.FirstName.Contains(term) ||
                d.Project.Customer.LastName.Contains(term) ||
                (digits != string.Empty && (
                    (d.TrackingNumber != null && d.TrackingNumber.Contains(digits)) ||
                    (d.ReferenceNumber != null && d.ReferenceNumber.Contains(digits)))));
        }

        if (query.ProjectId is > 0)
            deposits = deposits.Where(d => d.ProjectId == query.ProjectId);

        if (query.DepositDateFrom.HasValue)
        {
            var from = query.DepositDateFrom.Value.Date;
            deposits = deposits.Where(d => d.DepositDate >= from);
        }

        if (query.DepositDateTo.HasValue)
        {
            var toExclusive = query.DepositDateTo.Value.Date.AddDays(1);
            deposits = deposits.Where(d => d.DepositDate < toExclusive);
        }
        #endregion

        var totalCount = await deposits.CountAsync(cancellationToken);

        if (totalCount == 0)
            return PagedResultDto<DepositListItemDto>.Empty(query.PageNumber, query.PageSize);

        var ordered = ApplySort(deposits, query);

        var items = await ordered
            .Skip((query.PageNumber - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(d => new DepositListItemDto
            {
                Id = d.Id,
                DepositDate = d.DepositDate,
                Amount = d.Amount,
                Description = d.Description,
                TrackingNumber = d.TrackingNumber,
                ReferenceNumber = d.ReferenceNumber,
                FromAccountNo = d.FromAccountNo,
                ToAccountNo = d.ToAccountNo,
                ProjectId = d.ProjectId,
                ProjectName = d.Project.Name,
                CustomerId = d.Project.CustomerId,
                CustomerFullName = d.Project.Customer.FirstName == ""
                    ? d.Project.Customer.LastName
                    : d.Project.Customer.FirstName + " " + d.Project.Customer.LastName,
                CreatedAt = d.CreatedAt,
                ModifiedAt = d.ModifiedAt
            })
            .ToListAsync(cancellationToken);

        return new PagedResultDto<DepositListItemDto>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = query.PageNumber,
            PageSize = query.PageSize
        };
    }

    private static IQueryable<Deposit> ApplySort(IQueryable<Deposit> deposits, DepositQueryDto query)
    {
        var descending = query.SortDescending;

        return query.SortBy switch
        {
            DepositSortBy.Amount => descending
                ? deposits.OrderByDescending(d => d.Amount).ThenByDescending(d => d.Id)
                : deposits.OrderBy(d => d.Amount).ThenBy(d => d.Id),

            DepositSortBy.ProjectName => descending
                ? deposits.OrderByDescending(d => d.Project.Name).ThenByDescending(d => d.Id)
                : deposits.OrderBy(d => d.Project.Name).ThenBy(d => d.Id),

            DepositSortBy.CustomerName => descending
                ? deposits.OrderByDescending(d => d.Project.Customer.LastName)
                    .ThenByDescending(d => d.Project.Customer.FirstName)
                    .ThenByDescending(d => d.Id)
                : deposits.OrderBy(d => d.Project.Customer.LastName)
                    .ThenBy(d => d.Project.Customer.FirstName)
                    .ThenBy(d => d.Id),

            DepositSortBy.TrackingNumber => descending
                ? deposits.OrderByDescending(d => d.TrackingNumber).ThenByDescending(d => d.Id)
                : deposits.OrderBy(d => d.TrackingNumber).ThenBy(d => d.Id),

            DepositSortBy.CreatedAt => descending
                ? deposits.OrderByDescending(d => d.CreatedAt).ThenByDescending(d => d.Id)
                : deposits.OrderBy(d => d.CreatedAt).ThenBy(d => d.Id),

            _ => descending
                ? deposits.OrderByDescending(d => d.DepositDate).ThenByDescending(d => d.Id)
                : deposits.OrderBy(d => d.DepositDate).ThenBy(d => d.Id)
        };
    }

    public async Task<DepositFormDto?> GetForEditAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        return await dbContext.Deposits
            .AsNoTracking()
            .Where(d => d.Id == id)
            .Select(d => new DepositFormDto
            {
                Id = d.Id,
                ProjectId = d.ProjectId,
                ProjectName = d.Project.Name,
                CustomerFullName = d.Project.Customer.FirstName == ""
                    ? d.Project.Customer.LastName
                    : d.Project.Customer.FirstName + " " + d.Project.Customer.LastName,
                DepositDate = d.DepositDate,
                Amount = d.Amount,
                Description = d.Description,
                TrackingNumber = d.TrackingNumber,
                ReferenceNumber = d.ReferenceNumber,
                FromAccountNo = d.FromAccountNo,
                ToAccountNo = d.ToAccountNo
            })
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ProjectLookupDto>> SearchProjectsAsync(
        string? searchTerm,
        int take = 20,
        CancellationToken cancellationToken = default)
    {
        take = take is < 1 or > 50 ? 20 : take;

        await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        var projects = dbContext.Projects.AsNoTracking();

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
                CustomerFullName = p.Customer.FirstName == ""
                    ? p.Customer.LastName
                    : p.Customer.FirstName + " " + p.Customer.LastName
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
                CustomerFullName = p.Customer.FirstName == ""
                    ? p.Customer.LastName
                    : p.Customer.FirstName + " " + p.Customer.LastName
            })
            .FirstOrDefaultAsync(cancellationToken);
    }
    #endregion

    #region Write
    public async Task<OperationResultDto> CreateAsync(
        DepositFormDto form,
        int currentUserId,
        CancellationToken cancellationToken = default)
    {
        var sanitized = Sanitize(form);
        var errors = DepositValidationRules.ValidateForm(sanitized);
        if (errors.Count > 0)
            return OperationResultDto.Invalid(errors);

        await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        var projectExists = await dbContext.Projects
            .AnyAsync(p => p.Id == sanitized.ProjectId, cancellationToken);

        if (!projectExists)
            return OperationResultDto.Fail("پروژه انتخاب‌شده یافت نشد");

        var now = DateTime.UtcNow;

        var deposit = new Deposit
        {
            ProjectId = sanitized.ProjectId,
            DepositDate = sanitized.DepositDate.Date,
            FromAccountNo = sanitized.FromAccountNo,
            ToAccountNo = sanitized.ToAccountNo,
            Amount = sanitized.Amount,
            Description = sanitized.Description,
            TrackingNumber = sanitized.TrackingNumber,
            ReferenceNumber = sanitized.ReferenceNumber,
            CreatedAt = now,
            CreatedBy = currentUserId
        };

        await dbContext.Deposits.AddAsync(deposit, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        return OperationResultDto.Ok("واریزی با موفقیت ثبت شد", deposit.Id);
    }

    public async Task<OperationResultDto> UpdateAsync(
        DepositFormDto form,
        int currentUserId,
        CancellationToken cancellationToken = default)
    {
        if (form.Id <= 0)
            return OperationResultDto.Fail("شناسه واریزی نامعتبر است");

        var sanitized = Sanitize(form);
        var errors = DepositValidationRules.ValidateForm(sanitized);
        if (errors.Count > 0)
            return OperationResultDto.Invalid(errors);

        await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        var deposit = await dbContext.Deposits
            .FirstOrDefaultAsync(d => d.Id == form.Id, cancellationToken);

        if (deposit is null)
            return OperationResultDto.Fail("واریزی مورد نظر یافت نشد");

        var projectExists = await dbContext.Projects
            .AnyAsync(p => p.Id == sanitized.ProjectId, cancellationToken);

        if (!projectExists)
            return OperationResultDto.Fail("پروژه انتخاب‌شده یافت نشد");

        var now = DateTime.UtcNow;

        deposit.ProjectId = sanitized.ProjectId;
        deposit.DepositDate = sanitized.DepositDate.Date;
        deposit.FromAccountNo = sanitized.FromAccountNo;
        deposit.ToAccountNo = sanitized.ToAccountNo;
        deposit.Amount = sanitized.Amount;
        deposit.Description = sanitized.Description;
        deposit.TrackingNumber = sanitized.TrackingNumber;
        deposit.ReferenceNumber = sanitized.ReferenceNumber;
        deposit.ModifiedAt = now;
        deposit.ModifiedBy = currentUserId;

        await dbContext.SaveChangesAsync(cancellationToken);

        return OperationResultDto.Ok("اطلاعات واریزی با موفقیت به‌روزرسانی شد", deposit.Id);
    }

    public async Task<OperationResultDto> DeleteAsync(
        int id,
        int currentUserId,
        CancellationToken cancellationToken = default)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        var deposit = await dbContext.Deposits
            .FirstOrDefaultAsync(d => d.Id == id, cancellationToken);

        if (deposit is null)
            return OperationResultDto.Fail("واریزی مورد نظر یافت نشد");

        var now = DateTime.UtcNow;

        deposit.IsDeleted = true;
        deposit.DeletedAt = now;
        deposit.DeletedBy = currentUserId;
        deposit.ModifiedAt = now;
        deposit.ModifiedBy = currentUserId;

        await dbContext.SaveChangesAsync(cancellationToken);

        return OperationResultDto.Ok("واریزی با موفقیت حذف شد", id);
    }
    #endregion

    #region Helpers
    private static DepositFormDto Sanitize(DepositFormDto form) => new()
    {
        Id = form.Id,
        ProjectId = form.ProjectId,
        ProjectName = form.ProjectName?.Trim() ?? string.Empty,
        CustomerFullName = form.CustomerFullName?.Trim() ?? string.Empty,
        DepositDate = form.DepositDate == default ? DateTime.Today : form.DepositDate.Date,
        Amount = form.Amount,
        Description = string.IsNullOrWhiteSpace(form.Description) ? null : form.Description.Trim(),
        TrackingNumber = string.IsNullOrWhiteSpace(form.TrackingNumber) ? null : form.TrackingNumber.Trim(),
        ReferenceNumber = string.IsNullOrWhiteSpace(form.ReferenceNumber) ? null : form.ReferenceNumber.Trim(),
        FromAccountNo = form.FromAccountNo?.Trim() ?? string.Empty,
        ToAccountNo = form.ToAccountNo?.Trim() ?? string.Empty
    };
    #endregion
}
