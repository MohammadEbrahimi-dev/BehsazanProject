using Behsazan.Application.DTOs;
using Behsazan.Application.Interfaces;
using Behsazan.Application.Validation;
using Behsazan.Domain.Entities;
using Behsazan.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Behsazan.Infrastructure.Services;

public class CustomerService : ICustomerService
{
    private readonly IDbContextFactory<AppDbContext> _dbContextFactory;

    public CustomerService(IDbContextFactory<AppDbContext> dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    #region Read
    public async Task<PagedResultDto<CustomerListItemDto>> GetPagedAsync(
        CustomerQueryDto query,
        CancellationToken cancellationToken = default)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        var customers = dbContext.Customers.AsNoTracking();

        #region Search
        if (!string.IsNullOrWhiteSpace(query.SearchTerm))
        {
            var term = query.SearchTerm.Trim();
            var digits = CustomerValidationRules.NormalizeDigits(term);

            customers = customers.Where(c =>
                c.FirstName.Contains(term) ||
                c.LastName.Contains(term) ||
                (c.FirstName + " " + c.LastName).Contains(term) ||
                c.NationalCode != null && c.NationalCode.Contains(term) ||
                (digits != string.Empty && c.NationalCode != null && c.NationalCode.Contains(digits)) ||
                (digits != string.Empty && c.PhoneNumbers.Any(p => p.PhoneNumber.Contains(digits))));
        }
        #endregion

        var ordered = ApplySort(customers, query);
        var pageNumber = query.PageNumber < 1 ? 1 : query.PageNumber;
        var pageSize = query.PageSize < 1 ? 10 : query.PageSize;

        var buffer = await ordered
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize + 1)
            .Select(c => new CustomerListItemDto
            {
                Id = c.Id,
                FirstName = c.FirstName,
                LastName = c.LastName,
                FullName = c.FirstName == "" ? c.LastName : c.FirstName + " " + c.LastName,
                NationalCode = c.NationalCode ?? "",
                PrimaryPhoneNumber = c.PhoneNumbers
                    .Where(p => p.IsBaseNumber)
                    .Select(p => p.PhoneNumber)
                    .FirstOrDefault(),
                PhoneNumbersCount = c.PhoneNumbers.Count(),
                ProjectsCount = c.Projects.Count(),
                CreatedAt = c.CreatedAt,
                ModifiedAt = c.ModifiedAt,
                LastActivityAt = c.ModifiedAt ?? c.CreatedAt
            })
            .ToListAsync(cancellationToken);

        int totalCount;
        List<CustomerListItemDto> items;

        if (pageNumber == 1 && buffer.Count <= pageSize)
        {
            items = buffer;
            totalCount = buffer.Count;
        }
        else
        {
            var hasMore = buffer.Count > pageSize;
            items = hasMore ? buffer.Take(pageSize).ToList() : buffer;
            totalCount = await customers.CountAsync(cancellationToken);
        }

        if (totalCount == 0)
            return PagedResultDto<CustomerListItemDto>.Empty(pageNumber, pageSize);

        return new PagedResultDto<CustomerListItemDto>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        };
    }

    private static IQueryable<Customer> ApplySort(IQueryable<Customer> customers, CustomerQueryDto query)
    {
        var descending = query.SortDescending;

        return query.SortBy switch
        {
            CustomerSortBy.FullName => descending
                ? customers.OrderByDescending(c => c.LastName).ThenByDescending(c => c.FirstName)
                : customers.OrderBy(c => c.LastName).ThenBy(c => c.FirstName),

            CustomerSortBy.NationalCode => descending
                ? customers.OrderByDescending(c => c.NationalCode)
                : customers.OrderBy(c => c.NationalCode),

            CustomerSortBy.ProjectsCount => descending
                ? customers.OrderByDescending(c => c.Projects.Count()).ThenByDescending(c => c.Id)
                : customers.OrderBy(c => c.Projects.Count()).ThenBy(c => c.Id),

            CustomerSortBy.CreatedAt => descending
                ? customers.OrderByDescending(c => c.CreatedAt).ThenByDescending(c => c.Id)
                : customers.OrderBy(c => c.CreatedAt).ThenBy(c => c.Id),

            _ => descending
                ? customers.OrderByDescending(c => c.ModifiedAt ?? c.CreatedAt).ThenByDescending(c => c.Id)
                : customers.OrderBy(c => c.ModifiedAt ?? c.CreatedAt).ThenBy(c => c.Id)
        };
    }

    public async Task<CustomerDetailsDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        return await dbContext.Customers
            .AsNoTracking()
            .Where(c => c.Id == id)
            .Select(c => new CustomerDetailsDto
            {
                Id = c.Id,
                FirstName = c.FirstName,
                LastName = c.LastName,
                FullName = c.FirstName == "" ? c.LastName : c.FirstName + " " + c.LastName,
                NationalCode = c.NationalCode ?? "",
                ProjectsCount = c.Projects.Count(),
                CreatedAt = c.CreatedAt,
                ModifiedAt = c.ModifiedAt,
                PhoneNumbers = c.PhoneNumbers
                    .OrderByDescending(p => p.IsBaseNumber)
                    .ThenBy(p => p.PhoneType)
                    .ThenBy(p => p.Id)
                    .Select(p => new CustomerPhoneNumberDto
                    {
                        Id = p.Id,
                        CustomerId = p.CustomerId,
                        PhoneNumber = p.PhoneNumber,
                        PhoneType = p.PhoneType,
                        IsBaseNumber = p.IsBaseNumber
                    })
                    .ToList()
            })
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<CustomerFormDto?> GetForEditAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        return await dbContext.Customers
            .AsNoTracking()
            .Where(c => c.Id == id)
            .Select(c => new CustomerFormDto
            {
                Id = c.Id,
                FirstName = c.FirstName,
                LastName = c.LastName,
                NationalCode = c.NationalCode ?? "",
                PhoneNumbers = c.PhoneNumbers
                    .OrderByDescending(p => p.IsBaseNumber)
                    .ThenBy(p => p.Id)
                    .Select(p => new CustomerPhoneNumberDto
                    {
                        Id = p.Id,
                        CustomerId = p.CustomerId,
                        PhoneNumber = p.PhoneNumber,
                        PhoneType = p.PhoneType,
                        IsBaseNumber = p.IsBaseNumber
                    })
                    .ToList()
            })
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<bool> IsNationalCodeAvailableAsync(
        string nationalCode,
        int? excludeCustomerId = null,
        CancellationToken cancellationToken = default)
    {
        var code = CustomerValidationRules.NormalizeDigits(nationalCode);

        if (code.Length == 0)
            return true;

        await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        return !await dbContext.Customers
            .IgnoreQueryFilters()
            .AnyAsync(
                c => c.NationalCode == code && (excludeCustomerId == null || c.Id != excludeCustomerId),
                cancellationToken);
    }
    #endregion

    #region Write
    public async Task<OperationResultDto> CreateAsync(
        CustomerFormDto form,
        int currentUserId,
        CancellationToken cancellationToken = default)
    {
        var sanitized = Sanitize(form);

        var errors = Validate(sanitized);
        if (errors.Count > 0)
            return OperationResultDto.Invalid(errors);

        await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        var conflict = await FindNationalCodeConflictAsync(dbContext, sanitized.NationalCode, null, cancellationToken);
        if (conflict is not null)
            return OperationResultDto.Fail(conflict);

        var now = DateTime.UtcNow;

        var customer = new Customer
        {
            FirstName = sanitized.FirstName,
            LastName = sanitized.LastName,
            NationalCode = ToStoredNationalCode(sanitized.NationalCode),
            CreatedAt = now,
            CreatedBy = currentUserId
        };

        foreach (var phone in sanitized.PhoneNumbers)
        {
            customer.PhoneNumbers.Add(new CustomerPhoneNumber
            {
                PhoneNumber = phone.PhoneNumber,
                PhoneType = phone.PhoneType,
                IsBaseNumber = phone.IsBaseNumber,
                CreatedAt = now,
                CreatedBy = currentUserId
            });
        }

        await dbContext.Customers.AddAsync(customer, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        return OperationResultDto.Ok("مشتری با موفقیت ثبت شد", customer.Id);
    }

    public async Task<OperationResultDto> UpdateAsync(
        CustomerFormDto form,
        int currentUserId,
        CancellationToken cancellationToken = default)
    {
        if (form.Id <= 0)
            return OperationResultDto.Fail("شناسه مشتری نامعتبر است");

        var sanitized = Sanitize(form);

        var errors = Validate(sanitized);
        if (errors.Count > 0)
            return OperationResultDto.Invalid(errors);

        await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        var customer = await dbContext.Customers
            .Include(c => c.PhoneNumbers)
            .FirstOrDefaultAsync(c => c.Id == form.Id, cancellationToken);

        if (customer is null)
            return OperationResultDto.Fail("مشتری مورد نظر یافت نشد");

        var conflict = await FindNationalCodeConflictAsync(dbContext, sanitized.NationalCode, customer.Id, cancellationToken);
        if (conflict is not null)
            return OperationResultDto.Fail(conflict);

        var now = DateTime.UtcNow;

        customer.FirstName = sanitized.FirstName;
        customer.LastName = sanitized.LastName;
        customer.NationalCode = ToStoredNationalCode(sanitized.NationalCode);
        customer.ModifiedAt = now;
        customer.ModifiedBy = currentUserId;

        SyncPhoneNumbers(customer, sanitized.PhoneNumbers, currentUserId, now);

        await dbContext.SaveChangesAsync(cancellationToken);

        return OperationResultDto.Ok("اطلاعات مشتری با موفقیت به‌روزرسانی شد", customer.Id);
    }

    private static void SyncPhoneNumbers(
        Customer customer,
        IReadOnlyList<CustomerPhoneNumberDto> incoming,
        int currentUserId,
        DateTime now)
    {
        var existing = customer.PhoneNumbers.ToDictionary(p => p.Id);
        var keptIds = new HashSet<int>();

        foreach (var phone in incoming)
        {
            if (phone.Id != 0 && existing.TryGetValue(phone.Id, out var entity))
            {
                keptIds.Add(phone.Id);

                var changed = entity.PhoneNumber != phone.PhoneNumber
                    || entity.PhoneType != phone.PhoneType
                    || entity.IsBaseNumber != phone.IsBaseNumber;

                if (!changed)
                    continue;

                entity.PhoneNumber = phone.PhoneNumber;
                entity.PhoneType = phone.PhoneType;
                entity.IsBaseNumber = phone.IsBaseNumber;
                entity.ModifiedAt = now;
                entity.ModifiedBy = currentUserId;
                continue;
            }

            customer.PhoneNumbers.Add(new CustomerPhoneNumber
            {
                CustomerId = customer.Id,
                PhoneNumber = phone.PhoneNumber,
                PhoneType = phone.PhoneType,
                IsBaseNumber = phone.IsBaseNumber,
                CreatedAt = now,
                CreatedBy = currentUserId
            });
        }

        foreach (var removed in existing.Values.Where(p => !keptIds.Contains(p.Id)))
        {
            removed.IsDeleted = true;
            removed.DeletedAt = now;
            removed.DeletedBy = currentUserId;
            removed.ModifiedAt = now;
            removed.ModifiedBy = currentUserId;
        }
    }

    public async Task<OperationResultDto> DeleteAsync(
        int id,
        int currentUserId,
        CancellationToken cancellationToken = default)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        var customer = await dbContext.Customers
            .Include(c => c.PhoneNumbers)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

        if (customer is null)
            return OperationResultDto.Fail("مشتری مورد نظر یافت نشد");

        var projectsCount = await dbContext.Projects
            .CountAsync(p => p.CustomerId == id, cancellationToken);

        if (projectsCount > 0)
        {
            return OperationResultDto.Fail(
                $"این مشتری {projectsCount} پروژه فعال دارد و قابل حذف نیست. ابتدا پروژه‌های او را حذف کنید.");
        }

        var now = DateTime.UtcNow;

        customer.IsDeleted = true;
        customer.DeletedAt = now;
        customer.DeletedBy = currentUserId;
        customer.ModifiedAt = now;
        customer.ModifiedBy = currentUserId;

        foreach (var phone in customer.PhoneNumbers)
        {
            phone.IsDeleted = true;
            phone.DeletedAt = now;
            phone.DeletedBy = currentUserId;
            phone.ModifiedAt = now;
            phone.ModifiedBy = currentUserId;
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return OperationResultDto.Ok("مشتری با موفقیت حذف شد", id);
    }
    #endregion

    #region Helpers
    private static CustomerFormDto Sanitize(CustomerFormDto form)
    {
        var sanitized = new CustomerFormDto
        {
            Id = form.Id,
            FirstName = form.FirstName?.Trim() ?? string.Empty,
            LastName = form.LastName?.Trim() ?? string.Empty,
            NationalCode = NormalizeOptionalNationalCode(form.NationalCode),
            PhoneNumbers = form.PhoneNumbers
                .Where(p => !string.IsNullOrWhiteSpace(p.PhoneNumber))
                .Select(p => new CustomerPhoneNumberDto
                {
                    Id = p.Id,
                    CustomerId = form.Id,
                    PhoneNumber = CustomerValidationRules.NormalizeDigits(p.PhoneNumber),
                    PhoneType = p.PhoneType,
                    IsBaseNumber = p.IsBaseNumber
                })
                .ToList()
        };

        NormalizePrimaryFlag(sanitized.PhoneNumbers);

        return sanitized;
    }

    private static void NormalizePrimaryFlag(List<CustomerPhoneNumberDto> phones)
    {
        if (phones.Count == 0)
            return;

        var primaries = phones.Where(p => p.IsBaseNumber).ToList();

        if (primaries.Count == 0)
        {
            phones[0].IsBaseNumber = true;
            return;
        }

        foreach (var superseded in primaries.Take(primaries.Count - 1))
            superseded.IsBaseNumber = false;
    }

    private static List<string> Validate(CustomerFormDto form)
    {
        var errors = new List<string>();

        var firstNameError = CustomerValidationRules.ValidateFirstName(form.FirstName);
        if (firstNameError is not null)
            errors.Add(firstNameError);

        var lastNameError = CustomerValidationRules.ValidateLastName(form.LastName);
        if (lastNameError is not null)
            errors.Add(lastNameError);

        var nationalCodeError = CustomerValidationRules.ValidateNationalCode(form.NationalCode);
        if (nationalCodeError is not null)
            errors.Add(nationalCodeError);

        foreach (var phone in form.PhoneNumbers)
        {
            var phoneError = CustomerValidationRules.ValidatePhoneNumber(phone.PhoneNumber, phone.PhoneType);
            if (phoneError is not null)
                errors.Add($"{phone.PhoneNumber}: {phoneError}");
        }

        var duplicate = form.PhoneNumbers
            .GroupBy(p => p.PhoneNumber)
            .FirstOrDefault(g => g.Count() > 1);

        if (duplicate is not null)
            errors.Add($"شماره تماس {duplicate.Key} تکراری است");

        return errors;
    }

    private static async Task<string?> FindNationalCodeConflictAsync(
        AppDbContext dbContext,
        string? nationalCode,
        int? excludeCustomerId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(nationalCode))
            return null;

        var owner = await dbContext.Customers
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(c => c.NationalCode == nationalCode
                && (excludeCustomerId == null || c.Id != excludeCustomerId))
            .Select(c => new { c.Id, c.IsDeleted })
            .FirstOrDefaultAsync(cancellationToken);

        if (owner is null)
            return null;

        return owner.IsDeleted
            ? "این کد ملی به مشتری حذف‌شده‌ای تعلق دارد و قابل استفاده مجدد نیست"
            : "مشتری دیگری با این کد ملی ثبت شده است";
    }

    private static string? ToStoredNationalCode(string? code) =>
        string.IsNullOrWhiteSpace(code) ? null : code;

    private static string NormalizeOptionalNationalCode(string? value)
    {
        var digits = CustomerValidationRules.NormalizeDigits(value);
        return digits;
    }
    #endregion
}
