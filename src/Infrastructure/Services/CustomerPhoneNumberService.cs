using Behsazan.Application.DTOs;
using Behsazan.Application.Interfaces;
using Behsazan.Application.Validation;
using Behsazan.Domain.Entities;
using Behsazan.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Behsazan.Infrastructure.Services;

public class CustomerPhoneNumberService : ICustomerPhoneNumberService
{
    private readonly IDbContextFactory<AppDbContext> _dbContextFactory;

    public CustomerPhoneNumberService(IDbContextFactory<AppDbContext> dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    public async Task<IReadOnlyList<CustomerPhoneNumberDto>> GetByCustomerAsync(
        int customerId,
        CancellationToken cancellationToken = default)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        return await dbContext.CustomerPhoneNumbers
            .AsNoTracking()
            .Where(p => p.CustomerId == customerId)
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
            .ToListAsync(cancellationToken);
    }

    public async Task<OperationResultDto> AddAsync(
        CustomerPhoneNumberDto phone,
        int currentUserId,
        CancellationToken cancellationToken = default)
    {
        var number = CustomerValidationRules.NormalizeDigits(phone.PhoneNumber);

        var validationError = CustomerValidationRules.ValidatePhoneNumber(number, phone.PhoneType);
        if (validationError is not null)
            return OperationResultDto.Fail(validationError);

        await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        var customerExists = await dbContext.Customers
            .AnyAsync(c => c.Id == phone.CustomerId, cancellationToken);

        if (!customerExists)
            return OperationResultDto.Fail("مشتری مورد نظر یافت نشد");

        var isDuplicate = await dbContext.CustomerPhoneNumbers
            .AnyAsync(p => p.CustomerId == phone.CustomerId && p.PhoneNumber == number, cancellationToken);

        if (isDuplicate)
            return OperationResultDto.Fail("این شماره قبلاً برای این مشتری ثبت شده است");

        var siblings = await dbContext.CustomerPhoneNumbers
            .Where(p => p.CustomerId == phone.CustomerId)
            .ToListAsync(cancellationToken);

        var now = DateTime.UtcNow;

        var shouldBePrimary = siblings.Count == 0 || phone.IsBaseNumber;

        if (shouldBePrimary)
            DemoteAll(siblings, currentUserId, now);

        var entity = new CustomerPhoneNumber
        {
            CustomerId = phone.CustomerId,
            PhoneNumber = number,
            PhoneType = phone.PhoneType,
            IsBaseNumber = shouldBePrimary,
            CreatedAt = now,
            CreatedBy = currentUserId
        };

        await dbContext.CustomerPhoneNumbers.AddAsync(entity, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        return OperationResultDto.Ok("شماره تماس با موفقیت افزوده شد", entity.Id);
    }

    public async Task<OperationResultDto> UpdateAsync(
        CustomerPhoneNumberDto phone,
        int currentUserId,
        CancellationToken cancellationToken = default)
    {
        if (phone.Id <= 0)
            return OperationResultDto.Fail("شناسه شماره تماس نامعتبر است");

        var number = CustomerValidationRules.NormalizeDigits(phone.PhoneNumber);

        var validationError = CustomerValidationRules.ValidatePhoneNumber(number, phone.PhoneType);
        if (validationError is not null)
            return OperationResultDto.Fail(validationError);

        await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        var entity = await dbContext.CustomerPhoneNumbers
            .FirstOrDefaultAsync(p => p.Id == phone.Id, cancellationToken);

        if (entity is null)
            return OperationResultDto.Fail("شماره تماس مورد نظر یافت نشد");

        var isDuplicate = await dbContext.CustomerPhoneNumbers
            .AnyAsync(
                p => p.CustomerId == entity.CustomerId && p.PhoneNumber == number && p.Id != entity.Id,
                cancellationToken);

        if (isDuplicate)
            return OperationResultDto.Fail("این شماره قبلاً برای این مشتری ثبت شده است");

        var now = DateTime.UtcNow;

        var siblings = await dbContext.CustomerPhoneNumbers
            .Where(p => p.CustomerId == entity.CustomerId && p.Id != entity.Id)
            .ToListAsync(cancellationToken);

        bool shouldBePrimary;

        if (phone.IsBaseNumber || siblings.Count == 0)
        {
            shouldBePrimary = true;
            DemoteAll(siblings, currentUserId, now);
        }
        else if (entity.IsBaseNumber)
        {
            shouldBePrimary = false;

            var replacement = siblings
                .OrderBy(p => p.PhoneType)
                .ThenBy(p => p.Id)
                .First();

            replacement.IsBaseNumber = true;
            replacement.ModifiedAt = now;
            replacement.ModifiedBy = currentUserId;
        }
        else
        {
            shouldBePrimary = false;
        }

        entity.PhoneNumber = number;
        entity.PhoneType = phone.PhoneType;
        entity.IsBaseNumber = shouldBePrimary;
        entity.ModifiedAt = now;
        entity.ModifiedBy = currentUserId;

        await dbContext.SaveChangesAsync(cancellationToken);

        return OperationResultDto.Ok("شماره تماس با موفقیت ویرایش شد", entity.Id);
    }

    public async Task<OperationResultDto> DeleteAsync(
        int phoneId,
        int currentUserId,
        CancellationToken cancellationToken = default)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        var entity = await dbContext.CustomerPhoneNumbers
            .FirstOrDefaultAsync(p => p.Id == phoneId, cancellationToken);

        if (entity is null)
            return OperationResultDto.Fail("شماره تماس مورد نظر یافت نشد");

        var now = DateTime.UtcNow;

        entity.IsDeleted = true;
        entity.DeletedAt = now;
        entity.DeletedBy = currentUserId;
        entity.ModifiedAt = now;
        entity.ModifiedBy = currentUserId;

        if (entity.IsBaseNumber)
        {
            var replacement = await dbContext.CustomerPhoneNumbers
                .Where(p => p.CustomerId == entity.CustomerId && p.Id != entity.Id)
                .OrderBy(p => p.PhoneType)
                .ThenBy(p => p.Id)
                .FirstOrDefaultAsync(cancellationToken);

            if (replacement is not null)
            {
                replacement.IsBaseNumber = true;
                replacement.ModifiedAt = now;
                replacement.ModifiedBy = currentUserId;
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return OperationResultDto.Ok("شماره تماس حذف شد", phoneId);
    }

    public async Task<OperationResultDto> SetPrimaryAsync(
        int phoneId,
        int currentUserId,
        CancellationToken cancellationToken = default)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        var entity = await dbContext.CustomerPhoneNumbers
            .FirstOrDefaultAsync(p => p.Id == phoneId, cancellationToken);

        if (entity is null)
            return OperationResultDto.Fail("شماره تماس مورد نظر یافت نشد");

        if (entity.IsBaseNumber)
            return OperationResultDto.Ok("این شماره از قبل شماره اصلی است", phoneId);

        var now = DateTime.UtcNow;

        var siblings = await dbContext.CustomerPhoneNumbers
            .Where(p => p.CustomerId == entity.CustomerId && p.Id != entity.Id)
            .ToListAsync(cancellationToken);

        DemoteAll(siblings, currentUserId, now);

        entity.IsBaseNumber = true;
        entity.ModifiedAt = now;
        entity.ModifiedBy = currentUserId;

        await dbContext.SaveChangesAsync(cancellationToken);

        return OperationResultDto.Ok("شماره اصلی تغییر کرد", phoneId);
    }

    private static void DemoteAll(IEnumerable<CustomerPhoneNumber> phones, int currentUserId, DateTime now)
    {
        foreach (var phone in phones.Where(p => p.IsBaseNumber))
        {
            phone.IsBaseNumber = false;
            phone.ModifiedAt = now;
            phone.ModifiedBy = currentUserId;
        }
    }
}
