using Behsazan.Application.DTOs;

namespace Behsazan.Application.Interfaces;

public interface ICustomerPhoneNumberService
{
    Task<IReadOnlyList<CustomerPhoneNumberDto>> GetByCustomerAsync(
        int customerId,
        CancellationToken cancellationToken = default);

    Task<OperationResultDto> AddAsync(
        CustomerPhoneNumberDto phone,
        int currentUserId,
        CancellationToken cancellationToken = default);

    Task<OperationResultDto> UpdateAsync(
        CustomerPhoneNumberDto phone,
        int currentUserId,
        CancellationToken cancellationToken = default);

    Task<OperationResultDto> DeleteAsync(
        int phoneId,
        int currentUserId,
        CancellationToken cancellationToken = default);

    Task<OperationResultDto> SetPrimaryAsync(
        int phoneId,
        int currentUserId,
        CancellationToken cancellationToken = default);
}
