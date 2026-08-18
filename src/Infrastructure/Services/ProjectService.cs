using Behsazan.Application.DTOs;
using Behsazan.Application.Interfaces;
using Behsazan.Application.Validation;
using Behsazan.Domain.Entities;
using Behsazan.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Behsazan.Infrastructure.Services;

public class ProjectService : IProjectService
{
    private readonly IDbContextFactory<AppDbContext> _dbContextFactory;

    public ProjectService(IDbContextFactory<AppDbContext> dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    #region Read
    public async Task<PagedResultDto<ProjectListItemDto>> GetPagedAsync(
        ProjectQueryDto query,
        CancellationToken cancellationToken = default)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        var projects = dbContext.Projects.AsNoTracking();

        #region Filters
        if (!string.IsNullOrWhiteSpace(query.SearchTerm))
        {
            var term = query.SearchTerm.Trim();
            var digits = CustomerValidationRules.NormalizeDigits(term);
            int? ledgerSearch = int.TryParse(digits, out var ledger) ? ledger : null;

            projects = projects.Where(p =>
                p.Name.Contains(term) ||
                p.Address.Contains(term) ||
                (p.Customer.FirstName + " " + p.Customer.LastName).Contains(term) ||
                p.Customer.FirstName.Contains(term) ||
                p.Customer.LastName.Contains(term) ||
                (ledgerSearch != null && p.GeneralLedgerNumber == ledgerSearch));
        }

        if (query.CustomerId is > 0)
            projects = projects.Where(p => p.CustomerId == query.CustomerId);

        if (query.JoistType.HasValue)
            projects = projects.Where(p => p.JoistType == query.JoistType.Value);

        if (query.CreatedFrom.HasValue)
        {
            var from = query.CreatedFrom.Value.Date;
            projects = projects.Where(p => p.CreatedAt >= from);
        }

        if (query.CreatedTo.HasValue)
        {
            var toExclusive = query.CreatedTo.Value.Date.AddDays(1);
            projects = projects.Where(p => p.CreatedAt < toExclusive);
        }
        #endregion

        var totalCount = await projects.CountAsync(cancellationToken);

        if (totalCount == 0)
            return PagedResultDto<ProjectListItemDto>.Empty(query.PageNumber, query.PageSize);

        var ordered = ApplySort(projects, query);

        var items = await ordered
            .Skip((query.PageNumber - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(p => new ProjectListItemDto
            {
                Id = p.Id,
                Name = p.Name,
                Address = p.Address,
                GeneralLedgerNumber = p.GeneralLedgerNumber,
                JoistType = p.JoistType,
                CustomerId = p.CustomerId,
                CustomerFullName = p.Customer.FirstName + " " + p.Customer.LastName,
                InvoicesCount = p.Invoices.Count(),
                CreatedAt = p.CreatedAt,
                ModifiedAt = p.ModifiedAt,
                LastActivityAt = p.ModifiedAt ?? p.CreatedAt
            })
            .ToListAsync(cancellationToken);

        return new PagedResultDto<ProjectListItemDto>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = query.PageNumber,
            PageSize = query.PageSize
        };
    }

    private static IQueryable<Project> ApplySort(IQueryable<Project> projects, ProjectQueryDto query)
    {
        var descending = query.SortDescending;

        return query.SortBy switch
        {
            ProjectSortBy.Name => descending
                ? projects.OrderByDescending(p => p.Name).ThenByDescending(p => p.Id)
                : projects.OrderBy(p => p.Name).ThenBy(p => p.Id),

            ProjectSortBy.CustomerName => descending
                ? projects.OrderByDescending(p => p.Customer.LastName)
                    .ThenByDescending(p => p.Customer.FirstName)
                    .ThenByDescending(p => p.Id)
                : projects.OrderBy(p => p.Customer.LastName)
                    .ThenBy(p => p.Customer.FirstName)
                    .ThenBy(p => p.Id),

            ProjectSortBy.JoistType => descending
                ? projects.OrderByDescending(p => p.JoistType).ThenByDescending(p => p.Id)
                : projects.OrderBy(p => p.JoistType).ThenBy(p => p.Id),

            ProjectSortBy.CreatedAt => descending
                ? projects.OrderByDescending(p => p.CreatedAt).ThenByDescending(p => p.Id)
                : projects.OrderBy(p => p.CreatedAt).ThenBy(p => p.Id),

            ProjectSortBy.InvoicesCount => descending
                ? projects.OrderByDescending(p => p.Invoices.Count()).ThenByDescending(p => p.Id)
                : projects.OrderBy(p => p.Invoices.Count()).ThenBy(p => p.Id),

            _ => descending
                ? projects.OrderByDescending(p => p.ModifiedAt ?? p.CreatedAt).ThenByDescending(p => p.Id)
                : projects.OrderBy(p => p.ModifiedAt ?? p.CreatedAt).ThenBy(p => p.Id)
        };
    }

    public async Task<ProjectDetailsDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        return await dbContext.Projects
            .AsNoTracking()
            .Where(p => p.Id == id)
            .Select(p => new ProjectDetailsDto
            {
                Id = p.Id,
                Name = p.Name,
                Address = p.Address,
                GeneralLedgerNumber = p.GeneralLedgerNumber,
                JoistType = p.JoistType,
                CustomerId = p.CustomerId,
                CustomerFullName = p.Customer.FirstName + " " + p.Customer.LastName,
                CustomerNationalCode = p.Customer.NationalCode ?? "",
                InvoicesCount = p.Invoices.Count(),
                DepositsCount = p.Deposits.Count(),
                CreatedAt = p.CreatedAt,
                ModifiedAt = p.ModifiedAt
            })
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<ProjectFormDto?> GetForEditAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        return await dbContext.Projects
            .AsNoTracking()
            .Where(p => p.Id == id)
            .Select(p => new ProjectFormDto
            {
                Id = p.Id,
                CustomerId = p.CustomerId,
                CustomerFullName = p.Customer.FirstName + " " + p.Customer.LastName,
                Name = p.Name,
                Address = p.Address,
                GeneralLedgerNumber = p.GeneralLedgerNumber,
                JoistType = p.JoistType
            })
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ProjectListItemDto>> GetByCustomerAsync(
        int customerId,
        CancellationToken cancellationToken = default)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        return await dbContext.Projects
            .AsNoTracking()
            .Where(p => p.CustomerId == customerId)
            .OrderByDescending(p => p.ModifiedAt ?? p.CreatedAt)
            .ThenByDescending(p => p.Id)
            .Select(p => new ProjectListItemDto
            {
                Id = p.Id,
                Name = p.Name,
                Address = p.Address,
                GeneralLedgerNumber = p.GeneralLedgerNumber,
                JoistType = p.JoistType,
                CustomerId = p.CustomerId,
                InvoicesCount = p.Invoices.Count(),
                CreatedAt = p.CreatedAt,
                ModifiedAt = p.ModifiedAt,
                LastActivityAt = p.ModifiedAt ?? p.CreatedAt
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
    #endregion

    #region Write
    public async Task<OperationResultDto> CreateAsync(
        ProjectFormDto form,
        int currentUserId,
        CancellationToken cancellationToken = default)
    {
        var sanitized = Sanitize(form);

        var errors = Validate(sanitized);
        if (errors.Count > 0)
            return OperationResultDto.Invalid(errors);

        await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        var customerExists = await dbContext.Customers
            .AnyAsync(c => c.Id == sanitized.CustomerId, cancellationToken);

        if (!customerExists)
            return OperationResultDto.Fail("مشتری انتخاب‌شده یافت نشد");

        var now = DateTime.UtcNow;

        var project = new Project
        {
            CustomerId = sanitized.CustomerId,
            Name = sanitized.Name,
            Address = sanitized.Address,
            GeneralLedgerNumber = sanitized.GeneralLedgerNumber,
            JoistType = sanitized.JoistType,
            CreatedAt = now,
            CreatedBy = currentUserId
        };

        await dbContext.Projects.AddAsync(project, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        return OperationResultDto.Ok("پروژه با موفقیت ثبت شد", project.Id);
    }

    public async Task<OperationResultDto> UpdateAsync(
        ProjectFormDto form,
        int currentUserId,
        CancellationToken cancellationToken = default)
    {
        if (form.Id <= 0)
            return OperationResultDto.Fail("شناسه پروژه نامعتبر است");

        var sanitized = Sanitize(form);

        var errors = Validate(sanitized);
        if (errors.Count > 0)
            return OperationResultDto.Invalid(errors);

        await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        var project = await dbContext.Projects
            .FirstOrDefaultAsync(p => p.Id == form.Id, cancellationToken);

        if (project is null)
            return OperationResultDto.Fail("پروژه مورد نظر یافت نشد");

        var customerExists = await dbContext.Customers
            .AnyAsync(c => c.Id == sanitized.CustomerId, cancellationToken);

        if (!customerExists)
            return OperationResultDto.Fail("مشتری انتخاب‌شده یافت نشد");

        var now = DateTime.UtcNow;

        project.CustomerId = sanitized.CustomerId;
        project.Name = sanitized.Name;
        project.Address = sanitized.Address;
        project.GeneralLedgerNumber = sanitized.GeneralLedgerNumber;
        project.JoistType = sanitized.JoistType;
        project.ModifiedAt = now;
        project.ModifiedBy = currentUserId;

        await dbContext.SaveChangesAsync(cancellationToken);

        return OperationResultDto.Ok("اطلاعات پروژه با موفقیت به‌روزرسانی شد", project.Id);
    }

    public async Task<OperationResultDto> DeleteAsync(
        int id,
        int currentUserId,
        CancellationToken cancellationToken = default)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        var project = await dbContext.Projects
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

        if (project is null)
            return OperationResultDto.Fail("پروژه مورد نظر یافت نشد");

        var invoicesCount = await dbContext.Invoices
            .CountAsync(i => i.ProjectId == id, cancellationToken);

        if (invoicesCount > 0)
        {
            return OperationResultDto.Fail(
                $"این پروژه {invoicesCount} فاکتور دارد و قابل حذف نیست. ابتدا فاکتورهای آن را حذف کنید.");
        }

        var now = DateTime.UtcNow;

        project.IsDeleted = true;
        project.DeletedAt = now;
        project.DeletedBy = currentUserId;
        project.ModifiedAt = now;
        project.ModifiedBy = currentUserId;

        await dbContext.SaveChangesAsync(cancellationToken);

        return OperationResultDto.Ok("پروژه با موفقیت حذف شد", id);
    }
    #endregion

    #region Helpers
    private static ProjectFormDto Sanitize(ProjectFormDto form) => new()
    {
        Id = form.Id,
        CustomerId = form.CustomerId,
        CustomerFullName = form.CustomerFullName?.Trim() ?? string.Empty,
        Name = form.Name?.Trim() ?? string.Empty,
        Address = form.Address?.Trim() ?? string.Empty,
        GeneralLedgerNumber = form.GeneralLedgerNumber,
        JoistType = form.JoistType
    };

    private static List<string> Validate(ProjectFormDto form)
    {
        var errors = new List<string>();

        var customerError = ProjectValidationRules.ValidateCustomerId(form.CustomerId);
        if (customerError is not null)
            errors.Add(customerError);

        var nameError = ProjectValidationRules.ValidateName(form.Name);
        if (nameError is not null)
            errors.Add(nameError);

        var addressError = ProjectValidationRules.ValidateAddress(form.Address);
        if (addressError is not null)
            errors.Add(addressError);

        var joistError = ProjectValidationRules.ValidateJoistType(form.JoistType);
        if (joistError is not null)
            errors.Add(joistError);

        var ledgerError = ProjectValidationRules.ValidateGeneralLedgerNumber(form.GeneralLedgerNumber);
        if (ledgerError is not null)
            errors.Add(ledgerError);

        return errors;
    }
    #endregion
}
